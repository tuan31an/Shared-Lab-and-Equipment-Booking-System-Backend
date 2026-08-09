using LabBooking.Application.Features.PriorityRules.Commands;
using LabBooking.Application.Features.PriorityRules.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/priority-rules")]
    public class PriorityRulesController : ControllerBase
    {
        private readonly ISender _sender;

        public PriorityRulesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List()
            => Ok(await _sender.Send(new GetPriorityRulesQuery()));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePriorityRuleCommand command)
            => Created(string.Empty, await _sender.Send(command));

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePriorityRuleCommand command)
        {
            command.Id = id;
            return Ok(await _sender.Send(command));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _sender.Send(new DeletePriorityRuleCommand { Id = id });
            return NoContent();
        }
    }
}