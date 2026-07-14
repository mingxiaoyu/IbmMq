using Microsoft.Data.Sqlite;

namespace MqManager.Desktop.Data;

public class PreferencesRepository
{
    private readonly AppDbContext _ctx;

    public PreferencesRepository(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public string? Get(string key)
    {
        using var conn = _ctx.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM AppSettings WHERE Key = $k";
        cmd.Parameters.AddWithValue("$k", key);
        return cmd.ExecuteScalar() as string;
    }

    public void Set(string key, string value)
    {
        using var conn = _ctx.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AppSettings(Key, Value) VALUES($k, $v)
            ON CONFLICT(Key) DO UPDATE SET Value = $v
            """;
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.ExecuteNonQuery();
    }
}
