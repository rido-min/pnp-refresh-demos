const fs = require('fs')
const path = require('path')

/**
 * @description Validates DTMI with RegEx from https://github.com/Azure/digital-twin-model-identifier#validation-regular-expressions
 * @param {string} dtmi
 */
const isDtmi = dtmi => {
  return RegExp('^dtmi:[A-Za-z](?:[A-Za-z0-9_]*[A-Za-z0-9])?(?::[A-Za-z](?:[A-Za-z0-9_]*[A-Za-z0-9])?)*;[1-9][0-9]{0,8}$').test(dtmi)
}

/**
 * @description Converts DTMI to /dtmi/com/example/device-1.json path.
 * @param {string} dtmi
 * @returns {string}
 */
const dtmiToPath = dtmi => {
  if (!isDtmi(dtmi)) {
    return null
  }
  // dtmi:com:example:Thermostat;1 -> dtmi/com/example/thermostat-1.json
  return `/${dtmi.toLowerCase().replace(/:/g, '/').replace(';', '-')}.json`
}

/**
 * @description Returns external IDs in `extend` and `component` elements
 * @param {{ extends: any[]; contents: any[]; }} rootJson
 * @returns {Array<string>}
 */
const getDependencies = rootJson => {
  let deps = []
  if (Array.isArray(rootJson)) {
    deps = rootJson.map(d => d['@id'])
    return deps
  }
  if (rootJson.extends) {
    if (Array.isArray(rootJson.extends)) {
      rootJson.extends.forEach(e => deps.push(e))
    } else {
      deps.push(rootJson.extends)
    }
  }
  if (rootJson.contents) {
    const comps = rootJson.contents.filter(c => c['@type'] === 'Component')
    comps.forEach(c => {
      if (typeof c.schema !== 'object') {
        if (deps.indexOf(c.schema) === -1) {
          deps.push(c.schema)
        }
      }
    })
  }
  return deps
}

/**
 * @description Checks all dependencies are available
 * @param {Array<string>} deps
 * @returns {boolean}
 */
const checkDependencies = dtmi => {
  let result = true
  const fileName = path.join(__dirname, dtmiToPath(dtmi))
  console.log(`Validating dependencies for ${dtmi} from ${fileName}`)
  const dtdlJson = JSON.parse(fs.readFileSync(fileName, 'utf-8'))
  const deps = getDependencies(dtdlJson)
  deps.forEach(d => {
    const fileName = path.join(__dirname, dtmiToPath(d))
    if (fs.existsSync(fileName)) {
      console.log(`Dependency ${d} found`)
      const model = JSON.parse(fs.readFileSync(fileName, 'utf-8'))
      if (model['@id'] !== d) {
        console.error(`ERROR: LowerCase issue with dependent id ${d}. Was ${model['@id']}. Aborting`)
        result = result && true
      }
    } else {
      console.error(`ERROR: Dependency ${d} NOT found. Aborting`)
      result = false
    }
  })
  return result
}

/**
 * @description Checks if the folder/name convention matches the DTMI
 * @param {string} file
 * @returns {boolean}
 */
const checkDtmiPathFromFile = file => {
  const model = JSON.parse(fs.readFileSync(file, 'utf-8'))
  const id = model['@id']
  if (id) {
    const expectedPath = path.normalize(dtmiToPath(model['@id']))
    if (path.normalize('/' + file) !== expectedPath) {
      console.log(`ERROR: in current path ${path.normalize(file)}, expecting ${expectedPath}.`)
      return false
    } else {
      console.log(`FilePath ${file} for ${id} seems OK.`)
      return true
    }
  } else {
    console.log('ERROR: @id not found.')
    return false
  }
}
module.exports = { dtmiToPath, isDtmi, getDependencies, checkDependencies, checkDtmiPathFromFile }
