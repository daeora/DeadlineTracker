using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DeadlineTracker.Services
{
    public class AuthService
    {
        // PÄIVITÄ TÄMÄ riville joka vastaa sun MySQL-yhteyttä 👇
        // Esim:
        // "Server=localhost;Port=3306;Database=mydb;User ID=root;Password=M1nunS3rv3r1sepp0?;SslMode=None;";
        //

        //  - Database = se nimi missä taulut on (user, tehtava, projekti, ...)
        //  - User ID ja Password = paikallisen MySQL-tunnarin tiedot
        //
        private static readonly string _connStr =
           "Server=localhost;Port=3306;Database=mydb;User ID=root;Password=M1nunS3rv3r1sepp0?";

        // luku-ominaisuus muille
        public static string ConnectionString => _connStr;

        // Tätä kutsutaan kirjautuessa.
        // Palauttaa user_id:n olemassaolevalle käyttäjälle TAI lisää uuden rivin ja palauttaa uuden user_id:n.
        public async Task<int> LoginOrCreateUserAsync(string typedName)
        {
            if (string.IsNullOrWhiteSpace(typedName))
                throw new Exception("Käyttäjänimi ei voi olla tyhjä.");

            using (var conn = new MySqlConnection(_connStr))
            {
                await conn.OpenAsync();

                // 1. Onko käyttäjä jo olemassa?
                string selectSql = @"
                    SELECT user_id
                    FROM user
                    WHERE kayttajaNimi = @name
                    LIMIT 1;
                ";

                using (var selectCmd = new MySqlCommand(selectSql, conn))
                {
                    selectCmd.Parameters.AddWithValue("@name", typedName);

                    var result = await selectCmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        // Käyttäjä löytyi -> palauta olemassaoleva id
                        return Convert.ToInt32(result);
                    }
                }

                // 2. Jos ei löydy, luodaan uusi käyttäjä
                string insertSql = @"
                    INSERT INTO user (kayttajaNimi, luotupvm)
                    VALUES (@name, NOW());
                ";

                using (var insertCmd = new MySqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", typedName);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                // 3. Haetaan juuri luodun käyttäjän id
                string newIdSql = @"
                    SELECT user_id
                    FROM user
                    WHERE kayttajaNimi = @name
                    ORDER BY user_id DESC
                    LIMIT 1;
                ";

                using (var newIdCmd = new MySqlCommand(newIdSql, conn))
                {
                    newIdCmd.Parameters.AddWithValue("@name", typedName);
                    var newId = await newIdCmd.ExecuteScalarAsync();

                    if (newId == null)
                        throw new Exception("Uuden käyttäjän luonti epäonnistui.");

                    return Convert.ToInt32(newId);
                }
            }
        }
    }
}

