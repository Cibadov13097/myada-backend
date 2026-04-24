using LostFoundService.Api.Controllers;
using LostFoundService.Api.Models.LostAndFound.Requests;
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

        if (context.MethodInfo.Name == nameof(LostAndFoundController.CreateLostReport))
        {
            operation.RequestBody = BuildRequestBody(
                context,
                typeof(UserLostReportRequest),
                typeof(UserLostReportMultipartRequest));
            return;
        }

        if (context.MethodInfo.Name == nameof(LostAndFoundController.CreateFoundReport))
        {
            operation.RequestBody = BuildRequestBody(
                context,
                typeof(LostAndFoundJsonReportRequest),
                typeof(LostAndFoundMultipartReportRequest));
            return;
        }

        if (context.MethodInfo.Name == nameof(LostAndFoundController.CreateManualItem))
        {
            operation.RequestBody = BuildRequestBody(
                context,
                typeof(ManualCreateItemRequest),
                typeof(ManualCreateItemMultipartRequest));
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
