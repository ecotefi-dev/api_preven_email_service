using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using api_preven_email_service.DAO;
using api_preven_email_service.Helper;
using api_preven_email_service.Model.Email;
using api_preven_email_service.Model.SysParametro;
using api_preven_email_service.Negocio.Agente;
using api_preven_email_service.Negocio.SysParametro;
using Microsoft.IdentityModel.Tokens;
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
        private string email_notifica_pedido = string.Empty;

        public EmailNegocio(LoggerService log, PostgreSQLInterface postgreSQLInterface){
            _log = log;
            _postgreSQLInterface = postgreSQLInterface;
            _apiResponse = new APIResponse();
        }

        public async Task<APIResponse>Puntos(Guid uuid, int id_usuario, List<EmailPuntosModel> listaEmailPuntosModel)
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

                            APIResponse responseNP = await new SysParametroNegocio(_log, _postgreSQLInterface).SysParametroLista(uuid, id_usuario, 0, "NOTIFICA_PUNTOS", true);
                            if(responseNP.respuesta){
                                List<SysParametroModel> sysParNP = (List<SysParametroModel>)responseNP.resultado;
                                email_notifica_pedido = sysParNP[0].valor;
                            }

                            List<EmailPuntosModel> listaObservacion = [];
                            EmailPuntosModel emailObservacion = new();

                            foreach(EmailPuntosModel item in listaEmailPuntosModel){
                                emailObservacion = new()
                                {
                                    id_agente = item.id_agente,
                                    puntos = item.puntos
                                };
                                APIResponse infoAgente = await new AgenteEmailNegocio(_log, _postgreSQLInterface).AgenteEmailConsulta(uuid, id_usuario, item.id_agente);
                                if(infoAgente.respuesta) {
                                    EmailPuntosModel infoAgenteEmail = (EmailPuntosModel)infoAgente.resultado;
                                    _log.Add(uuid + " INFO - EmailPuntosModel: " + _log.ConvertirModeloATexto(infoAgenteEmail));
                                    emailObservacion.email = item.email;
                                    infoAgenteEmail.puntos = item.puntos;
                                    string body = EmailBody(infoAgenteEmail, true);
                                    bool respuesta = await envioEmail(uuid, infoAgenteEmail.email!, "Actualización de Puntos PREVÉN", body);
                                    if(respuesta)
                                        emailObservacion.observacion = "Correo enviado exitosamente al email: " + infoAgenteEmail.email;
                                    else 
                                        emailObservacion.observacion = "Ocurrio un error al enviar el correo al email: " + infoAgenteEmail.email;

                                    if(!email_notifica_pedido.IsNullOrEmpty()){
                                        body = EmailBody(infoAgenteEmail, false);
                                        respuesta = await envioEmail(uuid, email_notifica_pedido, "Actualización de Puntos PREVÉN", body);

                                        if(respuesta)
                                            emailObservacion.observacion += " | Correo enviado exitosamente al email: " + email_notifica_pedido;
                                        else 
                                            emailObservacion.observacion += " | Ocurrio un error al enviar el correo al email: " + email_notifica_pedido;
                                    }
                                } else {
                                    emailObservacion.email = item.email;
                                    emailObservacion.observacion = "No se encontro el agente.";
                                }

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

        private string EmailBody(EmailPuntosModel emailPuntosModel, bool agente){
            StringBuilder nombre = new();
            if(agente)
                nombre.AppendLine($@"Hola {emailPuntosModel.nombres}:");

            StringBuilder texto = new();
            if(agente)
                texto.AppendLine($@"Te informamos que hemos agregado puntos adicionales a tu usuario en el Portal de Puntos PREVÉN. Quedando de la siguiente manera:");
            else
                texto.AppendLine($@"Te informamos que hemos agregado puntos adicionales al usuario: {emailPuntosModel.nombre_completo} con código identificador número {emailPuntosModel.id_agente} 
                    en el Portal de Puntos PREVÉN. Quedando de la siguiente manera:");

            int puntos_anteriores = (int)emailPuntosModel.saldo_puntos! - emailPuntosModel.puntos;
            int puntos_acumulados = emailPuntosModel.puntos;
            int puntos_totales = (int)emailPuntosModel.saldo_puntos!;

            string body = string.Empty;
            body = $@"
                <!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
                <html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:o=""urn:schemas-microsoft-com:office:office"">
                    <head>
                        <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <title>Correo Electrónico</title>
                        <style>
                            body, table, td {{ font-family: Arial, sans-serif; }}
                            .container {{ width: 100%; max-width: 600px; margin: auto; padding: 20px; background-color: #f4f4f4; }}
                            .header {{ background-color: #1D366C; padding: 10px; color: white; text-align: center; }}
                            .content {{ padding: 20px; background-color: #ffffff; font-size: 13px; }}
                            .footer {{  background-color: #1D366C; padding: 10px; text-align: center; font-size: 11px; color: white; }}
                        </style>
                    </head>
                    <body>
                        <table class=""container"">
                            <tr>
                                <td class=""header"">
                                    <h1>ACTUALIZACIÓN DE PUNTOS</h1>
                                </td>
                            </tr>
                            <tr>
                                <td class=""content"">
                                    <h2 style='color: #1D366C; text-align: center;'>PREVÉN | Tu socio de seguros</h2>
                                    <p>{nombre}</p>
                                    <p>{texto}</p>
                                    <table class=""container"">
                                        <tr>
                                            <td style='text-align: left;'>
                                                Puntos anteriores:
                                            </td>
                                            <td style='text-align: right;'>
                                                {puntos_anteriores.ToString("N2", new CultureInfo("es-MX"))}
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='text-align: left;'>
                                                Puntos acumulados:
                                            </td>
                                            <td style='text-align: right;'>
                                                {puntos_acumulados.ToString("N2", new CultureInfo("es-MX"))}
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='text-align: left;'>
                                                <b>Puntos totales:</b>
                                            </td>
                                            <td style='text-align: right;'>
                                                <b>{puntos_totales.ToString("N2", new CultureInfo("es-MX"))}</b>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <tr> 
                                <td class=""footer"">
                                    <b><p>&copy; PREVÉN</p></b>
                                </td>
                            </tr>
                        </table>
                    </body>
                </html>";

            return body;
        }
    }
}