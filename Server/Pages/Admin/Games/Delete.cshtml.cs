using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin.Games
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _db;
        public DeleteModel(AppDbContext db) => _db = db;

        public class SummaryVm
        {
            public int Id { get; set; }
            public int PlayerId { get; set; }
            public string PlayerName { get; set; } = "";
            public string Result { get; set; } = "";
            public System.DateTime StartTime { get; set; }
            public System.DateTime? EndTime { get; set; }
            public int? DurationSec { get; set; }
            public int MovesCount { get; set; }
        }

        [BindProperty]
        public int Id { get; set; }
        public SummaryVm? Summary { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var g = await _db.Games.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (g == null) return NotFound();

            var playerName = await _db.Players.AsNoTracking()
                .Where(p => p.Id == g.PlayerId)
                .Select(p => p.FirstName)
                .FirstOrDefaultAsync() ?? "(unknown)";

            var movesCount = await _db.Moves.AsNoTracking().CountAsync(m => m.GameId == id);

            Summary = new SummaryVm
            {
                Id = g.Id,
                PlayerId = g.PlayerId,
                PlayerName = playerName,
                Result = g.Result.ToString(),
                StartTime = g.StartTime,
                EndTime = g.EndTime,
                DurationSec = g.DurationSec,
                MovesCount = movesCount
            };
            Id = id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var game = await _db.Games.FirstOrDefaultAsync(x => x.Id == Id);
            if (game == null) return NotFound();

            var moves = _db.Moves.Where(m => m.GameId == Id);
            _db.Moves.RemoveRange(moves);
            _db.Games.Remove(game);

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Game and its moves deleted.";
            return RedirectToPage("/Queries/Games/All");
        }
    }
}
