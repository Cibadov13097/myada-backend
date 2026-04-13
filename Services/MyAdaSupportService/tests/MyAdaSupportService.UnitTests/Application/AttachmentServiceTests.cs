using Moq;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Application.Services;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.UnitTests.Application;

public class AttachmentServiceTests
{
    [Fact]
    public async Task AddAttachmentAsync_persists_through_repository()
    {
        var repo = new Mock<IAttachmentRepository>();
        var mapper = TestMapperFactory.Create();
        repo.Setup(r => r.AddAsync(It.IsAny<Attachment>()))
            .ReturnsAsync((Attachment a) => { a.Id = 5; return a; });
        var sut = new AttachmentService(repo.Object, mapper);
        var dto = new AttachmentCreateDto { RequestId = 1, Url = "https://x/file.pdf" };

        var result = await sut.AddAttachmentAsync(dto);

        Assert.Equal(5, result.Id);
        repo.Verify(r => r.AddAsync(It.Is<Attachment>(a => a.Url.Contains("file.pdf"))), Times.Once);
    }

    [Fact]
    public async Task GetRequestAttachmentsAsync_maps_repository_results()
    {
        var repo = new Mock<IAttachmentRepository>();
        var mapper = TestMapperFactory.Create();
        repo.Setup(r => r.GetByRequestIdAsync(9))
            .ReturnsAsync(new List<Attachment>
            {
                new() { Id = 1, RequestId = 9, Url = "a" },
                new() { Id = 2, RequestId = 9, Url = "b" }
            });
        var sut = new AttachmentService(repo.Object, mapper);

        var result = await sut.GetRequestAttachmentsAsync(9);

        Assert.Equal(2, result.Count);
        repo.Verify(r => r.GetByRequestIdAsync(9), Times.Once);
    }

    [Fact]
    public async Task RemoveAttachmentAsync_deletes_when_found()
    {
        var repo = new Mock<IAttachmentRepository>();
        var mapper = new Mock<AutoMapper.IMapper>().Object;
        var entity = new Attachment { Id = 2, RequestId = 1, Url = "u" };
        repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(entity);
        repo.Setup(r => r.RemoveAsync(entity)).Returns(Task.CompletedTask);
        var sut = new AttachmentService(repo.Object, mapper);

        await sut.RemoveAttachmentAsync(2);

        repo.Verify(r => r.RemoveAsync(entity), Times.Once);
    }
}
