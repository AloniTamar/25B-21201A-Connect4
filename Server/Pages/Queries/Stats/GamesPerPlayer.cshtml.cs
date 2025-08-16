using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Stats
{
    public class GamesPerPlayerModel : PageModel
    {
        private readonly AppDbContext _db;
        public GamesPerPlayerModel(AppDbContext db) => _db = db;

        public class Row
        {
            public int PlayerId { get; set; }
            public string Name { get; set; } = "";
            public int GamesCount { get; set; }
        }

        public IList<Row> Items { get; private set; } = new List<Row>();

        public async Task OnGet()
        {
            // LEFT JOIN players -> games to include players with 0 games
            var data = await
                (from p in _db.Players.AsNoTracking()
                 join g in _db.Games.AsNoTracking() on p.Id equals g.PlayerId into gj
                 select new
                 {
                     p.Id,
                     p.FirstName,
                     NameCI = EF.Functions.Collate(p.FirstName, "SQL_Latin1_General_CP1_CI_AS"),
                     Count = gj.Count()
                 })
                .OrderByDescending(x => x.Count)   // sort by count desc
                .ThenBy(x => x.NameCI)             // tie-break by name (case-insensitive)
                .ToListAsync();

            Items = data.Select(x => new Row
            {
                PlayerId = x.Id,
                Name = x.FirstName,
                GamesCount = x.Count
            }).ToList();
        }
    }
}
