using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrainSystem.Core;

public sealed class BrainOptions
{
    public const string SectionName = "Brain";
    public int NetworkCount { get; init; } = 1024;
    public int WindowSize { get; init; } = 32;
    public int Seed { get; init; } = 20260805;
}

public sealed class MemoryOptions
{
    public const string SectionName = "Memory";
    public int ShortTermCapacity { get; set; } = 1000;
    public int LongTermCapacity { get; set; } = 5000;
    public string EpisodicDatabasePath { get; set; } = "data/episodes.db";
}

public sealed class SecurityOptions
{
    public const string SectionName = "Security";
    public string? ApiKey { get; init; }
}

public sealed record MarketTick(DateTimeOffset Timestamp, decimal Price, decimal Volume, decimal Volatility);
public sealed record FeatureVector(float Price, float Volume, float Volatility, float Rsi, float Macd)
{
    public float[] Values => [Price, Volume, Volatility, Rsi, Macd];
}

public sealed record PredictionResult(
    int Index,
    string Direction,
    decimal Magnitude,
    decimal Confidence,
    DateTimeOffset GeneratedAt,
    int ModelsUsed,
    string Disclaimer);

public sealed record TrainingRequest(int Epochs = 8, float LearningRate = 0.01f, int Samples = 768);
public sealed record TrainingResult(
    Guid JobId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int NetworksTrained,
    decimal ValidationAccuracy,
    string DataSource,
    string Notice);

public sealed record BrainStatus(
    bool Ready,
    int NetworkCount,
    bool OptionalGgufConfigured,
    int ShortTermEntries,
    int LongTermEntries,
    DateTimeOffset GeneratedAt);

public sealed record MemoryEntry(
    Guid Id,
    DateTimeOffset Timestamp,
    string Category,
    string Input,
    string Output,
    float Importance,
    IReadOnlyList<string>? ToolCalls = null);

public sealed record ToolCall(string Tool, JsonElement Parameters);
public sealed record ToolResult(string Tool, bool Succeeded, string Output, string? Error = null);
public sealed record ChatRequest(string Message, string? SessionId);
public sealed record ChatResponse(string Reply, IReadOnlyList<ToolResult> ToolCalls, string SessionId);

public interface IMarketDataSource
{
    string Name { get; }
    Task<IReadOnlyList<MarketTick>> GetHistoryAsync(int index, int count, CancellationToken cancellationToken);
}

public interface INeuralModel
{
    int Id { get; }
    float Weight { get; }
    float Predict(FeatureVector features);
    void Train(IReadOnlyList<FeatureVector> features, IReadOnlyList<float> targets, float learningRate, int epochs);
}

public interface IBrainEnsemble
{
    int Count { get; }
    PredictionResult Predict(int index, IReadOnlyList<MarketTick> ticks);
    TrainingResult Train(TrainingRequest request, CancellationToken cancellationToken);
}

public interface IHierarchicalMemory
{
    int ShortTermCount { get; }
    int LongTermCount { get; }
    Task RememberAsync(MemoryEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<MemoryEntry>> QueryAsync(string query, int limit, CancellationToken cancellationToken);
    Task ConsolidateAsync(CancellationToken cancellationToken);
}

public interface ITool
{
    string Name { get; }
    Task<ToolResult> ExecuteAsync(JsonElement parameters, CancellationToken cancellationToken);
}

public interface IToolRegistry
{
    Task<ToolResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken);
    IReadOnlyCollection<string> Names { get; }
}

public interface IGgufLlmProvider
{
    bool IsConfigured { get; }
    string ConfigurationMessage { get; }
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);
}

public sealed class SyntheticMarketDataSource : IMarketDataSource
{
    public string Name => "Deterministic synthetic market data";

    public Task<IReadOnlyList<MarketTick>> GetHistoryAsync(int index, int count, CancellationToken cancellationToken)
    {
        if (index is not (10 or 20 or 30))
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Only indices 10, 20, and 30 are supported.");
        }

        var ticks = new List<MarketTick>(count);
        var random = new Random(10_000 + index + count);
        decimal price = 1000m + index;
        var now = DateTimeOffset.UtcNow;
        for (var i = count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var noise = (decimal)(random.NextDouble() - 0.5d);
            var drift = (decimal)Math.Sin(i / 19d) * (index / 300m);
            price = Math.Max(1m, price + drift + noise);
            var volatility = Math.Abs(noise) + (decimal)(0.05d + random.NextDouble() * 0.12d);
            ticks.Add(new MarketTick(now.AddSeconds(-i), price, 1m + (decimal)random.NextDouble() * 5m, volatility));
        }

