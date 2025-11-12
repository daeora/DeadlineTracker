using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DeadlineTracker.Services
{
    /// <summary>
    /// Tietokantapalvelu projektien luontiin ja niihin liittyvien rivien tallentamiseen.
    /// - Vastaa ainoastaan tietokantatoiminnoista (CRUD) eikä sisällä UI-logiikkaa.
    /// - Kaikki INSERTit ajetaan yhden transaktion sisällä -> atomisuus.
    /// </summary>
    public class ProjectService
    {
        private readonly string _connStr;

        /// <summary>
        /// Luo palvelun annetulla MySQL-yhteysmerkkijonolla.
        /// ConnectionString annetaan ulkoa (AuthServicestä), jotta
        /// luokka on irtikytketty ja testattavissa.
        /// </summary>
        public ProjectService(string connStr)
        {
            // Talteen myöhempää käyttöä varten.
            _connStr = connStr;
        }



        /// <summary>
        /// Luo uuden projektin ja siihen liittyvät rivit (tehtävät + projektin osallistujat).
        /// </summary>
        /// <param name="name">Projektin nimi (pakollinen).</param>
        /// <param name="description">Vapaamuotoinen kuvaus (sallittu tyhjä).</param>
        /// <param name="startDate">Alkamispäivä (pakollinen).</param>
        /// <param name="endDate">Loppupäivä (pakollinen).</param>
        /// <param name="tasks">
        /// Lista tehtävistä. Jokaisella:
        ///  - title: tehtävän nimi
        ///  - done: onko valmis (true/false)
        ///  - due: eräpäivä (nullable)
        ///  - assigneeId: vastuuhenkilön user_id (nullable)
        /// </param>
        /// <param name="participantUserIds">
        /// Projektin osallistujien user_id:t (ei nimiä). Duplicaatit suodatetaan pois.
        /// </param>
        /// <returns>Luodun projektin tunnus (projekti_id).</returns>
        /// <exception cref="Exception">
        /// Heittää poikkeuksen mm. jos pakolliset tiedot puuttuvat tai DB-virhe tapahtuu.
        /// </exception>
        /// 



        public async Task<long> CreateProjectAsync(
            string name,
            string description,
            DateTime startDate,
            DateTime endDate,
            IReadOnlyList<(string title, bool done, DateTime? due, int? assigneeId)> tasks,
            IReadOnlyList<int> participantUserIds)
        {
            // --- Perusvalidaatiot (helpot virheilmoitukset UI:lle) ---
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Projektin nimi puuttuu.");
            if (startDate == default || endDate == default)
                throw new Exception("Alku- ja loppupäivä ovat pakollisia.");

            // Avataan yhteys ja aloitetaan transaktio.
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                long projectId;

                // 1) INSERT projekti
                //    Palautetaan uuden rivin id MySQL:n LAST_INSERT_ID:llä.
                const string insertProject = @"
                                            INSERT INTO projekti (projektiNimi, kuvausTeksti, alkupvm, loppupvm)
                                            VALUES (@n, @d, @a, @l);
                                            SELECT LAST_INSERT_ID();";

                await using (var cmd = new MySqlCommand(insertProject, conn, (MySqlTransaction)tx))
                {
                    // Parametrien käyttö estää SQL-injektion ja huolehtii arvojen tyyppimuunnoksista.
                    cmd.Parameters.AddWithValue("@n", name);
                    cmd.Parameters.AddWithValue("@d", description ?? "");
                    cmd.Parameters.AddWithValue("@a", startDate.Date);
                    cmd.Parameters.AddWithValue("@l", endDate.Date);

                    // ExecuteScalarAsync palauttaa ensimmäisen solun arvon (tässä LAST_INSERT_ID()).
                    projectId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                }

                // 2) INSERT tehtävät
                //    Minimikentät + due + assignee. NULL arvot sallitaan (DB:ssä kentät nullable).
                const string insertTaskSql = @"
                                            INSERT INTO tehtava
                                                (projekti_id, tehtavaNimi, tehtavaKuvaus, onValmis, erapaiva, prioriteetti, maarattyHenkilolle)
                                            VALUES
                                                (@pid, @title, NULL, @done, @due, 'keskitaso', @assignee);";

                foreach (var t in tasks)
                {
                    await using var ct = new MySqlCommand(insertTaskSql, conn, (MySqlTransaction)tx);
                    ct.Parameters.AddWithValue("@pid", projectId);
                    ct.Parameters.AddWithValue("@title", t.title);
                    ct.Parameters.AddWithValue("@done", t.done ? 1 : 0);
                    // DBNull.Value = SQL NULL; ilman tätä MySQL yritettäisiin antaa tyhjä DateTime/Int
                    ct.Parameters.AddWithValue("@due", (object?)t.due ?? DBNull.Value);
                    ct.Parameters.AddWithValue("@assignee", (object?)t.assigneeId ?? DBNull.Value);

                    await ct.ExecuteNonQueryAsync();
                }

                // 3) INSERT projektin osallistujat
                //    Tähän tulee vain VALMIIKSI luotujen käyttäjien user_id:t.
                //    Distinct varmuuden vuoksi – UI ei saa aiheuttaa duplikaatteja.
                const string insertMember = @"
                                            INSERT INTO projekti_osallistuja (projekti_id, user_id)
                                            VALUES (@pid, @uid);";

                foreach (var uid in participantUserIds.Distinct())
                {
                    await using var cp = new MySqlCommand(insertMember, conn, (MySqlTransaction)tx);
                    cp.Parameters.AddWithValue("@pid", projectId);
                    cp.Parameters.AddWithValue("@uid", uid);
                    await cp.ExecuteNonQueryAsync();
                }

                // Kaikki onnistui -> commit
                await tx.CommitAsync();
                return projectId;
            }
            catch
            {
                // Virhe missä tahansa vaiheessa -> perutaan kaikki
                await tx.RollbackAsync();
                throw; // Heitetään edelleen, jotta UI voi näyttää virheilmoituksen
            }
        }

        /// <summary>
        /// (Legacy) Luo user-rivin jos sellaista ei ole vielä — palauttaa user_id:n.
        /// <para>
        /// HUOM! Meidän sovelluksessa käyttäjät luodaan kirjautumisen yhteydessä.
        /// Tämän metodin käyttö EI ole enää toivottua projektisivulta, koska
        /// emme halua luoda uusia käyttäjiä projektia tallennettaessa.
        /// </para>
        /// </summary>
        [Obsolete("Käyttäjät luodaan kirjautumisessa. Älä kutsu tätä ProjectServicestä.")]
        private static async Task<int> EnsureUserAsync(MySqlConnection conn, MySqlTransaction tx, string username)
        {
            // 1) Yritetään hakea olemassa oleva käyttäjä nimen perusteella
            await using (var get = new MySqlCommand(
                "SELECT user_id FROM user WHERE kayttajaNimi=@u;", conn, tx))
            {
                get.Parameters.AddWithValue("@u", username);
                var id = await get.ExecuteScalarAsync();
                if (id != null && id != DBNull.Value)
                    return Convert.ToInt32(id);
            }

            // 2) Ei löytynyt -> lisätään uusi ja palautetaan sen id
            await using (var ins = new MySqlCommand(
                "INSERT INTO user(kayttajaNimi) VALUES(@u); SELECT LAST_INSERT_ID();", conn, tx))
            {
                ins.Parameters.AddWithValue("@u", username);
                return Convert.ToInt32(await ins.ExecuteScalarAsync());
            }
        }
    }
}