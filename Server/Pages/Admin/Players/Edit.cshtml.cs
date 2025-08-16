using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin.Players
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        public EditModel(AppDbContext db) => _db = db;

        // ViewModel to avoid coupling the view to the entity namespace
        public class InputModel
        {
            [Required]
            public int Id { get; set; }

            [Range(1, 1000, ErrorMessage = "Unique number must be between 1 and 1000.")]
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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var p = await _db.Players.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            Input = new InputModel
            {
                Id = p.Id,
                UniqueNumber = p.UniqueNumber,
                FirstName = p.FirstName ?? "",
                Phone = p.Phone,
                Country = p.Country
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Basic validation
            if (!ModelState.IsValid) return Page();

            // Uniqueness check (excluding this player)
            var exists = await _db.Players
                .AnyAsync(x => x.Id != Input.Id && x.UniqueNumber == Input.UniqueNumber);
            if (exists)
            {
                ModelState.AddModelError(nameof(Input.UniqueNumber), "This unique number is already in use.");
                return Page();
            }

            // Load, update, save (no need to reference the entity type by name)
            var player = await _db.Players.FirstOrDefaultAsync(x => x.Id == Input.Id);
            if (player == null) return NotFound();

            player.UniqueNumber = Input.UniqueNumber;
            player.FirstName = Input.FirstName;
            player.Phone = Input.Phone;
            player.Country = Input.Country;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Player updated.";
            return RedirectToPage("/Queries/Players/Index");
        }
    }
}
