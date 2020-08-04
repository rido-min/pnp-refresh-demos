const dt = require('azure-iot-digitaltwins-service')
const cs = 'HostName=ridohub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=EcSKltC/G6tc8jYaWWDvQh2wdCWMr5XLFRSBvwg0YdA='
const deviceId = 'adu-sim-01'
const dtc = new dt.DigitalTwinServiceClient(new dt.IoTHubTokenCredentials(cs))
const patch = [{
    op: 'add',
    path: '/Orchestrator',
    value: {
            "Action": 33,
            "TargetVersion": 33,
            "Files": {
                "aaaa": "https://aka.ms.33",
                "sdfa": "33"
            },
            "ExpectedContentId": "33",
            "InstalledCriteria": "33"
    }
}]

dtc.updateDigitalTwin(deviceId, patch)
    .then(resp => {
        console.log(resp)
    }
)