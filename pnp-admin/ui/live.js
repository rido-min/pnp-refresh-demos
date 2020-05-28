import * as apiClient from './apiClient.js'
import TelemetryData from './telemetryData.js'

const protocol = document.location.protocol.startsWith('https') ? 'wss://' : 'ws://'
const webSocket = new window.WebSocket(protocol + window.location.host)

const deviceId = new URLSearchParams(window.location.search).get('deviceId')

const chartData = {
  datasets: [
    {
      fill: false,
      label: 'Temperature',
      yAxisID: 'Temperature'
    }
  ]
}

const chartOptions = {
  scales: {
    yAxes: [{
      id: 'Temperature',
      type: 'linear',
      scaleLabel: {
        labelString: 'Temperature (ºC)',
        display: true
      },
      position: 'right',
      ticks: {
        beginAtZero: true
      }
    }]
  }
}

;(async () => {
  const app = new Vue({
    el: '#app',
    data: {
      deviceId: 'unset',
      currentTemp: 0,
      targetTemp: 0,
      modelId: ''
    },
    methods: {
      increase: async function () {
        this.targetTemp = Math.ceil((this.targetTemp + 2.3) * 100) / 100
        await apiClient.updateDeviceTwin(this.deviceId, 'targetTemperature', this.targetTemp)
      },
      decrease: async function () {
        this.targetTemp = Math.ceil((this.targetTemp - 2.6) * 100) / 100
        await apiClient.updateDeviceTwin(this.deviceId, 'targetTemperature', this.targetTemp)
      },
      reboot: async function () {
        await apiClient.invokeCommand(this.deviceId, 'reboot', 2)
      }
    }
  })

  app.deviceId = deviceId

  const telemetryDataName = 'temperature'
  const deviceData = new TelemetryData(deviceId, [telemetryDataName])

  const twin = await apiClient.getDeviceTwin(deviceId)
  let targetTempValue
  if (twin &&
      twin.properties &&
      twin.properties.desired &&
      twin.properties.desired.targetTemperature) {
    targetTempValue = twin.properties.desired.targetTemperature
    console.log('found targetTemp %s in desired props', targetTempValue)
  } else {
    targetTempValue = 3.21
    console.log('targetTemp not found, init to default value: %s', targetTempValue)
  }

  app.modelId = await apiClient.getModelId(deviceId)

  app.targetTemp = Math.ceil(targetTempValue * 100) / 100
  const myLineChart = new window.Chart(
    document.getElementById('iotChart').getContext('2d'),
    {
      type: 'line',
      data: chartData,
      options: chartOptions
    }
  )

  webSocket.onmessage = (message) => {
    const messageData = JSON.parse(message.data)
    if (messageData.IotData[telemetryDataName]) {
      const telemetryValue = messageData.IotData[telemetryDataName]
      deviceData.addDataPoint(messageData.MessageDate, telemetryDataName, telemetryValue)
      app.currentTemp = Math.ceil(telemetryValue * 100) / 100
      chartData.labels = deviceData.timeData
      chartData.datasets[0].data = deviceData.dataPoints[telemetryDataName]
      myLineChart.update()
    }
  }
})()
