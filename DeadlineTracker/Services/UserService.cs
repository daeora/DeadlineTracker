using DeadlineTracker.Models;
using SQLite;

namespace DeadlineTracker.Services;

public class UserService
{
    public const string PrefCurrentUserId = "current_user_id";

    static string Normalize(string name) => name.Trim().ToUpperInvariant();

    public async Task<User?> GetByNameAsync(string username)
    {
        var norm = Normalize(username);
        var db = await Database.GetAsync();
        return await db.Table<User>().Where(u => u.UsernameNormalized == norm).FirstOrDefaultAsync();
    }

    public static void SetCurrentUser(int id)
    {
        Preferences.Set(PrefCurrentUserId, id);
    }

    public async Task<(bool ok, string? error)> CreateAsync(string username)
    {
        if (!IsValid(username)) return (false, "Sallitut merkit: A–Z, a–z, 0–9, . _ - (pituus 2–40).");
        var norm = Normalize(username);
        var db = await Database.GetAsync();

        var exists = await db.Table<User>().Where(u => u.UsernameNormalized == norm).FirstOrDefaultAsync();
        if (exists is not null) return (false, "Käyttäjänimi on jo käytössä.");

        var user = new User { Username = username.Trim(), UsernameNormalized = norm };
        await db.InsertAsync(user);
        SetCurrentUser(user.Id);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> SignInAsync(string username)
    {
        var user = await GetByNameAsync(username);
        if (user is null) return (false, "Käyttäjää ei löydy. Jos olet uusi, valitse “Luo käyttäjä”.");
        user.LastLoginAt = DateTime.UtcNow;
        var db = await Database.GetAsync();
        await db.UpdateAsync(user);
        SetCurrentUser(user.Id);
        return (true, null);
    }

    public static int? GetCurrentUserId()
    {
        if (!Preferences.ContainsKey(PrefCurrentUserId)) return null;
        var id = Preferences.Get(PrefCurrentUserId, -1);
        return id <= 0 ? null : id;
    }

    public static void Logout() => Preferences.Remove(PrefCurrentUserId);

    public static bool IsValid(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        if (username.Length < 2 || username.Length > 40) return false;
        foreach (var ch in username)
        {
            if (!(char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-')) return false;
        }
        return true;
    }
}