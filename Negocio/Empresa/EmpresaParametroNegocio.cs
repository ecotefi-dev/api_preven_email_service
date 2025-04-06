using System.Net;
using api_preven_email_service;
using api_preven_email_service.DAO;
using api_preven_email_service.DAO.Empresa;
using api_preven_email_service.Model.Empresa;
using api_preven_email_service.Helper;
using Npgsql;
using api_preven_email_service.Model;
using api_preven_email_service.Model.Email;

namespace api_preven_email_service.Negocio.Empresa{
    public class EmpresaParametroNegocio(LoggerService log, PostgreSQLInterface postgreSQLInterface)
    {
        private readonly PostgreSQLInterface? _postgreSQLInterface = postgreSQLInterface;
        private NpgsqlConnection? _session;
        private NpgsqlTransaction? _transaction;
        private readonly LoggerService _log = log;
        protected APIResponse _apiResponse = new APIResponse();
        private EmpresaParametroDAO? _empresaParametroDAO;

        public async Task<APIResponse> EmpresaParametroConsulta(Guid uuid, int id_usuario, int id_empresa_parametro) {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " | Id Empresa Parametro: " + id_empresa_parametro + " - Ingresa clase EmpresaParametroNegocio método EmpresaParametroConsulta");
            _apiResponse.uuid = uuid;
            _empresaParametroDAO = new EmpresaParametroDAO(_log);

            try {
                if (id_empresa_parametro == 0) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "El id debe ser diferente de cero.";
                } else if(_postgreSQLInterface == null) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                } else {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase EmpresaParametroNegocio método EmpresaParametroConsulta");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase EmpresaParametroNegocio método EmpresaParametroConsulta");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Consulta exitosa.";

                    ResponseGetModel vRespuesta = await _empresaParametroDAO.EmpresaParametroConsulta(uuid, id_empresa_parametro, _session);

                    if (vRespuesta.estatus){
                        if(vRespuesta.entidad != null){
                            EmpresaParametroModel vResultado = (EmpresaParametroModel)vRespuesta.entidad;
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
                    _log.Add(uuid + " INFO - Commit clase EmpresaParametroNegocio método EmpresaParametroConsulta");
                    _session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroConsulta");
                }
            } catch (Exception ex) {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase EmpresaParametroNegocio método EmpresaParametroConsulta: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase EmpresaParametroNegocio método EmpresaParametroConsulta: " + ex.ToString());

                if (_session != null) {
                    if(_transaction != null) {
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase EmpresaParametroNegocio método EmpresaParametroConsulta");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroConsulta");
                    }
                }
            } finally {
                if (_session != null) {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroConsulta");
                }
            }

            return _apiResponse;
        }
        public async Task<APIResponse> EmpresaParametroLista(Guid uuid, int id_usuario, int id_empresa_parametro, string clave, bool estatus) {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " | Id Empresa Parametro: " + id_empresa_parametro + " | Clave: " + clave + " | Estatus: " + estatus + " - Ingresa clase EmpresaParametroNegocio método EmpresaParametroLista");
            _apiResponse.uuid = uuid;
            _empresaParametroDAO = new EmpresaParametroDAO(_log);

            try {
                if(_postgreSQLInterface == null)  {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                } else {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase EmpresaParametroNegocio método EmpresaParametroLista");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase EmpresaParametroNegocio método EmpresaParametroLista");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Consulta exitosa.";

                    ResponseGetModel vRespuesta = await _empresaParametroDAO.EmpresaParametroLista(uuid, id_empresa_parametro, clave, estatus, _session);

                    if (vRespuesta.estatus) {
                        if(vRespuesta.lista != null && vRespuesta.lista!.Count > 0){
                            List<EmpresaParametroModel> vResultado = vRespuesta.lista.Cast<EmpresaParametroModel>().ToList();
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
                    _log.Add(uuid + " INFO - Commit clase EmpresaParametroNegocio método EmpresaParametroLista");
                    _session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroLista");
                }
            } catch (Exception ex) {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase EmpresaParametroNegocio método EmpresaParametroLista: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase EmpresaParametroNegocio método EmpresaParametroLista: " + ex.ToString());

                if (_session != null) {
                    if(_transaction != null) {
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase EmpresaParametroNegocio método EmpresaParametroLista");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroLista");
                    }
                }
            } finally {
                if (_session != null) {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroLista");
                }
            }

            return _apiResponse;
        }
        public async Task<APIResponse> EmpresaParametroEmail(Guid uuid, int id_usuario) {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " - Ingresa clase EmpresaParametroNegocio método EmpresaParametroEmail");
            _apiResponse.uuid = uuid;
            _empresaParametroDAO = new EmpresaParametroDAO(_log);

            try {
                if(_postgreSQLInterface == null)  {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                } else {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase EmpresaParametroNegocio método EmpresaParametroEmail");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase EmpresaParametroNegocio método EmpresaParametroEmail");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Consulta exitosa.";

                    ResponseGetModel vRespuesta = await _empresaParametroDAO.EmpresaParametroEmail(uuid, _session);

                    if (vRespuesta.estatus){
                        if(vRespuesta.entidad != null){
                            SMTPModel vResultado = (SMTPModel)vRespuesta.entidad;
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
                    _log.Add(uuid + " INFO - Commit clase EmpresaParametroNegocio método EmpresaParametroEmail");
                    _session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroEmail");
                }
            } catch (Exception ex) {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase EmpresaParametroNegocio método EmpresaParametroEmail: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase EmpresaParametroNegocio método EmpresaParametroEmail: " + ex.ToString());

                if (_session != null) {
                    if(_transaction != null) {
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase EmpresaParametroNegocio método EmpresaParametroEmail");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroEmail");
                    }
                }
            } finally {
                if (_session != null) {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase EmpresaParametroNegocio método EmpresaParametroEmail");
                }
            }

            return _apiResponse;
        }
    }
}