using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;

namespace Server.Pages.Games;

public class NewModel : PageModel
{
    private readonly AppDbContext _db;
    public NewModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int? GameId { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        // Retrieve PlayerId from TempData (was set at Login)
        if (!(TempData["PlayerId"] is int playerId))
        {
            // If TempData lost, redirect to Login
            return RedirectToPage("/Login");
        }

        var game = new Game { PlayerId = playerId };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        GameId = game.Id;

        // Keep PlayerId for subsequent requests
        TempData.Keep("PlayerId");
        TempData.Keep("PlayerName");

        return Page();
    }

    public void OnGet() { } // renders a small form to start a game
}
