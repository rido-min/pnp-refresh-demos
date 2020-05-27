const hub = require('azure-iothub')
const hubClient = require('azure-iothub').Client
const dtService = require('azure-iot-digitaltwins-service')

const moment = require('moment')

const getModelId = async (connectionString, deviceId) => {
  const credentials = new dtService.IoTHubTokenCredentials(connectionString)
  const digitalTwinServiceClient = new dtService.DigitalTwinServiceClient(credentials)
  const twinResp = await digitalTwinServiceClient.getDigitalTwin(deviceId)
  const twin = twinResp._response.parsedBody
  if (twin && twin.$metadata && twin.$metadata.$model) return twin.$metadata.$model
  else return ''
}

const getDeviceList = (connectionString, cb) => {
  const registry = hub.Registry.fromConnectionString(connectionString)
  const queryText = `select deviceId,
                              lastActivityTime,
                              connectionState,
                              status,
                              properties.reported.[[$iotin:deviceinfo]].manufacturer.value as manufacturer
                       from devices
                       where capabilities.iotEdge != true`
  const query = registry.createQuery(queryText)
  query.nextAsTwin(async (err, devices) => {
    if (err) {
      console.error(`Failed to query devices due to ${err}`)
    } else {
      const devicesInfo = devices.map((d) => {
        const elapsed = moment(d.lastActivityTime)
        return {
          id: d.deviceId,
          time: elapsed.isBefore('2019-01-01', 'year') ? '' : elapsed.fromNow(),
          lastActivityTime: d.lastActivityTime,
          state: d.connectionState,
          status: d.status,
          manufacturer: d.manufacturer,
          modelId: ''
        }
      })

      // for await (const d of devicesInfo) {
      //   d.modelId = await getModelId(connectionString, d.id)
      // }

      console.log(`Found ${devicesInfo.length} registered devices.`)
      cb(devicesInfo)
    }
  })
}

const getDeviceTwin = async (connectionString, deviceId) => {
  const registry = hub.Registry.fromConnectionString(connectionString)
  const twin = await registry.getTwin(deviceId)
  return twin
}

const updateDeviceTwin = async (connectionString, deviceId, propertyName, propertyValue) => {
  const registry = hub.Registry.fromConnectionString(connectionString)
  const twin = await registry.getTwin(deviceId)
  const patch = { properties: { desired: {} } }
  patch.properties.desired[propertyName] = propertyValue
  const updateResult = await registry.updateTwin(deviceId, patch, twin.responseBody.etag)
  return updateResult
}

const invokeDeviceMethod = async (connectionString, deviceId, commandName, commandPayload) => {
  const client = hubClient.fromConnectionString(connectionString)
  const result = await client.invokeDeviceMethod(deviceId, { methodName: commandName, payload: commandPayload })
  return result.result
}

module.exports = { getDeviceList, getDeviceTwin, updateDeviceTwin, invokeDeviceMethod, getModelId }
