using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Players
{
    public class NamesDescModel : PageModel
    {
        private readonly AppDbContext _db;
        public NamesDescModel(AppDbContext db) => _db = db;

        public class Row
        {
            public string Name { get; set; } = "";
            public DateTime? LastGameDate { get; set; }
        }

        public IList<Row> Items { get; private set; } = new List<Row>();

        public async Task OnGet()
        {
            // NOTE: case-sensitive sort by name, descending
            var sorted = _db.Players
                .AsNoTracking()
                .OrderByDescending(p => EF.Functions.Collate(p.FirstName, "SQL_Latin1_General_CP1_CS_AS"));

            Items = await sorted
                .Select(p => new Row
                {
                    Name = p.FirstName,
                    // last game by StartTime (or EndTime if you prefer)
                    LastGameDate = _db.Games
                        .Where(g => g.PlayerId == p.Id)
                        .OrderByDescending(g => g.StartTime)
                        .Select(g => (DateTime?)g.StartTime)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
    }
}
