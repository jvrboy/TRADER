using Microsoft.Data.Sqlite;

namespace Brain.Memory;

/// <summary>
/// Episodic Memory: SQLite-backed audit log of all interactions.
/// Used for auditing and reinforcement learning.
/// </summary>
public sealed class EpisodicMemory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _lock = new();

    public EpisodicMemory(string dbPath = "episodic_memory.db")
    {
        _connection = new SqliteConnection("Data Source=" + dbPath);
        _connection.Open();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Episodes (
                EpisodeId TEXT PRIMARY KEY,
                Timestamp TEXT NOT NULL,
                Input TEXT NOT NULL,
                Output TEXT,
                ToolCalls TEXT,
                Outcome TEXT,
                SessionId TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_Episodes_Timestamp ON Episodes(Timestamp);
            CREATE INDEX IF NOT EXISTS IX_Episodes_SessionId ON Episodes(SessionId);
            """;
        cmd.ExecuteNonQuery();
    }

    public void Log(Episode episode)
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Episodes (EpisodeId, Timestamp, Input, Output, ToolCalls, Outcome, SessionId)
                VALUES (@id, @ts, @input, @output, @tools, @outcome, @session)
                """;
            cmd.Parameters.AddWithValue("@id", episode.EpisodeId.ToString());
            cmd.Parameters.AddWithValue("@ts", episode.Timestamp.ToString("O"));
            cmd.Parameters.AddWithValue("@input", episode.Input);
            cmd.Parameters.AddWithValue("@output", episode.Output ?? "");
            cmd.Parameters.AddWithValue("@tools", episode.ToolCalls ?? "");
            cmd.Parameters.AddWithValue("@outcome", episode.Outcome ?? "");
            cmd.Parameters.AddWithValue("@session", episode.SessionId ?? "");
            cmd.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<Episode> GetRecent(int count = 50)
    {
        var results = new List<Episode>();
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Episodes ORDER BY Timestamp DESC LIMIT @count";
            cmd.Parameters.AddWithValue("@count", count);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new Episode
                {
                    EpisodeId = Guid.Parse(reader.GetString(0)),
                    Timestamp = DateTime.Parse(reader.GetString(1)),
                    Input = reader.GetString(2),
                    Output = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ToolCalls = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Outcome = reader.IsDBNull(5) ? null : reader.GetString(5),
                    SessionId = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
        }
        return results;
    }

    public IReadOnlyList<Episode> GetBySession(string sessionId)
    {
        var results = new List<Episode>();
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Episodes WHERE SessionId = @session ORDER BY Timestamp ASC";
            cmd.Parameters.AddWithValue("@session", sessionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new Episode
                {
                    EpisodeId = Guid.Parse(reader.GetString(0)),
                    Timestamp = DateTime.Parse(reader.GetString(1)),
                    Input = reader.GetString(2),
                    Output = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ToolCalls = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Outcome = reader.IsDBNull(5) ? null : reader.GetString(5),
                    SessionId = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
        }
        return results;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

public sealed class Episode
{
    public Guid EpisodeId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Input { get; init; } = string.Empty;
    public string? Output { get; set; }
    public string? ToolCalls { get; set; }
    public string? Outcome { get; set; }
    public string? SessionId { get; set; }
}
