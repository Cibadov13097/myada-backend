using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.UnitTests.Domain;

public class BaseAuditedEntityTests
{
    [Fact]
    public void SetCreated_assigns_user_and_timestamp_and_clears_modified()
    {
        var category = new Category();
        var before = DateTime.UtcNow.AddSeconds(-2);

        category.SetCreated(42);

        Assert.Equal(42, category.CreatedUserId);
        Assert.True(category.CreatedAt >= before);
        Assert.Null(category.ModifiedAt);
        Assert.Null(category.ModifiedUserId);
    }
}