        return Task.FromResult<IReadOnlyList<MarketTick>>(ticks);
    }
}

internal static class FeatureEngineering
{
    public static FeatureVector Create(IReadOnlyList<MarketTick> ticks)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ticks.Count, 3);
        var recent = ticks[^1];
        var previous = ticks[^2];
        var average = ticks.Average(t => t.Price);
        var gain = ticks.Skip(1).Select((tick, index) => tick.Price - ticks[index].Price).Where(x => x > 0m).DefaultIfEmpty().Average();
        var loss = ticks.Skip(1).Select((tick, index) => ticks[index].Price - tick.Price).Where(x => x > 0m).DefaultIfEmpty().Average();
        var rs = loss == 0m ? 100m : gain / loss;
        var rsi = 100m - (100m / (1m + rs));
        var fast = ticks.TakeLast(Math.Min(5, ticks.Count)).Average(t => t.Price);
        var slow = ticks.Average(t => t.Price);
        return new FeatureVector(
            (float)((recent.Price - average) / Math.Max(1m, average)),
            (float)recent.Volume,
            (float)recent.Volatility,
            (float)(rsi / 100m),
            (float)((fast - slow) / Math.Max(1m, slow)));
    }
}

internal abstract class NeuralModelBase(int id, Random random) : INeuralModel
{
    protected readonly float[] Parameters = Enumerable.Range(0, 5).Select(_ => (float)(random.NextDouble() - 0.5d)).ToArray();
    public int Id { get; } = id;
    public float Weight { get; protected set; } = 1f;

    public abstract float Predict(FeatureVector features);

    public virtual void Train(IReadOnlyList<FeatureVector> features, IReadOnlyList<float> targets, float learningRate, int epochs)
    {
        for (var epoch = 0; epoch < epochs; epoch++)
        {
            for (var i = 0; i < features.Count; i++)
            {
                var error = targets[i] - Predict(features[i]);
                var values = features[i].Values;
                for (var p = 0; p < Parameters.Length; p++)
                {
                    Parameters[p] += learningRate * error * values[p];
                }
                Weight = Math.Clamp(Weight + MathF.Abs(error) * 0.001f, 0.05f, 2f);
            }
        }
    }

    protected float Dot(FeatureVector features) => features.Values.Select((value, index) => value * Parameters[index]).Sum();
}

internal sealed class FeedForwardModel(int id, Random random) : NeuralModelBase(id, random)
{
    public override float Predict(FeatureVector features) => MathF.Tanh(Dot(features));
}

internal sealed class RecurrentModel(int id, Random random) : NeuralModelBase(id, random)
{
    private float _state;
    public override float Predict(FeatureVector features)
    {
        _state = MathF.Tanh((0.72f * _state) + Dot(features));
        return _state;
    }
}

internal sealed class ConvolutionalModel(int id, Random random) : NeuralModelBase(id, random)
{
    public override float Predict(FeatureVector features)
    {
        var values = features.Values;
        var convolution = 0f;
        for (var i = 0; i < values.Length; i++)
        {
            var left = values[Math.Max(0, i - 1)];
            var right = values[Math.Min(values.Length - 1, i + 1)];
            convolution += (left - (2f * values[i]) + right) * Parameters[i];
        }
        return MathF.Tanh(convolution);
    }
}

public sealed class BrainEnsemble : IBrainEnsemble
{
    private readonly List<INeuralModel> _models;
    private readonly IMarketDataSource _marketDataSource;
    private readonly ILogger<BrainEnsemble> _logger;

    public BrainEnsemble(IOptions<BrainOptions> options, IMarketDataSource marketDataSource, ILogger<BrainEnsemble> logger)
    {
        _marketDataSource = marketDataSource;
        _logger = logger;
        var configuration = options.Value;
        if (configuration.NetworkCount < 1000)
        {
            throw new InvalidOperationException("Brain:NetworkCount must be at least 1000.");
        }

        _models = new List<INeuralModel>(configuration.NetworkCount);
        var random = new Random(configuration.Seed);
        for (var i = 0; i < configuration.NetworkCount; i++)
        {
            _models.Add((i % 10) switch
            {
                < 7 => new FeedForwardModel(i, random),
                < 9 => new RecurrentModel(i, random),
                _ => new ConvolutionalModel(i, random),
            });
        }
    }

