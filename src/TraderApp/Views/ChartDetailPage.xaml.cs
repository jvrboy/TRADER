using System.Text.Json;
using TraderUI.Models;
using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class ChartDetailPage : ContentPage
{
    private readonly ChartViewModel _vm;

    public ChartDetailPage(ChartViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        vm.Bars.CollectionChanged += (s, e) => UpdateChart();
    }

    private void UpdateChart()
    {
        if (_vm.Bars.Count == 0) return;
        var html = BuildChartHtml(_vm.Bars.ToList(), _vm.SelectedSymbol, _vm.SelectedTimeframe);
        FullChartWebView.Source = new HtmlWebViewSource { Html = html };
    }

    private static string BuildChartHtml(List<OhlcBar> bars, string symbol, string timeframe)
    {
        var candleData = bars.Select(b => new
        {
            time = ((DateTimeOffset)b.Time.ToUniversalTime()).ToUnixTimeSeconds(),
            open = (double)b.Open,
            high = (double)b.High,
            low = (double)b.Low,
            close = (double)b.Close
        });
        var json = JsonSerializer.Serialize(candleData);
        string tfLabel = timeframe switch { "1" => "1m", "5" => "5m", "15" => "15m", "60" => "1H", "240" => "4H", "1440" => "1D", "10080" => "1W", _ => timeframe };
        return $@"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no'>
<script src='https://unpkg.com/lightweight-charts/dist/lightweight-charts.standalone.production.js'></script>
<style>* {{ margin: 0; padding: 0; box-sizing: border-box; }} body {{ background: #0A0E1A; overflow: hidden; }} #chart {{ width: 100vw; height: 100vh; }}</style>
</head>
<body>
<div id='chart'></div>
<script>
const chart = LightweightCharts.createChart(document.getElementById('chart'), {{
  width: window.innerWidth, height: window.innerHeight,
  layout: {{ background: {{ color: '#0A0E1A' }}, textColor: '#A0AEC0' }},
  grid: {{ vertLines: {{ color: '#1A2235' }}, horzLines: {{ color: '#1A2235' }} }},
  crosshair: {{ mode: LightweightCharts.CrosshairMode.Normal }},
  rightPriceScale: {{ borderColor: '#1E2A3A' }},
  timeScale: {{ borderColor: '#1E2A3A', timeVisible: true }},
}});
const cs = chart.addCandlestickSeries({{ upColor: '#00E676', downColor: '#FF3D57', borderUpColor: '#00E676', borderDownColor: '#FF3D57', wickUpColor: '#00E676', wickDownColor: '#FF3D57' }});
cs.setData({json});
chart.timeScale().fitContent();
window.addEventListener('resize', () => chart.applyOptions({{ width: window.innerWidth, height: window.innerHeight }}));
</script>
</body>
</html>";
    }
}
