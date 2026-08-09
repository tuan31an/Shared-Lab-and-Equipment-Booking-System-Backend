using LabBooking.Application.Features.Maintenances.Commands;
using LabBooking.Application.Features.Maintenances.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MaintenancesController : ControllerBase
    {
        private readonly ISender _sender;

        public MaintenancesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetMaintenancesQuery query)
            => Ok(await _sender.Send(query));

        [HttpPost]
        public async Task<IActionResult> Schedule([FromBody] CreateMaintenanceCommand command)
            => Created(string.Empty, await _sender.Send(command));

        [HttpPost("{maintenanceId:guid}/resolve")]
        public async Task<IActionResult> Resolve(Guid maintenanceId, [FromBody] ResolveMaintenanceCommand? command)
        {
            command ??= new ResolveMaintenanceCommand();
            command.MaintenanceId = maintenanceId;
            return Ok(await _sender.Send(command));
        }
    }
}