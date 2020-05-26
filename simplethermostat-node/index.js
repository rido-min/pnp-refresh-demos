const iotHubTransport = require('azure-iot-device-mqtt').Mqtt
const Client = require('azure-iot-device').Client
const connectionString = 'HostName=basic.azure-devices.net;DeviceId=rywinter;SharedAccessKey=3GAoEYmod4eJ5LDUTy2gr9yIRtgKk2rOEoY4jNYCt2Y='
const hubClient = Client.fromConnectionString(connectionString, iotHubTransport)

hubClient.getTwin()
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
