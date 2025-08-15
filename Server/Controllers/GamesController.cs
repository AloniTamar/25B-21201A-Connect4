using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly AppDbContext _db;
    public GamesController(AppDbContext db) => _db = db;

    public class CreateGameRequest
    {
        public int PlayerId { get; set; }
    }

    public class CreateGameResponse
    {
        public int GameId { get; set; }
        public int[][] Board { get; set; } = Enumerable
            .Range(0, 6)
            .Select(_ => new int[7])
            .ToArray();
        public string Status { get; set; } = "Playing";
    }

    [HttpPost]
    public async Task<ActionResult<CreateGameResponse>> Create([FromBody] CreateGameRequest req)
    {
        // Validate player exists
        var playerExists = await _db.Players.AnyAsync(p => p.Id == req.PlayerId);
        if (!playerExists)
            return BadRequest(new { message = "Player not found." });

        var game = new Game { PlayerId = req.PlayerId };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new { id = game.Id }, new CreateGameResponse
        {
            GameId = game.Id,
            Board = Enumerable.Range(0, 6)
                .Select(_ => Enumerable.Repeat(0, 7).ToArray())
                .ToArray(),
            Status = "Playing"
        });
    }

    [HttpGet("by-player/{playerId:int}")]
    public async Task<ActionResult<object>> GetByPlayer(int playerId)
    {
        var exists = await _db.Players.AnyAsync(p => p.Id == playerId);
        if (!exists) return NotFound(new { message = "Player not found." });

        var games = await _db.Games
            .Where(g => g.PlayerId == playerId)
            .OrderByDescending(g => g.StartTime)
            .Select(g => new
            {
                g.Id,
                g.PlayerId,
                g.StartTime,
                g.EndTime,
                g.DurationSec,
                Result = g.Result.ToString(),
                Moves = g.Moves.Count
            })
            .ToListAsync();

        return Ok(games);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null) return NotFound(new { message = "Game not found." });

        _db.Games.Remove(game);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
