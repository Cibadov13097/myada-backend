using ClubManagementService.API.Controllers;
using ClubManagementService.API.Models;
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
        var multipartSchema = context.SchemaGenerator.GenerateSchema(typeof(ClubProposalMultipartForm), context.SchemaRepository);

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description = "Use application/json for existing file URLs, or multipart/form-data to upload logoFile and constitutionFile directly. For multipart requests, send otherMembersJson as repeated form fields.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = jsonSchema
                },
                ["multipart/form-data"] = new()
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
                }
            }
        };
    }
}
