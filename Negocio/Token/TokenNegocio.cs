using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using api_preven_email_service.DAO;
using api_preven_email_service.Model.SysParametro;
using api_preven_email_service.Negocio.SysParametro;
using api_preven_email_service.Helper;
using Microsoft.IdentityModel.Tokens;

namespace api_preven_email_service.Negocio.Token
{
    public class TokenNegocio
    {
        private readonly PostgreSQLInterface? _postgreSQLInterface;
        private readonly LoggerService _log;
        protected APIResponse _apiResponse;

        public TokenNegocio(LoggerService log, PostgreSQLInterface postgreSQLInterface)
        {
            _log = log;
            _postgreSQLInterface = postgreSQLInterface;
            _apiResponse = new APIResponse();
        }

        public async Task<APIResponse> GeneraToken(Guid uuid, int id_usuario)
        {
            _log.Add(uuid + " INFO - Id usuario: " + id_usuario + " Ingresa clase TokenNegocio metodo GeneraToken");
            _apiResponse.uuid = uuid;

            try
            {
                if (_postgreSQLInterface == null)
                {
                    _apiResponse.respuesta = false;
                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                    _apiResponse.mensaje = "La interfaz de la conexión no se encuentra referenciada.";
                }
                else
                {
                    APIResponse keyResponse = await new SysParametroNegocio(_log, _postgreSQLInterface).SysParametroConsulta(uuid, id_usuario, 1);

                    if (keyResponse.respuesta)
                    {
                        if (keyResponse.resultado != null)
                        {
                            SysParametroModel key = new SysParametroModel();
                            key = (SysParametroModel)keyResponse.resultado;

                            var tokenHandler = new JwtSecurityTokenHandler();
                            var byteKey = Encoding.UTF8.GetBytes(key.valor);

                            APIResponse vigenciaResponse = await new SysParametroNegocio(_log, _postgreSQLInterface).SysParametroConsulta(uuid, id_usuario, 2);
                            
                            if(vigenciaResponse.respuesta){
                                if(vigenciaResponse.resultado != null){
                                    SysParametroModel vigencia = new SysParametroModel();
                                    vigencia = (SysParametroModel)vigenciaResponse.resultado;

                                    var tokenDes = new SecurityTokenDescriptor
                                    {
                                        Subject = new ClaimsIdentity(
                                        [
                                            new Claim("id_usuario", id_usuario.ToString()),
                                            //new Claim("id_empresa", id_empresa.ToString())
                                        ]),
                                        
                                        Expires = DateTime.UtcNow.AddMinutes(double.Parse(vigencia.valor)),
                                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(byteKey), SecurityAlgorithms.HmacSha256Signature)
                                    };

                                    var token = tokenHandler.CreateToken(tokenDes);

                                    _apiResponse.respuesta = true;
                                    _apiResponse.statusCode = HttpStatusCode.OK;
                                    _apiResponse.mensaje = "Actualización de token exitoso.";
                                    _apiResponse.resultado = tokenHandler.WriteToken(token);
                                    _apiResponse.token = tokenHandler.WriteToken(token);
                                }
                                else
                                {
                                    _apiResponse.respuesta = false;
                                    _apiResponse.statusCode = HttpStatusCode.BadRequest;
                                    _apiResponse.mensaje = "No se encontro la vigencia para asignar al token.";
                                    _apiResponse.descripcion = "No se encontro la vigencia para asignar al token.";
                                }
                            }
                            else
                            {
                                _apiResponse.respuesta = false;
                                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                                _apiResponse.mensaje = "No se encontro la vigencia para asignar al token.";
                                _apiResponse.descripcion = "No se encontro la vigencia para asignar al token.";
                            }
                        }
                        else
                        {
                            _apiResponse.respuesta = false;
                            _apiResponse.statusCode = HttpStatusCode.BadRequest;
                            _apiResponse.mensaje = "No se encontro la llave para codificar el token.";
                            _apiResponse.descripcion = "No se encontro la llave para codificar el token.";
                        }
                    }
                    else
                    {
                        _apiResponse.respuesta = false;
                        _apiResponse.statusCode = HttpStatusCode.BadRequest;
                        _apiResponse.mensaje = "No se encontro la llave para codificar el token.";
                        _apiResponse.descripcion = "No se encontro la llave para codificar el token.";
                    }
                }
            }
            catch (Exception ex)
            {
                _apiResponse.respuesta = false;
                _apiResponse.statusCode = HttpStatusCode.BadRequest;
                _apiResponse.mensaje = "Ocurrio un error en el proceso. Comunicate con el administrador del sistema.";
                _apiResponse.descripcion = "Excepcion en clase TokenNegocio metodo GeneraToken: " + ex.ToString();
                _log.Add(uuid + " ERROR - Excepcion en clase TokenNegocio metodo GeneraToken: " + ex.ToString());
            }

            return _apiResponse;
        }
    }
}