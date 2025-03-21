using Npgsql;

namespace api_preven_email_service.DAO
{
    public class PostgreSQLConection : PostgreSQLInterface
    {
        private PostgreSQLConfiguracion _connectionString;

        public PostgreSQLConection(PostgreSQLConfiguracion connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection dbConnection()
        {
            return new NpgsqlConnection(_connectionString.ConnectionString);
        }
    }
}
