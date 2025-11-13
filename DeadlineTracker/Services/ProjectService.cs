using DeadlineTracker.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DeadlineTracker.Services
{
    /// <summary>
    /// Tietokantapalvelu projekteille ja tehtäville.
    /// Sisältää CRUD-operaatiot sekä dashboard-haut yleisnäkymään.
    /// </summary>
    public partial class ProjectService
    {
        private readonly string _connStr;
        public ProjectService(string connStr) => _connStr = connStr;

        // -----------------------------
        // DTO:t lukumalleihin (lukeminen)
        // -----------------------------

        public sealed class ProjectDetail
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public string? Description { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public sealed class TaskDetail
        {
            public long Id { get; set; }
            public string Title { get; set; } = "";
            public bool Done { get; set; }
            public DateTime? DueDate { get; set; }
            public int? AssigneeId { get; set; }
            public string? AssigneeName { get; set; }
        }

        public sealed class UserDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        /// <summary>
        /// Yleisnäkymän data: projektit + 0/0 -luvut + keskeneräiset tehtävät.
        /// </summary>
        public sealed class DashboardProject
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public DateTime EndDate { get; set; }
            public int DoneCount { get; set; }
            public int TotalCount { get; set; }
            public List<Tehtava> OpenTasks { get; } = new();
        }

        // -----------------------------
        // CREATE
        // -----------------------------
        /// <summary>
        /// Luo uuden projektin tehtävineen ja osallistujineen transaktiossa.
        /// </summary>
        public async Task<long> CreateProjectAsync(
            string name,
            string description,
            DateTime startDate,
            DateTime endDate,
            IReadOnlyList<(string title, bool done, DateTime? due, int? assigneeId)> tasks,
            IReadOnlyList<int> participantUserIds)
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

                const string insertProject = @"
                    INSERT INTO projekti (projektiNimi, kuvausTeksti, alkupvm, loppupvm)
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

                const string insertTaskSql = @"
                    INSERT INTO tehtava
                        (projekti_id, tehtavaNimi, tehtavaKuvaus, onValmis, erapaiva, prioriteetti, maarattyHenkilolle, luotupvm)
                    VALUES
                        (@pid, @title, NULL, @done, @due, 'keskitaso', @assignee, NOW());";

                foreach (var t in tasks)
                {
                    await using var ct = new MySqlCommand(insertTaskSql, conn, (MySqlTransaction)tx);
                    ct.Parameters.AddWithValue("@pid", projectId);
                    ct.Parameters.AddWithValue("@title", t.title);
                    ct.Parameters.AddWithValue("@done", t.done ? 1 : 0);
                    ct.Parameters.AddWithValue("@due", (object?)t.due ?? DBNull.Value);
                    ct.Parameters.AddWithValue("@assignee", (object?)t.assigneeId ?? DBNull.Value);
                    await ct.ExecuteNonQueryAsync();
                }

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

                await tx.CommitAsync();
                return projectId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // -----------------------------
        // READ – yksittäinen projekti
        // -----------------------------
        public async Task<ProjectDetail> GetProjectDetailAsync(long projectId)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT projekti_id, projektiNimi, kuvausTeksti, alkupvm, loppupvm
                FROM projekti
                WHERE projekti_id=@id;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", projectId);

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) throw new Exception("Projektia ei löydy.");

            return new ProjectDetail
            {
                Id = r.GetInt64("projekti_id"),
                Name = r.GetString("projektiNimi"),
                Description = r.IsDBNull("kuvausTeksti") ? null : r.GetString("kuvausTeksti"),
                StartDate = r.GetDateTime("alkupvm"),
                EndDate = r.GetDateTime("loppupvm")
            };
        }

        public async Task<List<UserDto>> GetProjectParticipantsAsync(long projectId)
        {
            var list = new List<UserDto>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT u.user_id AS Id, u.kayttajaNimi AS Name
                FROM projekti_osallistuja po
                JOIN user u ON u.user_id = po.user_id
                WHERE po.projekti_id = @pid
                ORDER BY u.kayttajaNimi;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", projectId);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new UserDto { Id = r.GetInt32("Id"), Name = r.GetString("Name") });

            return list;
        }

        public async Task<List<TaskDetail>> GetProjectTasksAsync(long projectId)
        {
            var list = new List<TaskDetail>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT t.tehtava_id, t.tehtavaNimi, t.onValmis, t.erapaiva,
                       t.maarattyHenkilolle, u.kayttajaNimi
                FROM tehtava t
                LEFT JOIN user u ON u.user_id = t.maarattyHenkilolle
                WHERE t.projekti_id = @pid
                ORDER BY t.tehtava_id;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", projectId);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new TaskDetail
                {
                    Id = r.GetInt64("tehtava_id"),
                    Title = r.GetString("tehtavaNimi"),
                    Done = r.GetInt32("onValmis") == 1,
                    DueDate = r.IsDBNull("erapaiva") ? null : r.GetDateTime("erapaiva"),
                    AssigneeId = r.IsDBNull("maarattyHenkilolle") ? (int?)null : r.GetInt32("maarattyHenkilolle"),
                    AssigneeName = r.IsDBNull("kayttajaNimi") ? null : r.GetString("kayttajaNimi")
                });
            }
            return list;
        }

        // -----------------------------
        // UPDATE (yksinkertainen ja varma: korvataan setit)
        // -----------------------------
        public async Task UpdateProjectAsync(
            long projectId,
            string name,
            string description,
            DateTime start,
            DateTime end,
            IReadOnlyList<(long? id, string title, bool done, DateTime? due, int? assigneeId)> tasks,
            IReadOnlyList<int> participantUserIds)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                const string upProj = @"
                    UPDATE projekti
                    SET projektiNimi=@n, kuvausTeksti=@d, alkupvm=@a, loppupvm=@l
                    WHERE projekti_id=@id;";
                await using (var up = new MySqlCommand(upProj, conn, (MySqlTransaction)tx))
                {
                    up.Parameters.AddWithValue("@n", name);
                    up.Parameters.AddWithValue("@d", description ?? "");
                    up.Parameters.AddWithValue("@a", start.Date);
                    up.Parameters.AddWithValue("@l", end.Date);
                    up.Parameters.AddWithValue("@id", projectId);
                    await up.ExecuteNonQueryAsync();
                }

                const string delMembers = @"DELETE FROM projekti_osallistuja WHERE projekti_id=@pid;";
                await using (var dm = new MySqlCommand(delMembers, conn, (MySqlTransaction)tx))
                {
                    dm.Parameters.AddWithValue("@pid", projectId);
                    await dm.ExecuteNonQueryAsync();
                }

                const string insMember = @"INSERT INTO projekti_osallistuja (projekti_id, user_id) VALUES (@pid, @uid);";
                foreach (var uid in participantUserIds.Distinct())
                {
                    await using var im = new MySqlCommand(insMember, conn, (MySqlTransaction)tx);
                    im.Parameters.AddWithValue("@pid", projectId);
                    im.Parameters.AddWithValue("@uid", uid);
                    await im.ExecuteNonQueryAsync();
                }

                const string delTasks = @"DELETE FROM tehtava WHERE projekti_id=@pid;";
                await using (var dt = new MySqlCommand(delTasks, conn, (MySqlTransaction)tx))
                {
                    dt.Parameters.AddWithValue("@pid", projectId);
                    await dt.ExecuteNonQueryAsync();
                }

                const string insTask = @"
                    INSERT INTO tehtava
                        (projekti_id, tehtavaNimi, tehtavaKuvaus, onValmis, erapaiva, prioriteetti, maarattyHenkilolle, luotupvm)
                    VALUES
                        (@pid, @title, NULL, @done, @due, 'keskitaso', @assignee, NOW());";
                foreach (var t in tasks)
                {
                    await using var it = new MySqlCommand(insTask, conn, (MySqlTransaction)tx);
                    it.Parameters.AddWithValue("@pid", projectId);
                    it.Parameters.AddWithValue("@title", t.title);
                    it.Parameters.AddWithValue("@done", t.done ? 1 : 0);
                    it.Parameters.AddWithValue("@due", (object?)t.due ?? DBNull.Value);
                    it.Parameters.AddWithValue("@assignee", (object?)t.assigneeId ?? DBNull.Value);
                    await it.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // -----------------------------
        // DELETE
        // -----------------------------
        public async Task DeleteProjectAsync(long projectId)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Jos ei ole ON DELETE CASCADE, poistetaan lapset ensin:
                await using (var t = new MySqlCommand("DELETE FROM tehtava WHERE projekti_id=@id;", conn, (MySqlTransaction)tx))
                { t.Parameters.AddWithValue("@id", projectId); await t.ExecuteNonQueryAsync(); }

                await using (var po = new MySqlCommand("DELETE FROM projekti_osallistuja WHERE projekti_id=@id;", conn, (MySqlTransaction)tx))
                { po.Parameters.AddWithValue("@id", projectId); await po.ExecuteNonQueryAsync(); }

                await using (var p = new MySqlCommand("DELETE FROM projekti WHERE projekti_id=@id;", conn, (MySqlTransaction)tx))
                { p.Parameters.AddWithValue("@id", projectId); await p.ExecuteNonQueryAsync(); }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // -----------------------------
        // MARK TASK DONE (checkbox)
        // -----------------------------
        public async Task<bool> MarkTaskDoneAsync(int tehtavaId)
        {
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"UPDATE tehtava SET onValmis = 1 WHERE tehtava_id = @id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", tehtavaId);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        // ---------------------------------------------------
        // DASHBOARD – projektit + 0/0 + keskeneräiset tehtävät
        // ---------------------------------------------------
        public async Task<List<DashboardProject>> GetDashboardProjectsAsync(int? userId)
        {
            var result = new List<DashboardProject>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            // 1) projektit + 0/0 -luvut
            var sql = @"
                SELECT  p.projekti_id, p.projektiNimi, p.loppupvm,
                        COALESCE(SUM(CASE WHEN t.onValmis = 1 THEN 1 ELSE 0 END),0) AS DoneCount,
                        COALESCE(COUNT(t.tehtava_id),0) AS TotalCount
                FROM projekti p
                LEFT JOIN tehtava t ON t.projekti_id = p.projekti_id
                /**userJoin**/
                /**where**/
                GROUP BY p.projekti_id, p.projektiNimi, p.loppupvm
                ORDER BY p.loppupvm ASC, p.projekti_id DESC;";

            var userJoin = userId.HasValue ? "JOIN projekti_osallistuja po ON po.projekti_id = p.projekti_id" : "";
            var where = userId.HasValue ? "WHERE po.user_id = @uid" : "";
            sql = sql.Replace("/**userJoin**/", userJoin).Replace("/**where**/", where);

            await using (var cmd = new MySqlCommand(sql, conn))
            {
                if (userId.HasValue) cmd.Parameters.AddWithValue("@uid", userId.Value);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    result.Add(new DashboardProject
                    {
                        Id = r.GetInt64("projekti_id"),
                        Name = r.GetString("projektiNimi"),
                        EndDate = r.GetDateTime("loppupvm"),
                        DoneCount = r.IsDBNull("DoneCount") ? 0 : r.GetInt32("DoneCount"),
                        TotalCount = r.IsDBNull("TotalCount") ? 0 : r.GetInt32("TotalCount")
                    });
                }
            }

            if (result.Count == 0) return result;

            // 2) keskeneräiset tehtävät yhdellä kyselyllä
            var idsCsv = string.Join(",", result.Select(x => x.Id));
            var openSql = $@"
                SELECT t.tehtava_id, t.projekti_id, t.tehtavaNimi, t.tehtavaKuvaus,
                       t.onValmis, t.luotupvm, t.erapaiva
                FROM tehtava t
                WHERE t.projekti_id IN ({idsCsv}) AND t.onValmis = 0
                ORDER BY t.projekti_id, t.tehtava_id;";

            await using (var ct = new MySqlCommand(openSql, conn))
            await using (var rt = await ct.ExecuteReaderAsync())
            {
                while (await rt.ReadAsync())
                {
                    var pid = rt.GetInt64("projekti_id");
                    var proj = result.FirstOrDefault(x => x.Id == pid);
                    if (proj == null) continue;

                    proj.OpenTasks.Add(new Tehtava
                    {
                        TehtavaId = rt.GetInt32("tehtava_id"),
                        ProjektiId = (int)pid,
                        TehtavaNimi = rt.GetString("tehtavaNimi"),
                        TehtavaKuvaus = rt.IsDBNull("tehtavaKuvaus") ? "" : rt.GetString("tehtavaKuvaus"),
                        OnValmis = false,
                        LuotuPvm = rt.IsDBNull("luotupvm") ? DateTime.MinValue : rt.GetDateTime("luotupvm"),
                        Erapaiva = rt.IsDBNull("erapaiva") ? DateTime.MinValue : rt.GetDateTime("erapaiva")
                    });
                }
            }

            return result;
        }

        // -----------------------------
        // LISTAHAUT (taaksepäinyhteensopivuus)
        // -----------------------------
        /// <summary>
        /// Kaikki projektit ilman suodatusta (kevyt esikatselu).
        /// </summary>
        public async Task<List<Project>> GetAllProjectsAsync(bool includeCompletedTasks = true)
        {
            var list = new List<Project>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT p.projekti_id, p.projektiNimi, p.kuvausTeksti, p.alkupvm, p.loppupvm
                FROM projekti p
                ORDER BY p.projekti_id DESC;";

            await using var cmd = new MySqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new Project
                {
                    ProjektiId = r.GetInt64("projekti_id"),
                    ProjektiNimi = r.GetString("projektiNimi"),
                    KuvausTeksti = r.IsDBNull("kuvausTeksti") ? null : r.GetString("kuvausTeksti"),
                    Alkupvm = r.GetDateTime("alkupvm"),
                    Loppupvm = r.GetDateTime("loppupvm")
                });
            }
            return list;
        }

        /// <summary>
        /// Projektit käyttäjälle (JOIN projekti_osallistuja), kevyt esikatselu.
        /// </summary>
        public async Task<List<Project>> GetProjectsForUserAsync(int userId, bool includeCompletedTasks)
        {
            var list = new List<Project>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT DISTINCT p.projekti_id, p.projektiNimi, p.kuvausTeksti, p.alkupvm, p.loppupvm
                FROM projekti p
                JOIN projekti_osallistuja po ON po.projekti_id = p.projekti_id
                WHERE po.user_id = @uid
                ORDER BY p.projekti_id DESC;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new Project
                {
                    ProjektiId = r.GetInt64("projekti_id"),
                    ProjektiNimi = r.GetString("projektiNimi"),
                    KuvausTeksti = r.IsDBNull("kuvausTeksti") ? null : r.GetString("kuvausTeksti"),
                    Alkupvm = r.GetDateTime("alkupvm"),
                    Loppupvm = r.GetDateTime("loppupvm")
                });
            }
            return list;
        }

        // -----------------------------
        // (Valinnainen) käyttäjän varmistus apumetodi
        // -----------------------------
        private static async Task<int> EnsureUserAsync(MySqlConnection conn, MySqlTransaction tx, string username)
        {
            await using (var get = new MySqlCommand("SELECT user_id FROM user WHERE kayttajaNimi=@u;", conn, tx))
            {
                get.Parameters.AddWithValue("@u", username);
                var id = await get.ExecuteScalarAsync();
                if (id != null && id != DBNull.Value)
                    return Convert.ToInt32(id);
            }
            await using (var ins = new MySqlCommand(
                "INSERT INTO user(kayttajaNimi) VALUES(@u); SELECT LAST_INSERT_ID();", conn, tx))
            {
                ins.Parameters.AddWithValue("@u", username);
                return Convert.ToInt32(await ins.ExecuteScalarAsync());
            }
        }
    }
}
