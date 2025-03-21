using System.Net;
using api_preven_email_service;
using api_preven_email_service.DAO;
using api_preven_email_service.DAO.SysParametro;
using api_preven_email_service.Model.SysParametro;
using api_preven_email_service.Helper;
using Npgsql;
using api_preven_email_service.Model;
using api_preven_email_service.Model.Email;

namespace api_preven_email_service.Negocio.SysParametro{
    public class SysParametroNegocio{
        private readonly PostgreSQLInterface? _postgreSQLInterface;
        private NpgsqlConnection? _session;
        private NpgsqlTransaction? _transaction;
        private readonly LoggerService _log;
        protected APIResponse _apiResponse;
        private SysParametroDAO? _sysParametroD;

        public SysParametroNegocio(LoggerService log, PostgreSQLInterface postgreSQLInterface){
            _log = log;
            _postgreSQLInterface = postgreSQLInterface;
            _apiResponse = new APIResponse();
        }

        public async Task<APIResponse> SysParametroConsulta(Guid uuid, int id_usuario, int id_sys_parametro) {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " | Id Sys Parametro: " + id_sys_parametro + " - Ingresa clase SysParametroNegocio método SysParametroConsulta");
            _apiResponse.uuid = uuid;
            _sysParametroD = new SysParametroDAO(_log);

            try
            {
                if (id_sys_parametro == 0)
                {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "El id debe ser diferente de cero.";
                }
                else if(_postgreSQLInterface == null) 
                {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                }
                else
                {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase SysParametroNegocio método SysParametroConsulta");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase SysParametroNegocio método SysParametroConsulta");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Consulta exitosa.";

                    ResponseGetModel vRespuesta = await _sysParametroD.SysParametroConsulta(uuid, id_sys_parametro, _session);

                    if (vRespuesta.estatus){
                        if(vRespuesta.entidad != null){
                            SysParametroModel vResultado = (SysParametroModel)vRespuesta.entidad;
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
                    _log.Add(uuid + " INFO - Commit clase SysParametroNegocio método SysParametroConsulta");
                    _session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase SysParametroNegocio método SysParametroConsulta");
                }
            }
            catch (Exception ex)
            {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase SysParametroNegocio método SysParametroConsulta: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase SysParametroNegocio método SysParametroConsulta: " + ex.ToString());

                if (_session != null)
                {
                    if(_transaction != null){
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase SysParametroNegocio método SysParametroConsulta");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase SysParametroNegocio método SysParametroConsulta");
                    }
                }
            }
            finally
            {
                if (_session != null)
                {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase SysParametroNegocio método SysParametroConsulta");
                }
            }

            return _apiResponse;
        }
        public async Task<APIResponse> SysParametroLista(Guid uuid, int id_usuario, int id_sys_parametro, string clave, bool estatus)
        {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " | Id Sys Parametro: " + id_sys_parametro + " | Clave: " + clave + " | Estatus: " + estatus + " - Ingresa clase SysParametroNegocio método SysParametroLista");
            _apiResponse.uuid = uuid;
            _sysParametroD = new SysParametroDAO(_log);

            try
            {
                if(_postgreSQLInterface == null) 
                {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                }
                else
                {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase SysParametroNegocio método SysParametroLista");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase SysParametroNegocio método SysParametroLista");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Consulta exitosa.";

                    ResponseGetModel vRespuesta = await _sysParametroD.SysParametroLista(uuid, id_sys_parametro, clave, estatus, _session);

                    if (vRespuesta.estatus){
                        if(vRespuesta.lista != null && vRespuesta.lista!.Count > 0){
                            List<SysParametroModel> vResultado = vRespuesta.lista.Cast<SysParametroModel>().ToList();
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
                    _log.Add(uuid + " INFO - Commit clase SysParametroNegocio método SysParametroLista");
                    _session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase SysParametroNegocio método SysParametroLista");
                }
            }
            catch (Exception ex)
            {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase SysParametroNegocio método SysParametroLista: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase SysParametroNegocio método SysParametroLista: " + ex.ToString());

                if (_session != null)
                {
                    if(_transaction != null){
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase SysParametroNegocio método SysParametroLista");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase SysParametroNegocio método SysParametroLista");
                    }
                }
            }
            finally
            {
                if (_session != null)
                {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase SysParametroNegocio método SysParametroLista");
                }
            }

            return _apiResponse;
        }
        public async Task<APIResponse> SysParametroEmail(Guid uuid, int id_usuario) {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " - Ingresa clase SysParametroNegocio método SysParametroEmail");
            _apiResponse.uuid = uuid;
            _sysParametroD = new SysParametroDAO(_log);

            try
            {
                if(_postgreSQLInterface == null) 
                {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                }
                else
                {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase SysParametroNegocio método SysParametroEmail");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase SysParametroNegocio método SysParametroEmail");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Consulta exitosa.";

                    ResponseGetModel vRespuesta = await _sysParametroD.SysParametroEmail(uuid, _session);

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
                    _log.Add(uuid + " INFO - Commit clase SysParametroNegocio método SysParametroEmail");
                    _session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase SysParametroNegocio método SysParametroEmail");
                }
            }
            catch (Exception ex)
            {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase SysParametroNegocio método SysParametroEmail: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase SysParametroNegocio método SysParametroEmail: " + ex.ToString());

                if (_session != null)
                {
                    if(_transaction != null){
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase SysParametroNegocio método SysParametroEmail");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase SysParametroNegocio método SysParametroEmail");
                    }
                }
            }
            finally
            {
                if (_session != null)
                {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase SysParametroNegocio método SysParametroEmail");
                }
            }

            return _apiResponse;
        }
    }
}