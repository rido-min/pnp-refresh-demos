const { isDtmi, dtmiToPath } = require('./repo-convention.js')
const fetch = require('node-fetch')
const repositoryEndpoint = 'devicemodeltest.azureedge.net'
const getModel = async (id) => {
  if (isDtmi(id)) {
    const path = dtmiToPath(id)
    try {
      return await (await fetch(`https://${repositoryEndpoint}${path}`)).json()
    } catch (e) {
      console.log(e)
    }
  }
}
module.exports = { getModel }
