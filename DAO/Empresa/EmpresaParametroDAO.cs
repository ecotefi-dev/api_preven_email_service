using api_preven_email_service.Model.Empresa;
using api_preven_email_service.Helper;
using Npgsql;
using Dapper;
using api_preven_email_service.Model;
using api_preven_email_service.Model.Email;

namespace api_preven_email_service.DAO.Empresa{
    public class EmpresaParametroDAO(LoggerService log)
    {
        private readonly LoggerService _log = log;

        public async Task<ResponseGetModel> EmpresaParametroConsulta(Guid uuid, int id_empresa_parametro, NpgsqlConnection session) {
            _log.Add(uuid + " INFO - Ingresa a clase EmpresaParametroDAO método EmpresaParametroConsulta");
            bool bandera = false;
            ResponseGetModel resultado = new();
            NpgsqlTransaction? transaction = null;

            try {
                if (session != null) {
                    var sql = @" SELECT id_empresa_parametro, clave, valor, encripta, id_empresa, estatus
                                   FROM empresa_parametro
                                  WHERE id_empresa_parametro = @IdEmpresaParametro";

                    var result = await session.QueryFirstOrDefaultAsync<EmpresaParametroModel>(sql, new { IdEmpresaParametro = id_empresa_parametro });

                    if (result != null) {
                        resultado.entidad = result;
                        resultado.estatus = true;
                    } else {
                        resultado.entidad = null;
                        resultado.estatus = true;
                    }
                    
                    if (bandera && transaction != null) {
                        transaction.Commit();
                        _log.Add(uuid + " INFO - Commit clase EmpresaParametroDAO método EmpresaParametroConsulta");
                        session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroDAO método EmpresaParametroConsulta");
                    }
                }
            }
            catch (Exception ex) {
                _log.Add(uuid + " ERROR - Excepción en clase EmpresaParametroDAO método EmpresaParametroConsulta: " + ex.Message);

                if (bandera && transaction != null) {
                    transaction.Rollback();
                    _log.Add(uuid + " INFO - Rollback clase EmpresaParametroDAO método EmpresaParametroConsulta");
                    session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroDAO método EmpresaParametroConsulta");
                }

                resultado.entidad = null;  // Entidad nula en caso de error
                resultado.estatus = false;
            } finally {
                if (bandera && session != null) {
                    session.Dispose();
                    session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase EmpresaParametroDAO método EmpresaParametroConsulta");
                }
            }

            return resultado;
        }
        public async Task<ResponseGetModel> EmpresaParametroLista(Guid uuid, int id_empresa_parametro, string clave, bool estatus, NpgsqlConnection session) {
            _log.Add(uuid + " INFO - Ingresa a clase EmpresaParametroDAO método EmpresaParametroLista");
            bool bandera = false;
            ResponseGetModel resultado = new();
            List<EmpresaParametroModel> lista = [];
            NpgsqlTransaction? transaction = null;

            try {
                if (session != null) {
                    string qIdEmpresaParametro = id_empresa_parametro > 0 ? $" AND id_empresa_parametro = {id_empresa_parametro} " : "";
                    string qClave = !string.IsNullOrEmpty(clave) ? $" AND clave = '{clave}' " : "";
                    string qEstatus = $" AND estatus = {estatus} ";

                    var sql = $@" SELECT id_empresa_parametro , clave, valor, encripta, id_empresa, estatus
                                    FROM empresa_parametro
                                   WHERE 1 = 1 {qIdEmpresaParametro} {qClave} {qEstatus}
                                ORDER BY id_empresa_parametro";

                    var result = await session.QueryAsync<EmpresaParametroModel>(sql);

                    EmpresaParametroModel res = new();
                    foreach(var item in result) {
                        res = new EmpresaParametroModel {
                            id_empresa_parametro = item.id_empresa_parametro,
                            clave = item.clave,
                            valor = item.valor,
                            encripta = item.encripta,
                            id_empresa = item.id_empresa,
                            estatus = item.estatus
                        };
                        lista.Add(res);
                    }
                    
                    if (bandera && transaction != null) {
                        transaction.Commit();
                        _log.Add(uuid + " INFO - Commit clase EmpresaParametroDAO método EmpresaParametroLista");
                        session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroDAO método EmpresaParametroLista");
                    }

                    resultado.lista = lista.Cast<object>().ToList();
                    resultado.estatus = true;
                }
            } catch (Exception ex) {
                _log.Add(uuid + " ERROR - Excepción en clase EmpresaParametroDAO método EmpresaParametroLista: " + ex.Message);

                if (bandera && transaction != null) {
                    transaction.Rollback();
                    _log.Add(uuid + " INFO - Rollback clase EmpresaParametroDAO método EmpresaParametroLista");
                    session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroDAO método EmpresaParametroLista");
                }

                resultado.lista = [];
                resultado.estatus = false;
            } finally {
                if (bandera && session != null) {
                    session.Dispose();
                    session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase EmpresaParametroDAO método EmpresaParametroLista");
                }
            }

            return resultado;
        }
        public async Task<ResponseGetModel> EmpresaParametroEmail(Guid uuid, NpgsqlConnection session) {
            _log.Add(uuid + " INFO - Ingresa a clase EmpresaParametroDAO método EmpresaParametroEmail");
            bool bandera = false;
            ResponseGetModel resultado = new();
            NpgsqlTransaction? transaction = null;

            try {
                if (session != null) {
                    var sql = @" 
                        SELECT MAX(valor) FILTER (WHERE clave = 'SMTP_HOST') AS host
                             , MAX(valor) FILTER (WHERE clave = 'SMTP_PORT') AS port
                             , MAX(valor) FILTER (WHERE clave = 'SMTP_USER') AS user
                             , MAX(valor) FILTER (WHERE clave = 'SMTP_PASS') AS pass
                          FROM empresa_parametro";

                    var result = await session.QueryFirstOrDefaultAsync<SMTPModel>(sql);

                    if (result != null) {
                        resultado.entidad = result;
                        resultado.estatus = true;
                    } else {
                        resultado.entidad = null;
                        resultado.estatus = true;
                    }
                    
                    if (bandera && transaction != null) {
                        transaction.Commit();
                        _log.Add(uuid + " INFO - Commit clase EmpresaParametroDAO método EmpresaParametroEmail");
                        session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroDAO método EmpresaParametroEmail");
                    }
                }
            } catch (Exception ex) {
                _log.Add(uuid + " ERROR - Excepción en clase EmpresaParametroDAO método EmpresaParametroEmail: " + ex.Message);

                if (bandera && transaction != null)
                {
                    transaction.Rollback();
                    _log.Add(uuid + " INFO - Rollback clase EmpresaParametroDAO método EmpresaParametroEmail");
                    session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase EmpresaParametroDAO método EmpresaParametroEmail");
                }

                resultado.entidad = null;  // Entidad nula en caso de error
                resultado.estatus = false;
            } finally {
                if (bandera && session != null)
                {
                    session.Dispose();
                    session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase EmpresaParametroDAO método EmpresaParametroEmail");
                }
            }

            return resultado;
        }
    }
}