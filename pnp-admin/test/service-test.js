const hub = require('azure-iothub')
// const dtService = require('azure-iot-digitaltwins-service')
const hubCs = 'HostName=summerrelease-test-03.private.azure-devices-int.net;SharedAccessKeyName=iothubowner;SharedAccessKey=7NOwXejCnfJKrlFJ8GcWXlTw51W1jDq+SBuQExEnh9c='
const registry = hub.Registry.fromConnectionString(hubCs)
const query = registry.createQuery("select * from devices where deviceId = 'rido-pnp-01'", 50)
query.nextAsTwin((err, devices) => {
  if (err) throw err
  devices.forEach(d => console.log(d))
})
