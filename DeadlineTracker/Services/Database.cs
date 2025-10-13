using SQLite;
using DeadlineTracker.Models;

namespace DeadlineTracker.Services;

public static class Database
{
    static SQLiteAsyncConnection? _conn;
    public static async Task<SQLiteAsyncConnection> GetAsync()
    {
        if (_conn is not null) return _conn;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "app.db");
        _conn = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _conn.CreateTableAsync<User>();
        return _conn;
    }
}