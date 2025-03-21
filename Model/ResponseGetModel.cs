namespace api_preven_email_service.Model
{
    public class ResponseGetModel
    {
        public ResponseGetModel() 
        {
            lista = [];
            entidad = null;
            estatus = false; 
        }

        public List<object> lista { get; set; }
        public object? entidad { get; set; }
        public bool estatus { get; set; }
    }
}
