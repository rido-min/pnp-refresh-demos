const express = require('express')
const path = require('path')

const bodyParser = require('body-parser')
const hub = require('./app.iothub.js')
const repo = require('./app.modelRepo.js')

const http = require('http')
const WebSocket = require('ws')
const EventHubReader = require('./app.eventHub')

const port = 3000

let connectionString = process.env.IOTHUB_CONNECTION_STRING

if (!connectionString || connectionString.length < 10) {
  console.log('IOTHUB_CONNECTION_STRING not found')
}

const app = express()
const router = express.Router()

app.use(bodyParser.json())
app.use(bodyParser.urlencoded({ extended: true }))
app.use('/api', router)
app.use(express.static('ui'))

const server = http.createServer(app)
const wss = new WebSocket.Server({ server })

router.get('/', (req, res, next) => res.sendFile('index.html', { root: path.join(__dirname, 'wwwroot/index.html') }))

router.get('/connection-string', (req, res) => {
  if (connectionString && connectionString.length > 0) {
    const hubRegex = /(?<=HostName=).*(?=;SharedAccessKeyName)/i.exec(connectionString)
    const hubName = hubRegex.length > 0 ? hubRegex[0] : ''
    res.json(hubName)
  } else {
    res.json('not configured')
  }
})

router.post('/connection-string', (req, res) => {
  connectionString = req.body.connectionstring
  if (connectionString && connectionString.length > 0) {
    const hubRegex = /(?<=HostName=).*(?=;SharedAccessKeyName)/i.exec(connectionString)
    const hubName = hubRegex.length > 0 ? hubRegex[0] : ''
    res.json(hubName)
  } else {
    res.json('not configured')
  }
})

router.get('/getDevices', (req, res) => {
  if (connectionString.length > 0) {
    hub.getDeviceList(connectionString, list => res.json(list))
  } else {
    res.json({})
  }
})

router.get('/getDeviceTwin', async (req, res) => {
  const result = await hub.getDeviceTwin(connectionString, req.query.deviceId)
  res.json(result.responseBody)
})

router.get('/getModelId', async (req, res) => {
  const result = await hub.getModelId(connectionString, req.query.deviceId)
  res.json(result)
})

router.get('/getModel', async (req, res) => {
  const result = await repo.getModel(req.query.modelId)
  if (result) {
    return res.json(result)
  } else {
    return res.json('')
  }
})

router.post('/updateDeviceTwin', async (req, res) => {
  const result = await hub.updateDeviceTwin(connectionString, req.body.deviceId, req.body.propertyName, req.body.propertyValue)
  res.json(result.responseBody)
})

router.post('/invokeCommand', async (req, res) => {
  const result = await hub.invokeDeviceMethod(
    connectionString,
    req.body.deviceId,
    req.body.commandName,
    req.body.payload)

  res.json(result)
})

const eventHubConsumerGroup = 'node-web-chart'
const eventHubReader = new EventHubReader(connectionString, eventHubConsumerGroup)

server.listen(port, () => console.log(`IoT Express app listening on port ${port}`))

;(async () => {
  await eventHubReader.startReadMessage((message, date, deviceId) => {
    // console.log(deviceId)
    // console.log(message.properties.reported.$metadata)
    const payload = {
      IotData: message,
      MessageDate: date || Date.now().toISOString(),
      DeviceId: deviceId
    }
    // console.log(message)
    wss.clients.forEach((client) => {
      if (client.readyState === WebSocket.OPEN) {
        client.send(JSON.stringify(payload))
      }
    })
  })
})()
