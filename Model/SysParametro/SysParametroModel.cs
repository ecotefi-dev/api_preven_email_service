using System.ComponentModel.DataAnnotations;

namespace api_preven_email_service.Model.SysParametro
{
    public class SysParametroModel
    {
        public SysParametroModel() 
        {
            id_sys_parametro = 0;
            clave = string.Empty;
            valor = string.Empty;
            estatus = false; 
        }

        public int id_sys_parametro { get; set; }

        [Required]
        [MaxLength(50)]
        public string clave { get; set; }

        [Required]
        [MaxLength(100)]
        public string valor { get; set; }
        
        public bool estatus { get; set; }
    }
}
