using System.ComponentModel.DataAnnotations;

namespace api_preven_email_service.Model.Email
{
    public class EmailPruebaModel
    {
        [Required(
            ErrorMessage = "El correo destino es obligatorio."
        )]
        [EmailAddress(
            ErrorMessage = "El correo destino no tiene un formato válido."
        )]
        public string email { get; set; } =
            string.Empty;
    }
}