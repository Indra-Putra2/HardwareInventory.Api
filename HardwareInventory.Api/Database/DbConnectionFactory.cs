using Microsoft.Data.Sqlite;

namespace HardwareInventory.Api.Database
{
    public class DbConnectionFactory
    {
        private readonly string _connectionString;
        public DbConnectionFactory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("Default");
        }
        public SqliteConnection Create()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}
