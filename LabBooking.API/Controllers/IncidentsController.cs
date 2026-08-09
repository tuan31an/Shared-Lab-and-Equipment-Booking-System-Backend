using LabBooking.Application.Features.Incidents.Commands;
using LabBooking.Application.Features.Incidents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IncidentsController : ControllerBase
    {
        private readonly ISender _sender;

        public IncidentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetIncidentsQuery query)
            => Ok(await _sender.Send(query));

        [HttpPost]
        public async Task<IActionResult> Report([FromBody] CreateIncidentCommand command)
            => Created(string.Empty, await _sender.Send(command));
    }
}