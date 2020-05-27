import * as apiClient from './apiClient.js'
import TelemetryData from './telemetryData.js'
import createChart from './pnpChart.js'

const protocol = document.location.protocol.startsWith('https') ? 'wss://' : 'ws://'
const webSocket = new window.WebSocket(protocol + window.location.host)
const deviceId = new URLSearchParams(window.location.search).get('deviceId')

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
  const myLineChart = createChart('iotChart', telNames)
  webSocket.onmessage = (message) => {
    const messageData = JSON.parse(message.data)
    telNames.forEach(t => {
      if (messageData.IotData[t]) {
        const telemetryValue = messageData.IotData[t]
        myLineChart.data.labels = deviceData.timeData
        deviceData.addDataPoint(messageData.MessageDate, t, telemetryValue)
        myLineChart.data.datasets[0].data = deviceData.dataPoints[t]
        myLineChart.update()
      }
    })
  }
})()