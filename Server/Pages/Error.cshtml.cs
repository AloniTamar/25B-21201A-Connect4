using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Server.Pages
{
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;
        public ErrorModel(ILogger<ErrorModel> logger) => _logger = logger;

        public string? RequestId { get; set; }
        public int? HttpStatus { get; set; }   // renamed to avoid hiding PageModel.StatusCode(int)
        public string? OriginalPath { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public void OnGet(int? code = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Unhandled exceptions
            var exFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exFeature != null)
            {
                HttpStatus = 500;
                OriginalPath = exFeature.Path;
                _logger.LogError(exFeature.Error,
                    "Unhandled exception at {Path}. RequestId={RequestId}",
                    exFeature.Path, RequestId);
                return;
            }

            // Re-executed 4xx/5xx
            var reexec = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            HttpStatus = code ?? HttpContext.Response?.StatusCode;
            OriginalPath = reexec?.OriginalPath ?? HttpContext.Request.Path;

            _logger.LogWarning("HTTP {Status} at {Path}. RequestId={RequestId}",
                HttpStatus, OriginalPath, RequestId);
        }
    }
}
