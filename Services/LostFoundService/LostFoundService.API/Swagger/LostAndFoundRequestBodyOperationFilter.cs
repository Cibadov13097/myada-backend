using LostFoundService.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LostFoundService.Api.Swagger;

public sealed class LostAndFoundRequestBodyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(LostAndFoundController))
        {
            return;
        }

        if (context.MethodInfo.Name is nameof(LostAndFoundController.CreateLostReport)
            or nameof(LostAndFoundController.CreateFoundReport)
            or nameof(LostAndFoundController.CreateManualItem))
        {
            operation.RequestBody = BuildRequestBody(
                context,
                typeof(LostAndFoundJsonReportRequest),
                typeof(LostAndFoundMultipartReportRequest));
            return;
        }

        if (context.MethodInfo.Name == nameof(LostAndFoundController.Upload))
        {
            operation.RequestBody = BuildRequestBody(
                context,
                multipartSchemaType: typeof(LostAndFoundUploadRequest));
        }
    }

    private static OpenApiRequestBody BuildRequestBody(
        OperationFilterContext context,
        Type? jsonSchemaType = null,
        Type? multipartSchemaType = null)
    {
        var content = new Dictionary<string, OpenApiMediaType>();

        if (jsonSchemaType is not null)
        {
            content["application/json"] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(jsonSchemaType, context.SchemaRepository)
            };
        }

        if (multipartSchemaType is not null)
        {
            content["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(multipartSchemaType, context.SchemaRepository)
            };
        }

        return new OpenApiRequestBody
        {
            Required = true,
            Content = content
        };
    }
}

public sealed class LostAndFoundJsonReportRequest
{
    public string? Type { get; set; }
    public string? ItemName { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? Location { get; set; }
    public string? LocationType { get; set; }
    public string? BuildingCode { get; set; }
    public string? IsRoom { get; set; }
    public string? RoomOrArea { get; set; }
    public string? CampusLocation { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? RoomArea { get; set; }
    public string? LastKnownLocation { get; set; }
    public string? DateLost { get; set; }
    public string? TimeLost { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? CollectionPlace { get; set; }
    public string? Status { get; set; }
    public string? AdminStatus { get; set; }
    public string? StorageBinId { get; set; }
    public string? Condition { get; set; }
    public string? AdminNotes { get; set; }
    public string? ReportedColor { get; set; }
    public bool? ConfirmAccuracy { get; set; }
    public string? ClaimantName { get; set; }
    public string? ClaimantStudentId { get; set; }
    public bool? StudentIdVerified { get; set; }
}

public sealed class LostAndFoundMultipartReportRequest
{
    public string? Type { get; set; }
    public string? ItemName { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? Location { get; set; }
    public string? CustomLocation { get; set; }
    public string? LocationType { get; set; }
    public string? BuildingCode { get; set; }
    public string? IsRoom { get; set; }
    public string? RoomOrArea { get; set; }
    public string? CampusLocation { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? RoomArea { get; set; }
    public string? LastKnownLocation { get; set; }
    public string? DateLost { get; set; }
    public string? TimeLost { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public List<IFormFile>? Files { get; set; }
    public IFormFile? IntakePhotoFile { get; set; }
    public IFormFile? HandoverProofFile { get; set; }
    public string? CollectionPlace { get; set; }
    public string? Status { get; set; }
    public string? AdminStatus { get; set; }
    public string? StorageBinId { get; set; }
    public string? Condition { get; set; }
    public string? AdminNotes { get; set; }
    public string? ReportedColor { get; set; }
    public bool? ConfirmAccuracy { get; set; }
    public string? ClaimantName { get; set; }
    public string? ClaimantStudentId { get; set; }
    public bool? StudentIdVerified { get; set; }
}

public sealed class LostAndFoundUploadRequest
{
    public IFormFile? File { get; set; }
    public IFormFile? Photo { get; set; }
    public IFormFile? Image { get; set; }
    public IFormFile? ImageFile { get; set; }
}
