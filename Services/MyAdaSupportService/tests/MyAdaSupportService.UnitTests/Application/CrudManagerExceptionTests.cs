using Moq;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.DTOs.Category;
using MyAdaSupportService.Application.Services;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.UnitTests.Application;

public class CrudManagerExceptionTests
{
    [Fact]
    public async Task RemoveAsync_propagates_KeyNotFound_when_entity_missing()
    {
        var repo = new Mock<ICategoryRepository>();
        var mapper = new Mock<AutoMapper.IMapper>().Object;
        repo.Setup(r => r.GetByIdAsync(404)).ThrowsAsync(new KeyNotFoundException("missing"));
        var sut = new CategoryService(repo.Object, mapper);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.DeleteCategoryAsync(404));
    }
}
