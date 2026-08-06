namespace Brain.API.Models;

public record TrainRequest(int[]? DriftIndices = null, int Epochs = 10, float LearningRate = 0.001f);
public record TrainResponse(bool Success, string Message, long TrainingTimeMs, List<IndexResult> Results);
public record IndexResult(int DriftIndex, float ValidationAccuracy, float TestAccuracy, int SampleCount);

public record PredictResponse(int DriftIndex, string Direction, float DirectionValue, float Magnitude, float Confidence, int NetworkCount, DateTime Timestamp);

public record ChatRequest(string Message, string? SessionId = null);
public record ChatResponse(string Reply, List<ToolCallInfo> ToolCalls, string? SessionId);
public record ToolCallInfo(string Tool, Dictionary<string, object> Parameters, string Result);

public record MemoryQueryResponse(List<MemoryItem> Memories);
public record MemoryItem(string Content, float RelevanceScore, DateTime Timestamp, string Source);

public record StatusResponse(string Status, bool ModelLoaded, int NetworkCount, int MemoryCount, string ModelPath, DateTime Timestamp);
