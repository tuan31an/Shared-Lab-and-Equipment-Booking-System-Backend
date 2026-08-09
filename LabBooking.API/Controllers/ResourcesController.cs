using LabBooking.Application.Features.Departments.Queries;
using LabBooking.Application.Features.Resources.Commands;
using LabBooking.Application.Features.Resources.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResourcesController : ControllerBase
    {
        private readonly ISender _sender;

        public ResourcesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetResourcesQuery query)
            => Ok(await _sender.Send(query));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
            => Ok(await _sender.Send(new GetResourceByIdQuery { Id = id }));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateResourceCommand command)
            => Created(string.Empty, await _sender.Send(command));

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResourceCommand command)
        {
            command.Id = id;
            return Ok(await _sender.Send(command));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _sender.Send(new DeleteResourceCommand { Id = id });
            return NoContent();
        }
    }
}