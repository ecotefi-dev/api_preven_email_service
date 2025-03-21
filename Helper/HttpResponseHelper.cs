using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace api_preven_email_service.Helper
{
    public static class HttpResponseHelper
    {
        public static ActionResult<APIResponse> CreateHttpResponse(APIResponse apiResponse)
        {
            switch (apiResponse.statusCode)
            {
                case HttpStatusCode.OK:
                    return new OkObjectResult(apiResponse);
                case HttpStatusCode.NotFound:
                    return new NotFoundObjectResult(apiResponse);
                case HttpStatusCode.Unauthorized:
                    return new UnauthorizedObjectResult(apiResponse);
                case HttpStatusCode.BadRequest:
                    return new BadRequestObjectResult(apiResponse);
                case HttpStatusCode.InternalServerError:
                    return new ObjectResult(apiResponse) { StatusCode = StatusCodes.Status500InternalServerError };
                default:
                    return new BadRequestObjectResult(apiResponse);
            }
        }
    }
}
