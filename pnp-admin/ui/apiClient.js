const getDeviceTwin = (deviceId) => {
  return new Promise((resolve, reject) => {
    window.fetch(`/api/getDeviceTwin?deviceId=${deviceId}`)
      .then(resp => resp.json())
      .then(twin => resolve(twin))
      .catch(err => reject(err))
  })
}

const getModelId = (deviceId) => {
  return new Promise((resolve, reject) => {
    window.fetch(`/api/getModelId?deviceId=${deviceId}`)
      .then(resp => resp.json())
      .then(m => resolve(m))
      .catch(err => reject(err))
  })
}

const getModel = async (modelId) => {
  const repositoryEndpoint = 'devicemodels.azure.com'
  const isDtmi = dtmi => RegExp('^dtmi:[A-Za-z](?:[A-Za-z0-9_]*[A-Za-z0-9])?(?::[A-Za-z](?:[A-Za-z0-9_]*[A-Za-z0-9])?)*;[1-9][0-9]{0,8}$').test(dtmi)
  const dtmiToPath = dtmi => {
    if (isDtmi(dtmi)) {
      return `/${dtmi.toLowerCase().replace(/:/g, '/').replace(';', '-')}.json`
    } else return null
  }
  const url = `https://${repositoryEndpoint}/${dtmiToPath(modelId)}` 
  return await (await window.fetch(url)).json()
}

const updateDeviceTwin = (deviceId, propertyName, propertyValue) => {
  const options = {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ deviceId, propertyName, propertyValue })
  }
  return new Promise((resolve, reject) => {
    window.fetch('/api/updateDeviceTwin', options)
      .then(resp => resp.json())
      .then(d => resolve(d))
      .catch(err => reject(err))
  })
}

const invokeCommand = (deviceId, commandName, payload) => {
  const options = {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ deviceId, commandName, payload })
  }
  return new Promise((resolve, reject) => {
    window.fetch('/api/invokeCommand', options)
      .then(resp => resp.json())
      .then(d => resolve(d))
      .catch(err => reject(err))
  })
}

export { getDeviceTwin, updateDeviceTwin, invokeCommand, getModelId, getModel }
