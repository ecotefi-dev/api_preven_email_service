using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using api_preven_email_service.DAO;
using api_preven_email_service.Helper;
using api_preven_email_service.Model.Email;
using api_preven_email_service.Model.Empresa;
using api_preven_email_service.Negocio.Agente;
using api_preven_email_service.Negocio.Empresa;
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

                    APIResponse responseSMTP = await new EmpresaParametroNegocio(_log, _postgreSQLInterface).EmpresaParametroEmail(uuid, id_usuario);

                    if(responseSMTP.respuesta){
                        if(responseSMTP.resultado != null){
                            SMTPModel smtpModel = (SMTPModel)responseSMTP.resultado;
                            smtpHost = smtpModel.host;
                            smtpPort = smtpModel.port;
                            smtpUser = smtpModel.user;
                            smtpPass = smtpModel.pass;

                            APIResponse responseNP = await new EmpresaParametroNegocio(_log, _postgreSQLInterface).EmpresaParametroLista(uuid, id_usuario, 0, "NOTIFICA_PUNTOS", true);
                            if(responseNP.respuesta){
                                List<EmpresaParametroModel> sysParNP = (List<EmpresaParametroModel>)responseNP.resultado;
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
                texto.AppendLine($@"Te informamos que hemos agregado puntos adicionales al usuario: {emailPuntosModel.nombre_completo} con número identificador {emailPuntosModel.id_agente} 
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
                        <title>PREVÉN | Tu socio de seguros</title>
                        <style>
                             body {{ font-family: roboto, ""helvetica neue"", helvetica, arial, sans-serif; background-color: #f4f4f4; }}

                            .titulo {{ padding: 20px 0px 20px 0px ; background-color: #ffffff; font-size: 30px; text-align: center; font-weight: bold; }}
                            .tituloDos {{ background-color: #f4f4f4; font-size: 13px; text-align: center; }}
                            .containerDos {{ width: 100%; max-width: 600px; margin: auto; background-color: #ffffff; padding: 20px 0px 5px 0px; font-size: 20px; }}

                            .container {{ width: 100%; max-width: 600px; margin: auto; padding: 15px 0px 5px 0px; }}
                            .content {{ padding: 20px; background-color: #ffffff; text-align: justify;}}

                            .footer {{ width: 100%; max-width: 600px; margin: auto; background-color: #ffffff; padding: 5px 0px 5px 0px; text-align: center; font-size: 11px; }}
                        </style>
                    </head>
                    <body>
                        <div class=""titulo"">
                            <p>ACTUALIZACIÓN DE PUNTOS</p>
                        </div>
                        <div class=""tituloDos"">
                            <div class=""containerDos"">
                                <p>PREVÉN | Tu socio de seguros</p>
                                <div class=""tituloDos"" style=""padding-top: 15px;""></div>
                                <div class=""content"">
                                    <p style=""font-size:15.5px"">{nombre}</p>
                                    <p style=""font-size:15.5px"">{texto}</p>
                                    <table class=""container"" style=""border: 3px solid black; color: rgb(53, 53, 53); border-spacing: 0 !important; border-collapse: collapse !important; font-size:12px;"">
                                        <tr>
                                            <td style='text-align: left; padding-top: 20px;'>
                                                Puntos anteriores:
                                            </td>
                                            <td style='text-align: right; padding-top: 20px;'>
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
                                </div>
                            </div>
                            <div class=""footer"">
                                <b><p>&copy; PREVÉN</p></b>
                            </div>
                            <div class=""tituloDos"" style=""padding-top: 15px;""></div>
                        </div>
                    </body>
                </html>";

            return body;
        }
    }
}