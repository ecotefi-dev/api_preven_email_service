using System.Net;
using api_preven_email_service.DAO;
using api_preven_email_service.Helper;
using api_preven_email_service.Model.Email;
using api_preven_email_service.Negocio.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_preven.Controllers
{
    [ApiController]
    [Route("api_preven_email_service/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly PostgreSQLInterface _postgreSQLInterface;
        private readonly EmailNegocio _emailNegocio;
        private Guid _uuid;
        protected APIResponse _apiResponse;
        private readonly LoggerService _log;

        public EmailController(LoggerService log, PostgreSQLInterface postgreSQLInterface)
        {
            _log = log;
            _postgreSQLInterface = postgreSQLInterface;
            _emailNegocio = new EmailNegocio(_log, _postgreSQLInterface);
            _uuid = Guid.NewGuid();
            _apiResponse = new();
        }

        [HttpPost("puntos")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> Puntos([FromBody] List<EmailPuntosModel> emailPuntosModel) {
            _apiResponse.uuid = Guid.NewGuid();

            try {
                if(!ModelState.IsValid) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "El modelo del email no es valido.";
                    _apiResponse.resultado = ModelState;
                    return HttpResponseHelper.CreateHttpResponse(_apiResponse);
                }

                if (emailPuntosModel == null) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "El modelo del email de puntos es nulo.";
                    return HttpResponseHelper.CreateHttpResponse(_apiResponse);
                }

                if(emailPuntosModel.Count == 0){
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La lista enviada debe de contener al menos un remitente.";
                    return HttpResponseHelper.CreateHttpResponse(_apiResponse);
                }

                var user = HttpContext.User;

                if (user.Identity == null && !user.Identity!.IsAuthenticated) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.Unauthorized;
                    _apiResponse.mensaje = "La identidad del encabezado no tiene autorización.";
                    return HttpResponseHelper.CreateHttpResponse(_apiResponse);
                }

                var id_usuario_claim = user.FindFirst("id_usuario");

                if (id_usuario_claim == null || !int.TryParse(id_usuario_claim.Value, out int id_usuario)) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "El claim 'id_usuario' es inválido o no existe.";
                    return HttpResponseHelper.CreateHttpResponse(_apiResponse);
                }

                _apiResponse = await _emailNegocio.Puntos(_uuid, id_usuario, emailPuntosModel);
                return HttpResponseHelper.CreateHttpResponse(_apiResponse);
            } catch(Exception ex) {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.InternalServerError;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en controlador AgenteTiendaController método AgenteTiendaConsulta: " + ex.ToString();
                return HttpResponseHelper.CreateHttpResponse(_apiResponse);
            }
        }
    }
}


/*
Carga de puntos masivos e individuales
Cada cambio de estatus de pedido - Solicitado - En transito - Entregado - Cancelado
*/