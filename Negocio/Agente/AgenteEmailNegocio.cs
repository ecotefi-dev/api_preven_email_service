using System.Net;
using api_preven_email_service.DAO;
using api_preven_email_service.DAO.Agente;
using api_preven_email_service.Helper;
using Npgsql;
using api_preven_email_service.Model;
using api_preven_email_service.Model.Email;
using Microsoft.IdentityModel.Tokens;

namespace api_preven_email_service.Negocio.Agente{
    public class AgenteEmailNegocio{
        private readonly PostgreSQLInterface? _postgreSQLInterface;
        private NpgsqlConnection? _session;
        private NpgsqlTransaction? _transaction;
        private readonly LoggerService _log;
        protected APIResponse _apiResponse;
        private AgenteEmailDAO? _agenteEmailDAO;

        public AgenteEmailNegocio(LoggerService log, PostgreSQLInterface postgreSQLInterface){
            _log = log;
            _postgreSQLInterface = postgreSQLInterface;
            _apiResponse = new APIResponse();
        }

        public async Task<APIResponse> AgenteEmailConsulta(Guid uuid, int id_usuario, int id_agente, string email) {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " | Id Agente: " + id_agente + " - Ingresa clase AgenteNegocio método AgenteConsulta");
            _apiResponse.uuid = uuid;
            _agenteEmailDAO = new AgenteEmailDAO(_log);

            try {
                if (email.IsNullOrEmpty()) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "Se debe de proporcionar el email.";
                } else if(_postgreSQLInterface == null)  {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                } else {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase AgenteNegocio método AgenteConsulta");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase AgenteNegocio método AgenteConsulta");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Consulta exitosa.";

                    ResponseGetModel vRespuesta = await _agenteEmailDAO.AgenteEmailConsulta(uuid, id_agente, email, _session);

                    if (vRespuesta.estatus){
                        if(vRespuesta.entidad != null){
                            EmailPuntosModel vResultado = (EmailPuntosModel)vRespuesta.entidad;
                            _apiResponse.resultado = vResultado;
                        } else{ 
                            _apiResponse.respuesta = false;
                            _apiResponse.statusCode = HttpStatusCode.NotFound;
                            _apiResponse.mensaje = "No se encontraron registros.";
                        }
                    } else { //Entro catch del DAO
                        _apiResponse.respuesta = false;
                        _apiResponse.statusCode = HttpStatusCode.InternalServerError;
                        _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                    }

                    _transaction.Commit();
                    _transaction.Dispose();
                    _log.Add(uuid + " INFO - Commit clase AgenteNegocio método AgenteConsulta");
                    _session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase AgenteNegocio método AgenteConsulta");
                }
            }
            catch (Exception ex)
            {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase AgenteNegocio método AgenteConsulta: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase AgenteNegocio método AgenteConsulta: " + ex.ToString());

                if (_session != null)
                {
                    if(_transaction != null){
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase AgenteNegocio método AgenteConsulta");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase AgenteNegocio método AgenteConsulta");
                    }
                }
            }
            finally
            {
                if (_session != null)
                {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase AgenteNegocio método AgenteConsulta");
                }
            }

            return _apiResponse;
        }
    }
}