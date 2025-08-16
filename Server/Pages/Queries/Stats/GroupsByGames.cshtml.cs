using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Stats
{
    public class GroupsByGamesModel : PageModel
    {
        private readonly AppDbContext _db;
        public GroupsByGamesModel(AppDbContext db) => _db = db;

        public record PlayerRow(int Id, string Name, string Phone, string Country, int GamesCount);

        public List<PlayerRow> G3 { get; private set; } = new();   // 3 or more
        public List<PlayerRow> G2 { get; private set; } = new();   // exactly 2
        public List<PlayerRow> G1 { get; private set; } = new();   // exactly 1
        public List<PlayerRow> G0 { get; private set; } = new();   // zero

        public async Task OnGet()
        {
            // LEFT JOIN players -> games so players with 0 games are included
            var data = await
                (from p in _db.Players.AsNoTracking()
                 join g in _db.Games.AsNoTracking() on p.Id equals g.PlayerId into gj
                 select new
                 {
                     p.Id,
                     p.FirstName,
                     p.Phone,
                     p.Country,
                     Count = gj.Count()
                 })
                .ToListAsync();

            var rows = data
                .Select(x => new PlayerRow(
                    x.Id,
                    x.FirstName ?? "",
                    x.Phone ?? "",
                    x.Country ?? "",
                    x.Count))
                .ToList();

            // Split into 4 groups; sort each group by name (case-insensitive)
            G3 = rows.Where(r => r.GamesCount >= 3)
                     .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();

            G2 = rows.Where(r => r.GamesCount == 2)
                     .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();

            G1 = rows.Where(r => r.GamesCount == 1)
                     .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();

            G0 = rows.Where(r => r.GamesCount == 0)
                     .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
