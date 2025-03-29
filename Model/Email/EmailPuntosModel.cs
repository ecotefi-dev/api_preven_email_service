using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace api_preven_email_service.Model.Email{
    public class EmailPuntosModel{
        public EmailPuntosModel(){
            id_agente = 0;
            email = string.Empty;
            observacion = string.Empty;
            nombres = string.Empty;
            nombre_completo = string.Empty;
            puntos = 0;
            saldo_puntos = 0;
        }

        [Required(ErrorMessage = "El id del agente es obligatorio.")]
        public int id_agente { get; set; } 
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? email { get; set; }
        public string? observacion { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? nombres { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? nombre_completo { get; set; }
        [Required(ErrorMessage = "Los puntos que se abonaron son obligatorios.")]
        public int puntos { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? saldo_puntos { get; set; }       
    }
}