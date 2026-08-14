using LabBooking.Application.Features.Waitlists.Commands;
using LabBooking.Application.Features.Waitlists.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WaitlistsController : ControllerBase
    {
        private readonly ISender _sender;

        public WaitlistsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetMyWaitlistsQuery query)
            => Ok(await _sender.Send(query));

        [HttpPost]
        public async Task<IActionResult> Join([FromBody] JoinWaitlistCommand command)
            => Created(string.Empty, await _sender.Send(command));

        [HttpDelete("{waitlistId:guid}")]
        public async Task<IActionResult> Leave(Guid waitlistId)
            => Ok(await _sender.Send(new LeaveWaitlistCommand { WaitlistId = waitlistId }));
    }
}