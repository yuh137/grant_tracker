using GrantTracker.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Api.Errors;

public class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = null;

        if (exception is BusinessRuleException)
        {
            var rule = (BusinessRuleException)exception;
            problem = new ();
            problem.Status = 422;
            problem.Title = "Business rule violated";
            problem.Detail = rule.Message;
            problem.Extensions.Add("code", rule.ErrorCode.ToString());
        }
        else if (exception is NotFoundException)
        {
            var notFound = (NotFoundException)exception;
            problem = new ();
            problem.Status = 404;
            problem.Title = "Resource not found";
            problem.Detail = notFound.Message;
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            problem = new ();
            problem.Status = 409;
            problem.Title = "Concurrent modification";
            problem.Detail = "The record was changed by another request. Reload it and try again.";
        }

        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }
}
