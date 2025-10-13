using System.Data.SqlTypes;

namespace DeadlineTracker.Models;

public class User
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public int Id { get; set; }

    [SQLite.Indexed(Unique = true)]
    public string UsernameNormalized { get; set; } = default!; // esim. "MILLA" → uniikki

    public string Username { get; set; } = default!;           // näytettävä nimi

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}