using ClubManagementService.API.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClubManagementService.API.Swagger;

public sealed class EventProposalCreateRequestBodyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerType = context.MethodInfo.DeclaringType;
        var methodName = context.MethodInfo.Name;
        var isClubAdminEndpoint = controllerType == typeof(ClubAdminController)
            && string.Equals(methodName, nameof(ClubAdminController.EventProposal), StringComparison.Ordinal);
        var isStudentServicesEndpoint = controllerType == typeof(StudentServicesController)
            && string.Equals(methodName, nameof(StudentServicesController.CreateStaffEventProposal), StringComparison.Ordinal);

        if (!isClubAdminEndpoint && !isStudentServicesEndpoint)
            return;

        if (isClubAdminEndpoint)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Description = "Use multipart/form-data and attach imageFile directly for club-admin event proposals. Send subEventsJson as a JSON array string and logisticsJson as a JSON object string.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new()
                    {
                        Schema = BuildMultipartSchema(includeImageUrl: false)
                    }
                }
            };
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description = "Use application/json when imageUrl already exists, or multipart/form-data to upload imageFile directly. For multipart requests, send subEventsJson as a JSON array string and logisticsJson as a JSON object string.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = BuildJsonSchema()
                },
                ["multipart/form-data"] = new()
                {
                    Schema = BuildMultipartSchema(includeImageUrl: true)
                }
            }
        };
    }

    private static OpenApiSchema BuildJsonSchema() =>
        new()
        {
            Type = "object",
            Required = new HashSet<string>
            {
                "name",
                "dateTime",
                "duration",
                "attendance",
                "venue",
                "description",
                "objectives"
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["name"] = StringSchema(),
                ["dateTime"] = StringSchema("Event start datetime, for example 2026-04-26T14:30:00Z."),
                ["duration"] = StringSchema("Duration in hours."),
                ["attendance"] = StringSchema("Expected attendance."),
                ["venue"] = StringSchema(),
                ["description"] = StringSchema(),
                ["objectives"] = StringSchema(),
                ["imageUrl"] = StringSchema("Already-uploaded proposal image URL."),
                ["submittedByOrganization"] = StringSchema(),
                ["subEvents"] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["title"] = StringSchema(),
                            ["capacity"] = StringSchema(),
                            ["start"] = StringSchema(),
                            ["end"] = StringSchema(),
                            ["date"] = StringSchema()
                        }
                    }
                },
                ["logistics"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["avSetup"] = new OpenApiSchema { Type = "boolean" },
                        ["security"] = new OpenApiSchema { Type = "boolean" },
                        ["catering"] = new OpenApiSchema { Type = "boolean" },
                        ["cleaning"] = new OpenApiSchema { Type = "boolean" },
                        ["otherNeeds"] = StringSchema()
                    }
                }
            }
        };

    private static OpenApiSchema BuildMultipartSchema(bool includeImageUrl)
    {
        var properties = new Dictionary<string, OpenApiSchema>
        {
            ["name"] = StringSchema(),
            ["dateTime"] = StringSchema("Event start datetime, for example 2026-04-26T14:30:00Z."),
            ["duration"] = StringSchema("Duration in hours."),
            ["attendance"] = StringSchema("Expected attendance."),
            ["venue"] = StringSchema(),
            ["description"] = StringSchema(),
            ["objectives"] = StringSchema(),
            ["imageFile"] = BinarySchema(),
            ["submittedByOrganization"] = StringSchema(),
            ["subEventsJson"] = StringSchema("JSON array of sub-events."),
            ["logisticsJson"] = StringSchema("JSON object for logistics settings.")
        };

        if (includeImageUrl)
            properties["imageUrl"] = StringSchema("Optional existing image URL. If imageFile is also provided, the uploaded file wins.");

        return new OpenApiSchema
        {
            Type = "object",
            Required = new HashSet<string>
            {
                "name",
                "dateTime",
                "duration",
                "attendance",
                "venue",
                "description",
                "objectives"
            },
            Properties = properties
        };
    }

    private static OpenApiSchema StringSchema(string? description = null) => new()
    {
        Type = "string",
        Description = description
    };

    private static OpenApiSchema BinarySchema() => new()
    {
        Type = "string",
        Format = "binary"
    };
}
