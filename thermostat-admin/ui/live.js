
import * as apiClient from './apiClient.js'

const protocol = document.location.protocol.startsWith('https') ? 'wss://' : 'ws://'
const webSocket = new window.WebSocket(protocol + window.location.host)

class DeviceData {
  constructor (deviceId) {
    this.deviceId = deviceId
    this.maxLen = 50
    this.timeData = new Array(this.maxLen)
    this.temperatureData = new Array(this.maxLen)
    this.humidityData = new Array(this.maxLen)
  }

  addData (time, temperature, humidity) {
    const t = new Date(time)
    const timeString = `${t.getHours()}:${t.getMinutes()}:${t.getSeconds()}`

    this.timeData.push(timeString)
    this.temperatureData.push(temperature)

    if (this.timeData.length > this.maxLen) {
      this.timeData.shift()
      this.temperatureData.shift()
    }
  }
}

const deviceId = new URLSearchParams(window.location.search).get('deviceId')
const deviceData = new DeviceData(deviceId)

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
      targetTemp: 0
    },
    methods: {
      increase: async function () {
        this.targetTemp = Math.ceil((this.targetTemp + 2) * 100) / 100
        await apiClient.updateDeviceTwin(this.deviceId, 'targetTemperature', this.targetTemp)
      },
      decrease: async function () {
        this.targetTemp = Math.ceil((this.targetTemp - 2) * 100) / 100
        await apiClient.updateDeviceTwin(this.deviceId, 'targetTemperature', this.targetTemp)
      },
      reboot: async function () {
        console.log('reboot')
        await apiClient.invokeCommand(this.deviceId, 'reboot', 2)
      }
    }
  })

  app.deviceId = deviceId
  const twin = await apiClient.getDeviceTwin(deviceId)
  let targetTempValue = 12.3
  if (twin &&
      twin.properties &&
      twin.properties.desired &&
      twin.properties.desired.targetTemperature) {
    targetTempValue = twin.properties.desired.targetTemperature
  }

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
    if (messageData.IotData.temperature) {
      deviceData.addData(messageData.MessageDate, messageData.IotData.temperature, messageData.IotData.humidity)
      app.currentTemp = Math.ceil(messageData.IotData.temperature * 100) / 100
      chartData.labels = deviceData.timeData
      chartData.datasets[0].data = deviceData.temperatureData
      myLineChart.update()
    }
  }
})()
