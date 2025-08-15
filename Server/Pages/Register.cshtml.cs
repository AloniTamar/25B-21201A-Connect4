using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;
    public RegisterModel(AppDbContext db) => _db = db;

    // Countries for the combo (you can expand later)
    public List<string> Countries { get; } = new()
    { "Israel", "USA", "UK", "France", "Germany", "Spain", "Italy" };

    [BindProperty]
    public InputModel Form { get; set; } = new();

    public class InputModel
    {
        [Display(Name = "Unique Number")]
        [Range(1, 1000, ErrorMessage = "Unique number must be between 1 and 1000.")]
        public int? UniqueNumber { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First name is required.")]
        [MinLength(2, ErrorMessage = "First name must be at least 2 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        public string Country { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // Just render the form
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Server-side validation
        if (!ModelState.IsValid)
            return Page();

        // Uniqueness check for UniqueNumber
        var num = Form.UniqueNumber ?? 0;
        var exists = await _db.Players.AnyAsync(p => p.UniqueNumber == num);
        if (exists)
        {
            ModelState.AddModelError("Form.UniqueNumber", "This unique number already exists.");
            return Page();
        }

        // Create and save the player
        var player = new Player
        {
            UniqueNumber = num,
            FirstName = Form.FirstName.Trim(),
            Phone = Form.Phone.Trim(),
            Country = Form.Country
        };

        _db.Players.Add(player);
        await _db.SaveChangesAsync();

        // TODO: redirect to a success page or enable "Start New Game"
        TempData["RegisteredPlayerId"] = player.Id;
        return RedirectToPage("/Index");
    }
}
