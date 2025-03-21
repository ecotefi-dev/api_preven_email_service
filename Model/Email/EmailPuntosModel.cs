using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace api_preven_email_service.Model.Email{
    public class EmailPuntosModel{
        public EmailPuntosModel(){
            email = string.Empty;
            observacion = string.Empty;
            nombre_completo = string.Empty;
            puntos = 0;
            total = 0;
        }

        public string? email { get; set; }
        public string? observacion { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? nombre_completo { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? puntos { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? total { get; set; }       
    }
}