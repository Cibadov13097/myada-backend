using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.UnitTests.Domain;

public class EntityAndEnumTests
{
    [Fact]
    public void Request_default_Status_is_New()
    {
        var request = new Request();
        Assert.Equal(RequestStatus.New, request.Status);
    }

    [Fact]
    public void Request_Attachments_collection_is_initialized()
    {
        var request = new Request();
        Assert.NotNull(request.Attachments);
        Assert.Empty(request.Attachments);
    }

    [Fact]
    public void Category_Requests_collection_is_initialized()
    {
        var category = new Category();
        Assert.NotNull(category.Requests);
        Assert.Empty(category.Requests);
    }

    [Fact]
    public void Attachment_Url_defaults_to_empty_string()
    {
        var attachment = new Attachment();
        Assert.Equal(string.Empty, attachment.Url);
    }

    [Fact]
    public void RequestStatus_defines_full_lifecycle()
    {
        var values = Enum.GetValues<RequestStatus>();
        Assert.Equal(5, values.Length);
        Assert.Contains(RequestStatus.New, values);
        Assert.Contains(RequestStatus.Completed, values);
    }

    [Fact]
    public void SupportArea_covers_IT_and_FM()
    {
        Assert.Equal(2, Enum.GetValues<SupportArea>().Length);
    }

    [Fact]
    public void StaffAvailability_and_Urgency_enums_are_non_empty()
    {
        Assert.NotEmpty(Enum.GetValues<StaffAvailabilityStatus>());
        Assert.NotEmpty(Enum.GetValues<UrgencyLevel>());
    }
}
