using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LocationService.Domain.Entities;
using LocationService.Domain.Enums;

namespace LocationService.Tests.Layers.Domain;

/// <summary>Medium service — domain layer guideline ~15–40 tests.</summary>
public sealed class LocationServiceDomainLayerTests
{
    public static IEnumerable<object[]> RoomTypes() =>
        Enum.GetValues<RoomType>().Cast<RoomType>().Select(v => new object[] { v });

    [Theory]
    [MemberData(nameof(RoomTypes))]
    public void RoomType_values_are_defined(RoomType value) =>
        Enum.IsDefined(value).Should().BeTrue();

    [Fact]
    public void Building_Name_roundtrip()
    {
        var b = new Building { Name = "Hall A" };
        b.Name.Should().Be("Hall A");
        b.Rooms.Should().NotBeNull();
    }

    [Fact]
    public void Room_can_reference_building_and_type()
    {
        var building = new Building { Name = "B1" };
        var room = new Room
        {
            Name = "101",
            RoomType = RoomType.Classroom,
            Building = building
        };
        room.RoomType.Should().Be(RoomType.Classroom);
        room.Building.Should().BeSameAs(building);
    }

    [Theory]
    [InlineData(RoomType.Classroom)]
    [InlineData(RoomType.Lab)]
    [InlineData(RoomType.Office)]
    [InlineData(RoomType.Auditorium)]
    [InlineData(RoomType.Other)]
    public void RoomType_to_string_is_stable(RoomType type) =>
        type.ToString().Should().NotBeNullOrWhiteSpace();

    [Fact]
    public void Building_collection_initializes_empty_list()
    {
        new Building().Rooms.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(0)]
    public void Base_entity_id_on_building_roundtrips(int id)
    {
        var b = new Building { Id = id };
        b.Id.Should().Be(id);
    }
}
