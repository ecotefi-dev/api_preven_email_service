using Newtonsoft.Json;

namespace api_preven_email_service.Helper
{
    public class LoggerService
    {
        private readonly Serilog.ILogger _logger;

        public LoggerService(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void Add(string message)
        {
            _logger.Information(message);
        }

        public string ConvertirModeloATexto<T>(T modelo)
        {
            if (modelo == null)
                throw new ArgumentNullException(nameof(modelo), "El modelo no debe ser nulo.");

            return JsonConvert.SerializeObject(modelo, Formatting.Indented);
        }
    }
}
