using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LostFoundService.Domain.Enums;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Tests.Layers.Domain;

/// <summary>Medium service — domain layer guideline ~15–40 tests.</summary>
public sealed class LostFoundServiceDomainLayerTests
{
    public static IEnumerable<object[]> AllEnumsInDomain() =>
        from t in typeof(LostFoundItemType).Assembly.GetTypes()
        where t.IsEnum && t.Namespace == "LostFoundService.Domain.Enums"
        from object v in Enum.GetValues(t)
        select new object[] { t, v };

    [Theory]
    [MemberData(nameof(AllEnumsInDomain))]
    public void LostFound_domain_enums_are_defined(Type enumType, object value) =>
        Enum.IsDefined(enumType, value).Should().BeTrue();

    [Theory]
    [InlineData("missing")]
    [InlineData("x")]
    public void NotFoundException_preserves_message(string msg)
    {
        var ex = new NotFoundException(msg);
        ex.Message.Should().Be(msg);
    }

    [Theory]
    [InlineData("bad")]
    public void BadRequestException_preserves_message(string msg) =>
        new BadRequestException("ERR_BAD", msg).Message.Should().Be(msg);

    [Theory]
    [InlineData("forbidden")]
    public void ForbiddenException_preserves_message(string msg) =>
        new ForbiddenException(msg).Message.Should().Be(msg);

    [Theory]
    [InlineData("conflict")]
    public void ConflictException_preserves_message(string msg) =>
        new ConflictException("ERR_CONFLICT", msg).Message.Should().Be(msg);

    [Theory]
    [InlineData("unauth")]
    public void UnauthorizedException_preserves_message(string msg) =>
        new UnauthorizedException(msg).Message.Should().Be(msg);

    [Theory]
    [InlineData("svc")]
    public void ServiceUnavailableException_preserves_message(string msg) =>
        new ServiceUnavailableException(msg).Message.Should().Be(msg);

    [Theory]
    [InlineData(LostFoundItemType.Lost)]
    [InlineData(LostFoundItemType.Found)]
    public void LostFoundItemType_values_exist(LostFoundItemType v) =>
        Enum.IsDefined(v).Should().BeTrue();

    [Theory]
    [InlineData(LostFoundClaimStatus.PendingReview)]
    [InlineData(LostFoundClaimStatus.Approved)]
    [InlineData(LostFoundClaimStatus.Rejected)]
    public void LostFoundClaimStatus_defined(LostFoundClaimStatus v) =>
        Enum.IsDefined(v).Should().BeTrue();
}
