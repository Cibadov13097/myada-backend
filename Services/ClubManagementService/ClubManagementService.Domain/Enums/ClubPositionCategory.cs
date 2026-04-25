using System.Text.Json.Serialization;

namespace ClubManagementService.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClubPositionCategory
{
    Marketing = 1,
    Technology = 2,
    Events = 3,
    Finance = 4,
    Outreach = 5,
    Content = 6,
    Other = 7
}
