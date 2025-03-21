namespace api_preven_email_service.DAO
{
    public class PostgreSQLConfiguracion
    {
        public PostgreSQLConfiguracion(string connectionString) => ConnectionString = connectionString;

        public string ConnectionString { get; set; }
    }
}
