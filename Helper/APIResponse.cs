using System.Net;

namespace api_preven_email_service.Helper
{
    public class APIResponse
    {
        public APIResponse()
        {
            mensaje = string.Empty;
            descripcion = string.Empty;
            resultado = string.Empty;
            token = string.Empty;
        }

        public HttpStatusCode statusCode { get; set; }
        public bool respuesta { get; set; } = true;
        public string? mensaje { get; set; }
        public string? descripcion { get; set; }
        public object resultado { get; set; }
        public Guid uuid { get; set; }
        public string? token { get; set; }
 
    }
}
