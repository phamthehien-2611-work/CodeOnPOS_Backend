using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using UserManagementService.Protos;
using System.Net;
using Google.Protobuf.WellKnownTypes;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly UserAuthService.UserAuthServiceClient _userAuthServiceClient;
        private readonly ILogger<UserController> _logger;

        public UserController(UserAuthService.UserAuthServiceClient userAuthServiceClient, ILogger<UserController> logger)
        {
            _userAuthServiceClient = userAuthServiceClient;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var grpcResponse = await _userAuthServiceClient.RegisterAsync(request);

                if (grpcResponse.StatusCode == (int)HttpStatusCode.OK)
                {
                    return Ok(grpcResponse);
                }
                else
                {
                    return BadRequest(grpcResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling Register service.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Gateway internal error");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var grpcResponse = await _userAuthServiceClient.LoginAsync(request);

                if (grpcResponse.StatusCode == (int)HttpStatusCode.OK)
                {
                    // Giải nén payload để lấy token
                    if (grpcResponse.Data.Is(LoginSuccessPayload.Descriptor))
                    {
                        var loginPayload = grpcResponse.Data.Unpack<LoginSuccessPayload>();
                        // Trả về token cho client ban đầu
                        return Ok(grpcResponse);
                    }
                }

                // Nếu không thành công hoặc không giải nén được payload
                return Unauthorized(grpcResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling Login service.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Gateway internal error");
            }
        }
    }
}
