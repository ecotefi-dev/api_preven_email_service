using api_preven_email_service.Model.SysParametro;
using api_preven_email_service.Helper;
using Npgsql;
using Dapper;
using api_preven_email_service.Model;
using api_preven_email_service.Model.Email;

namespace api_preven_email_service.DAO.SysParametro{
    public class SysParametroDAO {
        private readonly LoggerService _log;

        public SysParametroDAO(LoggerService log)
        {
            _log = log;
        }

        public async Task<ResponseGetModel> SysParametroConsulta(Guid uuid, int id_sys_parametro, NpgsqlConnection session){
            _log.Add(uuid + " INFO - Ingresa a clase SysParametroDAO método SysParametroConsulta");
            bool bandera = false;
            ResponseGetModel resultado = new();
            NpgsqlTransaction? transaction = null;
            string qIdSysParametro = string.Empty;

            try
            {
                if (session != null)
                {
                    var sql = @" SELECT id_sys_parametro, clave, valor, estatus
                                   FROM sys.sys_parametro
                                  WHERE id_sys_parametro = @IdSysParametro";

                    var result = await session.QueryFirstOrDefaultAsync<SysParametroModel>(sql, new { IdSysParametro = id_sys_parametro });

                    if (result != null)
                    {
                        resultado.entidad = result;
                        resultado.estatus = true;
                    }
                    else
                    {
                        resultado.entidad = null;
                        resultado.estatus = true;
                    }
                    
                    if (bandera && transaction != null)
                    {
                        transaction.Commit();
                        _log.Add(uuid + " INFO - Commit clase SysParametroDAO método SysParametroConsulta");
                        session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase SysParametroDAO método SysParametroConsulta");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Add(uuid + " ERROR - Excepción en clase SysParametroDAO método SysParametroConsulta: " + ex.Message);

                if (bandera && transaction != null)
                {
                    transaction.Rollback();
                    _log.Add(uuid + " INFO - Rollback clase SysParametroDAO método SysParametroConsulta");
                    session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase SysParametroDAO método SysParametroConsulta");
                }

                resultado.entidad = null;  // Entidad nula en caso de error
                resultado.estatus = false;
            }
            finally
            {
                if (bandera && session != null)
                {
                    session.Dispose();
                    session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase SysParametroDAO método SysParametroConsulta");
                }
            }

            return resultado;
        }
        public async Task<ResponseGetModel> SysParametroLista(Guid uuid, int id_sys_parametro, string clave, bool estatus, NpgsqlConnection session){
            _log.Add(uuid + " INFO - Ingresa a clase SysParametroDAO método SysParametroLista");
            bool bandera = false;
            ResponseGetModel resultado = new();
            List<SysParametroModel> lista = [];
            NpgsqlTransaction? transaction = null;

            try
            {
                if (session != null)
                {
                    string qIdSysParametro = id_sys_parametro > 0 ? $" AND id_sys_parametro = {id_sys_parametro} " : "";
                    string qClave = !string.IsNullOrEmpty(clave) ? $" AND clave LIKE '%{clave}%' " : "";
                    string qEstatus = $" AND estatus = {estatus} ";

                    var sql = $@" SELECT id_sys_parametro , clave, valor, estatus
                                    FROM sys.sys_parametro
                                   WHERE 1 = 1 {qIdSysParametro} {qClave} {qEstatus}
                                ORDER BY id_sys_parametro";

                    var result = await session.QueryAsync<SysParametroModel>(sql);

                    SysParametroModel res = new SysParametroModel();
                    foreach(var item in result){
                        res = new SysParametroModel
                        {
                            id_sys_parametro = item.id_sys_parametro,
                            clave = item.clave,
                            valor = item.valor,
                            estatus = item.estatus
                        };
                        lista.Add(res);
                    }
                    
                    if (bandera && transaction != null) {
                        transaction.Commit();
                        _log.Add(uuid + " INFO - Commit clase SysParametroDAO método SysParametroLista");
                        session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase SysParametroDAO método SysParametroLista");
                    }

                    resultado.lista = lista.Cast<object>().ToList();
                    resultado.estatus = true;
                }
            }
            catch (Exception ex)
            {
                _log.Add(uuid + " ERROR - Excepción en clase SysParametroDAO método SysParametroLista: " + ex.Message);

                if (bandera && transaction != null) {
                    transaction.Rollback();
                    _log.Add(uuid + " INFO - Rollback clase SysParametroDAO método SysParametroLista");
                    session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase SysParametroDAO método SysParametroLista");
                }

                resultado.lista = new List<object>();
                resultado.estatus = false;
            }
            finally
            {
                if (bandera && session != null) {
                    session.Dispose();
                    session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase SysParametroDAO método SysParametroLista");
                }
            }

            return resultado;
        }
        public async Task<ResponseGetModel> SysParametroEmail(Guid uuid, NpgsqlConnection session){
            _log.Add(uuid + " INFO - Ingresa a clase SysParametroDAO método SysParametroEmail");
            bool bandera = false;
            ResponseGetModel resultado = new();
            NpgsqlTransaction? transaction = null;
            string qIdSysParametro = string.Empty;

            try
            {
                if (session != null)
                {
                    var sql = @" 
                        SELECT MAX(valor) FILTER (WHERE clave = 'SMTP_HOST') AS host
                             , MAX(valor) FILTER (WHERE clave = 'SMTP_PORT') AS port
                             , MAX(valor) FILTER (WHERE clave = 'SMTP_USER') AS user
                             , MAX(valor) FILTER (WHERE clave = 'SMTP_PASS') AS pass
                          FROM sys.sys_parametro";

                    var result = await session.QueryFirstOrDefaultAsync<SMTPModel>(sql);

                    if (result != null)
                    {
                        resultado.entidad = result;
                        resultado.estatus = true;
                    }
                    else
                    {
                        resultado.entidad = null;
                        resultado.estatus = true;
                    }
                    
                    if (bandera && transaction != null)
                    {
                        transaction.Commit();
                        _log.Add(uuid + " INFO - Commit clase SysParametroDAO método SysParametroEmail");
                        session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase SysParametroDAO método SysParametroEmail");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Add(uuid + " ERROR - Excepción en clase SysParametroDAO método SysParametroEmail: " + ex.Message);

                if (bandera && transaction != null)
                {
                    transaction.Rollback();
                    _log.Add(uuid + " INFO - Rollback clase SysParametroDAO método SysParametroEmail");
                    session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase SysParametroDAO método SysParametroEmail");
                }

                resultado.entidad = null;  // Entidad nula en caso de error
                resultado.estatus = false;
            }
            finally
            {
                if (bandera && session != null)
                {
                    session.Dispose();
                    session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase SysParametroDAO método SysParametroEmail");
                }
            }

            return resultado;
        }
    }
}