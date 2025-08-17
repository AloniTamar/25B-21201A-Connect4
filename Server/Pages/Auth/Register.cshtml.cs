using System.Collections.Generic;
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

            [Required, StringLength(50, ErrorMessage = "Phone is required (max 50).")]
            public string Phone { get; set; } = "";

            [Required, StringLength(100, ErrorMessage = "Country is required.")]
            public string Country { get; set; } = "";
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Country dropdown options for the view
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
            // Keep dropdown populated on validation errors
            PopulateCountries(Input.Country);

            // Normalize/trims
            var unique = Input.UniqueNumber;
            var first = (Input.FirstName ?? "").Trim();
            var phoneRaw = (Input.Phone ?? "").Trim();
            var country = (Input.Country ?? "").Trim();

            if (string.IsNullOrWhiteSpace(first) || first.Length < 2)
                ModelState.AddModelError(nameof(Input.FirstName), "First name must be at least 2 letters.");

            if (unique < 1 || unique > 1000)
                ModelState.AddModelError(nameof(Input.UniqueNumber), "Unique number must be between 1 and 1000.");

            // Keep digits only, and enforce 9–10 digits
            var phoneDigits = new string(phoneRaw.Where(char.IsDigit).ToArray());
            if (phoneDigits.Length < 9 || phoneDigits.Length > 10)
                ModelState.AddModelError(nameof(Input.Phone), "Phone must be digits only (9–10 digits).");

            if (string.IsNullOrWhiteSpace(country))
                ModelState.AddModelError(nameof(Input.Country), "Country is required.");

            // Ensure selected country is from the dropdown
            var allowedCountries = CountryOptions.Select(o => o.Value).ToHashSet();
            if (!allowedCountries.Contains(country))
                ModelState.AddModelError(nameof(Input.Country), "Please choose a country from the list.");

            if (!ModelState.IsValid)
                return Page();

            // Uniqueness check
            var exists = await _db.Players.AsNoTracking()
                .AnyAsync(p => p.UniqueNumber == unique);
            if (exists)
            {
                ModelState.AddModelError(nameof(Input.UniqueNumber), "That unique number is already taken.");
                return Page();
            }

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO Players (UniqueNumber, FirstName, Phone, Country, CreatedAt)
                VALUES ({unique}, {first}, {phoneDigits}, {country}, SYSUTCDATETIME());
            ");

            // Load the newly created player
            var player = await _db.Players.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UniqueNumber == unique);

            if (player == null)
            {
                TempData["Err"] = "Registration failed to persist. Please try again.";
                return RedirectToPage("/Auth/Register");
            }

            // Extract Id/Name safely
            var idProp = player.GetType().GetProperty("Id");
            var nameProp = player.GetType().GetProperty("FirstName");
            var id = (int)(idProp?.GetValue(player) ?? 0);
            var name = (string?)nameProp?.GetValue(player) ?? "";

            // Session sign-in (keeps your existing behavior)
            HttpContext.Session.SetInt32("PlayerId", id);
            HttpContext.Session.SetString("PlayerName", name);

            // Also set short cookies, in case your layout/home checks cookies
            var opts = new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("PlayerId", id.ToString(), opts);
            Response.Cookies.Append("PlayerName", name, opts);
            Response.Cookies.Append("Identifier", id.ToString(), opts); // if your home uses Identifier

            TempData["Msg"] = $"Welcome, {name}! Your account was created.";
            return RedirectToPage("/Index");
        }
    }
}
