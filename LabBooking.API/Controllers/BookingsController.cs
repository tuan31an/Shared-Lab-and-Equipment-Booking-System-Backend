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

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetBookingsQuery query)
            => Ok(await _sender.Send(query));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingCommand command)
            => Created(string.Empty, await _sender.Send(command));

        [HttpPost("check-conflict")]
        public async Task<IActionResult> CheckConflict([FromBody] CheckBookingConflictCommand command)
            => Ok(await _sender.Send(command));

        [HttpPost("{bookingId:guid}/approve")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> Approve(Guid bookingId)
            => Ok(await _sender.Send(new ApproveBookingCommand { BookingId = bookingId }));

        [HttpPost("{bookingId:guid}/reject")]
        [Authorize(Roles = "Admin,LabManager")]
        public async Task<IActionResult> Reject(Guid bookingId, [FromBody] RejectBookingCommand? command)
        {
            command ??= new RejectBookingCommand { BookingId = bookingId };
            command.BookingId = bookingId;
            return Ok(await _sender.Send(command));
        }

        [HttpPost("{bookingId:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid bookingId)
            => Ok(await _sender.Send(new CancelBookingCommand { BookingId = bookingId }));

        [HttpPost("{bookingId:guid}/checkin")]
        public async Task<IActionResult> CheckIn(Guid bookingId, [FromBody] CheckInBookingCommand? command)
        {
            command ??= new CheckInBookingCommand();
            command.BookingId = bookingId;
            return Ok(await _sender.Send(command));
        }

        [HttpPost("{bookingId:guid}/checkout")]
        public async Task<IActionResult> CheckOut(Guid bookingId)
            => Ok(await _sender.Send(new CheckOutBookingCommand { BookingId = bookingId }));

        [HttpGet("{bookingId:guid}")]
        public async Task<IActionResult> GetById(Guid bookingId)
            => Ok(await _sender.Send(new GetBookingByIdQuery { BookingId = bookingId }));
    }
}