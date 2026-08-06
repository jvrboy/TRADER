using Brain.Core.Ensemble;

namespace Brain.Tools;

/// <summary>
/// Drift switch predictor tool: calls the neural ensemble directly.
/// Exposed as a tool so the LLM can invoke predictions during chat.
/// </summary>
public sealed class DriftSwitchPredictorTool : ITool
{
    private readonly NeuralEnsemble _ensemble;

    public DriftSwitchPredictorTool(NeuralEnsemble ensemble)
    {
        _ensemble = ensemble;
    }

    public string Name => "DriftSwitchPredictor";
    public string Description => "Predicts drift direction and magnitude for drift switch indices (10, 20, 30) using the neural ensemble.";
    public string ParameterSchema => "{\"index\": \"int\", \"features\": \"float[]\"}";

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("index", out var indexObj))
            return Task.FromResult(ToolResult.Fail("Missing required parameter: index"));

        var index = Convert.ToInt32(indexObj);
        if (index != 10 && index != 20 && index != 30)
            return Task.FromResult(ToolResult.Fail("Invalid drift index. Must be 10, 20, or 30."));

        float[] features;
        if (parameters.TryGetValue("features", out var featObj) && featObj is JsonElement featElement)
        {
            features = featElement.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
        }
        else if (parameters.TryGetValue("features", out var featObj2) && featObj2 is float[] featArray)
        {
            features = featArray;
        }
        else
        {
            features = GenerateDefaultFeatures();
        }

        var prediction = _ensemble.Predict(features, index);
        var result = new
        {
            driftIndex = prediction.DriftIndex,
            direction = prediction.Direction > 0 ? "UP" : "DOWN",
            directionValue = prediction.Direction,
            magnitude = prediction.Magnitude,
            confidence = prediction.Confidence,
            networkCount = prediction.NetworkCount,
            timestamp = prediction.Timestamp
        };

        return Task.FromResult(ToolResult.Ok(result));
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
