using LabBooking.Application.Features.Bookings.Commands;
using LabBooking.Application.Features.Bookings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly ISender _sender;

        public BookingsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingCommand command)
            => Created(string.Empty, await _sender.Send(command));

        [HttpPost("check-conflict")]
        public async Task<IActionResult> CheckConflict([FromBody] CheckBookingConflictCommand command)
            => Ok(await _sender.Send(command));

        [HttpPost("{bookingId:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid bookingId)
            => Ok(await _sender.Send(new CancelBookingCommand { BookingId = bookingId }));

        [HttpGet("{bookingId:guid}")]
        public async Task<IActionResult> GetById(Guid bookingId)
            => Ok(await _sender.Send(new GetBookingByIdQuery { BookingId = bookingId }));
    }
}