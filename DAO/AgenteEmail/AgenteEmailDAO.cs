using api_preven_email_service.Model;
using api_preven_email_service.Helper;
using Npgsql;
using Dapper;
using api_preven_email_service.Model.Email;

namespace api_preven_email_service.DAO.Agente{
    public class AgenteEmailDAO(LoggerService log) {
        private readonly LoggerService _log = log;

        public async Task<ResponseGetModel> AgenteEmailConsulta(Guid uuid, int id_agente, NpgsqlConnection session) {
            _log.Add(uuid + " INFO - Ingresa a clase AgenteEmailDAO método AgenteEmailConsulta");
            bool bandera = false;
            ResponseGetModel resultado = new();
            NpgsqlTransaction? transaction = null;

            try {
                if (session != null) {
                    var sql = @" 
                        SELECT id_agente
                             , email
                             , TRIM(primer_nombre || ' ' || segundo_nombre) nombres
                             , fn_nombre_completo(primer_nombre, segundo_nombre, apellido_paterno, apellido_materno, 1) nombre_completo
                             , preven.fn_saldo_puntos(id_agente, id_empresa) saldo_puntos
                          FROM preven.agente
                         WHERE id_agente = @IdAgente";

                    var result = await session.QueryFirstOrDefaultAsync<EmailPuntosModel>(sql, new { IdAgente = id_agente });

                    if (result != null) {
                        resultado.entidad = result;
                        resultado.estatus = true;
                    } else {
                        resultado.entidad = null;
                        resultado.estatus = true;
                    }
                    
                    if (bandera && transaction != null) {
                        transaction.Commit();
                        _log.Add(uuid + " INFO - Commit clase AgenteEmailDAO método AgenteEmailConsulta");
                        session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase AgenteEmailDAO método AgenteEmailConsulta");
                    }
                }
            } catch (Exception ex) {
                _log.Add(uuid + " ERROR - Excepción en clase AgenteEmailDAO método AgenteEmailConsulta: " + ex.Message);

                if (bandera && transaction != null) {
                    transaction.Rollback();
                    _log.Add(uuid + " INFO - Rollback clase AgenteEmailDAO método AgenteEmailConsulta");
                    session.Close();
                    _log.Add(uuid + " INFO - Cierra conexión clase AgenteEmailDAO método AgenteEmailConsulta");
                }

                resultado.entidad = null;  // Entidad nula en caso de error
                resultado.estatus = false;
            } finally {
                if (bandera && session != null) {
                    session.Dispose();
                    session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase AgenteEmailDAO método AgenteEmailConsulta");
                }
            }

            return resultado;
        }
    }
}