using ClubManagementService.API.Controllers;
using ClubManagementService.Application.DTOs.Contracts;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClubManagementService.API.Swagger;

public sealed class ClubProposalCreateRequestBodyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(ClubProposalsController)
            || !string.Equals(context.MethodInfo.Name, nameof(ClubProposalsController.Create), StringComparison.Ordinal))
        {
            return;
        }

        var jsonSchema = context.SchemaGenerator.GenerateSchema(typeof(ClubProposalSubmissionDto), context.SchemaRepository);
        var multipartSchema = BuildMultipartSchema();

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description = "Use application/json for existing file URLs, or multipart/form-data to upload logoFile and constitutionFile directly. For multipart requests, send otherMembersJson as repeated form fields.",
            Content = new Dictionary<string, OpenApiMediaType>()
        };

        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = multipartSchema,
            Encoding = new Dictionary<string, OpenApiEncoding>
            {
                ["otherMembersJson"] = new()
                {
                    Style = ParameterStyle.Form,
                    Explode = true
                }
            }
        };

        operation.RequestBody.Content["application/json"] = new OpenApiMediaType
        {
            Schema = jsonSchema
        };
    }

    private static OpenApiSchema BuildMultipartSchema() =>
        new()
        {
            Type = "object",
            Required = new HashSet<string>
            {
                "name",
                "description",
                "logoFile",
                "constitutionFile"
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["id"] = StringSchema(),
                ["name"] = StringSchema(),
                ["description"] = StringSchema(),
                ["logoFile"] = BinarySchema(),
                ["constitutionFile"] = BinarySchema(),
                ["presidentStudentId"] = StringSchema(),
                ["vicePresidentStudentId"] = StringSchema(),
                ["otherMembersJson"] = new()
                {
                    Type = "array",
                    Items = StringSchema(),
                    Description = "Repeat this field for each user id when submitting multipart/form-data."
                },
                ["commitment"] = StringSchema(),
                ["shortDesc"] = StringSchema(),
                ["uniqueDesc"] = StringSchema(),
                ["goals"] = StringSchema(),
                ["activities"] = StringSchema(),
                ["alignment"] = StringSchema(),
                ["vision"] = StringSchema()
            }
        };

    private static OpenApiSchema StringSchema() => new()
    {
        Type = "string"
    };

    private static OpenApiSchema BinarySchema() => new()
    {
        Type = "string",
        Format = "binary"
    };
}
