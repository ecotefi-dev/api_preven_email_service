using System.Globalization;
using System.Net;
using System.Text;

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Identity.Client;

using api_preven_email_service.DAO;
using api_preven_email_service.Helper;
using api_preven_email_service.Model.Email;
using api_preven_email_service.Model.Empresa;
using api_preven_email_service.Negocio.Agente;
using api_preven_email_service.Negocio.Empresa;

namespace api_preven_email_service.Negocio.Email{
    public class EmailNegocio
    {
        private readonly LoggerService _log;
        protected APIResponse _apiResponse;
        private readonly PostgreSQLInterface _postgreSQLInterface;
        
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
                    return _apiResponse;
                } else {
                    
                    // ==========================================================
                    // CONFIGURACIÓN MICROSOFT GRAPH
                    // ==========================================================
                    EmailConfiguracionModel configuracionEmail = await ObtenerConfiguracionEmail(uuid, id_usuario);

                    if (string.IsNullOrWhiteSpace(configuracionEmail.notifica_puntos))
                    {
                        throw new Exception(
                            "No se encuentra configurado NOTIFICA_PUNTOS."
                        );
                    }

                    List<EmailPuntosModel> listaObservacion = [];

                    // ==========================================================
                    // PROCESAR CORREOS
                    // ==========================================================

                    foreach(EmailPuntosModel item in listaEmailPuntosModel){
                        EmailPuntosModel emailObservacion = new()
                        {
                            email = item.email,
                            puntos = item.puntos
                        };
                        APIResponse infoAgente = await new AgenteEmailNegocio(_log, _postgreSQLInterface).AgenteEmailConsulta(uuid, id_usuario, item.id_agente, item.email!);
                        if(infoAgente.respuesta) {
                            EmailPuntosModel infoAgenteEmail = (EmailPuntosModel)infoAgente.resultado;
                            _log.Add(uuid + " INFO - EmailPuntosModel: " + _log.ConvertirModeloATexto(infoAgenteEmail));
                            emailObservacion.id_agente = infoAgenteEmail.id_agente;
                            infoAgenteEmail.puntos = item.puntos;
                            string body = EmailBody(infoAgenteEmail);
                            bool respuesta = await envioEmail(uuid, configuracionEmail, configuracionEmail.notifica_puntos, infoAgenteEmail.email!, "PREVÉN - Actualización de puntos", body, true);
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
                    
                    _apiResponse.respuesta = true;
                    _apiResponse.statusCode = HttpStatusCode.OK;
                    _apiResponse.mensaje = "Proceso de envío finalizado.";
                    _apiResponse.resultado = listaObservacion;
                }
            } catch (Exception ex) {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.InternalServerError;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepción en clase EmailNegocio método Puntos: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepción en clase EmailNegocio método Puntos: " + ex.ToString());  
            } 

            return _apiResponse;
        }
        private async Task<bool> envioEmail(Guid uuid, EmailConfiguracionModel configuracion, string senderEmail, string recipientEmail, string subject, string body, bool incluirImagenes = true)
        {
            _log.Add(uuid + " INFO - clase EmailNegocio método envioEmail");
            bool respuesta = true;

            try {
                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    throw new Exception(
                        "El correo remitente no se encuentra configurado."
                    );
                }

                // ==========================================================
                // MICROSOFT ENTRA ID
                // ==========================================================

                IConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder
                        .Create(configuracion.client_id)
                        .WithClientSecret(
                            configuracion.client_secret
                        )
                        .WithAuthority(
                            $"https://login.microsoftonline.com/" +
                            $"{configuracion.tenant_id}"
                        )
                        .Build();

                string[] scopes =
                [
                    "https://graph.microsoft.com/.default"
                ];

                AuthenticationResult authResult =
                    await app
                        .AcquireTokenForClient(scopes)
                        .ExecuteAsync();

                _log.Add(uuid + " INFO - Token Microsoft Graph obtenido correctamente");

                // ==========================================================
                // HTML
                // ==========================================================

                string htmlBody = body;
                List<object> attachments = [];

                // ==========================================================
                // IMÁGENES INLINE
                // ==========================================================

                if (incluirImagenes)
                {
                    // ------------------------------------------------------
                    // ENCABEZADO
                    // ------------------------------------------------------

                    string imagePath = Path.Combine(AppContext.BaseDirectory, "images", "encabezado_1.png");

                    if (File.Exists(imagePath))
                    {
                        string contentId = Guid.NewGuid().ToString();

                        htmlBody = htmlBody.Replace("ENCABEZADO_IMG", contentId);

                        attachments.Add(
                            new
                            {
                                odata_type = "#microsoft.graph.fileAttachment",
                                name = "encabezado_1.png",
                                contentType = "image/png",
                                contentId = contentId,
                                isInline = true,
                                contentBytes = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath) )
                            }
                        );
                    }
                    else
                    {
                        _log.Add(uuid + $" ERROR - Imagen no encontrada: {imagePath}");
                    }


