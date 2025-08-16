using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Server.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            HttpContext.Session.Clear();
            TempData["Msg"] = "You have been logged out.";
            return RedirectToPage("/Index");
        }
    }
}
