using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Features.Incidents.Commands;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using Xunit;

namespace LabBooking.Tests;

public class IncidentTests
{
    private readonly FakeRepository<Resource> _resources = new();
    private readonly FakeRepository<Booking> _bookings = new();
    private readonly FakeRepository<Incident> _incidents = new();
    private readonly FakeRepository<Notification> _notifications = new();
    private readonly FakeRepository<User> _users = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.NewGuid(), Role = "Requester" };
    private readonly FakeUnitOfWork _uow = new();

    [Fact]
    public async Task Create_Incident_Notifies_Lab_Manager()
    {
        var managerId = Guid.NewGuid();
        var resource = new Resource { Name = "Lab A", LabManagerId = managerId };
        _resources.Items.Add(resource);
        var handler = new CreateIncidentCommandHandler(_resources, _bookings, _incidents, _notifications, _user, _uow);

        var dto = await handler.Handle(new CreateIncidentCommand
        {
            ResourceId = resource.Id,
            Description = "  Broken projector  "
        }, CancellationToken.None);

        Assert.Equal(IncidentStatus.Open.ToString(), dto.Status);
        Assert.Equal("Broken projector", dto.Description);
        Assert.Equal(resource.Name, dto.ResourceName);
        Assert.Single(_incidents.Items);

        var notification = Assert.Single(_notifications.Items);
        Assert.Equal(managerId, notification.UserId);
        Assert.Equal(NotificationType.IncidentReported, notification.Type);
    }

    [Fact]
    public async Task Create_Incident_Without_Manager_Sends_No_Notification()
    {
        var resource = new Resource { Name = "Lab B", LabManagerId = null };
        _resources.Items.Add(resource);
        var handler = new CreateIncidentCommandHandler(_resources, _bookings, _incidents, _notifications, _user, _uow);

        await handler.Handle(new CreateIncidentCommand { ResourceId = resource.Id, Description = "Issue" }, CancellationToken.None);

        Assert.Single(_incidents.Items);
        Assert.Empty(_notifications.Items);
    }

    [Fact]
    public async Task Create_Incident_Missing_Resource_Throws()
    {
        var handler = new CreateIncidentCommandHandler(_resources, _bookings, _incidents, _notifications, _user, _uow);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateIncidentCommand
        {
            ResourceId = Guid.NewGuid(),
            Description = "Issue"
        }, CancellationToken.None));
    }
}