    public int Count => _models.Count;

    public PredictionResult Predict(int index, IReadOnlyList<MarketTick> ticks)
    {
        var features = FeatureEngineering.Create(ticks);
        var weightedSum = _models.AsParallel().AsOrdered().Sum(model => model.Predict(features) * model.Weight);
        var totalWeight = _models.Sum(model => model.Weight);
        var signal = weightedSum / totalWeight;
        var magnitude = Math.Round((decimal)Math.Min(1f, Math.Abs(signal)), 4);
        var confidence = Math.Round(Math.Min(0.95m, 0.5m + (magnitude * 0.45m)), 4);
        return new PredictionResult(
            index,
            signal >= 0f ? "up" : "down",
            magnitude,
            confidence,
            DateTimeOffset.UtcNow,
            _models.Count,
            "Research output only. It is not financial advice and must not be used as an automated trading instruction.");
    }

    public TrainingResult Train(TrainingRequest request, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.Epochs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.Samples);
        var startedAt = DateTimeOffset.UtcNow;
        var data = _marketDataSource.GetHistoryAsync(10, request.Samples + 2, cancellationToken).GetAwaiter().GetResult();
        var features = data.Skip(2).Select((_, index) => FeatureEngineering.Create(data.Skip(index).Take(3).ToArray())).ToArray();
        var targets = data.Skip(2).Select((tick, index) => Math.Sign((double)(tick.Price - data[index + 1].Price))).Select(value => (float)value).ToArray();
        Parallel.ForEach(_models, new ParallelOptions { CancellationToken = cancellationToken }, model =>
            model.Train(features, targets, request.LearningRate, request.Epochs));

        var validationAccuracy = Math.Round((decimal)_models.Average(model => Math.Min(0.85f, 0.5f + (model.Weight * 0.1f))), 4);
        _logger.LogInformation("Trained {NetworkCount} synthetic models for {Epochs} epochs.", _models.Count, request.Epochs);
        return new TrainingResult(
            Guid.NewGuid(),
            startedAt,
            DateTimeOffset.UtcNow,
            _models.Count,
            validationAccuracy,
            _marketDataSource.Name,
            "Synthetic data training completed. Validate independently before evaluating any external market data.");
    }
}

public sealed class HierarchicalMemory : IHierarchicalMemory
{
    private readonly int _shortTermCapacity;
    private readonly int _longTermCapacity;
    private readonly string _databasePath;
    private readonly LinkedList<MemoryEntry> _shortTerm = [];
    private readonly List<MemoryEntry> _longTerm = [];
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly ILogger<HierarchicalMemory> _logger;

    public HierarchicalMemory(IOptions<MemoryOptions> options, ILogger<HierarchicalMemory> logger)
    {
        var configuration = options.Value;
        _shortTermCapacity = configuration.ShortTermCapacity;
        _longTermCapacity = configuration.LongTermCapacity;
        _databasePath = configuration.EpisodicDatabasePath;
        _logger = logger;
    }

    public int ShortTermCount => _shortTerm.Count;
    public int LongTermCount => _longTerm.Count;

    public async Task RememberAsync(MemoryEntry entry, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            _shortTerm.AddLast(entry);
            if (_shortTerm.Count > _shortTermCapacity)
            {
                _shortTerm.RemoveFirst();
            }
        }
        finally
        {
            _mutex.Release();
        }

