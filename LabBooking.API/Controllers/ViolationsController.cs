using LabBooking.Application.Features.Violations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ViolationsController : ControllerBase
    {
        private readonly ISender _sender;

        public ViolationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetViolationsQuery query)
            => Ok(await _sender.Send(query));
    }
}