using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Queries.Games
{
    public class ByPlayerModel : PageModel
    {
        private readonly AppDbContext _db;
        public ByPlayerModel(AppDbContext db) => _db = db;

        [BindProperty(SupportsGet = true)]
        public int? PlayerId { get; set; }

        public class Row
        {
            public int Id { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public int? DurationSec { get; set; }
            public string Result { get; set; } = "";
            public string? Notes { get; set; }
        }

        public IList<SelectListItem> PlayerOptions { get; private set; } = new List<SelectListItem>();
        public IList<Row> Items { get; private set; } = new List<Row>();
        public int? SelectedPlayerId { get; private set; }
        public string? PlayerName { get; private set; }

        public async Task OnGet(int? playerId)
        {
            SelectedPlayerId = playerId;

            // Combo: all players, ascending by name (case-insensitive)
            var players = await _db.Players
                .AsNoTracking()
                .OrderBy(p => EF.Functions.Collate(p.FirstName, "SQL_Latin1_General_CP1_CI_AS"))
                .Select(p => new { p.Id, p.FirstName })
                .ToListAsync();

            PlayerOptions = players
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.FirstName,
                    Selected = (SelectedPlayerId == p.Id)
                })
                .ToList();

            if (SelectedPlayerId.HasValue)
            {
                PlayerName = players.FirstOrDefault(p => p.Id == SelectedPlayerId.Value)?.FirstName;

                Items = await _db.Games
                    .AsNoTracking()
                    .Where(g => g.PlayerId == SelectedPlayerId.Value)
                    .OrderByDescending(g => g.StartTime)
                    .Select(g => new Row
                    {
                        Id = g.Id,
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
}
