using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DeadlineTracker.Services
{
    public class UserService
    {
        private readonly string _connStr;
        public UserService(string connStr) => _connStr = connStr;

        public async Task<List<UserDto>> GetAllAsync()
        {
            var list = new List<UserDto>();
            await using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            const string sql = @"SELECT user_id, kayttajaNimi FROM user ORDER BY kayttajaNimi;";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new UserDto
                {
                    Id = r.GetInt32("user_id"),
                    Name = r.GetString("kayttajaNimi")
                });
            }
            return list;
        }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}