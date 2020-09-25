const registry = require('azure-iothub').Registry
;(async () => {
  const cs = 'HostName=ridohub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=lbobW7o9SKg1WJho6kZ6ZlQkub325YI3eLXmlvzXLOw='
  const reg = registry.fromConnectionString(cs)
  const t = await reg.getTwin('test')
  console.log(t.responseBody.properties)
})()
