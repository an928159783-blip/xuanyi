using System.Text;
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

    public void Add(string source, string target, string provider) =>
        InsertRow(source, target, provider, DateTime.UtcNow, transaction: null);

    /// <summary>写入历史；若与最近一条原文+译文完全相同则跳过。</summary>
    public bool TryAdd(string source, string target, string provider)
    {
        source = source.Trim();
        target = target.Trim();
        if (IsDuplicateOfLatest(source, target))
            return false;

        InsertRow(source, target, provider, DateTime.UtcNow, transaction: null);
        return true;
    }

    private void InsertRow(
        string source,
        string target,
        string provider,
        DateTime createdAt,
        SqliteTransaction? transaction)
    {
        using var cmd = _connection.CreateCommand();
        if (transaction is not null)
            cmd.Transaction = transaction;
        cmd.CommandText = """
            INSERT INTO history (source, target, provider, created_at)
            VALUES ($s, $t, $p, $c)
            """;
        cmd.Parameters.AddWithValue("$s", source);
        cmd.Parameters.AddWithValue("$t", target);
        cmd.Parameters.AddWithValue("$p", provider);
        cmd.Parameters.AddWithValue("$c", createdAt.ToUniversalTime().ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private bool IsDuplicateOfLatest(string source, string target)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT source, target FROM history ORDER BY id DESC LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return false;

        return string.Equals(reader.GetString(0), source, StringComparison.Ordinal)
               && string.Equals(reader.GetString(1), target, StringComparison.Ordinal);
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

    /// <summary>导出为人类可读的 UTF-8 文本（仅原文与译文，便于查看与再导入）。</summary>
    public void ExportToTextFile(string filePath, int limit = 10_000)
    {
        var entries = GetRecent(limit);
        File.WriteAllText(filePath, HistoryTextFormat.FormatExport(entries),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    /// <summary>从 TXT 或 JSON 导入历史。</summary>
    public int ImportFromFile(string filePath, bool merge = true)
    {
        var ext = Path.GetExtension(filePath);
        if (ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
            return ImportFromJsonFile(filePath, merge);

        if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return ImportFromTextFile(filePath, merge);

        var head = File.ReadAllText(filePath).TrimStart();
        if (head.StartsWith('[') || head.StartsWith('{'))
            return ImportFromJsonFile(filePath, merge);

        return ImportFromTextFile(filePath, merge);
    }

    public int ImportFromTextFile(string filePath, bool merge = true)
    {
        var text = File.ReadAllText(filePath);
        var pairs = HistoryTextFormat.ParseImport(text);
        if (pairs.Count == 0)
            throw new InvalidOperationException("文件中没有可导入的历史记录。");

        using var tx = _connection.BeginTransaction();
        try
        {
            if (!merge)
            {
                using var clear = _connection.CreateCommand();
                clear.Transaction = tx;
                clear.CommandText = "DELETE FROM history";
                clear.ExecuteNonQuery();
            }

            foreach (var (source, target) in pairs)
                InsertRow(source, target, "", DateTime.UtcNow, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        return pairs.Count;
    }

    public static ImportParseSummary AnalyzeImportFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            var json = File.ReadAllText(filePath);
            var entries = JsonSerializer.Deserialize<List<HistoryEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var pairs = (entries ?? [])
                .Where(e => !string.IsNullOrWhiteSpace(e.Source) || !string.IsNullOrWhiteSpace(e.Target))
                .Select(e =>
                {
                    var source = e.Source?.Trim() ?? "";
                    var target = e.Target?.Trim() ?? "";
                    if (string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(source))
                        target = source;
                    return (source, target);
                })
                .ToList();
            return new ImportParseSummary { EntryCount = pairs.Count, Pairs = pairs };
        }

        return HistoryTextFormat.SummarizeImport(File.ReadAllText(filePath));
    }

    /// <summary>从 JSON 导入历史。merge=true 追加，false 先清空再导入。</summary>
    public int ImportFromJsonFile(string filePath, bool merge = true)
    {
        var json = File.ReadAllText(filePath);
        var entries = JsonSerializer.Deserialize<List<HistoryEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (entries is null || entries.Count == 0)
            throw new InvalidOperationException("文件中没有可导入的历史记录。");

        var ordered = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Source) || !string.IsNullOrWhiteSpace(e.Target))
            .OrderBy(e => e.CreatedAt == default ? DateTime.UtcNow : e.CreatedAt)
            .Select(e =>
            {
                var source = e.Source?.Trim() ?? "";
                var target = e.Target?.Trim() ?? "";
                if (string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(source))
                    target = source;
                return (source, target, e.Provider?.Trim() ?? "", e.CreatedAt);
            })
            .ToList();

        using var tx = _connection.BeginTransaction();
        try
        {
            if (!merge)
            {
                using var clear = _connection.CreateCommand();
                clear.Transaction = tx;
                clear.CommandText = "DELETE FROM history";
                clear.ExecuteNonQuery();
            }

            foreach (var (source, target, provider, createdAt) in ordered)
            {
                var at = createdAt == default ? DateTime.UtcNow : createdAt.ToUniversalTime();
                InsertRow(source, target, provider, at, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        return ordered.Count;
    }

    public static string DatabasePath => Path.Combine(ConfigService.ConfigDirectory, "history.db");

    public void Dispose() => _connection.Dispose();
}
