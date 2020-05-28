const inquirer = require('inquirer')
const PluginManager = require('live-plugin-manager').PluginManager
const repo = require('../app.modelRepo')

const manager = new PluginManager({
  pluginsPath: 'dtdl_models',
  npmInstallMode: 'noCache'
})

inquirer.prompt([
  {
    type: 'list',
    name: 'operation',
    choices: ['List Models', 'Add Models', 'Clean Models']
  }
]).then(async (answer) => {
  switch (answer.operation) {
    case 'List Models': {
      const models = await repo.loadModelsFromFS()
      models.forEach(m => console.log(m.id + ' -> ' + m.pkg.replace('./dtdl_models/', '')))
      break
    }
    case 'Clean Models': {
      console.log('not implemented')
      break
    }
    case 'Add Models':
      inquirer.prompt([
        { type: 'input', name: 'scope', question: 'scope', default: '@digital-twins' },
        { type: 'input', name: 'pkgSearch', question: 'pkg to search', default: 'com-example-thermostat' }
      ])
        .then(answer => {
          manager.queryPackage(answer.scope + '/' + answer.pkgSearch)
            .then(pi => {
              console.log('package found in registry %s %s', pi.name, pi.version)
              inquirer.prompt([{ name: 'install', type: 'confirm' }])
                .then(answer => {
                  if (answer.install) {
                    manager.install(pi.name, pi.version)
                      .then(ipi => {
                        console.log(ipi.dependencies)
                      })
                  }
                })
            })
            .catch(e => console.error(e))
        })
      break
  }
})
