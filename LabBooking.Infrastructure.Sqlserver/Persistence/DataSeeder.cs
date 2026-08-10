using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LabBooking.Infrastructure.Sqlserver.Persistence;

public static class DataSeeder
{
    // Development-only account password: ChangeMe123!
    private const string DevelopmentPasswordHash = "$2b$12$g7KgCoZKtmX0EaVxVZ6hTuc7NxKmDeLkvXm996Bg1w1gcrs6DlLOa";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // Existing development databases created by older versions had blank
        // password hashes. Repair only the known sample accounts, then stop.
        if (await context.Departments.AnyAsync())
        {
            var sampleEmails = new[]
            {
                "admin@example.com",
                "admin2@example.com",
                "alice.manager@example.com",
                "evan.electronics@example.com",
                "mary.mechanical@example.com",
                "bob.requester@example.com",
                "charlie.student@example.com",
                "diana.researcher@example.com"
            };
            var sampleUsers = await context.Users
                .Where(user => sampleEmails.Contains(user.Email) && user.PasswordHash == "")
                .ToListAsync();
            foreach (var sampleUser in sampleUsers)
                sampleUser.PasswordHash = DevelopmentPasswordHash;

            if (sampleUsers.Count > 0)
                await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return;
        }

        // Departments
        var cs = new Department { Name = "Computer Science" };
        var electronics = new Department { Name = "Electronics" };
        var mechanical = new Department { Name = "Mechanical" };

        await context.Departments.AddRangeAsync(new[] { cs, electronics, mechanical });
        await context.SaveChangesAsync();

        // Users
        var admin = new User
        {
            FullName = "System Admin",
            Email = "admin@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.Admin,
            DepartmentId = null
        };

        var admin2 = new User
        {
            FullName = "Backup Admin",
            Email = "admin2@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.Admin,
            DepartmentId = null
        };

        var labManager = new User
        {
            FullName = "Dr. Alice Manager",
            Email = "alice.manager@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.LabManager,
            DepartmentId = cs.Id
        };

        var labManagerElectronics = new User
        {
            FullName = "Dr. Evan Electronics",
            Email = "evan.electronics@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.LabManager,
            DepartmentId = electronics.Id
        };

        var labManagerMechanical = new User
        {
            FullName = "Dr. Mary Mechanical",
            Email = "mary.mechanical@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.LabManager,
            DepartmentId = mechanical.Id
        };

        var requester = new User
        {
            FullName = "Bob Requester",
            Email = "bob.requester@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.Requester,
            DepartmentId = cs.Id
        };

        var requester2 = new User
        {
            FullName = "Charlie Student",
            Email = "charlie.student@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.Requester,
            DepartmentId = electronics.Id
        };

        var requester3 = new User
        {
            FullName = "Diana Researcher",
            Email = "diana.researcher@example.com",
            PasswordHash = DevelopmentPasswordHash,
            Role = UserRole.Requester,
            DepartmentId = mechanical.Id
        };

        await context.Users.AddRangeAsync(new[] { admin, admin2, labManager, labManagerElectronics, labManagerMechanical, requester, requester2, requester3 });
        await context.SaveChangesAsync();

        // Priority rules
        var research = new PriorityRule { Name = "Research Project", PriorityLevel = 1, Description = "Priority for official research projects." };
        var course = new PriorityRule { Name = "Course", PriorityLevel = 2, Description = "Priority for scheduled course activities." };
        var selfStudy = new PriorityRule { Name = "Self-study", PriorityLevel = 3, Description = "General usage." };

        await context.PriorityRules.AddRangeAsync(new[] { research, course, selfStudy });
        await context.SaveChangesAsync();

        // Resources
        var csLab = new Resource { Name = "CS Lab A", Type = ResourceType.Room, DepartmentId = cs.Id, LabManagerId = labManager.Id };
        var oscilloscope = new Resource { Name = "Oscilloscope", Type = ResourceType.Equipment, DepartmentId = electronics.Id, LabManagerId = labManagerElectronics.Id };
        var mechWorkshop = new Resource { Name = "Mech Workshop", Type = ResourceType.Room, DepartmentId = mechanical.Id, LabManagerId = labManagerMechanical.Id };

        await context.Resources.AddRangeAsync(new[] { csLab, oscilloscope, mechWorkshop });
        await context.SaveChangesAsync();



        // Booking: approved booking by requester for CS Lab A
        var now = DateTime.UtcNow;
        var bookingStart = now.AddHours(24).Date.AddHours(9); // tomorrow 09:00 UTC
        var bookingEnd = bookingStart.AddHours(2);

        var booking = new Booking
        {
            ResourceId = csLab.Id,
            RequesterId = requester.Id,
            RuleId = course.Id,
            StartTime = bookingStart,
            EndTime = bookingEnd,
            Purpose = "Course lab session",
            Status = BookingStatus.Approved,
            ApprovedBy = labManager.Id,
            ApprovedAt = DateTime.UtcNow
        };

        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();

        // Additional bookings
        var booking2 = new Booking
        {
            ResourceId = oscilloscope.Id,
            RequesterId = requester2.Id,
            RuleId = research.Id,
            StartTime = now.AddDays(2).Date.AddHours(10),
            EndTime = now.AddDays(2).Date.AddHours(12),
            Purpose = "Lab experiment",
            Status = BookingStatus.Approved,
            ApprovedBy = labManagerElectronics.Id,
            ApprovedAt = DateTime.UtcNow
        };

        var booking3 = new Booking
        {
            ResourceId = mechWorkshop.Id,
            RequesterId = requester3.Id,
            RuleId = research.Id,
            StartTime = now.AddDays(3).Date.AddHours(13),
            EndTime = now.AddDays(3).Date.AddHours(16),
            Purpose = "Prototype test",
            Status = BookingStatus.Pending
        };

