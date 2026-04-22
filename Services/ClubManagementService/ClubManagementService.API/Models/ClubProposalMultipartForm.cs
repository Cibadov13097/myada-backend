using System.Text.Json;
using ClubManagementService.Application.DTOs.Contracts;
using Microsoft.AspNetCore.Http;

namespace ClubManagementService.API.Models;

public sealed class ClubProposalMultipartForm
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public IFormFile? LogoFile { get; set; }
    public IFormFile? ConstitutionFile { get; set; }

    public string? PresidentStudentId { get; set; }
    public string? VicePresidentStudentId { get; set; }
    public string? OtherMembersJson { get; set; }
    public string? Commitment { get; set; }

    public string? ShortDesc { get; set; }
    public string? UniqueDesc { get; set; }
    public string? Goals { get; set; }
    public string? Activities { get; set; }
    public string? Alignment { get; set; }
    public string? Vision { get; set; }

    public ClubProposalSubmissionDto ToSubmissionDto() =>
        new()
        {
            Id = Id,
            Name = Name,
            Description = Description,
            PresidentStudentId = PresidentStudentId,
            VicePresidentStudentId = VicePresidentStudentId,
            OtherMembersJson = ParseOtherMembersJson(OtherMembersJson),
            Commitment = Commitment,
            ShortDesc = ShortDesc,
            UniqueDesc = UniqueDesc,
            Goals = Goals,
            Activities = Activities,
            Alignment = Alignment,
            Vision = Vision
        };

    private static JsonElement? ParseOtherMembersJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        using var doc = JsonDocument.Parse(value);
        return doc.RootElement.Clone();
    }

    public ClubProposalFilePart ToLogoPart() =>
        new()
        {
            FileName = LogoFile?.FileName ?? string.Empty,
            SizeBytes = LogoFile?.Length ?? 0,
            ContentType = LogoFile?.ContentType
        };

    public ClubProposalFilePart ToConstitutionPart() =>
        new()
        {
            FileName = ConstitutionFile?.FileName ?? string.Empty,
            SizeBytes = ConstitutionFile?.Length ?? 0,
            ContentType = ConstitutionFile?.ContentType
        };
}
