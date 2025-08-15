using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Data
{
    public enum PlayerKind { Human = 1, Server = 2 }
    public enum GameResult { Unknown = 0, Win = 1, Loss = 2, Draw = 3 }

    public class Player
    {
        public int Id { get; set; }

        [Range(1, 1000)]
        public int UniqueNumber { get; set; } // must be unique

        [Required, MinLength(2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Game> Games { get; set; } = new List<Game>();
    }

    public class Game
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Player))]
        public int PlayerId { get; set; }
        public Player? Player { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public int? DurationSec { get; set; }

        public GameResult Result { get; set; } = GameResult.Unknown;

        public string? Notes { get; set; }

        public ICollection<Move> Moves { get; set; } = new List<Move>();
    }

    public class Move
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Game))]
        public int GameId { get; set; }
        public Game? Game { get; set; }

        public int TurnIndex { get; set; } // 0..n

        public PlayerKind PlayerKind { get; set; } // Human or Server

        [Range(0, 6)]
        public int Column { get; set; } // 7 columns (0..6)

        [Range(0, 5)]
        public int Row { get; set; }    // 6 rows (0..5)
    }
}
