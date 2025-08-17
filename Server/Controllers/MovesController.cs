using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<MovesController> _log;
    private readonly Random _rng = new();

    public MovesController(AppDbContext db, ILogger<MovesController> log)
    {
        _db = db;
        _log = log;
    }

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
        _log.LogInformation("Move received: GameId={GameId}, Column={Column}", req.GameId, req.Column);

        var game = await _db.Games
            .Include(g => g.Moves)
            .FirstOrDefaultAsync(g => g.Id == req.GameId);

        if (game == null)
        {
            _log.LogWarning("Move rejected: game not found. GameId={GameId}", req.GameId);
            return NotFound(new { message = "Game not found." });
        }

        if (game.Result != GameResult.Unknown)
        {
            _log.LogInformation("Move rejected: game already finished. GameId={GameId}, Result={Result}", game.Id, game.Result);
            return BadRequest(new { message = "Game already finished." });
        }

        // Rebuild board from stored moves
        var board = GameLogic.EmptyBoard();
        foreach (var m in game.Moves.OrderBy(m => m.TurnIndex))
            board[m.Row][m.Column] = (m.PlayerKind == PlayerKind.Human) ? 1 : 2;

        _log.LogDebug("Board rebuilt from {Count} moves. GameId={GameId}", game.Moves.Count, game.Id);

        var response = new MoveResponse { Board = board };

        // 1) Apply human move
        var humanCol = req.Column;
        var humanRow = GameLogic.ApplyMove(board, humanCol, player: 1);
        if (humanRow < 0)
        {
            _log.LogWarning("Illegal human move: column out of range or full. GameId={GameId}, Column={Column}", game.Id, humanCol);
            return BadRequest(new { message = "Illegal move: column is full or out of range." });
        }
        _log.LogInformation("Human move applied: GameId={GameId}, Col={Col}, Row={Row}", game.Id, humanCol, humanRow);

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

            _log.LogInformation("Human WON. GameId={GameId}, DurationSec={Duration}", game.Id, game.DurationSec);
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

            _log.LogInformation("Game DRAW after human move. GameId={GameId}", game.Id);
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
            response.ServerReplyMove = new { player = "Server", col = serverCol, row = serverRow };

            _log.LogInformation("Server move applied: GameId={GameId}, Col={Col}, Row={Row}", game.Id, serverCol, serverRow);

            // Check outcome after server move
            if (GameLogic.CheckWin(board, serverRow, serverCol, player: 2))
            {
                game.Result = GameResult.Loss; // player lost
                game.EndTime = DateTime.UtcNow;
                game.DurationSec = (int)(game.EndTime.Value - game.StartTime).TotalSeconds;

                await _db.SaveChangesAsync();
                response.Status = "Lost";
                response.LastMove = new { player = "Human", col = humanCol, row = humanRow };

                _log.LogInformation("SERVER WON (player lost). GameId={GameId}, DurationSec={Duration}", game.Id, game.DurationSec);
                return Ok(response);
            }
        }
        else
        {
            _log.LogWarning("No legal server moves found (should imply draw soon). GameId={GameId}", game.Id);
        }

        // If here, still playing (or draw re-checked below for safety)
        if (GameLogic.IsBoardFull(board))
        {
            game.Result = GameResult.Draw;
            game.EndTime = DateTime.UtcNow;
            game.DurationSec = (int)(game.EndTime.Value - game.StartTime).TotalSeconds;
            response.Status = "Draw";

            _log.LogInformation("Game DRAW at end of turn. GameId={GameId}", game.Id);
        }
        else
        {
            response.Status = "Playing";
            _log.LogDebug("Game still playing after turn. GameId={GameId}", game.Id);
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

        _log.LogInformation(
            "Move processed: GameId={GameId}, HumanCol={HumanCol}, ServerCol={ServerCol}, Status={Status}",
            game.Id, humanCol, serverCol, response.Status);

        return Ok(response);
    }
}
