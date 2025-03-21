using System.ComponentModel.DataAnnotations;

namespace api_preven_email_service.Model.Email{
    public class EmailPedidoModel{
        public EmailPedidoModel(){
            email = string.Empty;
            observacion = string.Empty;
            nombre_completo = string.Empty;
            id_pedido = 0;
            total = 0;
        }

        public string email { get; set; }
        public string observacion { get; set; }
        public string nombre_completo { get; set; }
        public int id_pedido { get; set; }
        public int total { get; set; }       
    }
}