                    // ------------------------------------------------------
                    // FACEBOOK
                    // ------------------------------------------------------

                    string imagePathFacebook = Path.Combine(AppContext.BaseDirectory, "images", "ic_facebook.png");

                    if (File.Exists(imagePathFacebook))
                    {
                        string contentId = Guid.NewGuid().ToString();
                        htmlBody = htmlBody.Replace("FACEBOOK_IMG", contentId);

                        attachments.Add(
                            new
                            {
                                odata_type = "#microsoft.graph.fileAttachment",
                                name = "ic_facebook.png",
                                contentType = "image/png",
                                contentId = contentId,
                                isInline = true,
                                contentBytes = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePathFacebook))
                            }
                        );
                    }
                    else
                    {
                        _log.Add(uuid + $" ERROR - Imagen no encontrada: {imagePathFacebook}");
                    }


                    // ------------------------------------------------------
                    // INSTAGRAM
                    // ------------------------------------------------------

                    string imagePathInstagram = Path.Combine(AppContext.BaseDirectory, "images", "ic_instagram.png");

                    if (File.Exists(imagePathInstagram))
                    {
                        string contentId = Guid.NewGuid().ToString();

                        htmlBody = htmlBody.Replace("INSTAGRAM_IMG", contentId);

                        attachments.Add(
                            new
                            {
                                odata_type = "#microsoft.graph.fileAttachment",
                                name = "ic_instagram.png",
                                contentType = "image/png",
                                contentId = contentId,
                                isInline = true,
                                contentBytes = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePathInstagram))
                            }
                        );
                    }
                    else
                    {
                        _log.Add(uuid + $" ERROR - Imagen no encontrada: {imagePathInstagram}");
                    }
                }

                // ==========================================================
                // MENSAJE
                // ==========================================================

                var email =
                    new
                    {
                        message =
                            new
                            {
                                subject = subject,
                                body = new { contentType = "HTML", content = htmlBody },
                                toRecipients = new[] { new { emailAddress = new { address = recipientEmail } } },
                                // El remitente recibe una copia.
                                ccRecipients = new[] { new { emailAddress = new { address = senderEmail } } },
                                attachments = attachments
                            },
                        saveToSentItems = true
                    };


                JsonSerializerOptions options = new() {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
                string json = JsonSerializer.Serialize(email, options);


                // Transformar odata_type en @odata.type
                json = json.Replace("\"odata_type\"", "\"@odata.type\"");

                // ==========================================================
                // MICROSOFT GRAPH
                // ==========================================================

                using HttpClient httpClient = new();

                httpClient
                    .DefaultRequestHeaders
                    .Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                using StringContent content = new(json, Encoding.UTF8, "application/json");

                string url = "https://graph.microsoft.com/v1.0/users/" + Uri.EscapeDataString(senderEmail) + "/sendMail";

                _log.Add(
                    uuid +
                    $" INFO - Enviando correo mediante Microsoft Graph. " +
                    $"FROM: {senderEmail} - " +
                    $"TO: {recipientEmail} - " +
                    $"CC: {senderEmail}"
                );

                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();

                    _log.Add(
                        uuid +
                        " ERROR - Microsoft Graph - " +
                        $"HTTP {(int)response.StatusCode} " +
                        $"{response.StatusCode} - " +
                        error
                    );

                    return false;
                }


                _log.Add(uuid + " INFO - Microsoft Graph aceptó correctamente el correo.");
                _log.Add(uuid + $@" INFO - Email enviado a: {recipientEmail}");

                return true;
                
            } 
            catch (MsalServiceException ex)
            {
                _log.Add(uuid + $" ERROR - Microsoft Entra ID / MSAL - ErrorCode: {ex.ErrorCode} - StatusCode: {ex.StatusCode} - " + ex);
                return false;
            }
            catch (Exception ex)
            {
                _log.Add(uuid + " ERROR - Excepción en clase EmailNegocio método EnvioEmail: " + ex);
                return false;
            }
        }
        private string EmailBody(EmailPuntosModel emailPuntosModel){
            StringBuilder leyenda = new();
            StringBuilder texto = new();
            StringBuilder descripcionPuntos = new();
            
            if(emailPuntosModel.puntos > 0) {
                leyenda.AppendLine($@"Hemos agregado puntos a tu usuario.");
                texto.AppendLine($@"Te informamos que hemos agregado puntos adicionales a tu usuario en el Portal de Puntos PREVÉN.<br><br>Podrás consultar tus puntos iniciando sesión en <a href=""https://puntos.preven.mx/"" target=""_blank"" style=""color:#4CB5F5; text-decoration: none;"">puntos.preven.mx</a> y buscar productos de tu interés en nuestro catálogo.");
                descripcionPuntos.AppendLine($@"Puntos acumulados.");
            } else {
                leyenda.AppendLine($@"Hemos ajustado puntos a tu usuario.");
                texto.AppendLine($@"Te informamos que hemos ajustado puntos a tu usuario en el Portal de Puntos PREVÉN.<br><br>Podrás consultar tus puntos iniciando sesión en <a href=""https://puntos.preven.mx/"" target=""_blank"" style=""color:#4CB5F5; text-decoration: none;"">puntos.preven.mx</a> y buscar productos de tu interés en nuestro catálogo.");
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
                    <body style=""background-color: white; width: 100%;"">
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; background-color: white;"">
                            <tr>
                                <td align=""center"">
                                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 800px; width: 100%; border: 2px solid #1b2a4e; box-sizing: border-box;"">
                                        <tr>
                                            <td style=""padding: 0; margin: 0; background-color: white; text-align: left;"">
                                                <img src=""cid:ENCABEZADO_IMG"" alt=""Encabezado"" width=""100%"" style=""display: block; width: 100%; max-width: 750px; margin: 0; padding: 0;"">
                                            </td>
                                        </tr>
                                        <tr>
                                            <td height=""70"" style=""text-align: center; font-size: 34px; font-weight: bold; color: #071f55; background-color: white;"">
                                                <p>ACTUALIZACIÓN DE PUNTOS</p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <table width=""80%"" align=""center"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 80%; background-color: white;"">
                                                    <tr>
                                                        <td style=""font-size: 16px; color: #646464; text-align: justify;"">
                                                            <p>Buen día {emailPuntosModel.nombres}</p>
                                                            <p style=""font-weight: bold; text-align: center;"">{leyenda}</p>
                                                            <p style=""text-align: center; color: #646464; font-size: 16px"">{texto}</p>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td align=""center"">
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
                                            </td>
                                        </tr>
                                        <tr>
                                            <td align=""center"">
                                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 800px; width: 100%; background-color: white;"">
                                                    <tr>
                                                        <td style=""text-align: center; font-weight: bold; font-size: 18px; color: #1b2a4e; padding-top: 20px;"">
                                                            PREVÉN | Tu socio de seguros
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <table style=""width: 100%; border-collapse: collapse;"">
                                                                <tr>
                                                                    <td style=""padding: 0px 0px 10px 20px; font-size: 13px; text-align: left; font-weight: bold;"">
                                                                        <a href=""https://preven.mx"" target=""_blank"" style=""text-decoration: none !important;"">
                                                                        <span style=""border-bottom: none; color: #1b2a4e;"">www.preven.mx</span>
                                                                        </a>
                                                                    </td>
                                                                    <td style=""padding: 0px 20px 10px 0px; font-size: 13px; text-align: right; font-weight: bold;"">
                                                                        <span style=""vertical-align: middle; color: #1b2a4e;"">prevenmx</span>
                                                                        &nbsp;
                                                                        <a href=""https://www.facebook.com/prevenmx"" target=""_blank"" style=""text-decoration: none !important;"">
                                                                        <img src=""cid:FACEBOOK_IMG"" alt=""Facebook"" width=""24"" height=""24"" style=""width: 24px; height: 24px; vertical-align: middle; border: 0;"">
                                                                        </a>
                                                                        <a href=""https://www.instagram.com/prevenmx"" target=""_blank"" style=""text-decoration: none !important;"">
                                                                        <img src=""cid:INSTAGRAM_IMG"" alt=""Instagram"" width=""24"" height=""24"" style=""width: 24px; height: 24px; vertical-align: middle; border: 0;"">
                                                                        </a>
                                                                    </td>
                                                                </tr>
                                                            </table>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </body>
                </html>";

            return body;
        }
        private async Task<EmailConfiguracionModel>ObtenerConfiguracionEmail(Guid uuid, int id_usuario)
        {
            APIResponse response = await new EmpresaParametroNegocio(_log, _postgreSQLInterface).EmpresaParametroEmail(uuid, id_usuario);

            if (!response.respuesta ||
                response.resultado == null)
            {
                throw new Exception(
                    "No fue posible obtener la configuración de Microsoft Exchange."
                );
            }

            EmailConfiguracionModel configuracion = (EmailConfiguracionModel)response.resultado;
            ValidarConfiguracionEmail(configuracion);
            return configuracion;
        }
        private void ValidarConfiguracionEmail(EmailConfiguracionModel configuracion)
        {
            if (string.IsNullOrWhiteSpace(configuracion.tenant_id))
                throw new Exception("No se encuentra configurado EMAIL_TENANT_ID.");

            if (string.IsNullOrWhiteSpace(configuracion.client_id))
                throw new Exception("No se encuentra configurado EMAIL_CLIENT_ID.");

            if (string.IsNullOrWhiteSpace(configuracion.client_secret))
                throw new Exception("No se encuentra configurado EMAIL_CLIENT_SECRET.");
        }
        public async Task<APIResponse> Prueba(Guid uuid, int id_usuario, string recipientEmail)
        {
            _log.Add(uuid + " INFO - Id Usuario: " + id_usuario + " - Ingresa clase EmailNegocio método Prueba");

            APIResponse response = new()
            {
                uuid = uuid
            };


            try
            {
                // Una sola consulta a empresa_parametro.
                EmailConfiguracionModel configuracion = await ObtenerConfiguracionEmail(uuid, id_usuario);

                if (string.IsNullOrWhiteSpace(configuracion.notifica_puntos))
                {
                    throw new Exception(
                        "No se encuentra configurado NOTIFICA_PUNTOS."
                    );
                }


                string subject =
                    "PREVÉN - Prueba de envío de correo";


                string body = $@"
                <!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN""
                    ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">

                <html xmlns=""http://www.w3.org/1999/xhtml"">
                <head>
                    <meta http-equiv=""Content-Type""
                        content=""text/html; charset=utf-8"">

                    <meta name=""viewport""
                        content=""width=device-width, initial-scale=1.0"">

                    <title>PREVÉN | Prueba de correo</title>
                </head>

                <body style=""
                    margin: 0;
                    padding: 10px;
                    background-color: #ffffff;
                    font-family: Arial, Helvetica, sans-serif;
                    color: #646464;
                "">

                    <table width=""100%""
                        cellpadding=""0""
                        cellspacing=""0""
                        border=""0""
                        style=""background-color: #ffffff;"">

                        <tr>
                            <td align=""center"">

                                <!-- ========================================= -->
                                <!-- CONTENEDOR PRINCIPAL -->
                                <!-- ========================================= -->

                                <table width=""100%""
                                    cellpadding=""0""
                                    cellspacing=""0""
                                    border=""0""
                                    style=""
                                        max-width: 800px;
                                        width: 100%;
                                        border: 2px solid #1b2a4e;
                                        background-color: #ffffff;
                                    "">


                                    <!-- ===================================== -->
                                    <!-- ENCABEZADO -->
                                    <!-- ===================================== -->

                                    <tr>
                                        <td style=""
                                            padding: 0;
                                            margin: 0;
                                            text-align: left;
                                            background-color: #ffffff;
                                        "">

                                            <img
                                                src=""cid:ENCABEZADO_IMG""
                                                alt=""PREVÉN - Portal de Puntos""
                                                width=""100%""
                                                style=""
                                                    display: block;
                                                    width: 100%;
                                                    max-width: 750px;
                                                    margin: 0;
                                                    padding: 0;
                                                    border: 0;
                                                "">

                                        </td>
                                    </tr>


                                    <!-- ===================================== -->
                                    <!-- TÍTULO -->
                                    <!-- ===================================== -->

                                    <tr>
                                        <td style=""
                                            padding: 35px 20px 15px 20px;
                                            text-align: center;
                                            font-size: 32px;
                                            font-weight: bold;
                                            color: #071f55;
                                        "">

                                            PRUEBA DE ENVÍO DE CORREO

                                        </td>
                                    </tr>


                                    <!-- ===================================== -->
                                    <!-- CONTENIDO -->
                                    <!-- ===================================== -->

                                    <tr>
                                        <td align=""center"">

                                            <table width=""80%""
                                                cellpadding=""0""
                                                cellspacing=""0""
                                                border=""0"">

                                                <tr>
                                                    <td style=""
                                                        padding: 20px 0;
                                                        font-size: 16px;
                                                        line-height: 24px;
                                                        color: #646464;
                                                        text-align: center;
                                                    "">

                                                        <p>
                                                            Este es un correo de prueba
                                                            generado desde el servicio
                                                            de correo de PREVÉN.
                                                        </p>

                                                        <p style=""
                                                            font-weight: bold;
                                                            color: #071f55;
                                                        "">
                                                            Microsoft Graph está
                                                            funcionando correctamente.
                                                        </p>

                                                        <p>
                                                            Esta prueba también valida
                                                            la visualización de las
                                                            imágenes incorporadas en
                                                            el correo.
                                                        </p>

                                                    </td>
                                                </tr>

                                            </table>

                                        </td>
                                    </tr>


                                    <!-- ===================================== -->
                                    <!-- INFORMACIÓN DE LA PRUEBA -->
                                    <!-- ===================================== -->

                                    <tr>
                                        <td align=""center"">

                                            <table width=""80%""
                                                cellpadding=""0""
                                                cellspacing=""0""
                                                border=""0""
                                                style=""
                                                    border: 2px solid #1b2a4e;
                                                    border-collapse: collapse;
                                                "">

                                                <tr>
                                                    <td style=""
                                                        padding: 15px 25px;
                                                        font-size: 15px;
                                                    "">
                                                        Fecha de prueba
                                                    </td>

                                                    <td style=""
                                                        padding: 15px 25px;
                                                        text-align: right;
                                                        font-size: 15px;
                                                    "">
                                                        {DateTime.Now:dd/MM/yyyy HH:mm:ss}
                                                    </td>
                                                </tr>

                                                <tr style=""
                                                    background-color: #1b2a4e;
                                                    color: #ffffff;
                                                "">

                                                    <td style=""
                                                        padding: 12px 25px;
                                                        font-weight: bold;
                                                    "">
                                                        RESULTADO
                                                    </td>

                                                    <td style=""
                                                        padding: 12px 25px;
                                                        text-align: right;
                                                        font-weight: bold;
                                                    "">
                                                        PRUEBA DE IMÁGENES
                                                    </td>

                                                </tr>

                                            </table>

                                        </td>
                                    </tr>


                                    <!-- ===================================== -->
                                    <!-- FOOTER -->
                                    <!-- ===================================== -->

                                    <tr>
                                        <td style=""
                                            padding: 30px 20px 10px 20px;
                                            text-align: center;
                                            color: #1b2a4e;
                                            font-weight: bold;
                                            font-size: 18px;
                                        "">

                                            PREVÉN | Tu socio de seguros

                                        </td>
                                    </tr>


                                    <!-- ===================================== -->
                                    <!-- REDES SOCIALES -->
                                    <!-- ===================================== -->

                                    <tr>
                                        <td>

                                            <table width=""100%""
                                                cellpadding=""0""
                                                cellspacing=""0""
                                                border=""0"">

                                                <tr>

                                                    <!-- SITIO WEB -->

                                                    <td style=""
                                                        padding:
                                                            0 0 15px 20px;
                                                        font-size: 13px;
                                                        text-align: left;
                                                        font-weight: bold;
                                                    "">

                                                        <a
                                                            href=""https://preven.mx""
                                                            target=""_blank""
                                                            style=""
                                                                text-decoration: none;
                                                                color: #1b2a4e;
                                                            "">

                                                            www.preven.mx

                                                        </a>

                                                    </td>


                                                    <!-- REDES -->

                                                    <td style=""
                                                        padding:
                                                            0 20px 15px 0;
                                                        font-size: 13px;
                                                        text-align: right;
                                                        font-weight: bold;
                                                    "">

                                                        <span style=""
                                                            vertical-align: middle;
                                                            color: #1b2a4e;
                                                        "">
                                                            prevenmx
                                                        </span>

                                                        &nbsp;


                                                        <!-- FACEBOOK -->

                                                        <a
                                                            href=""https://www.facebook.com/prevenmx""
                                                            target=""_blank""
                                                            style=""
                                                                text-decoration: none;
                                                            "">

                                                            <img
                                                                src=""cid:FACEBOOK_IMG""
                                                                alt=""Facebook""
                                                                width=""24""
                                                                height=""24""
                                                                style=""
                                                                    width: 24px;
                                                                    height: 24px;
                                                                    vertical-align: middle;
                                                                    border: 0;
                                                                "">

                                                        </a>


                                                        <!-- INSTAGRAM -->

                                                        <a
                                                            href=""https://www.instagram.com/prevenmx""
                                                            target=""_blank""
                                                            style=""
                                                                text-decoration: none;
                                                            "">

                                                            <img
                                                                src=""cid:INSTAGRAM_IMG""
                                                                alt=""Instagram""
                                                                width=""24""
                                                                height=""24""
                                                                style=""
                                                                    width: 24px;
                                                                    height: 24px;
                                                                    vertical-align: middle;
                                                                    border: 0;
                                                                "">

                                                        </a>

                                                    </td>

                                                </tr>

                                            </table>

                                        </td>
                                    </tr>


                                    <!-- ===================================== -->
                                    <!-- UUID -->
                                    <!-- ===================================== -->

                                    <tr>
                                        <td style=""
                                            padding: 5px 20px 20px 20px;
                                            text-align: center;
                                            font-size: 10px;
                                            color: #999999;
                                        "">

                                            Identificador de prueba:
                                            {uuid}

                                        </td>
                                    </tr>

                                </table>

                            </td>
                        </tr>

                    </table>

                </body>
                </html>";


                bool enviado =
                    await envioEmail(
                        uuid,
                        configuracion,
                        configuracion.notifica_puntos,
                        recipientEmail,
                        subject,
                        body,
                        // Incluye imágenes inline para validar el formato completo.
                        true
                    );


                if (enviado)
                {
                    response.respuesta = true;
                    response.statusCode = HttpStatusCode.OK;
                    response.mensaje = "Correo de prueba enviado correctamente.";
                    response.resultado =
                        new
                        {
                            remitente = configuracion.notifica_puntos,
                            destinatario = recipientEmail,
                            fecha = DateTime.Now
                        };
                }
                else
                {
                    response.respuesta = false;
                    response.statusCode = HttpStatusCode.InternalServerError;
                    response.mensaje = "No fue posible enviar el correo de prueba.";
                }
            }
            catch (Exception ex)
            {
                response.respuesta = false;
                response.statusCode = HttpStatusCode.InternalServerError;
                response.mensaje = "Ocurrió un error al realizar la prueba de correo.";
                response.descripcion = ex.ToString();

                _log.Add(uuid + " ERROR - Excepción en clase EmailNegocio método Prueba: " + ex);
            }

            return response;
        }
    }
}