        await context.Bookings.AddRangeAsync(new[] { booking2, booking3 });
        await context.SaveChangesAsync();

        // CheckInOut for a completed booking (simulate check-in/out)
        var checkIn = new CheckInOut
        {
            BookingId = booking.Id,
            CheckInTime = bookingStart.AddMinutes(5),
            CheckOutTime = bookingEnd,
            ActualDuration = (int)(bookingEnd - bookingStart).TotalMinutes
        };

        await context.CheckInOuts.AddAsync(checkIn);
        await context.SaveChangesAsync();

        // Check-in/out for booking2
        var checkIn2 = new CheckInOut
        {
            BookingId = booking2.Id,
            CheckInTime = booking2.StartTime.AddMinutes(0),
            CheckOutTime = booking2.EndTime.AddMinutes(10),
            ActualDuration = (int)(booking2.EndTime - booking2.StartTime).TotalMinutes + 10
        };

        await context.CheckInOuts.AddAsync(checkIn2);
        await context.SaveChangesAsync();

        // Incident related to booking (optional)
        var incident = new Incident
        {
            BookingId = booking.Id,
            ResourceId = csLab.Id,
            ReportedBy = labManager.Id,
            Description = "Projector not working",
            Status = IncidentStatus.Open,
            ReportedAt = DateTime.UtcNow
        };

        await context.Incidents.AddAsync(incident);
        await context.SaveChangesAsync();

        // Incident on oscilloscope reported by requester2
        var incident2 = new Incident
        {
            BookingId = booking2.Id,
            ResourceId = oscilloscope.Id,
            ReportedBy = requester2.Id,
            Description = "Oscilloscope display flickers",
            Status = IncidentStatus.InReview,
            ReportedAt = DateTime.UtcNow
        };

        await context.Incidents.AddAsync(incident2);
        await context.SaveChangesAsync();

        // Maintenance scheduled for oscilloscope
        var maintenance = new Maintenance
        {
            ResourceId = oscilloscope.Id,
            StartTime = now.AddDays(7),
            EndTime = now.AddDays(7).AddHours(4),
            Description = "Calibration",
            Cost = 150.0m,
            Status = MaintenanceStatus.Scheduled,
            CreatedBy = labManagerElectronics.Id
        };

        await context.Maintenances.AddAsync(maintenance);
        await context.SaveChangesAsync();

        // Additional maintenance for mech workshop
        var maintenance2 = new Maintenance
        {
            ResourceId = mechWorkshop.Id,
            StartTime = now.AddDays(10),
            EndTime = now.AddDays(10).AddHours(8),
            Description = "Full machinery inspection",
            Cost = 500.0m,
            Status = MaintenanceStatus.Scheduled,
            CreatedBy = labManagerMechanical.Id
        };

        await context.Maintenances.AddAsync(maintenance2);
        await context.SaveChangesAsync();

        // Violation: simulate a late check-out for requester
        var violation = new Violation
        {
            UserId = requester.Id,
            BookingId = booking.Id,
            Type = ViolationType.Late,
            RecordedAt = DateTime.UtcNow,
            Note = "Returned equipment late"
        };

        await context.Violations.AddAsync(violation);
        await context.SaveChangesAsync();

        // Violation for requester2 (no-show)
        var violation2 = new Violation
        {
            UserId = requester2.Id,
            BookingId = booking2.Id,
            Type = ViolationType.NoShow,
            RecordedAt = DateTime.UtcNow,
            Note = "Did not show up for scheduled experiment"
        };

        await context.Violations.AddAsync(violation2);
        await context.SaveChangesAsync();

        // Restriction: temporarily restrict a user
        var restriction = new Restriction
        {
            UserId = requester.Id,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            Reason = "Multiple late returns",
            CreatedBy = admin.Id
        };

        await context.Restrictions.AddAsync(restriction);
        await context.SaveChangesAsync();

        // Restrict requester2 for no-show
        var restriction2 = new Restriction
        {
            UserId = requester2.Id,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(3),
            Reason = "No-show for booking",
            CreatedBy = admin2.Id
        };

        await context.Restrictions.AddAsync(restriction2);
        await context.SaveChangesAsync();

        // Waitlist entry
        var waitlist = new Waitlist
        {
            ResourceId = csLab.Id,
            RequesterId = requester.Id,
            DesiredStart = bookingStart,
            DesiredEnd = bookingEnd,
            Status = WaitlistStatus.Waiting
        };

        await context.Waitlists.AddAsync(waitlist);
        await context.SaveChangesAsync();

        // Additional waitlist entries
        var waitlist2 = new Waitlist
        {
            ResourceId = oscilloscope.Id,
            RequesterId = requester3.Id,
            DesiredStart = booking2.StartTime,
            DesiredEnd = booking2.EndTime,
            Status = WaitlistStatus.Waiting
        };

        await context.Waitlists.AddAsync(waitlist2);
        await context.SaveChangesAsync();

        // Notification
        var notification = new Notification
        {
            UserId = requester.Id,
            Type = NotificationType.BookingApproved,
            Content = "Your booking for CS Lab A has been approved.",
            IsRead = false
        };

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        // Notifications for others
        var notification2 = new Notification
        {
            UserId = requester2.Id,
            Type = NotificationType.BookingApproved,
            Content = "Your booking for Oscilloscope has been approved.",
            IsRead = false
        };

        var notification3 = new Notification
        {
            UserId = labManagerElectronics.Id,
            Type = NotificationType.WaitlistAvailable,
            Content = "A waitlist entry is waiting for your approval.",
            IsRead = false
        };

        await context.Notifications.AddRangeAsync(new[] { notification2, notification3 });
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
