using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectFourWeb.Pages
{
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;
        public ErrorModel(ILogger<ErrorModel> logger) => _logger = logger;

        public string? RequestId { get; set; }
        public int? StatusCode { get; set; }
        public string? OriginalPath { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public void OnGet(int? code = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Unhandled exceptions:
            var exFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exFeature != null)
            {
                StatusCode = 500;
                OriginalPath = exFeature.Path;
                _logger.LogError(exFeature.Error,
                    "Unhandled exception at {Path}. RequestId={RequestId}",
                    exFeature.Path, RequestId);
                return;
            }

            // Re-executed 4xx/5xx:
            var reexec = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            StatusCode = code ?? HttpContext.Response?.StatusCode;
            OriginalPath = reexec?.OriginalPath ?? HttpContext.Request.Path;

            _logger.LogWarning("HTTP {Status} at {Path}. RequestId={RequestId}",
                StatusCode, OriginalPath, RequestId);
        }
    }
}
