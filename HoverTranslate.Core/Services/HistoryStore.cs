using System.Text.Json;
using HoverTranslate.Core.Models;
using Microsoft.Data.Sqlite;

namespace HoverTranslate.Core.Services;

public sealed class HistoryStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public HistoryStore()
    {
        var dbPath = Path.Combine(ConfigService.ConfigDirectory, "history.db");
        Directory.CreateDirectory(ConfigService.ConfigDirectory);
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        try
        {
            if (!TableExists("history"))
            {
                CreateTable();
                return;
            }

            var columns = GetColumnNames("history");
            if (!columns.Contains("provider"))
            {
                Execute("ALTER TABLE history ADD COLUMN provider TEXT NOT NULL DEFAULT ''");
                if (columns.Contains("model"))
                    Execute("UPDATE history SET provider = COALESCE(model, '')");
            }
        }
        catch
        {
            Execute("DROP TABLE IF EXISTS history");
            CreateTable();
        }
    }

    private void CreateTable()
    {
        Execute("""
            CREATE TABLE IF NOT EXISTS history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source TEXT NOT NULL,
                target TEXT NOT NULL,
                provider TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL
            );
            """);
    }

    private bool TableExists(string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$n";
        cmd.Parameters.AddWithValue("$n", name);
        return cmd.ExecuteScalar() != null;
    }

    private HashSet<string> GetColumnNames(string table)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            set.Add(reader.GetString(1));
        return set;
    }

    private void Execute(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Add(string source, string target, string provider)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO history (source, target, provider, created_at)
            VALUES ($s, $t, $p, $c)
            """;
        cmd.Parameters.AddWithValue("$s", source);
        cmd.Parameters.AddWithValue("$t", target);
        cmd.Parameters.AddWithValue("$p", provider);
        cmd.Parameters.AddWithValue("$c", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<HistoryEntry> GetRecent(int limit = 80)
    {
        var columns = GetColumnNames("history");
        var providerExpr = columns.Contains("provider")
            ? "provider"
            : columns.Contains("model") ? "model" : "''";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT id, source, target, {providerExpr}, created_at
            FROM history ORDER BY id DESC LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$limit", limit);
        var list = new List<HistoryEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new HistoryEntry
            {
                Id = reader.GetInt64(0),
                Source = reader.GetString(1),
                Target = reader.GetString(2),
                Provider = reader.IsDBNull(3) ? "" : reader.GetString(3),
                CreatedAt = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.MinValue
            });
        }
        return list;
    }

    public void Clear() => Execute("DELETE FROM history");

    public void ExportToJsonFile(string filePath, int limit = 10_000)
    {
        var entries = GetRecent(limit);
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public static string DatabasePath => Path.Combine(ConfigService.ConfigDirectory, "history.db");

    public void Dispose() => _connection.Dispose();
}
