using LabBooking.Application.Features.Restrictions.Commands;
using LabBooking.Application.Features.Restrictions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RestrictionsController : ControllerBase
    {
        private readonly ISender _sender;

        public RestrictionsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetRestrictionsQuery query)
            => Ok(await _sender.Send(query));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateRestrictionCommand command)
            => Created(string.Empty, await _sender.Send(command));

        [HttpDelete("{restrictionId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remove(Guid restrictionId)
            => Ok(await _sender.Send(new RemoveRestrictionCommand { RestrictionId = restrictionId }));
    }
}