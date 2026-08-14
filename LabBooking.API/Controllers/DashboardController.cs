using LabBooking.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,LabManager")]
    public class DashboardController : ControllerBase
    {
        private readonly ISender _sender;

        public DashboardController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("usage")]
        public async Task<IActionResult> Usage([FromQuery] GetUsageDashboardQuery query)
            => Ok(await _sender.Send(query));

        [HttpGet("maintenance-report")]
        public async Task<IActionResult> MaintenanceReport([FromQuery] GetMaintenanceReportQuery query)
            => Ok(await _sender.Send(query));
    }
}