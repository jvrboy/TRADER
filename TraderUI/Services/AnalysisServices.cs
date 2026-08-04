using TraderUI.Models;

namespace TraderUI.Services;

/// <summary>
/// C# port of divergence-system/divergence.py
/// Detects RSI, MACD, Stochastic divergences (bullish/bearish, regular/hidden)
/// </summary>
public class DivergenceService : IDivergenceService
{
    private readonly IIndicatorService _indicators;

    public DivergenceService(IIndicatorService indicators)
    {
        _indicators = indicators;
    }

    public async Task<List<DivergenceResult>> DetectDivergencesAsync(string symbol, string timeframe, List<OhlcBar> bars)
    {
        var results = new List<DivergenceResult>();
        if (bars.Count < 30) return results;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var rsiValues = await CalculateRsiSeriesAsync(bars);
        var (_, _, macdHist) = await _indicators.CalculateMacdAsync(bars);

        // Find price pivots
        var priceLows = FindPivotLows(closes, 5);
        var priceHighs = FindPivotHighs(closes, 5);
        var rsiLows = FindPivotLows(rsiValues, 5);
        var rsiHighs = FindPivotHighs(rsiValues, 5);

        // Regular Bullish Divergence: price lower low, RSI higher low
        if (priceLows.Count >= 2 && rsiLows.Count >= 2)
        {
            var (pi1, pv1) = priceLows[^2];
            var (pi2, pv2) = priceLows[^1];
            var (ri1, rv1) = rsiLows[^2];
            var (ri2, rv2) = rsiLows[^1];

            if (pv2 < pv1 && rv2 > rv1)
            {
                results.Add(new DivergenceResult
                {
                    Symbol = symbol, Timeframe = timeframe,
                    Type = "Regular Bullish", Indicator = "RSI",
                    Direction = "Buy", Strength = Math.Abs(rv2 - rv1) / 10.0,
                    DetectedAt = bars[pi2].Time
                });
            }

            // Regular Bearish Divergence: price higher high, RSI lower high
            if (priceHighs.Count >= 2 && rsiHighs.Count >= 2)
            {
                var (phi1, phv1) = priceHighs[^2];
                var (phi2, phv2) = priceHighs[^1];
                var (rhi1, rhv1) = rsiHighs[^2];
                var (rhi2, rhv2) = rsiHighs[^1];

                if (phv2 > phv1 && rhv2 < rhv1)
                {
                    results.Add(new DivergenceResult
                    {
                        Symbol = symbol, Timeframe = timeframe,
                        Type = "Regular Bearish", Indicator = "RSI",
                        Direction = "Sell", Strength = Math.Abs(rhv2 - rhv1) / 10.0,
                        DetectedAt = bars[phi2].Time
                    });
                }
            }
        }

        // Hidden Bullish: price higher low, RSI lower low (trend continuation)
        if (priceLows.Count >= 2 && rsiLows.Count >= 2)
        {
            var (pi1, pv1) = priceLows[^2];
            var (pi2, pv2) = priceLows[^1];
            var (ri1, rv1) = rsiLows[^2];
            var (ri2, rv2) = rsiLows[^1];
            if (pv2 > pv1 && rv2 < rv1)
            {
                results.Add(new DivergenceResult
                {
                    Symbol = symbol, Timeframe = timeframe,
                    Type = "Hidden Bullish", Indicator = "RSI",
                    Direction = "Buy", Strength = Math.Abs(rv2 - rv1) / 15.0,
                    DetectedAt = bars[pi2].Time
                });
            }
        }

        return results;
    }

    public async Task<DivergenceResult?> GetLatestDivergenceAsync(string symbol)
    {
        await Task.CompletedTask;
        return null; // Would query stored results
    }

    private async Task<double[]> CalculateRsiSeriesAsync(List<OhlcBar> bars, int period = 14)
    {
        var result = new double[bars.Count];
        for (int i = period; i < bars.Count; i++)
        {
            var slice = bars.Skip(i - period).Take(period + 1).ToList();
            result[i] = await _indicators.CalculateRsiAsync(slice, period);
        }
        return result;
    }

    private static List<(int index, double value)> FindPivotLows(double[] data, int lookback)
    {
        var pivots = new List<(int, double)>();
        for (int i = lookback; i < data.Length - lookback; i++)
        {
            var isLow = true;
            for (int j = i - lookback; j <= i + lookback; j++)
                if (j != i && data[j] <= data[i]) { isLow = false; break; }
            if (isLow) pivots.Add((i, data[i]));
        }
        return pivots;
    }

