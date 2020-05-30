'use strict'

const fs = require('fs')
const glob = require('glob')

// const npmorg = '@digital-twins/'
const dir = './dtdl_models/' // + npmorg

let models = []

const loadModelsFromFS = () => {
  models = []
  return new Promise((resolve, reject) => {
    glob(dir + '/**/package.json', (err, files) => {
      if (err) reject(err)
      files.forEach(f => {
        const pjson = JSON.parse(fs.readFileSync(f, 'utf-8'))
        pjson.models.forEach(m => {
          const dtdlModel = JSON.parse(fs.readFileSync(f.replace('package.json', m), 'utf-8'))
          models.push({ id: dtdlModel['@id'], version: pjson.version, pkg: f, dtdlModel })
        })
      })
      resolve(models)
    })
  })
}

const getModel = async (id) => {
  await loadModelsFromFS()
  const m = models.find(e => e.id.toLowerCase() === id.toLowerCase())
  if (m) {
    return m.dtdlModel
  }
}

module.exports = { loadModelsFromFS, getModel }
