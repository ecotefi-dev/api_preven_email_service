namespace api_preven_email_service.Model.Email
{
    public class EmailConfiguracionModel
    {
        public string tenant_id { get; set; } = string.Empty;
        public string client_id { get; set; } = string.Empty;
        public string client_secret { get; set; } = string.Empty;

        public string notifica_puntos { get; set; } = string.Empty;
        public string notifica_pedido { get; set; } = string.Empty;
    }
}