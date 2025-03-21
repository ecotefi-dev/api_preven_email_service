namespace api_preven_email_service.Model
{
    public class ResponseDataModel
    {
        public ResponseDataModel() 
        {
            id = 0;
            mensaje = string.Empty;
            descripcion = string.Empty;
            statusCode = 0;
            estatus = false; 
        }

        public int? id { get; set; }
        public string? mensaje { get; set; }
        public string? descripcion { get; set; }
        public int statusCode { get; set; }
        public bool estatus { get; set; }
    }
}
