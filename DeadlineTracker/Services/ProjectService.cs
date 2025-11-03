using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DeadlineTracker.Services
{
    public class ProjectService
    {
        private readonly string _connStr;
        public ProjectService(string connStr)
        {
            _connStr = connStr; // Tallennetaan kirjautumista varten, voidaan kutsua muilta sivuilta (Hakee Auth Servicestä ConnectionStringin)
        }

        // name, desc, dates, tasks, participants (names). Participants -> ensure user_id.
        public async Task<long> CreateProjectAsync(
            string name,
            string description,
            DateTime startDate,
            DateTime endDate,
            IReadOnlyList<(string title, bool done)> tasks,
            IReadOnlyList<string> participantNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Projektin nimi puuttuu.");
            if (startDate == default || endDate == default)
                throw new Exception("Alku- ja loppupäivä ovat pakollisia.");

            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                long projectId;

                // 1) projekti
                const string insertProject =
                    @"INSERT INTO projekti (projektiNimi, kuvausTeksti, alkupvm, loppupvm)
                      VALUES (@n, @d, @a, @l);
                      SELECT LAST_INSERT_ID();";
                await using (var cmd = new MySqlCommand(insertProject, conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@n", name);
                    cmd.Parameters.AddWithValue("@d", description ?? "");
                    cmd.Parameters.AddWithValue("@a", startDate.Date);
                    cmd.Parameters.AddWithValue("@l", endDate.Date);
                    projectId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                }

                // 2) tehtävät (minimi + oletukset)
                const string insertTask =
                    @"INSERT INTO tehtava (projekti_id, tehtavaNimi, tehtavaKuvaus, onValmis, erapaiva, prioriteetti, maarattyHenkilolle)
                      VALUES (@pid, @title, NULL, @done, @epaiva, @prio, NULL);";
                foreach (var t in tasks)
                {
                    await using var ct = new MySqlCommand(insertTask, conn, (MySqlTransaction)tx);
                    ct.Parameters.AddWithValue("@pid", projectId);
                    ct.Parameters.AddWithValue("@title", t.title);
                    ct.Parameters.AddWithValue("@done", t.done ? 1 : 0);
                    ct.Parameters.AddWithValue("@prio", "keskitaso"); // ENUM: matala | keskitaso | tärkeä
                    ct.Parameters.AddWithValue("@epaiva", DBNull.Value); // ei eräpäivää oletuksena
                    await ct.ExecuteNonQueryAsync();
                }

                // 3) osallistujat -> varmistetaan user_id ja linkitetään
                const string insertMember =
                    @"INSERT INTO projekti_osallistuja (projekti_id, user_id)
                      VALUES (@pid, @uid);";
                foreach (var nameOnly in participantNames)
                {
                    if (string.IsNullOrWhiteSpace(nameOnly)) continue;
                    var userId = await EnsureUserAsync(conn, (MySqlTransaction)tx, nameOnly.Trim());
                    await using var cp = new MySqlCommand(insertMember, conn, (MySqlTransaction)tx);
                    cp.Parameters.AddWithValue("@pid", projectId);
                    cp.Parameters.AddWithValue("@uid", userId);
                    await cp.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return projectId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Luo user-rivin jos puuttuu, palauttaa user_id
        private static async Task<int> EnsureUserAsync(MySqlConnection conn, MySqlTransaction tx, string username)
        {
            // yritä hakea
            await using (var get = new MySqlCommand("SELECT user_id FROM user WHERE kayttajaNimi=@u;", conn, tx))
            {
                get.Parameters.AddWithValue("@u", username);
                var id = await get.ExecuteScalarAsync();
                if (id != null && id != DBNull.Value)
                    return Convert.ToInt32(id);
            }
            // lisää
            await using (var ins = new MySqlCommand(
                "INSERT INTO user(kayttajaNimi) VALUES(@u); SELECT LAST_INSERT_ID();", conn, tx))
            {
                ins.Parameters.AddWithValue("@u", username);
                return Convert.ToInt32(await ins.ExecuteScalarAsync());
            }
        }
    }
}