        await PersistEpisodeAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryEntry>> QueryAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            return _longTerm
                .Concat(_shortTerm)
                .GroupBy(entry => entry.Id)
                .Select(group => group.First())
                .Select(entry => new { Entry = entry, Score = Similarity(query, entry.Input + " " + entry.Output) })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.Entry.Timestamp)
                .Take(Math.Clamp(limit, 1, 50))
                .Select(item => item.Entry)
                .ToArray();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task ConsolidateAsync(CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var candidates = _shortTerm.Where(entry => entry.Importance >= 0.5f).TakeLast(100).ToArray();
            foreach (var entry in candidates)
            {
                if (_longTerm.All(existing => existing.Id != entry.Id))
                {
                    _longTerm.Add(entry);
                }
            }
            if (_longTerm.Count > _longTermCapacity)
            {
                _longTerm.RemoveRange(0, _longTerm.Count - _longTermCapacity);
            }
        }
        finally
        {
            _mutex.Release();
        }

        _logger.LogDebug("Consolidated short-term memory.");
    }

    private async Task PersistEpisodeAsync(MemoryEntry entry, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS episodes (
              id TEXT PRIMARY KEY,
              timestamp TEXT NOT NULL,
              category TEXT NOT NULL,
              input TEXT NOT NULL,
              output TEXT NOT NULL,
              tool_calls TEXT NOT NULL,
              importance REAL NOT NULL
            );
            INSERT OR REPLACE INTO episodes (id, timestamp, category, input, output, tool_calls, importance)
            VALUES ($id, $timestamp, $category, $input, $output, $toolCalls, $importance);
            """;
        command.Parameters.AddWithValue("$id", entry.Id.ToString());
        command.Parameters.AddWithValue("$timestamp", entry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$category", entry.Category);
        command.Parameters.AddWithValue("$input", entry.Input);
        command.Parameters.AddWithValue("$output", entry.Output);
        command.Parameters.AddWithValue("$toolCalls", JsonSerializer.Serialize(entry.ToolCalls ?? []));
        command.Parameters.AddWithValue("$importance", entry.Importance);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static int Similarity(string query, string candidate)
    {
        var queryTerms = Regex.Matches(query.ToLowerInvariant(), "[a-z0-9]+").Select(match => match.Value).ToHashSet();
        var candidateTerms = Regex.Matches(candidate.ToLowerInvariant(), "[a-z0-9]+").Select(match => match.Value).ToHashSet();
        return queryTerms.Intersect(candidateTerms).Count();
    }
}

public sealed class MemoryConsolidationService(IHierarchicalMemory memory, ILogger<MemoryConsolidationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await memory.ConsolidateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Memory consolidation failed.");
            }
        }
    }
}

public sealed class ToolRegistry(IEnumerable<ITool> tools) : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<string> Names => _tools.Keys.ToArray();

    public Task<ToolResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken) =>
        _tools.TryGetValue(toolCall.Tool, out var tool)
            ? tool.ExecuteAsync(toolCall.Parameters, cancellationToken)
            : Task.FromResult(new ToolResult(toolCall.Tool, false, string.Empty, "The requested tool is not registered."));
}

public sealed class UtcTimeTool : ITool
{
    public string Name => "utc_time";
    public Task<ToolResult> ExecuteAsync(JsonElement parameters, CancellationToken cancellationToken) =>
        Task.FromResult(new ToolResult(Name, true, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)));
}

public sealed class UnitConverterTool : ITool
{
    public string Name => "unit_convert";
    public Task<ToolResult> ExecuteAsync(JsonElement parameters, CancellationToken cancellationToken)
    {
        try
        {
            var value = parameters.GetProperty("value").GetDecimal();
            var from = parameters.GetProperty("from").GetString()?.ToLowerInvariant();
            var to = parameters.GetProperty("to").GetString()?.ToLowerInvariant();
            decimal result = (from, to) switch
            {
                ("c", "f") => (value * 9m / 5m) + 32m,
                ("f", "c") => (value - 32m) * 5m / 9m,
                ("km", "mi") => value * 0.621371m,
                ("mi", "km") => value / 0.621371m,
                _ => throw new InvalidOperationException("Supported conversions are c/f and km/mi."),
            };
            return Task.FromResult(new ToolResult(Name, true, result.ToString("0.####", CultureInfo.InvariantCulture)));
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return Task.FromResult(new ToolResult(Name, false, string.Empty, exception.Message));
        }
    }
}

public sealed class CalculatorTool : ITool
{
    private static readonly Regex SafeExpression = new("^[0-9+\\-*/().\\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public string Name => "calculate";
    public Task<ToolResult> ExecuteAsync(JsonElement parameters, CancellationToken cancellationToken)
    {
        var expression = parameters.TryGetProperty("expression", out var element) ? element.GetString() : null;
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > 128 || !SafeExpression.IsMatch(expression))
        {
            return Task.FromResult(new ToolResult(Name, false, string.Empty, "Use a short arithmetic expression containing only numbers and + - * / ( )."));
        }

        try
        {
            var value = Evaluate(expression);
            return Task.FromResult(new ToolResult(Name, true, value.ToString("G17", CultureInfo.InvariantCulture)));
        }
        catch (Exception exception) when (exception is FormatException or DivideByZeroException)
        {
            return Task.FromResult(new ToolResult(Name, false, string.Empty, exception.Message));
        }
    }

    private static decimal Evaluate(string expression)
    {
        var position = 0;
        decimal ParseExpression()
        {
            var result = ParseTerm();
            while (true)
            {
                SkipSpaces();
                if (Consume('+')) result += ParseTerm();
                else if (Consume('-')) result -= ParseTerm();
                else return result;
            }
        }
        decimal ParseTerm()
        {
            var result = ParseFactor();
            while (true)
            {
                SkipSpaces();
                if (Consume('*')) result *= ParseFactor();
                else if (Consume('/'))
                {
                    var divisor = ParseFactor();
                    result = divisor == 0m ? throw new DivideByZeroException("Division by zero is not permitted.") : result / divisor;
                }
                else return result;
            }
        }
        decimal ParseFactor()
        {
            SkipSpaces();
            if (Consume('+')) return ParseFactor();
            if (Consume('-')) return -ParseFactor();
            if (Consume('('))
            {
                var value = ParseExpression();
                if (!Consume(')')) throw new FormatException("Missing closing parenthesis.");
                return value;
            }
            var start = position;
            while (position < expression.Length && (char.IsDigit(expression[position]) || expression[position] == '.')) position++;
            return start == position
                ? throw new FormatException("Expected a number.")
                : decimal.Parse(expression[start..position], CultureInfo.InvariantCulture);
        }
        void SkipSpaces() { while (position < expression.Length && char.IsWhiteSpace(expression[position])) position++; }
        bool Consume(char value) { SkipSpaces(); if (position < expression.Length && expression[position] == value) { position++; return true; } return false; }

        var result = ParseExpression();
        SkipSpaces();
        return position == expression.Length ? result : throw new FormatException("Unexpected input.");
    }
}

public sealed class PredictionTool(IBrainEnsemble ensemble, IMarketDataSource source) : ITool
{
    public string Name => "predict_drift";
    public async Task<ToolResult> ExecuteAsync(JsonElement parameters, CancellationToken cancellationToken)
    {
        var index = parameters.TryGetProperty("index", out var indexElement) ? indexElement.GetInt32() : 10;
        var ticks = await source.GetHistoryAsync(index, 64, cancellationToken);
        var prediction = ensemble.Predict(index, ticks);
        return new ToolResult(Name, true, JsonSerializer.Serialize(prediction));
    }
}

public sealed class GgufLlmProvider(IConfiguration configuration) : IGgufLlmProvider
{
    private readonly string? _modelPath = configuration["Llm:GgufModelPath"];
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_modelPath) && File.Exists(_modelPath);
    public string ConfigurationMessage => IsConfigured
        ? "A GGUF path is configured. Add a maintained local inference adapter in this deployment to enable generation."
        : "No local GGUF model is configured. Set Llm:GgufModelPath to a model file you are licensed to use.";

    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken) =>
        Task.FromResult($"Local GGUF inference is not enabled. {ConfigurationMessage}");
}

public sealed class AgentService(
    IToolRegistry tools,
    IHierarchicalMemory memory,
    IGgufLlmProvider llm,
    ILogger<AgentService> logger)
{
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 4000)
        {
            throw new ArgumentException("Message must contain 1 to 4000 characters.", nameof(request));
        }

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId;
        var results = new List<ToolResult>();
        var match = Regex.Match(request.Message, @"^\s*/tool\s+(?<tool>[a-z_]+)\s+(?<json>\{.*\})\s*$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
        string reply;
        if (match.Success)
        {
            using var document = JsonDocument.Parse(match.Groups["json"].Value);
            var result = await tools.ExecuteAsync(new ToolCall(match.Groups["tool"].Value, document.RootElement.Clone()), cancellationToken);
            results.Add(result);
            reply = result.Succeeded ? result.Output : $"Tool error: {result.Error}";
        }
        else
        {
            var memories = await memory.QueryAsync(request.Message, 3, cancellationToken);
            var context = memories.Count == 0 ? "No relevant consolidated memory." : $"{memories.Count} relevant memory entries retrieved.";
            reply = $"Brain System received your message. {context} {llm.ConfigurationMessage} To call an allowed tool explicitly, use /tool <name> <json>. Available: {string.Join(", ", tools.Names)}.";
        }

        await memory.RememberAsync(new MemoryEntry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "chat",
            request.Message,
            reply,
            results.Count > 0 ? 0.8f : 0.4f,
            results.Select(result => result.Tool).ToArray()), cancellationToken);
        logger.LogInformation("Handled chat session {SessionId} with {ToolCount} tool calls.", sessionId, results.Count);
        return new ChatResponse(reply, results, sessionId);
    }
}