const fs = require('fs')
const file = 'dtdl_models/@digital-twins/com-example-simplethermostat/simplethermostat.json'

const dtdlJson = fs.readFileSync(file, 'utf-8')
const dtdl = JSON.parse(dtdlJson)

const reportedProps = dtdl.contents.filter(c => c['@type'] === 'Property' && c.writable === false).map(e => e)
reportedProps.forEach(p => console.log(p))

const desiredProps = dtdl.contents.filter(c => c['@type'] === 'Property' && c.writable === true).map(e => e)
desiredProps.forEach(p => console.log(p))

const commands = dtdl.contents.filter(c => c['@type'] === 'Command').map(e => e)
commands.forEach(p => console.log(p))

const telemetry = dtdl.contents.filter(c => c['@type'].includes('Telemetry')).map(e => e)
telemetry.forEach(p => console.log(p))

console.log(telemetry.map(t => t.name))
