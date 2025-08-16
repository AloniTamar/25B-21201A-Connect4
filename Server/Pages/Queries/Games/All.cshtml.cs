using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Games
{
    public class AllModel : PageModel
    {
        private readonly AppDbContext _db;
        public AllModel(AppDbContext db) => _db = db;

        public class Row
        {
            public int Id { get; set; }
            public int PlayerId { get; set; }
            public string PlayerName { get; set; } = "";
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public int? DurationSec { get; set; }
            public string Result { get; set; } = "";
            public string? Notes { get; set; }
        }

        public IList<Row> Items { get; private set; } = new List<Row>();

        public async Task OnGet()
        {
            Items = await _db.Games
                .AsNoTracking()
                .OrderByDescending(g => g.StartTime)
                .Select(g => new Row
                {
                    Id = g.Id,
                    PlayerId = g.PlayerId,
                    PlayerName = _db.Players.Where(p => p.Id == g.PlayerId)
                                            .Select(p => p.FirstName)
                                            .FirstOrDefault() ?? "(unknown)",
                    StartTime = g.StartTime,
                    EndTime = g.EndTime,
                    DurationSec = g.DurationSec,
                    Result = g.Result.ToString(),
                    Notes = g.Notes
                })
                .ToListAsync();
        }
    }
}
