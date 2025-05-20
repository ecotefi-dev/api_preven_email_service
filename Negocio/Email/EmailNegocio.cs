using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
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
                                    string body = EmailBody(infoAgenteEmail);
                                    _log.Add(body);
                                    bool respuesta = await envioEmail(uuid, infoAgenteEmail.email!, "Actualización de Puntos PREVÉN", body);
                                    if(respuesta)
                                        emailObservacion.observacion = "Correo enviado exitosamente al email: " + infoAgenteEmail.email;
                                    else 
                                        emailObservacion.observacion = "Ocurrio un error al enviar el correo al email: " + infoAgenteEmail.email;
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
            _log.Add(uuid + " INFO - clase EmailNegocio método envioEmail");
            bool respuesta = true;

            try {
                //recipientEmail = "hugo.glz.mora@gmail.com";
                SmtpClient mailClient = new(smtpHost, smtpPort);
                MailMessage mailMessage = new(smtpUser, recipientEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                if(!email_notifica_pedido!.IsNullOrEmpty())
                    mailMessage.CC.Add(email_notifica_pedido);

                NetworkCredential mailAuthentication = new(smtpUser, smtpPass);
                mailClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                mailClient.EnableSsl = false;
                mailClient.UseDefaultCredentials = false;
                mailClient.Credentials = mailAuthentication;

                // Preparar lista de recursos
                List<LinkedResource> linkedResources = [];
                string htmlBody = body;

                // Imagen encabezado
                string imagePath = Path.Combine(AppContext.BaseDirectory, "images", "encabezado_1.png");
                if (File.Exists(imagePath))
                {
                    string contentId = Guid.NewGuid().ToString();
                    htmlBody = htmlBody.Replace("ENCABEZADO_IMG", contentId);

                    LinkedResource inlineImage = new(imagePath, MediaTypeNames.Image.Jpeg)
                    {
                        ContentId = contentId,
                        TransferEncoding = TransferEncoding.Base64,
                        ContentType = new ContentType(MediaTypeNames.Image.Jpeg)
                    };
                    linkedResources.Add(inlineImage);
                }
                else
                {
                    _log.Add($"ERROR - Imagen no encontrada: {imagePath}");
                }

                string imagePathFacebook = Path.Combine(AppContext.BaseDirectory, "images", "ic_facebook.png");
                if (File.Exists(imagePathFacebook))
                {
                    string contentId = Guid.NewGuid().ToString();
                    htmlBody = htmlBody.Replace("FACEBOOK_IMG", contentId);

                    LinkedResource inlineImage = new(imagePathFacebook, MediaTypeNames.Image.Jpeg)
                    {
                        ContentId = contentId,
                        TransferEncoding = TransferEncoding.Base64,
                        ContentType = new ContentType(MediaTypeNames.Image.Jpeg)
                    };
                    linkedResources.Add(inlineImage);
                }
                else
                {
                    _log.Add($"ERROR - Imagen no encontrada: {imagePathFacebook}");
                }

                string imagePathInstagram = Path.Combine(AppContext.BaseDirectory, "images", "ic_instagram.png");
                if (File.Exists(imagePathInstagram))
                {
                    string contentId = Guid.NewGuid().ToString();
                    htmlBody = htmlBody.Replace("INSTAGRAM_IMG", contentId);

                    LinkedResource inlineImage = new(imagePathInstagram, MediaTypeNames.Image.Jpeg)
                    {
                        ContentId = contentId,
                        TransferEncoding = TransferEncoding.Base64,
                        ContentType = new ContentType(MediaTypeNames.Image.Jpeg)
                    };
                    linkedResources.Add(inlineImage);
                }
                else
                {
                    _log.Add($"ERROR - Imagen no encontrada: {imagePathInstagram}");
                }

                // Crear una sola vista HTML
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
                foreach (var resource in linkedResources)
                {
                    htmlView.LinkedResources.Add(resource);
                }

                mailMessage.AlternateViews.Add(htmlView);

                await mailClient.SendMailAsync(mailMessage);
                _log.Add(uuid + $@" INFO - Email enviado a: {recipientEmail}");

            } catch (Exception ex) {
                _log.Add(uuid + " ERROR - Excepción en clase EmailNegocio método envioEmail: " + ex.ToString());
                respuesta = false;
            }

            return respuesta;
        }
        private string EmailBody(EmailPuntosModel emailPuntosModel){
            StringBuilder leyenda = new();
            StringBuilder texto = new();
            StringBuilder descripcionPuntos = new();
            
            if(emailPuntosModel.puntos > 0) {
                leyenda.AppendLine($@"Hemos agregado puntos a tu usuario.");
                texto.AppendLine($@"Te informamos que hemos agregado puntos adicionales a tu usuario en el Portal de Puntos PREVÉN.<br><br>Podrás consultar tus puntos iniciando sesión en <a href=""https://www.preven.mx/puntos"" target=""_blank"" style=""color:#4CB5F5; text-decoration: none;"">www.preven.mx/puntos</a> y buscar productos de tu interés en nuestro catálogo.");
                descripcionPuntos.AppendLine($@"Puntos acumulados.");
            } else {
                leyenda.AppendLine($@"Hemos ajustado puntos a tu usuario.");
                texto.AppendLine($@"Te informamos que hemos ajustado puntos a tu usuario en el Portal de Puntos PREVÉN.<br><br>Podrás consultar tus puntos iniciando sesión en <a href=""https://www.preven.mx/puntos"" target=""_blank"" style=""color:#4CB5F5; text-decoration: none;"">www.preven.mx/puntos</a> y buscar productos de tu interés en nuestro catálogo.");
                descripcionPuntos.AppendLine($@"Puntos ajustados.");
            }
            
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
                            @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@400;700&display=swap');
                            body {{
                                font-family: 'Montserrat', 'Segoe UI', sans-serif;
                                background-color: white;
                                font-size: 15px;
                                color: #646464;
                                text-align: justify;
                                width: 100%;
                                margin: auto;
                                padding-top: 10px;
                            }}

                            .titulo {{
                                font-size: 30px;
                                text-align: center;
                                font-weight: bold;
                                color: #071f55;
                                background-color: white;
                            }}

                            .container {{
                                border: 2px solid #1b2a4e; 
                                box-sizing: border-box;
                                width: 100%;
                                max-width: 800px;
                                margin: auto;
                            }}

                            .firstcontent {{
                                width: 90%;
                                margin: auto;
                                background-color: white;
                            }}

                            .secondcontent {{
                                width: 80%;
                                margin: auto;
                                background-color: white;
                                color: #646464;
                            }}

                            .footer {{
                                text-align: center;
                                color: #1b2a4e;
                                font-weight: bold;
                                font-size: 18px;
                                background-color: white;
                            }}
                        </style>
                    </head>
                    <body style=""background-color: white;"">
                        <div class=""container"">
                            <div style=""text-align: left; padding: 0; margin: 0; background-color: white;"">
                                <img src=""cid:ENCABEZADO_IMG"" alt=""Encabezado"" style=""display: block; width: 100%; max-width:750px; margin: 0; padding: 0;"">
                            </div>
                            <div class=""titulo"">
                                <p>ACTUALIZACIÓN DE PUNTOS</p>
                            </div>
                            <div class=""firstcontent"">
                                <p style=""color: #646464; font-size: 16px;"">Buen día {emailPuntosModel.nombres}</p>
                                <p style=""font-weight: bold; font-size: 16px; text-align: center; color: #646464;"">{leyenda}</p>
                                <div class=""secondcontent"">
                                    <p style=""text-align: center; color: #646464; font-size: 16px;"">{texto}</p>
                                </div>
                                <div style=""width: 100%;"">
                                    <table cellpadding=""0"" cellspacing=""0"" 
                                        style=""width: 80%; margin: auto; min-width: 300px; border-collapse: collapse; background-color: white; border: solid; border-color: #1b2a4e;"">
                                        <tbody>
                                            <tr>
                                                <td style=""padding: 20px 0px 0px 3em; font-size: 16px; color: #646464;"">Puntos anteriores</td>
                                                <td style=""padding: 20px 3em 0px 0px; text-align: right; font-size: 16px; color: #646464;"">{puntos_anteriores.ToString("N0", new CultureInfo("es-MX"))}</td>
                                            </tr>
                                            <tr>
                                                <td style=""padding: 5px 0px 20px 3em; font-size: 16px; color: #646464;"">{descripcionPuntos}</td>
                                                <td style=""padding: 5px 3em 20px 0px; text-align: right; font-size: 16px; color: #646464;"">{puntos_acumulados.ToString("N0", new CultureInfo("es-MX"))}</td>
                                            </tr>
                                        </tbody>
                                        <tfoot>
                                            <tr style=""background-color: #1b2a4e; color: white;"">
                                                <td style=""padding: 0px 0px 0px 3em; font-weight: bold; font-size: 16px;"">PUNTOS TOTALES</td>
                                                <td style=""padding: 0px 3em 0px 0px; text-align: right; font-size: 16px;"">{puntos_totales.ToString("N0", new CultureInfo("es-MX"))}</td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                </div>
                            </div>
                            <div class=""footer"">
                                <p style=""text-align: center; font-weight: bold; color: #071f55;"">PREVÉN | Tu socio de seguros</p>
                                <table style=""width: 100%; border-collapse: collapse;"">
                                    <tr>
                                        <td style=""padding: 0px 0px 10px 20px; font-size: 13px; text-align: left; font-weight: bold;"">
                                            <a href=""https://preven.mx"" target=""_blank"" style=""text-decoration: none !important;"">
                                            <span style=""border-bottom: none; color: #1b2a4e;"">www.preven.mx</span>
                                            </a>
                                        </td>
                                        <td style=""padding: 0px 20px 10px 0px; font-size: 13px; text-align: right; font-weight: bold;"">
                                            <span style=""vertical-align: middle;"">prevenmx</span>
                                            &nbsp;
                                            <a href=""https://www.facebook.com/prevenmx"" target=""_blank"" style=""text-decoration: none !important;"">
                                            <img src=""cid:FACEBOOK_IMG"" alt=""Facebook"" width=""24px"" height=""24px"" style=""vertical-align: middle;"">
                                            </a>
                                            <a href=""https://www.instagram.com/prevenmx"" target=""_blank"" style=""text-decoration: none !important;"">
                                            <img src=""cid:INSTAGRAM_IMG"" alt=""Instagram"" width=""24px"" height=""24px"" style=""vertical-align: middle;"">
                                            </a>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </div>
                    </body>
                </html>";

            return body;
        }
    }
}