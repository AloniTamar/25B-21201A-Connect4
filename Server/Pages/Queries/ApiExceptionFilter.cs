using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Server.Queries
{
    /// <summary>
    /// Catches unhandled exceptions in Web API actions and returns ProblemDetails JSON.
    /// </summary>
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;
        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) => _logger = logger;

        public void OnException(ExceptionContext context)
        {
            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

            _logger.LogError(context.Exception,
                "Unhandled API exception at {Path}. TraceId={TraceId}",
                context.HttpContext.Request.Path, traceId);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Please try again. If the problem persists, contact support.",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = traceId;

            context.Result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
        }
    }
}