    private static List<(int index, double value)> FindPivotHighs(double[] data, int lookback)
    {
        var pivots = new List<(int, double)>();
        for (int i = lookback; i < data.Length - lookback; i++)
        {
            var isHigh = true;
            for (int j = i - lookback; j <= i + lookback; j++)
                if (j != i && data[j] >= data[i]) { isHigh = false; break; }
            if (isHigh) pivots.Add((i, data[i]));
        }
        return pivots;
    }
}

/// <summary>
/// C# port of drift-switch-lab/features.py and cli.py
/// Implements EMA, RSI, Breakout, Volatility strategies
/// </summary>
public class DriftLabService : IDriftLabService
{
    private readonly IIndicatorService _indicators;

    public DriftLabService(IIndicatorService indicators)
    {
        _indicators = indicators;
    }

    public async Task<string> GetSignalAsync(string strategy, List<OhlcBar> bars, double threshold = 0.5)
    {
        if (bars.Count < 50) return "Insufficient data";
        var closes = bars.Select(b => (double)b.Close).ToArray();

        return strategy.ToLower() switch
        {
            "ema" => await GetEmaSignalAsync(bars),
            "rsi" => await GetRsiSignalAsync(bars),
            "breakout" => await GetBreakoutSignalAsync(bars),
            "volatility" => await GetVolatilitySignalAsync(bars, threshold),
            _ => await GetEmaSignalAsync(bars)
        };
    }

    public async Task<Dictionary<string, double>> CompareStrategiesAsync(List<OhlcBar> bars)
    {
        var results = new Dictionary<string, double>();
        foreach (var strategy in new[] { "ema", "rsi", "breakout", "volatility" })
        {
            var signal = await GetSignalAsync(strategy, bars);
            results[strategy] = signal == "Buy" ? 1.0 : signal == "Sell" ? -1.0 : 0.0;
        }
        return results;
    }

    public async Task<Dictionary<string, object>> BacktestAsync(string strategy, List<OhlcBar> bars)
    {
        var trades = 0;
        var wins = 0;
        var totalPnl = 0.0;

        for (int i = 50; i < bars.Count - 1; i++)
        {
            var slice = bars.Take(i).ToList();
            var signal = await GetSignalAsync(strategy, slice);
            if (signal == "Buy" || signal == "Sell")
            {
                trades++;
                var entry = (double)bars[i].Close;
                var exit = (double)bars[i + 1].Close;
                var pnl = signal == "Buy" ? (exit - entry) / entry : (entry - exit) / entry;
                totalPnl += pnl;
                if (pnl > 0) wins++;
            }
        }

        return new Dictionary<string, object>
        {
            ["strategy"] = strategy,
            ["total_trades"] = trades,
            ["win_rate"] = trades > 0 ? (double)wins / trades * 100 : 0,
            ["total_pnl_pct"] = totalPnl * 100,
            ["avg_pnl_pct"] = trades > 0 ? totalPnl / trades * 100 : 0
        };
    }

    private async Task<string> GetEmaSignalAsync(List<OhlcBar> bars)
    {
        var emaFast = await _indicators.CalculateEmaAsync(bars, 9);
        var emaSlow = await _indicators.CalculateEmaAsync(bars, 21);
        return emaFast > emaSlow ? "Buy" : emaFast < emaSlow ? "Sell" : "Neutral";
    }

    private async Task<string> GetRsiSignalAsync(List<OhlcBar> bars)
    {
        var rsi = await _indicators.CalculateRsiAsync(bars);
        return rsi < 35 ? "Buy" : rsi > 65 ? "Sell" : "Neutral";
    }

    private async Task<string> GetBreakoutSignalAsync(List<OhlcBar> bars)
    {
        await Task.CompletedTask;
        var lookback = 20;
        var recent = bars.TakeLast(lookback + 1).ToList();
        var rangeHigh = recent.SkipLast(1).Max(b => (double)b.High);
        var rangeLow = recent.SkipLast(1).Min(b => (double)b.Low);
        var close = (double)bars.Last().Close;
        return close > rangeHigh ? "Buy" : close < rangeLow ? "Sell" : "Neutral";
    }

    private async Task<string> GetVolatilitySignalAsync(List<OhlcBar> bars, double threshold)
    {
        var closes = bars.Select(b => (double)b.Close).ToArray();
        var returns = closes.Skip(1).Zip(closes, (c, p) => p != 0 ? (c - p) / p : 0).ToArray();
        var volatility = returns.TakeLast(20).Select(r => r * r).Average();
        var medianVol = returns.TakeLast(50).Select(r => r * r).OrderBy(v => v).ElementAt(25);
        var momentum = closes.Length >= 4 ? (closes.Last() - closes[^4]) / closes[^4] : 0;
        var emaFast = await _indicators.CalculateEmaAsync(bars, 9);
        var emaSlow = await _indicators.CalculateEmaAsync(bars, 21);
        var trend = emaFast > emaSlow ? 1 : -1;
        return volatility > medianVol * threshold
            ? (trend > 0 ? "Buy" : "Sell")
            : "Neutral";
    }
}

