const getDeviceTwin = (deviceId) => {
  return new Promise((resolve, reject) => {
    window.fetch(`/api/getDeviceTwin?deviceId=${deviceId}`)
      .then(resp => resp.json())
      .then(twin => resolve(twin))
      .catch(err => reject(err))
  })
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

export { getDeviceTwin, updateDeviceTwin, invokeCommand }
