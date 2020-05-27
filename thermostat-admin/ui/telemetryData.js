export default class TelemetryData {
  /** @param {string deviceId */
  /** @param {Array<string} telNames */
  constructor (deviceId, telNames) {
    this.deviceId = deviceId
    this.maxLen = 50
    this.timeData = new Array(this.maxLen)
    this.dataPoints = new Array(telNames.length)
    telNames.forEach(el => {
      this.dataPoints[el] = new Array(this.maxLen)
    })

    this.temperatureData = new Array(this.maxLen)
  }

  addDataPoint (time, telName, dataPoint) {
    const t = new Date(time)
    const timeString = `${t.getHours()}:${t.getMinutes()}:${t.getSeconds()}`

    this.timeData.push(timeString)
    this.dataPoints[telName].push(dataPoint)

    if (this.timeData.length > this.maxLen) {
      this.timeData.shift()
      this.dataPoints[telName].shift()
    }
  }
}
