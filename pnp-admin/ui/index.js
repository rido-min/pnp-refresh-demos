/* global connectionstring, $ */
(() => {
  function createVueApp () {
    let intervalId
    let asc = 1
    const oneSecond = 1000
    const defaultRefreshSeconds = 10
    const defaultRefreshInterval = defaultRefreshSeconds * oneSecond

    const app = new Vue({
      el: '#deviceList',
      data: {
        hub: 'not set',
        devices: [],
        deviceStatus: {},
        elapsed: defaultRefreshSeconds,
        refresh: defaultRefreshInterval,
        refreshEnabled: false,
        loading: false
      },
      methods: {
        sortBy: function (by) {
          if (this.devices.length > 0) {
            this.devices.sort((a, b) => a[by] > b[by] ? asc : -asc)
            asc = -asc
          }
        },
        updateRefresh: function (event) {
          let interval = parseInt(window.prompt('Seconds to refresh', this.refresh / oneSecond), 10) * oneSecond
          if (isNaN(interval)) interval = defaultRefreshInterval
          this.refresh = interval
        },
        refreshCount: function () {
          this.deviceStatus.Disconnected = this.devices.filter(d => d.state === 'Disconnected').length
          this.deviceStatus.Connected = this.devices.filter(d => d.state === 'Connected').length
          this.deviceStatus.Total = this.devices.length
          this.loading = false
        },
        refreshDevices: async function () {
          this.loading = true
          window.fetch('/api/getDevices')
            .then(resp => resp.json())
            .then(devicesDto => {
              devicesDto.forEach(d => {
                window.fetch(`/api/getModelId?deviceId=${d.id}`)
                  .then(resp => resp.json())
                  .then(m => {
                    d.modelId = m
                  })
              })
              this.devices = devicesDto
              this.refreshCount()
            })
        },
        postConnectionString: async function (event) {
          console.log(connectionstring.value)
          if (connectionstring.value.length > 0) {
            await window.fetch('/api/connection-string',
              {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: `connectionstring=${encodeURIComponent(connectionstring.value)}`
              })
              .then(resp => resp.json())
              .then(text => { this.hub = text })
            await this.refreshDevices()
            $('#formConnectionString').collapse('hide')
          }
        },
        toggleAutoRefresh: function (event) {
          this.refreshEnabled = event.srcElement.checked
          if (this.refreshEnabled) {
            let lastRefreshTime = new Date()
            intervalId = setInterval(async () => {
              const currentTime = new Date()
              const elapsedTime = currentTime - lastRefreshTime
              this.elapsed = Math.round((this.refresh - Math.abs(elapsedTime)) / oneSecond)
              if (elapsedTime > this.refresh) {
                console.log('Refresh timer fired.')
                await this.refreshDevices()
                lastRefreshTime = currentTime
              }
            }, oneSecond)
          } else {
            clearInterval(intervalId)
          }
        }
      }
    })
    return app
  }

  const app = createVueApp()

  window.fetch('/api/connection-string')
    .then(resp => resp.json())
    .then(async (json) => {
      if (json.length < 20) {
        app.hub = '<not configured>'
      } else {
        app.hub = json
        await app.refreshDevices()
      }
    })
})()
