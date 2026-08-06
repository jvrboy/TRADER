using System.Net.Http.Json;

namespace AetherBrain.Memory;

public sealed class NetlifyMemoryClient(HttpClient httpClient)
{
    public async Task SyncAsync(MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/memory", new
        {
            externalId = record.Id,
            record.Content,
            layer = record.Layer.ToString(),
            record.Importance,
            record.CreatedAt
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
