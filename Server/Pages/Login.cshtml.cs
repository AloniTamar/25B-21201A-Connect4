using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    public LoginModel(AppDbContext db) => _db = db;

    [BindProperty]
    public InputModel Form { get; set; } = new();

    public class InputModel
    {
        [Display(Name = "Unique Number")]
        [Required, Range(1, 1000)]
        public int? UniqueNumber { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var num = Form.UniqueNumber ?? 0;
        var player = await _db.Players.FirstOrDefaultAsync(p => p.UniqueNumber == num);
        if (player == null)
        {
            ModelState.AddModelError("Form.UniqueNumber", "Player not found. Please register first.");
            return Page();
        }

        // simple session via TempData for now (we can switch to cookies later)
        TempData["PlayerId"] = player.Id;
        TempData["PlayerName"] = player.FirstName;

        // TODO: redirect to game lobby/creation page next step
        return RedirectToPage("/Index");
    }
}