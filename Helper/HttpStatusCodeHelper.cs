using System.Net;

namespace api_preven_email_service.Helper
{
    public static class HttpStatusCodeHelper
    {
        public static HttpStatusCode GetHttpStatusCode(int statusCode)
        {
            if (Enum.IsDefined(typeof(HttpStatusCode), statusCode))
            {
                return (HttpStatusCode)statusCode;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(statusCode), "Código de estado HTTP no válido.");
            }
        }
    }
}