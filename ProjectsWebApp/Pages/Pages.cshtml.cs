using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

public class ErrorModel : PageModel
{
    private readonly ILogger<ErrorModel> _logger;

    public int? StatusCode { get; private set; }

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet(int? statusCode = null)
    {
        StatusCode = statusCode;
        _logger.LogError("Error page hit with status code {StatusCode}", statusCode);
    }
}
