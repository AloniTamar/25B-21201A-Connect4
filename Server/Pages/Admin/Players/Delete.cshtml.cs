using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin.Players
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _db;
        public DeleteModel(AppDbContext db) => _db = db;

        public class SummaryVm
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public int GamesCount { get; set; }
            public int MovesCount { get; set; }
        }

        [BindProperty]
        public int Id { get; set; }

        public SummaryVm? Summary { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var player = await _db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            var gamesQ = _db.Games.AsNoTracking().Where(g => g.PlayerId == id);
            var games = await gamesQ.Select(g => g.Id).ToListAsync();
            var movesCount = await _db.Moves.AsNoTracking().CountAsync(m => games.Contains(m.GameId));

            Summary = new SummaryVm
            {
                Id = player.Id,
                Name = player.FirstName ?? "",
                GamesCount = games.Count,
                MovesCount = movesCount
            };
            Id = id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == Id);
            if (player == null) return NotFound();

            // delete all moves for this player's games
            var gameIds = await _db.Games
                .Where(g => g.PlayerId == Id)
                .Select(g => g.Id)
                .ToListAsync();

            if (gameIds.Count > 0)
            {
                var moves = _db.Moves.Where(m => gameIds.Contains(m.GameId));
                _db.Moves.RemoveRange(moves);

                var games = _db.Games.Where(g => gameIds.Contains(g.Id));
                _db.Games.RemoveRange(games);
            }

            _db.Players.Remove(player);

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Player and related games/moves deleted.";
            return RedirectToPage("/Queries/Players/Index");
        }
    }
}
