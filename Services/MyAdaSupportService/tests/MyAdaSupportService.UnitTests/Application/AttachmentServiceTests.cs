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
        var requestRepo = new Mock<IRequestRepository>();
        var fileStorage = new Mock<IFileStorageService>();
        var realtime = new Mock<ISupportRealtimeNotifier>();
        realtime.Setup(r => r.NotifySupportRequestChangedAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var mapper = TestMapperFactory.Create();
        requestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Request { Id = 1, ImgUrl = null });
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        fileStorage.Setup(s => s.IsManagedImageUrl(It.IsAny<string?>())).Returns(true);
        repo.Setup(r => r.AddAsync(It.IsAny<Attachment>()))
            .ReturnsAsync((Attachment a) => { a.Id = 5; return a; });
        var sut = new AttachmentService(repo.Object, requestRepo.Object, fileStorage.Object, mapper, realtime.Object);
        var dto = new AttachmentCreateDto { RequestId = 1, Url = "https://bucket.example.com/support/requests/attachments/file.png" };

        var result = await sut.AddAttachmentAsync(dto);

        Assert.Equal(5, result.Id);
        repo.Verify(r => r.AddAsync(It.Is<Attachment>(a => a.Url.Contains("file.png"))), Times.Once);
    }

    [Fact]
    public async Task GetRequestAttachmentsAsync_maps_repository_results()
    {
        var repo = new Mock<IAttachmentRepository>();
        var requestRepo = new Mock<IRequestRepository>();
        var fileStorage = new Mock<IFileStorageService>();
        var realtime = new Mock<ISupportRealtimeNotifier>();
        realtime.Setup(r => r.NotifySupportRequestChangedAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var mapper = TestMapperFactory.Create();
        fileStorage.Setup(s => s.IsManagedImageUrl(It.IsAny<string?>())).Returns(true);
        repo.Setup(r => r.GetByRequestIdAsync(9))
            .ReturnsAsync(new List<Attachment>
            {
                new() { Id = 1, RequestId = 9, Url = "a" },
                new() { Id = 2, RequestId = 9, Url = "b" }
            });
        var sut = new AttachmentService(repo.Object, requestRepo.Object, fileStorage.Object, mapper, realtime.Object);

        var result = await sut.GetRequestAttachmentsAsync(9);

        Assert.Equal(2, result.Count);
        repo.Verify(r => r.GetByRequestIdAsync(9), Times.Once);
    }

    [Fact]
    public async Task RemoveAttachmentAsync_deletes_when_found()
    {
        var repo = new Mock<IAttachmentRepository>();
        var requestRepo = new Mock<IRequestRepository>();
        var fileStorage = new Mock<IFileStorageService>();
        var realtime = new Mock<ISupportRealtimeNotifier>();
        realtime.Setup(r => r.NotifySupportRequestChangedAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var mapper = new Mock<AutoMapper.IMapper>().Object;
        var entity = new Attachment { Id = 2, RequestId = 1, Url = "u" };
        repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(entity);
        repo.Setup(r => r.RemoveAsync(entity)).Returns(Task.CompletedTask);
        requestRepo.Setup(r => r.GetByIdWithDetailsOrDefaultAsync(1))
            .ReturnsAsync(new Request { Id = 1, ImgUrl = "u", Attachments = new List<Attachment>() });
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        fileStorage.Setup(s => s.DeleteIfManagedAsync("u", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = new AttachmentService(repo.Object, requestRepo.Object, fileStorage.Object, mapper, realtime.Object);

        await sut.RemoveAttachmentAsync(2);

        repo.Verify(r => r.RemoveAsync(entity), Times.Once);
        requestRepo.Verify(r => r.UpdateAsync(It.Is<Request>(x => x.Id == 1 && x.ImgUrl == null)), Times.Once);
        fileStorage.Verify(s => s.DeleteIfManagedAsync("u", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAttachmentAsync_rejects_non_image_bucket_url()
    {
        var repo = new Mock<IAttachmentRepository>();
        var requestRepo = new Mock<IRequestRepository>();
        var fileStorage = new Mock<IFileStorageService>();
        var realtime = new Mock<ISupportRealtimeNotifier>();
        var mapper = TestMapperFactory.Create();

        fileStorage.Setup(s => s.IsManagedImageUrl(It.IsAny<string?>())).Returns(false);
        var sut = new AttachmentService(repo.Object, requestRepo.Object, fileStorage.Object, mapper, realtime.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddAttachmentAsync(new AttachmentCreateDto
            {
                RequestId = 1,
                Url = "https://bucket.example.com/support/requests/attachments/file.pdf"
            }));
    }
}
