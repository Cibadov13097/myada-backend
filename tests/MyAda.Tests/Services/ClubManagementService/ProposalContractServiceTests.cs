using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;

namespace MyAda.Tests.Services.ClubManagementService;

public class ProposalContractServiceTests
{
    [Fact]
    public async Task ApproveClubProposalAsync_ShouldCreateMembershipsForOtherMembers()
    {
        const string organizationId1 = "123456781";
        const string organizationId2 = "123456782";
        var additionalMember1 = Guid.NewGuid();
        var additionalMember2 = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var createdMemberships = new List<ClubMembership>();

        var store = new global::ClubManagementService.Infrastructure.Services.ProposalStateStore();
        store.AddClubProposal(new global::ClubManagementService.Infrastructure.Services.ClubProposalRecord
        {
            Id = Guid.NewGuid().ToString("D"),
            ProposalId = "PROP-2026-001",
            SubmittedByOrganizationId = "123456789",
            Name = "Robotics Club",
            Description = "Club description",
            SubmittedAt = DateTimeOffset.UtcNow,
            LogoUrl = "https://bucket.example/logo.png",
            ConstitutionDoc = new global::ClubManagementService.Infrastructure.Services.ConstitutionDoc
            {
                Name = "constitution.pdf",
                SizeBytes = 128,
                UploadedAt = DateTimeOffset.UtcNow,
                Url = "https://bucket.example/constitution.pdf"
            },
            OtherMembers = new List<global::ClubManagementService.Infrastructure.Services.OtherOfficer>
            {
                new() { UserId = organizationId1, Status = "Pending" },
                new() { UserId = organizationId2, Status = "Pending" }
            }
        });

        var clubService = new Mock<IClubService>();
        clubService.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ClubDto>());
        clubService.Setup(x => x.CreateAsync(It.IsAny<CreateClubDto>())).ReturnsAsync(new ClubDto
        {
            Id = 42,
            Name = "Robotics Club",
            Description = "Club description",
            ApprovedBy = actorUserId,
            CategoryId = 1
        });

        var membershipRepository = new Mock<IClubMembershipRepository>();
        membershipRepository.Setup(x => x.GetUserIdsByClubAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        membershipRepository.Setup(x => x.AddAsync(It.IsAny<ClubMembership>()))
            .Callback<ClubMembership>(createdMemberships.Add)
            .Returns(Task.CompletedTask);
        membershipRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var authUserLookupService = new Mock<IAuthUserLookupService>();
        authUserLookupService
            .Setup(x => x.GetUserIdsByOrganizationalIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { organizationId1, organizationId2 })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Guid>
            {
                [organizationId1] = additionalMember1,
                [organizationId2] = additionalMember2
            });

        var dbOptions = new DbContextOptionsBuilder<ClubManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var dbContext = new ClubManagementDbContext(dbOptions);

        var service = new global::ClubManagementService.Infrastructure.Services.ProposalContractService(
            store,
            clubService.Object,
            Mock.Of<IEventService>(),
            membershipRepository.Object,
            authUserLookupService.Object,
            dbContext);

        var result = await service.ApproveClubProposalAsync("PROP-2026-001", actorUserId, CancellationToken.None);

        result.Should().NotBeNull();
        createdMemberships.Should().HaveCount(2);
        createdMemberships.Select(x => x.UserId).Should().BeEquivalentTo(new[] { additionalMember1, additionalMember2 });
        createdMemberships.Should().OnlyContain(x => x.ClubId == 42 && x.CreatedByUserId == actorUserId);
        membershipRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitClubProposalAsync_ShouldRejectIdentifiersThatAreNotNineDigits()
    {
        var dbOptions = new DbContextOptionsBuilder<ClubManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var dbContext = new ClubManagementDbContext(dbOptions);

        var service = new global::ClubManagementService.Infrastructure.Services.ProposalContractService(
            new global::ClubManagementService.Infrastructure.Services.ProposalStateStore(),
            Mock.Of<IClubService>(),
            Mock.Of<IEventService>(),
            Mock.Of<IClubMembershipRepository>(),
            Mock.Of<IAuthUserLookupService>(),
            dbContext);

        var submission = new ClubProposalSubmissionDto
        {
            OrganizationId = "123",
            Name = "Robotics Club",
            LogoFile = "https://bucket.example/logo.png",
            ConstitutionFile = "https://bucket.example/constitution.pdf",
            PresidentStudentId = "12345678",
            VicePresidentStudentId = "1234567890",
            OtherMembersJson = new List<string> { "abc123" },
            Commitment = "yes",
            ShortDesc = "short text"
        };

        var result = await service.SubmitClubProposalAsync(submission.OrganizationId, submission, CancellationToken.None);

        result.Success.Should().BeFalse();
        var errorJson = JsonSerializer.Serialize(result.ErrorBody);
        errorJson.Should().Contain("Organization id must be exactly 9 digits.");
        errorJson.Should().Contain("President student id must be exactly 9 digits.");
        errorJson.Should().Contain("Vice president student id must be exactly 9 digits.");
        errorJson.Should().Contain("otherMembersJson must be a list of valid 9-digit user ids.");
    }
}
