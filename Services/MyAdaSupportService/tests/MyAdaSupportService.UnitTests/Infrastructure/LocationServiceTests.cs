using MyAdaSupportService.Infrastructure.Services;

namespace MyAdaSupportService.UnitTests.Infrastructure;

public class LocationServiceTests
{
    private readonly LocationService _sut = new();

    [Fact]
    public async Task ResolveLocationAsync_returns_default_when_no_details()
    {
        var text = await _sut.ResolveLocationAsync(false, null, false, null, null);
        Assert.Equal("Location not specified", text);
    }

    [Fact]
    public async Task ResolveLocationAsync_includes_building_when_flag_and_id_set()
    {
        var text = await _sut.ResolveLocationAsync(true, 12, false, null, null);
        Assert.Contains("Building 12", text);
    }

    [Fact]
    public async Task ResolveLocationAsync_includes_room_when_flag_and_id_set()
    {
        var text = await _sut.ResolveLocationAsync(false, null, true, 3, null);
        Assert.Contains("Room 3", text);
    }

    [Fact]
    public async Task ResolveLocationAsync_appends_area_details()
    {
        var text = await _sut.ResolveLocationAsync(false, null, false, null, "  North wing  ");
        Assert.Contains("North wing", text);
    }
}
