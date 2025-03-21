using Npgsql;

namespace api_preven_email_service.DAO
{
    public interface PostgreSQLInterface
    {
        NpgsqlConnection dbConnection();
    }
}