using LabBooking.Application.Features.Notifications.Commands;
using LabBooking.Application.Features.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public NotificationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetNotificationsQuery query)
            => Ok(await _sender.Send(query));

        [HttpPut("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid notificationId)
            => Ok(await _sender.Send(new MarkNotificationReadCommand { NotificationId = notificationId }));
    }
}