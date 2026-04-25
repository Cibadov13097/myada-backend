using ClubManagementService.Application.DTOs.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
    [FromForm(Name = "otherMembersJson")]
    public List<string> OtherMembersJson { get; set; } = new();
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
            LogoFilePart = ToLogoPart(),
            ConstitutionFilePart = ToConstitutionPart(),
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

    private static List<string> ParseOtherMembersJson(List<string>? values)
    {
        if (values is not { Count: > 0 })
            return new List<string>();

        var result = new List<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var trimmed = value.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(trimmed)
                                 ?? new List<string>();
                    result.AddRange(parsed.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                    continue;
                }
                catch (JsonException)
                {
                    // Let downstream validation surface malformed input consistently.
                }
            }

            result.Add(trimmed);
        }

        return result;
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
