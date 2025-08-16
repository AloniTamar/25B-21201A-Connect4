using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;
        public LoginModel(AppDbContext db) => _db = db;

        public class InputModel
        {
            [Required, Range(1, 1000, ErrorMessage = "Unique number must be 1–1000.")]
            public int UniqueNumber { get; set; }

            public string? Next { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public void OnGet(string? next = null)
        {
            Input.Next = next;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var p = await _db.Players.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UniqueNumber == Input.UniqueNumber);

            if (p == null)
            {
                ModelState.AddModelError(string.Empty, "No player found with that number.");
                return Page();
            }

            HttpContext.Session.SetInt32("PlayerId", p.Id);
            HttpContext.Session.SetString("PlayerName", p.FirstName ?? "");
            TempData["Msg"] = $"Welcome, {p.FirstName}!";

            if (!string.IsNullOrWhiteSpace(Input.Next))
                return Redirect(Input.Next!);

            return RedirectToPage("/Index");
        }
    }
}
