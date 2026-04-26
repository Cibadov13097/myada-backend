using System.Collections;
using System.Collections.Generic;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;
using FluentAssertions;

namespace ClubManagementService.Tests.Layers.Domain;

/// <summary>Large service — domain layer guideline ~40–100+ tests.</summary>
public sealed class ClubManagementServiceDomainLayerTests
{
    public static IEnumerable<object[]> ClubStatuses() =>
        Enum.GetValues<ClubStatus>().Cast<ClubStatus>().Select(v => new object[] { v });

    public static IEnumerable<object[]> JobApplicationStatuses() =>
        Enum.GetValues<JobApplicationStatus>().Cast<JobApplicationStatus>().Select(v => new object[] { v });

    public static IEnumerable<object[]> ClubPositionCategories() =>
        Enum.GetValues<ClubPositionCategory>().Cast<ClubPositionCategory>().Select(v => new object[] { v });

    [Theory]
    [MemberData(nameof(ClubStatuses))]
    public void ClubStatus_is_defined(ClubStatus value) => Enum.IsDefined(value).Should().BeTrue();

    [Theory]
    [MemberData(nameof(JobApplicationStatuses))]
    public void JobApplicationStatus_is_defined(JobApplicationStatus value) => Enum.IsDefined(value).Should().BeTrue();

    [Theory]
    [MemberData(nameof(ClubPositionCategories))]
    public void ClubPositionCategory_is_defined(ClubPositionCategory value) => Enum.IsDefined(value).Should().BeTrue();

    [Fact]
    public void Event_entity_exposes_status_and_times()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(2);
        var e = new Event
        {
            ClubId = 1,
            Name = "Meetup",
            Description = "D",
            StartTime = start,
            EndTime = end,
            Location = "Hall",
            SeatLimit = 50,
            Requirements = "None",
            Prerequisites = "None",
            Notes = "N",
            ApprovedBy = Guid.NewGuid(),
            Status = ClubStatus.Approved
        };
        e.Status.Should().Be(ClubStatus.Approved);
        e.StartTime.Should().Be(start);
        e.EndTime.Should().Be(end);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void Club_entity_id_roundtrips(int id)
    {
        var club = new Club { Id = id, Name = "C" };
        club.Id.Should().Be(id);
    }

    [Fact]
    public void Category_entity_name_roundtrip() =>
        new Category { Name = "Sports" }.Name.Should().Be("Sports");

    [Fact]
    public void JobVacancy_entity_title_roundtrip() =>
        new JobVacancy { Title = "Engineer" }.Title.Should().Be("Engineer");

    [Fact]
    public void Subevent_entity_links_main_event_id()
    {
        var s = new Subevent { MainEventId = 9, Name = "Workshop" };
        s.MainEventId.Should().Be(9);
    }

    [Fact]
    public void EventRegistration_entity_has_event_and_user()
    {
        var now = DateTime.UtcNow;
        var er = new EventRegistration
        {
            EventId = 3,
            UserId = Guid.NewGuid(),
            TicketId = "ticket",
            RegistrationJwt = "jwt",
            EventNameSnapshot = "E",
            EventStartTimeSnapshot = now,
            EventEndTimeSnapshot = now.AddHours(1),
            EventLocationSnapshot = "Campus",
            EventSeatLimitSnapshot = 10
        };
        er.EventId.Should().Be(3);
        er.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void JobApplication_entity_has_status()
    {
        var ja = new JobApplication
        {
            ExperienceLinks = "",
            FileUrl = "",
            Status = JobApplicationStatus.Reviewing
        };
        ja.Status.Should().Be(JobApplicationStatus.Reviewing);
    }

    [Theory]
    [InlineData(ClubStatus.Pending)]
    [InlineData(ClubStatus.Archived)]
    public void ClubStatus_display_names_are_non_empty(ClubStatus s) =>
        s.ToString().Should().NotBeNullOrWhiteSpace();

    [Theory]
    [InlineData(JobApplicationStatus.Cancelled)]
    [InlineData(JobApplicationStatus.InterviewScheduled)]
    public void JobApplication_terminal_and_late_stages_exist(JobApplicationStatus s) =>
        Enum.IsDefined(s).Should().BeTrue();
}
