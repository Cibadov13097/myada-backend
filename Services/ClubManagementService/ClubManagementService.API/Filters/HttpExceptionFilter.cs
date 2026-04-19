using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClubManagementService.API.Filters;

public sealed class HttpExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case KeyNotFoundException:
                context.Result = new NotFoundObjectResult(new { message = "The requested resource was not found." });
                context.ExceptionHandled = true;
                break;
            case ArgumentException ex:
                context.Result = new BadRequestObjectResult(new { message = ex.Message });
                context.ExceptionHandled = true;
                break;
            case InvalidOperationException ex:
                context.Result = new ConflictObjectResult(new { message = ex.Message });
                context.ExceptionHandled = true;
                break;
        }
    }
}
