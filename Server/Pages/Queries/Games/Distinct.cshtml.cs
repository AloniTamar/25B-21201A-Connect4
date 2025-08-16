using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Games
{
    public class DistinctModel : PageModel
    {
        private readonly AppDbContext _db;
        public DistinctModel(AppDbContext db) => _db = db;

        public class Row
        {
            public int GameId { get; set; }
            public int PlayerId { get; set; }
            public string PlayerName { get; set; } = "";
            public DateTime StartTime { get; set; }
            public string Result { get; set; } = "";
        }

        public IList<Row> Items { get; private set; } = new List<Row>();

        public async Task OnGet()
        {
            var latestPerPlayer =
                from x in _db.Games
                group x by x.PlayerId into grp
                select new { PlayerId = grp.Key, LastStart = grp.Max(t => t.StartTime) };

            Items = await
                (from g in _db.Games
                 join lp in latestPerPlayer
                     on new { g.PlayerId, g.StartTime }
                     equals new { lp.PlayerId, StartTime = lp.LastStart }
                 join p in _db.Players on g.PlayerId equals p.Id
                 orderby g.StartTime descending
                 select new Row
                 {
                     GameId = g.Id,
                     PlayerId = p.Id,
                     PlayerName = p.FirstName,
                     StartTime = g.StartTime,
                     Result = g.Result.ToString()
                 })
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
