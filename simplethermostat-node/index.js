const iotHubTransport = require('azure-iot-device-mqtt').Mqtt
const Client = require('azure-iot-device').Client
const connectionString = process.env.DEVICE_CONNECTION_STRING
const deviceClient = Client.fromConnectionString(connectionString, iotHubTransport)

;(async () => {
  deviceClient.setOptions({ modelId: 'dtmi:test:version:dev01' })
  deviceClient.on('error', (err) => console.error(err))

  await deviceClient.open((err, res) => {
    if (err) throw (err)
    console.log(res.transportObj.transportObj.cmd)
  })

  deviceClient.getTwin()
    .then(t => {
      console.log('twin received')
      var patch = {
        firmwareVersion: '1.2.1',
        weather: {
          temperature: 72,
          humidity: 17
        }
      }
      // console.log(t.properties)
      t.properties.reported.update(patch, (e) => console.log(e))
    })
    .catch(e => console.log(e))

  // try {
  //   const twin = await deviceClient.getTwin()
  //   console.log(twin)
  // } catch (err) {
  //   console.error(err)
  // }
})().catch(e => console.error(e))

/**
  deviceClient.getTwin()
    .then(t => {
      console.log('twin received')
      var patch = {
        firmwareVersion: '1.2.1',
        weather: {
          temperature: 72,
          humidity: 17
        }
      }
      console.log(t.properties)
      t.properties.reported.update(patch, (e) => console.log(e))
    })
    .catch(e => console.log(e))
 */
