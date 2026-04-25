using ClubManagementService.API.Controllers;
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

        var jsonSchema = BuildJsonSchema();
        var multipartSchema = BuildMultipartSchema();

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description = "Use application/json for existing file URLs, or multipart/form-data to upload logoFile and constitutionFile directly. organizationId, presidentStudentId, vicePresidentStudentId, and each otherMembersJson item must be exactly 9 digits. For multipart requests, send otherMembersJson as repeated form fields.",
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
                "organizationId",
                "name",
                "logoFile",
                "constitutionFile"
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["organizationId"] = NineDigitSchema("Submitter organization id. Must be exactly 9 digits."),
                ["name"] = StringSchema(),
                ["description"] = StringSchema(),
                ["logoFile"] = BinarySchema(),
                ["constitutionFile"] = BinarySchema(),
                ["presidentStudentId"] = NineDigitSchema("President student id. Must be exactly 9 digits."),
                ["vicePresidentStudentId"] = NineDigitSchema("Vice president student id. Must be exactly 9 digits."),
                ["otherMembersJson"] = new()
                {
                    Type = "array",
                    Items = NineDigitSchema("Club member organization id. Must be exactly 9 digits."),
                    Description = "Repeat this field for each 9-digit user organization id when submitting multipart/form-data."
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

    private static OpenApiSchema BuildJsonSchema() =>
        new()
        {
            Type = "object",
            Required = new HashSet<string>
            {
                "organizationId",
                "name",
                "logoFile",
                "constitutionFile"
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["organizationId"] = NineDigitSchema("Submitter organization id. Must be exactly 9 digits."),
                ["name"] = StringSchema(),
                ["description"] = StringSchema(),
                ["logoFile"] = StringSchema("Already-uploaded logo URL/reference."),
                ["constitutionFile"] = StringSchema("Already-uploaded constitution URL/reference."),
                ["presidentStudentId"] = NineDigitSchema("President student id. Must be exactly 9 digits."),
                ["vicePresidentStudentId"] = NineDigitSchema("Vice president student id. Must be exactly 9 digits."),
                ["otherMembersJson"] = new()
                {
                    Type = "array",
                    Items = NineDigitSchema("Club member organization id. Must be exactly 9 digits.")
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

    private static OpenApiSchema StringSchema(string? description = null) => new()
    {
        Type = "string",
        Description = description
    };

    private static OpenApiSchema NineDigitSchema(string? description = null) => new()
    {
        Type = "string",
        Pattern = @"^\d{9}$",
        Description = description
    };

    private static OpenApiSchema BinarySchema() => new()
    {
        Type = "string",
        Format = "binary"
    };
}
