using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Server.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsLoggedIn { get; private set; }
        public string? PlayerName { get; private set; }
        public int? PlayerId { get; private set; }

        public void OnGet()
        {
            PlayerId = HttpContext.Session.GetInt32("PlayerId");
            IsLoggedIn = PlayerId.HasValue;
            PlayerName = HttpContext.Session.GetString("PlayerName");
        }
    }
}
