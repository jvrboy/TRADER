using LLama;
using LLama.Common;
using LLama.Abstractions;

namespace Brain.LLM;

/// <summary>
/// GGUF model runner: loads .gguf files and runs inference.
/// Wraps LLamaSharp for CPU/GPU execution of Llama, Mistral, Phi-3, etc.
/// </summary>
public sealed class GGUFRunner : IDisposable
{
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private bool _loaded;
    private readonly object _lock = new();

    public bool IsLoaded => _loaded;
    public string ModelPath { get; private set; } = string.Empty;

    /// <summary>
    /// Loads a GGUF model from the specified file path.
    /// </summary>
    public void LoadModel(string path)
    {
        lock (_lock)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("GGUF model file not found: " + path);

            var parameters = new ModelParams(path)
            {
                ContextSize = 4096,
                GpuLayerCount = 0
            };

            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _loaded = true;
            ModelPath = path;
        }
    }

    /// <summary>
    /// Generates text from a prompt.
    /// </summary>
    public string Generate(string prompt, int maxTokens = 256, float temperature = 0.7f)
    {
        if (!_loaded || _context == null)
            throw new InvalidOperationException("Model not loaded. Call LoadModel first.");

        lock (_lock)
        {
            var executor = new InteractiveExecutor(_context);
            var ex = new InferenceParams
            {
                MaxTokens = maxTokens,
                Temperature = temperature,
                AntiPrompts = new List<string> { "User:", "System:" }
            };

            var result = new System.Text.StringBuilder();
            foreach (var response in executor.Infer(prompt, ex))
            {
                result.Append(response);
            }
            return result.ToString();
        }
    }

    /// <summary>
    /// Generates an embedding vector for the given text.
    /// Uses the model's embedding capability for memory storage.
    /// </summary>
    public float[] GetEmbedding(string text)
    {
        if (!_loaded || _context == null)
            throw new InvalidOperationException("Model not loaded. Call LoadModel first.");

        lock (_lock)
        {
            var embeddings = _context.GetEmbeddings(text);
            return embeddings.ToArray();
        }
    }

    /// <summary>
    /// Generates a response using a chat-style prompt with system and user messages.
    /// </summary>
    public string Chat(string systemPrompt, string userMessage, int maxTokens = 512, float temperature = 0.7f)
    {
        var prompt = BuildChatPrompt(systemPrompt, userMessage);
        return Generate(prompt, maxTokens, temperature);
    }

    private static string BuildChatPrompt(string systemPrompt, string userMessage)
    {
        return "System: " + systemPrompt + "\nUser: " + userMessage + "\nAssistant: ";
    }

    public void Dispose()
    {
        _context?.Dispose();
        _model?.Dispose();
        _loaded = false;
    }
}