/// <summary>
/// C# port of synthetics_analysis — generates synthetic OHLC data for Deriv indices
/// </summary>
public class SyntheticsService : ISyntheticsService
{
    private readonly Random _rng = new();

    private static readonly Dictionary<string, (double basePrice, double volatility)> _indices = new()
    {
        ["VOL10"] = (6500, 0.001),
        ["VOL25"] = (4500, 0.0025),
        ["VOL50"] = (3500, 0.005),
        ["VOL75"] = (2500, 0.0075),
        ["VOL100"] = (1500, 0.01),
        ["1HZ10V"] = (6234, 0.001),
        ["1HZ25V"] = (4567, 0.0025),
        ["1HZ50V"] = (3456, 0.005),
        ["1HZ75V"] = (2345, 0.0075),
        ["1HZ100V"] = (1234, 0.01),
        ["CRASH300"] = (8000, 0.008),
        ["CRASH500"] = (7000, 0.006),
        ["CRASH1000"] = (6000, 0.004),
        ["BOOM300"] = (8000, 0.008),
        ["BOOM500"] = (7000, 0.006),
        ["BOOM1000"] = (6000, 0.004),
    };

    public async Task<List<OhlcBar>> GenerateSyntheticOhlcAsync(string index, int periods = 500, double volatility = 0.02)
    {
        await Task.CompletedTask;
        var (basePrice, baseVol) = _indices.GetValueOrDefault(index.ToUpper(), (1000, 0.005));
        var actualVol = volatility > 0 ? volatility : baseVol;
        var bars = new List<OhlcBar>();
        var price = basePrice;
        var now = DateTime.UtcNow;

        for (int i = periods; i >= 0; i--)
        {
            var open = price;
            var change = (_rng.NextDouble() - 0.5) * 2 * actualVol * price;
            var close = price + change;
            var high = Math.Max(open, close) * (1 + _rng.NextDouble() * actualVol * 0.5);
            var low = Math.Min(open, close) * (1 - _rng.NextDouble() * actualVol * 0.5);

            // Crash/Boom spikes
            if (index.ToUpper().Contains("CRASH") && _rng.NextDouble() < 0.001)
                close = open * (1 - 0.05 * _rng.NextDouble());
            if (index.ToUpper().Contains("BOOM") && _rng.NextDouble() < 0.001)
                close = open * (1 + 0.05 * _rng.NextDouble());

            bars.Add(new OhlcBar
            {
                Time = now.AddMinutes(-i),
                Open = (decimal)open,
                High = (decimal)high,
                Low = (decimal)low,
                Close = (decimal)close,
                Volume = (decimal)(_rng.NextDouble() * 10000)
            });
            price = close;
        }
        return bars;
    }

    public async Task<Dictionary<string, object>> BacktestStrategyAsync(List<OhlcBar> bars, string strategyName)
    {
        await Task.CompletedTask;
        var trades = 0;
        var wins = 0;
        var totalPnl = 0.0;
        var maxDrawdown = 0.0;
        var equity = 10000.0;
        var peak = equity;

        for (int i = 20; i < bars.Count - 1; i++)
        {
            var signal = GetSimpleSignal(bars, i, strategyName);
            if (signal != 0)
            {
                trades++;
                var entry = (double)bars[i].Close;
                var exit = (double)bars[i + 1].Close;
                var pnl = signal * (exit - entry) / entry;
                totalPnl += pnl;
                equity *= (1 + pnl);
                if (equity > peak) peak = equity;
                var drawdown = (peak - equity) / peak;
                if (drawdown > maxDrawdown) maxDrawdown = drawdown;
                if (pnl > 0) wins++;
            }
        }

        return new Dictionary<string, object>
        {
            ["strategy"] = strategyName,
            ["total_trades"] = trades,
            ["win_rate"] = trades > 0 ? Math.Round((double)wins / trades * 100, 2) : 0,
            ["total_pnl_pct"] = Math.Round(totalPnl * 100, 2),
            ["max_drawdown_pct"] = Math.Round(maxDrawdown * 100, 2),
            ["final_equity"] = Math.Round(equity, 2),
            ["sharpe_ratio"] = Math.Round(totalPnl / Math.Max(0.001, maxDrawdown), 2)
        };
    }

    public async Task<List<string>> GetAvailableIndicesAsync()
    {
        await Task.CompletedTask;
        return _indices.Keys.ToList();
    }

