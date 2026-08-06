using Brain.Core.Ensemble;
using Brain.LLM;
using Brain.Memory;
using Brain.Tools;
using Brain.Training;

namespace Brain.API.Services;

/// <summary>
/// Orchestration service: coordinates the ensemble, LLM, memory, and tools.
/// </summary>
public sealed class OrchestrationService
{
    public NeuralEnsemble Ensemble { get; }
    public GGUFRunner LlmRunner { get; }
    public ShortTermMemory ShortTermMemory { get; }
    public LongTermMemory LongTermMemory { get; }
    public EpisodicMemory EpisodicMemory { get; }
    public ToolRegistry ToolRegistry { get; }
    public PromptManager PromptManager { get; }
    public EnsembleTrainer Trainer { get; }
    public ModelValidator Validator { get; }
    public DerivApiClient DerivClient { get; }

    private readonly MemoryConsolidator _consolidator;

    public OrchestrationService()
    {
        Ensemble = EnsembleBuilder.Build(1024, 20, 2);
        LlmRunner = new GGUFRunner();
        ShortTermMemory = new ShortTermMemory(1000);
        LongTermMemory = new LongTermMemory(10000);
        EpisodicMemory = new EpisodicMemory("episodic_memory.db");
        ToolRegistry = new ToolRegistry();
        PromptManager = new PromptManager(4096);
        DerivClient = new DerivApiClient();
        Trainer = new EnsembleTrainer(Ensemble, DerivClient);
        Validator = new ModelValidator();

        RegisterTools();
        _consolidator = new MemoryConsolidator(ShortTermMemory, LongTermMemory, null, TimeSpan.FromMinutes(10));
    }

    private void RegisterTools()
    {
        ToolRegistry.Register(new WebSearchTool());
        ToolRegistry.Register(new CalculatorTool());
        ToolRegistry.Register(new DataFetcherTool());
        ToolRegistry.Register(new CodeRunnerTool());
        ToolRegistry.Register(new DriftSwitchPredictorTool(Ensemble));
        ToolRegistry.Register(new DateTimeTool());
        ToolRegistry.Register(new UnitConverterTool());
        ToolRegistry.Register(new NewsSummaryTool());
    }

    /// <summary>
    /// Predicts drift for a given index.
    /// </summary>
    public PredictResponse Predict(int driftIndex, float[]? features = null)
    {
        if (driftIndex != 10 && driftIndex != 20 && driftIndex != 30)
            throw new ArgumentException("Invalid drift index. Must be 10, 20, or 30.");

        features ??= GenerateDefaultFeatures();
        var prediction = Ensemble.Predict(features, driftIndex);

        var entry = new MemoryEntry
        {
            Content = "Prediction for index " + driftIndex + ": " + prediction.Direction,
            RelevanceScore = prediction.Confidence,
            Source = "prediction"
        };
        ShortTermMemory.Add(entry);

        return new PredictResponse(
            prediction.DriftIndex,
            prediction.Direction > 0 ? "UP" : "DOWN",
            prediction.Direction,
            prediction.Magnitude,
            prediction.Confidence,
            prediction.NetworkCount,
            prediction.Timestamp
        );
    }

    /// <summary>
    /// Processes a chat message using LLM + tools + memory.
    /// </summary>
    public async Task<ChatResponse> Chat(string message, string? sessionId = null)
    {
        sessionId ??= Guid.NewGuid().ToString();
        var toolCalls = new List<ToolCallInfo>();

        // Retrieve relevant memories
        var memories = LongTermMemory.QueryByText(message, 3);
        var memoryContext = memories.Count > 0
            ? "Previous context: " + string.Join("; ", memories.Select(m => m.Content))
            : "";

        // Build prompt with tools description
        var systemPrompt = "You are an intelligent AI assistant with access to tools. " +
            ToolRegistry.GetToolsDescription() + " " + memoryContext +
            " To call a tool, output JSON: {\"tool\": \"ToolName\", \"params\": {...}}";
        PromptManager.SetSystemPrompt(systemPrompt);
        PromptManager.AddMessage("user", message);

        string reply;
        if (LlmRunner.IsLoaded)
        {
            var prompt = PromptManager.BuildPrompt();
            var llmOutput = LlmRunner.Generate(prompt, 512, 0.7f);

            // Parse and execute tool calls
            var calls = ToolCallParser.Parse(llmOutput);
            foreach (var call in calls)
            {
                var result = await ToolRegistry.ExecuteAsync(call.Tool, call.Parameters);
                toolCalls.Add(new ToolCallInfo(call.Tool, call.Parameters, result.Output));
                llmOutput += "\n[Tool " + call.Tool + " result: " + result.Output + "]";
            }

            reply = llmOutput;
        }
        else
        {
            reply = "LLM model not loaded. Please provide a GGUF model file. " +
                "I can still use tools directly. Available tools: " +
                string.Join(", ", ToolRegistry.GetToolNames());
        }

        PromptManager.AddMessage("assistant", reply);

        // Log to episodic memory
        EpisodicMemory.Log(new Episode
        {
            Input = message,
            Output = reply,
            ToolCalls = string.Join("; ", toolCalls.Select(t => t.Tool)),
            SessionId = sessionId
        });

        // Add to short-term memory
        ShortTermMemory.Add(new MemoryEntry
        {
            Content = message + " -> " + reply,
            RelevanceScore = 0.5f,
            Source = "chat"
        });

        return new ChatResponse(reply, toolCalls, sessionId);
    }

    /// <summary>
    /// Triggers training on drift indices.
    /// </summary>
    public async Task<TrainResponse> Train(int[]? driftIndices, int epochs, float learningRate)
    {
        driftIndices ??= new[] { 10, 20, 30 };
        var result = await Trainer.TrainAsync(driftIndices, epochs, learningRate);

        var indexResults = result.IndexResults.Select(r => new IndexResult(
            r.DriftIndex, r.ValidationAccuracy, r.TestAccuracy, r.SampleCount
        )).ToList();

        return new TrainResponse(result.Success, "Training completed", result.TrainingTimeMs, indexResults);
    }

    /// <summary>
    /// Queries long-term memory.
    /// </summary>
    public MemoryQueryResponse QueryMemory(string query)
    {
        var memories = LongTermMemory.QueryByText(query, 10);
        var items = memories.Select(m => new MemoryItem(m.Content, m.RelevanceScore, m.Timestamp, m.Source)).ToList();
        return new MemoryQueryResponse(items);
    }

    /// <summary>
    /// Returns system status.
    /// </summary>
    public StatusResponse GetStatus()
    {
        return new StatusResponse(
            "healthy",
            LlmRunner.IsLoaded,
            Ensemble.Count,
            ShortTermMemory.Count,
            LlmRunner.ModelPath,
            DateTime.UtcNow
        );
    }

    private static float[] GenerateDefaultFeatures()
    {
        var rng = new Random();
        var features = new float[20];
        for (int i = 0; i < features.Length; i++)
            features[i] = (float)(rng.NextDouble() * 2 - 1);
        return features;
    }
}
