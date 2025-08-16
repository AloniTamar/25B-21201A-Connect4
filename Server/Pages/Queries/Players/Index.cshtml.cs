using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Players
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db) => _db = db;

        public class PlayerRow
        {
            public int Id { get; set; }
            public int UniqueNumber { get; set; }
            public string FirstName { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Country { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        public IList<PlayerRow> Players { get; private set; } = new List<PlayerRow>();

        public async Task OnGet(string sort = "name_asc")
        {
            // case-insensitive sort by name
            IQueryable<Player> q = _db.Players.AsNoTracking();

            q = sort switch
            {
                "name_desc" => q.OrderByDescending(p => EF.Functions.Collate(p.FirstName, "SQL_Latin1_General_CP1_CI_AS")),
                _ => q.OrderBy(p => EF.Functions.Collate(p.FirstName, "SQL_Latin1_General_CP1_CI_AS")),
            };

            Players = await q
                .Select(p => new PlayerRow
                {
                    Id = p.Id,
                    UniqueNumber = p.UniqueNumber,
                    FirstName = p.FirstName,
                    Phone = p.Phone,
                    Country = p.Country,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
        }
    }
}
