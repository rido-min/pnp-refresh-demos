import * as apiClient from './apiClient.js'
import TelemetryData from './telemetryData.js'
import createChart from './pnpChart.js'

const protocol = document.location.protocol.startsWith('https') ? 'wss://' : 'ws://'
const webSocket = new window.WebSocket(protocol + window.location.host)
const deviceId = new URLSearchParams(window.location.search).get('deviceId')

;(async () => {
  const createVueApp = () => {
    return new Vue({
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
        parseModel: async function (modelJson) {
          this.telemetryProps = modelJson.contents.filter(c => c['@type'].includes('Telemetry')).map(e => e)
          this.commands = modelJson.contents.filter(c => c['@type'] === 'Command').map(e => e)
          this.desiredProps = modelJson.contents.filter(c => c['@type'].includes('Property') && c.writable === true).map(e => e)
          const reported = modelJson.contents.filter(c => c['@type'].includes('Property') && (c.writable === false || c.writable === undefined)).map(e => e)
          this.reportedProps = reported.concat(this.desiredProps)
        },
        runCommand: async function (cmdName) {
          const el = document.getElementById(cmdName + '-payload')
          let cmdPayload = el.value
          const cmd = this.commands.filter(c => c.name === cmdName)[0]

          if (cmd.request.schema === 'boolean') {
            cmdPayload = (cmdPayload.toLowerCase() === 'true')
          }
          if (cmd.request.schema === 'double') {
            cmdPayload = parseFloat(cmdPayload)
          }
          if (cmd.request.schema === 'integer') {
            cmdPayload = parseInt(cmdPayload, 10)
          }

          console.log(cmdName + cmdPayload)
          await apiClient.invokeCommand(this.deviceId, cmdName, cmdPayload)
        },
        updateDesiredProp: async function (propName) {
          const el = document.getElementById(propName)
          const prop = this.desiredProps.filter(x => x.name === propName)[0]
          Vue.set(prop, 'desiredValue', el.value)
          await apiClient.updateDeviceTwin(this.deviceId, propName, parseInt(el.value))
        }
      }
    })
  }

  if (!deviceId || deviceId.length < 1) {
    document.getElementById('errorMsg').innerHTML = 'Device Id was not found in the querystring.'
    return
  }

  const modelId = await apiClient.getModelId(deviceId)
  if (!modelId || modelId.length < 5) {
    document.getElementById('errorMsg').innerHTML = `Model Id not found for device ${deviceId}`
    return
  }

  const modelJson = await apiClient.getModel(modelId)
  if (!modelJson) {
    document.getElementById('errorMsg').innerHTML = `Model not found for ModelID ${modelId}`
    return
  }

  const app = createVueApp()
  app.deviceId = deviceId
  app.modelId = modelId
  await app.parseModel(modelJson)

  const twin = await apiClient.getDeviceTwin(deviceId)
  // reported props
  Vue.set(app.reportedProps, 'version', twin.properties.reported.$version)
  app.reportedProps.forEach(p => {
    if (twin &&
      twin.properties &&
      twin.properties.reported &&
      twin.properties.reported[p.name]) {
      const updated = moment(twin.properties.reported.$metadata[p.name].$lastUpdated).fromNow()
      Vue.set(p, 'reportedValue', twin.properties.reported[p.name].value || twin.properties.reported[p.name])
      Vue.set(p, 'lastUpdated', updated)
    }
  })

  // desired props
  Vue.set(app.desiredProps, 'version', twin.properties.desired.$version)
  app.desiredProps.forEach(p => {
    if (twin &&
      twin.properties &&
      twin.properties.desired &&
      twin.properties.desired[p.name]) {
      Vue.set(p, 'desiredValue', twin.properties.desired[p.name])
    }
  })

  // commands
  app.commands.forEach(c => {
    if (c.request) {
      Vue.set(c, 'payload', ' ')
    }
  })

  const updateReported = (reported) => {
    Vue.set(app.reportedProps, 'version', reported.$version)
    for (const p in reported) {
      if (!p.startsWith('$')) {
        const prop = app.reportedProps.filter(x => x.name === p)[0]
        if (prop) {
          Vue.set(prop, 'reportedValue', reported[p].value || reported[p])
          Vue.set(prop, 'lastUpdated', moment(reported.$metadata[p].$lastUpdated).fromNow())
        }
      }
    }
  }

  const updateDesired = (desired) => {
    Vue.set(app.desiredProps, 'version', desired.$version)
    for (const p in desired) {
      if (!p.startsWith('$')) {
        const prop = app.desiredProps.filter(x => x.name === p)[0]
        if (prop) {
          Vue.set(prop, 'desiredValue', desired[p])
          Vue.set(prop, 'lastUpdated', moment(desired.$metadata[p].$lastUpdated).fromNow())
        }
      }
    }
  }

  // telemetry
  const telNames = app.telemetryProps.map(t => t.name)
  const deviceData = new TelemetryData(deviceId, app.telemetryProps.map(t => t.name))
  const myLineChart = createChart('iotChart', telNames)

  webSocket.onmessage = (message) => {
    const messageData = JSON.parse(message.data)
    // console.log(messageData)
    if (messageData.IotData.properties && messageData.IotData.properties.reported) {
      updateReported(messageData.IotData.properties.reported)
      return
    }
    if (messageData.IotData.properties && messageData.IotData.properties.desired) {
      updateDesired(messageData.IotData.properties.desired)
      return
    }

    telNames.forEach(t => {
      if (messageData.IotData[t]) {
        const telemetryValue = messageData.IotData[t]
        myLineChart.data.labels = deviceData.timeData
        deviceData.addDataPoint(messageData.MessageDate, t, telemetryValue)
        const curDataSet = myLineChart.data.datasets.filter(ds => ds.yAxisID === t)
        curDataSet[0].data = deviceData.dataPoints[t]
        myLineChart.update()
      }
    })
  }
})()
