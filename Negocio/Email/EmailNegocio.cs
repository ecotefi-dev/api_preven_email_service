using System.Net;
using System.Net.Mail;
using api_preven_email_service.DAO;
using api_preven_email_service.Helper;
using api_preven_email_service.Model.Email;
using api_preven_email_service.Negocio.SysParametro;
using Npgsql;

namespace api_preven_email_service.Negocio.Email{
    public class EmailNegocio
    {
        private readonly LoggerService _log;
        protected APIResponse _apiResponse;
        private readonly PostgreSQLInterface _postgreSQLInterface;
        private NpgsqlConnection? _session;
        private NpgsqlTransaction? _transaction;
        private string smtpHost = string.Empty;
        private int smtpPort = 0;
        private string smtpUser = string.Empty;
        private string smtpPass = string.Empty;

        public EmailNegocio(LoggerService log, PostgreSQLInterface postgreSQLInterface){
            _log = log;
            _postgreSQLInterface = postgreSQLInterface;
            _apiResponse = new APIResponse();
        }

        public async Task<APIResponse>Puntos(Guid uuid, int id_usuario, List<EmailPuntosModel> emailPuntosModel)
        {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " - Ingresa clase EmailNegocio método Puntos");
            _apiResponse.uuid = uuid;

            try {
                
                if(_postgreSQLInterface == null) {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.InternalServerError;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada. Comunicate con el administrador del sistema.";
                } else {
                    _session = _postgreSQLInterface.dbConnection();
                    _session.Open();
                    _log.Add(uuid + " INFO - Crea conexión clase EmailNegocio método Puntos");
                    _transaction = _session.BeginTransaction();
                    _log.Add(uuid + " INFO - Crea transacción clase EmailNegocio método Puntos");

                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Envío exitoso.";

                    APIResponse responseSMTP = await new SysParametroNegocio(_log, _postgreSQLInterface).SysParametroEmail(uuid, id_usuario);

                    if(responseSMTP.respuesta){
                        if(responseSMTP.resultado != null){
                            SMTPModel smtpModel = (SMTPModel)responseSMTP.resultado;
                            smtpHost = smtpModel.host;
                            smtpPort = smtpModel.port;
                            smtpUser = smtpModel.user;
                            smtpPass = smtpModel.pass;

                            List<EmailPuntosModel> listaObservacion = [];
                            EmailPuntosModel emailObservacion = new();
                            string body = string.Empty;
                            foreach(var item in emailPuntosModel){
                                body = $@"<!DOCTYPE html>
                                    <html lang='es'>
                                        <head>
                                            <meta charset='utf-8'>
                                            <title>PREVÉN</title>
                                        </head>
                                        <body>
                                            <table style='max-width: 600px; padding: 10px; margin:0 auto; border-collapse: collapse;'>
                                                <tr style='background-color: #1D366C; height:10px;'>
                                                    <td>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td style='background-color: #ecf0f1; color: #5a5a5a;  font-size: 18px; font-family: sans-serif; font-weight:bold; padding: 10px 20px 40px 20px; text-align: center;'>
                                                        <h2 style='color: #1D366C; text-align: left;'>HOLA {item.nombre_completo}!</h2>
                                                        <p></p>
                                                        <p>Se han abonado {item.puntos} puntos a su cuenta de PREVÉN, hoy en día tiene un total de {item.total} puntos.</p>
                                                    </td>
                                                </tr>
                                                <tr style='background-color: #1D366C'>
                                                    <td>
                                                        <p style='color:white; text-align: center; margin: 30px 0px 30px 0px; font-size: 15px; font-weight:bold;'>&copy; PREVÉN</p>
                                                    </td>
                                                </tr>
                                            </table>
                                        </body>
                                    </html>";

                                bool resp = await envioEmail(uuid, item.email!, "Abono de puntos PREVÉN", body);

                                emailObservacion = new() { email = item.email, puntos = null, total = null, nombre_completo = null};

                                if (resp)
                                    emailObservacion.observacion = "Envío realizado correctamente"; 
                                else 
                                    emailObservacion.observacion = "Ocurrio un error al enviar el correo. Contacte al administrador del sistema";
                                
                                listaObservacion.Add(emailObservacion);
                            }

                            _apiResponse.resultado = listaObservacion;
                        } else { 
                            _apiResponse.respuesta = false;
                            _apiResponse.statusCode = HttpStatusCode.NotFound;
                            _apiResponse.mensaje = "No se encontraron registros.";
                        }
                    } else { //Entro catch del DAO
                        _apiResponse.respuesta = false;
                        _apiResponse.statusCode = HttpStatusCode.InternalServerError;
                        _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                    }
                }
            } catch (Exception ex) {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.InternalServerError;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase EmailNegocio método Puntos: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase EmailNegocio método Puntos: " + ex.ToString());

                if (_session != null) {
                    if(_transaction != null) {
                        _transaction.Rollback();
                        _log.Add(uuid + " INFO - Rollback clase EmailNegocio método Puntos");
                        _session.Close();
                        _log.Add(uuid + " INFO - Cierra conexión clase EmailNegocio método Puntos");
                    }
                }
            } finally {
                if (_session != null) {
                    _session.Dispose();
                    _session.Close();
                    _log.Add(uuid + " INFO - Finally Cierra conexión clase EmailNegocio método Puntos");
                }
            }

            return _apiResponse;
        }

        private async Task<bool> envioEmail(Guid uuid, string recipientEmail, string subject, string body)
        {
            bool respuesta = true;

            try {
                SmtpClient mailClient = new SmtpClient(smtpHost, smtpPort);
                MailMessage mailMessage = new MailMessage(smtpUser, recipientEmail, subject, body);
                mailMessage.IsBodyHtml = true;
                NetworkCredential mailAuthentication = new NetworkCredential(smtpUser, smtpPass);
                mailClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                mailClient.EnableSsl = false;
                mailClient.UseDefaultCredentials = false;

                mailClient.Credentials = mailAuthentication;
                await mailClient.SendMailAsync(mailMessage);
                _log.Add(uuid + $@" INFO - Email enviado a: {recipientEmail}");

            } catch (Exception ex) {
                _log.Add(uuid + " ERROR - Excepción en clase EmailNegocio método envioEmail: " + ex.ToString());
                respuesta = false;
            }

            return respuesta;
    
        }
    }
}