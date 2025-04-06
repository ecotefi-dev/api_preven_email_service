using System.ComponentModel.DataAnnotations;

namespace api_preven_email_service.Model.Empresa
{
    public class EmpresaParametroModel
    {
        public EmpresaParametroModel() 
        {
            id_empresa_parametro = 0;
            clave = string.Empty;
            valor = string.Empty;
            encripta = false;
            id_empresa = 0;
            estatus = false; 
        }

        public int id_empresa_parametro { get; set; }

        [Required(ErrorMessage = "La clave es obligatorio.")]
        [MaxLength(50, ErrorMessage = "La clave no puede tener más de 50 caracteres.")]
        public string clave { get; set; }

        [Required(ErrorMessage = "El valor es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El valor no puede tener más de 100 caracteres.")]
        public bool encripta { get; set; } 
        public string valor { get; set; }
        public int id_empresa { get; set; }
        public bool estatus { get; set; }
    }
}
