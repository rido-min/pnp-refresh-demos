const lineColors = ['red', 'blue', 'green', 'black']
const getChartData = (telNames) => {
  const chartData = {
    datasets: []
  }
  telNames.forEach(t => {
    chartData.datasets.push({ fill: false, label: t, yAxisID: t, borderColor: lineColors[~~(lineColors.length * Math.random())] })
  })
  return chartData
}

const getChartOptions = (telNames) => {
  const chartOptions = {
    maintainAspectRatio: false,
    scales: {
      yAxes: []
    }
  }
  telNames.forEach(t => {
    chartOptions.scales.yAxes.push(
      {
        id: t,
        type: 'linear',
        scaleLabel: {
          labelString: t,
          display: true
        },
        ticks: {
          beginAtZero: true
        }
      }
    )
  })
  return chartOptions
}

const createChart = (chartId, telNames) => {
  const chartData = getChartData(telNames)
  const chartOptions = getChartOptions(telNames)
  const myLineChart = new window.Chart(
    document.getElementById(chartId).getContext('2d'),
    {
      type: 'line',
      data: chartData,
      options: chartOptions
    }
  )
  return myLineChart
}

export default createChart
