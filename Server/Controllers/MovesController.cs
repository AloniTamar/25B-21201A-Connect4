using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Random _rng = new();

    public MovesController(AppDbContext db) => _db = db;

    public class MoveRequest
    {
        public int GameId { get; set; }
        public int Column { get; set; } // 0..6
    }

    public class MoveResponse
    {
        public int[][] Board { get; set; } = GameLogic.EmptyBoard();
        public string Status { get; set; } = "Playing"; // Playing|Won|Lost|Draw
        public object? LastMove { get; set; }
        public object? ServerReplyMove { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<MoveResponse>> Post([FromBody] MoveRequest req)
    {
        var game = await _db.Games
            .Include(g => g.Moves)
            .FirstOrDefaultAsync(g => g.Id == req.GameId);

        if (game == null)
            return NotFound(new { message = "Game not found." });

        if (game.Result != GameResult.Unknown)
            return BadRequest(new { message = "Game already finished." });

        // Rebuild board from stored moves
        var board = GameLogic.EmptyBoard();
        foreach (var m in game.Moves.OrderBy(m => m.TurnIndex))
            board[m.Row][m.Column] = (m.PlayerKind == PlayerKind.Human) ? 1 : 2;

        var response = new MoveResponse { Board = board };

        // 1) Apply human move
        var humanCol = req.Column;
        var humanRow = GameLogic.ApplyMove(board, humanCol, player: 1);
        if (humanRow < 0)
            return BadRequest(new { message = "Illegal move: column is full or out of range." });

        var humanMove = new Move
        {
            GameId = game.Id,
            TurnIndex = game.Moves.Count,
            PlayerKind = PlayerKind.Human,
            Column = humanCol,
            Row = humanRow
        };
        _db.Moves.Add(humanMove);

        // Check win/draw after human move
        if (GameLogic.CheckWin(board, humanRow, humanCol, player: 1))
        {
            game.Result = GameResult.Win;
            game.EndTime = DateTime.UtcNow;
            game.DurationSec = (int)(game.EndTime.Value - game.StartTime).TotalSeconds;

            await _db.SaveChangesAsync();
            response.Status = "Won";
            response.LastMove = new { player = "Human", col = humanCol, row = humanRow };
            return Ok(response);
        }

        if (GameLogic.IsBoardFull(board))
        {
            game.Result = GameResult.Draw;
            game.EndTime = DateTime.UtcNow;
            game.DurationSec = (int)(game.EndTime.Value - game.StartTime).TotalSeconds;

            await _db.SaveChangesAsync();
            response.Status = "Draw";
            response.LastMove = new { player = "Human", col = humanCol, row = humanRow };
            return Ok(response);
        }

        // 2) Server random legal move
        var serverCol = GameLogic.PickRandomLegalMove(board, _rng);
        if (serverCol >= 0)
        {
            var serverRow = GameLogic.ApplyMove(board, serverCol, player: 2);
            var serverMove = new Move
            {
                GameId = game.Id,
                TurnIndex = game.Moves.Count + 1,
                PlayerKind = PlayerKind.Server,
                Column = serverCol,
                Row = serverRow
            };
            _db.Moves.Add(serverMove);

            // set the server reply in the response immediately
            response.ServerReplyMove = new { player = "Server", col = serverCol, row = serverRow };

            // Check outcome after server move
            if (GameLogic.CheckWin(board, serverRow, serverCol, player: 2))
            {
                game.Result = GameResult.Loss;
                game.EndTime = DateTime.UtcNow;
                game.DurationSec = (int)(game.EndTime.Value - game.StartTime).TotalSeconds;

                await _db.SaveChangesAsync();
                response.Status = "Lost";
                response.LastMove = new { player = "Human", col = humanCol, row = humanRow };
                return Ok(response);
            }
        }

        // If here, still playing (or draw re-checked below for safety)
        if (GameLogic.IsBoardFull(board))
        {
            game.Result = GameResult.Draw;
            game.EndTime = DateTime.UtcNow;
            game.DurationSec = (int)(game.EndTime.Value - game.StartTime).TotalSeconds;
            response.Status = "Draw";
        }
        else
        {
            response.Status = "Playing";
        }

        response.LastMove = new { player = "Human", col = humanCol, row = humanRow };
        if (game.Moves.Count % 2 == 0) // if server moved, we added one more
        {
            var last = await _db.Moves
                .Where(m => m.GameId == game.Id && m.PlayerKind == PlayerKind.Server)
                .OrderByDescending(m => m.TurnIndex)
                .FirstOrDefaultAsync();

            if (last != null)
                response.ServerReplyMove = new { player = "Server", col = last.Column, row = last.Row };
        }

        await _db.SaveChangesAsync();
        return Ok(response);
    }
}
