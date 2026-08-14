using LabBooking.API.Models;
using LabBooking.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await _sender.Send(command);
            return Created(string.Empty, result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            return Ok(await _sender.Send(command));
        }

        /// <summary>Làm mới access token bằng refresh token. Token cũ bị thu hồi (rotate).</summary>
        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Refresh([FromBody] RefreshCommand command)
        {
            return Ok(await _sender.Send(command));
        }

        /// <summary>Đăng xuất, thu hồi refresh token hiện tại.</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand? command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.RefreshToken))
                return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "RefreshToken is required."));

            await _sender.Send(command);
            return NoContent();
        }
    }
}
