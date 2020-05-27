const inquirer = require('inquirer')
const PluginManager = require('live-plugin-manager').PluginManager

const manager = new PluginManager({
  pluginsPath: 'dtdl_models',
  npmInstallMode: 'noCache'
})

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
