using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Stats
{
    public class ByCountryModel : PageModel
    {
        private readonly AppDbContext _db;
        public ByCountryModel(AppDbContext db) => _db = db;

        public class PlayerRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Phone { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int GamesCount { get; set; }
        }

        public class CountryGroup
        {
            public string Country { get; set; } = "";
            public List<PlayerRow> Players { get; set; } = new();
        }

        public List<CountryGroup> Groups { get; private set; } = new();

        public async Task OnGet()
        {
            // Left-join players -> games to include players with 0 games
            var data = await
                (from p in _db.Players.AsNoTracking()
                 join g in _db.Games.AsNoTracking() on p.Id equals g.PlayerId into gj
                 select new
                 {
                     p.Id,
                     p.FirstName,
                     p.Phone,
                     p.Country,
                     p.CreatedAt,
                     Count = gj.Count()
                 })
                .ToListAsync();

            // Group by country, sort countries A?Z (case-insensitive), and players A?Z
            Groups = data
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Country) ? "(Unknown)" : x.Country)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CountryGroup
                {
                    Country = g.Key,
                    Players = g
                        .OrderBy(x => x.FirstName, StringComparer.OrdinalIgnoreCase)
                        .Select(x => new PlayerRow
                        {
                            Id = x.Id,
                            Name = x.FirstName ?? "",
                            Phone = x.Phone ?? "",
                            CreatedAt = x.CreatedAt,
                            GamesCount = x.Count
                        })
                        .ToList()
                })
                .ToList();
        }
    }
}
