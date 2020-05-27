import * as apiClient from './apiClient.js'
import TelemetryData from './telemetryData.js'

const protocol = document.location.protocol.startsWith('https') ? 'wss://' : 'ws://'
const webSocket = new window.WebSocket(protocol + window.location.host)
const deviceId = new URLSearchParams(window.location.search).get('deviceId')

const getChartData = (telNames) => {
  const chartData = {
    datasets: []
  }
  telNames.forEach(t => {
    chartData.datasets.push({ fill: false, label: t, yAxisID: t })
  })
  return chartData
}

const getChartOptions = (telNames) => {
  const chartOptions = { scales: { yAxes: [] } }
  telNames.forEach(t => {
    chartOptions.scales.yAxes.push(
      {
        id: t,
        type: 'linear',
        scaleLabel: {
          labelString: t,
          display: true
        },
        ticks: {
          beginAtZero: true
        }
      }
    )
  })
  return chartOptions
}

;(async () => {
  const app = new Vue({
    el: '#app',
    data: {
      deviceId: 'unset',
      modelId: '',
      telemetryProps: [],
      reportedProps: [],
      desiredProps: [],
      commands: []
    },
    methods: {
      parseModel: async function () {
        const modelJson = await apiClient.getModel(this.modelId)
        this.telemetryProps = modelJson.contents.filter(c => c['@type'].includes('Telemetry')).map(e => e)
        this.reportedProps = modelJson.contents.filter(c => c['@type'] === 'Property' && c.writable === false).map(e => e)
        this.desiredProps = modelJson.contents.filter(c => c['@type'] === 'Property' && c.writable === true).map(e => e)
        this.commands = modelJson.contents.filter(c => c['@type'] === 'Command').map(e => e)
      },
      runCommand: async function (cmdName) {
        await apiClient.invokeCommand(this.deviceId, cmdName, 2)
      },
      updateDesiredProp: async function (propName) {
        const el = document.getElementById(propName)
        await apiClient.updateDeviceTwin(this.deviceId, propName, el.value)
      }
    }
  })

  app.deviceId = deviceId
  app.modelId = await apiClient.getModelId(deviceId)
  await app.parseModel()

  const twin = await apiClient.getDeviceTwin(deviceId)
  // reported props
  app.reportedProps.forEach(p => {
    if (twin &&
      twin.properties &&
      twin.properties.reported &&
      twin.properties.reported[p.name]) {
      Vue.set(p, 'reportedValue', twin.properties.reported[p.name])
    }
  })

  // desired props
  app.desiredProps.forEach(p => {
    if (twin &&
      twin.properties &&
      twin.properties.desired &&
      twin.properties.desired[p.name]) {
      Vue.set(p, 'desiredValue', twin.properties.desired[p.name])
    }
  })

  // telemetry
  const telNames = app.telemetryProps.map(t => t.name)
  const deviceData = new TelemetryData(deviceId, app.telemetryProps.map(t => t.name))
  const chartData = getChartData(telNames)
  const chartOptions = getChartOptions(telNames)
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
    telNames.forEach(t => {
      if (messageData.IotData[t]) {
        const telemetryValue = messageData.IotData[t]
        chartData.labels = deviceData.timeData
        deviceData.addDataPoint(messageData.MessageDate, t, telemetryValue)
        chartData.datasets[0].data = deviceData.dataPoints[t]
        myLineChart.update()
      }
    })
  }
})()
