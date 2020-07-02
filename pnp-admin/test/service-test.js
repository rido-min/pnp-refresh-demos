const hub = require('azure-iothub')
// const dtService = require('azure-iot-digitaltwins-service')
const hubCs = 'HostName=rido-smr-01.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=7eHWv8XFTjnYOmoUykG02/9Oup0X6mjGUuvQqYmh0kA='
const registry = hub.Registry.fromConnectionString(hubCs)

// const query = registry.createQuery("select * from devices where deviceId = 'd2'", 50)

;(async () => {
  const twin = await (await registry.getTwin('d2')).responseBody
  // console.log(twin)
  const patch = {
    properties: {
      reported: {
        deviceInfo: {
          manufacturer: null
        }
      }
    }
  }
  twin.update(patch, (err, updTwin) => {
    if (err) throw err
    console.log('patched')
    console.log(updTwin)
  })
})()

// query.nextAsTwin((err, devices) => {
//   if (err) throw err
//   devices.forEach(d => console.log(JSON.stringify(d)))
// })
