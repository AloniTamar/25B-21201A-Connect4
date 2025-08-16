using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _db;
        public RegisterModel(AppDbContext db) => _db = db;

        public class InputModel
        {
            [Required, Range(1, 1000, ErrorMessage = "Unique number must be 1–1000.")]
            public int UniqueNumber { get; set; }

            [Required, StringLength(100)]
            public string FirstName { get; set; } = "";

            [StringLength(50)]
            public string? Phone { get; set; }

            [StringLength(100)]
            public string? Country { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // 🔽 Country dropdown options for the view
        public IList<SelectListItem> CountryOptions { get; private set; } = new List<SelectListItem>();

        private void PopulateCountries(string? selected = null)
        {
            var countries = new[]
            {
                "Israel","United States","United Kingdom","Canada","Germany","France",
                "Spain","Italy","India","Japan","Australia","Other"
            };

            CountryOptions = countries
                .Select(c => new SelectListItem { Value = c, Text = c, Selected = (selected == c) })
                .ToList();
        }

        public void OnGet()
        {
            PopulateCountries(Input.Country);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Ensure dropdown is populated on validation errors
            PopulateCountries(Input.Country);

            if (!ModelState.IsValid) return Page();

            // Uniqueness check
            var exists = await _db.Players.AsNoTracking()
                .AnyAsync(p => p.UniqueNumber == Input.UniqueNumber);
            if (exists)
            {
                ModelState.AddModelError(nameof(Input.UniqueNumber), "That unique number is already taken.");
                return Page();
            }

            // Insert without referencing the entity type directly
            var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO Players (UniqueNumber, FirstName, Phone, Country, CreatedAt)
                VALUES ({Input.UniqueNumber}, {Input.FirstName}, {Input.Phone}, {Input.Country}, SYSUTCDATETIME());
            ");

            // Load the newly created player
            var player = await _db.Players.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UniqueNumber == Input.UniqueNumber);

            if (player == null)
            {
                TempData["Err"] = "Registration failed to persist. Please try again.";
                return RedirectToPage("/Auth/Register");
            }

            // Auto-login
            var idProp = player.GetType().GetProperty("Id");
            var nameProp = player.GetType().GetProperty("FirstName");

            var id = (int)(idProp?.GetValue(player) ?? 0);
            var name = (string?)nameProp?.GetValue(player) ?? "";

            HttpContext.Session.SetInt32("PlayerId", id);
            HttpContext.Session.SetString("PlayerName", name);

            TempData["Msg"] = $"Welcome, {name}! Your account was created.";
            return RedirectToPage("/Index");
        }
    }
}