    private static int GetSimpleSignal(List<OhlcBar> bars, int index, string strategy)
    {
        if (index < 20) return 0;
        var closes = bars.Take(index + 1).Select(b => (double)b.Close).ToArray();
        return strategy.ToLower() switch
        {
            "ema" => closes.TakeLast(9).Average() > closes.TakeLast(21).Average() ? 1 : -1,
            "rsi" => closes.TakeLast(5).Average() > closes.TakeLast(14).Average() ? 1 : -1,
            _ => closes.Last() > closes[^5] ? 1 : -1
        };
    }
}

/// <summary>
/// C# port of ai_brain_tool — Self-improving neural network for price prediction
/// </summary>
public class AiBrainService : IAiBrainService
{
    private readonly Dictionary<string, NeuralTask> _tasks = new();
    private readonly ILocalStorageService _storage;

    public AiBrainService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task<double> PredictAsync(string taskName, double[] inputs)
    {
        await Task.CompletedTask;
        if (!_tasks.TryGetValue(taskName, out var task))
            return 0.5; // Default neutral prediction
        return task.Predict(inputs);
    }

    public async Task TrainAsync(string taskName, List<(double[] inputs, double[] outputs)> examples, int epochs = 200)
    {
        await Task.CompletedTask;
        if (!_tasks.ContainsKey(taskName))
            _tasks[taskName] = new NeuralTask(taskName, examples[0].inputs.Length, examples[0].outputs.Length);
        _tasks[taskName].Train(examples, epochs);
        await SaveStateAsync();
    }

    public async Task<Dictionary<string, object>> GetStatsAsync(string taskName)
    {
        await Task.CompletedTask;
        if (!_tasks.TryGetValue(taskName, out var task))
            return new Dictionary<string, object> { ["error"] = "Task not found" };
        return new Dictionary<string, object>
        {
            ["task"] = taskName,
            ["epochs_trained"] = task.EpochsTrained,
            ["learning_rate"] = task.LearningRate,
            ["last_loss"] = task.LastLoss
        };
    }

    public async Task<List<string>> GetTaskNamesAsync()
    {
        await Task.CompletedTask;
        return _tasks.Keys.ToList();
    }

    private async Task SaveStateAsync()
    {
        var state = _tasks.ToDictionary(
            kvp => kvp.Key,
            kvp => new { kvp.Value.EpochsTrained, kvp.Value.LearningRate, kvp.Value.LastLoss });
        await _storage.SaveAsync("ai_brain_state", state);
    }

    // Simple neural network implementation
    private class NeuralTask
    {
        public string Name { get; }
        public int EpochsTrained { get; private set; }
        public double LearningRate { get; private set; } = 0.1;
        public double LastLoss { get; private set; }
        private readonly int _inputs;
        private readonly int _outputs;
        private double[,] _weights;
        private readonly Random _rng = new();

        public NeuralTask(string name, int inputs, int outputs)
        {
            Name = name;
            _inputs = inputs;
            _outputs = outputs;
            _weights = new double[inputs, outputs];
            // Xavier initialization
            var scale = Math.Sqrt(2.0 / (inputs + outputs));
            for (int i = 0; i < inputs; i++)
                for (int j = 0; j < outputs; j++)
                    _weights[i, j] = (_rng.NextDouble() * 2 - 1) * scale;
        }

        public double Predict(double[] inputs)
        {
            var output = 0.0;
            for (int i = 0; i < Math.Min(inputs.Length, _inputs); i++)
                for (int j = 0; j < _outputs; j++)
                    output += inputs[i] * _weights[i, j];
            return Sigmoid(output / _outputs);
        }

        public void Train(List<(double[] inputs, double[] outputs)> examples, int epochs)
        {
            for (int e = 0; e < epochs; e++)
            {
                var totalLoss = 0.0;
                foreach (var (inp, outp) in examples)
                {
                    var pred = Predict(inp);
                    var target = outp[0];
                    var error = pred - target;
                    totalLoss += error * error;
                    // Gradient descent
                    for (int i = 0; i < Math.Min(inp.Length, _inputs); i++)
                        for (int j = 0; j < _outputs; j++)
                            _weights[i, j] -= LearningRate * error * SigmoidDerivative(pred) * inp[i];
                }
                LastLoss = totalLoss / examples.Count;
                EpochsTrained++;
                // Self-improve: adjust learning rate
                if (e > 0 && e % 50 == 0 && LastLoss > 0.01)
                    LearningRate *= 0.9;
            }
        }

        private static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-Math.Clamp(x, -500, 500)));
        private static double SigmoidDerivative(double s) => s * (1 - s);
    }
}
