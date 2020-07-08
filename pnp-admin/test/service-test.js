const hub = require('azure-iothub')
// const dtService = require('azure-iot-digitaltwins-service')
const hubCs = 'HostName=SwickSummerHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=vRPQThaXTa1AjyhKiTkE2DU0e4md27ZkhsnvHjWP0+w='
const registry = hub.Registry.fromConnectionString(hubCs)

// const query = registry.createQuery("select * from devices where deviceId = 'd2'", 50)

;(async () => {
  const twin = (await registry.getTwin('repdestest')).responseBody
  // console.log(twin)
  const patch = {
    properties: {
      desired: {
        CustomerName: 'Mahou'
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
