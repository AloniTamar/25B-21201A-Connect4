using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.WinForms.Models
{
    public enum ReplayPlayerKind { Human = 1, Server = 2 }

    public class ReplayMove
    {
        [Key]
        public int Id { get; set; }

        public int TurnIndex { get; set; }           // 0,1,2,...
        public ReplayPlayerKind Player { get; set; }
        public int Column { get; set; }              // 0..6
        public int Row { get; set; }                 // 0..5

        // FK -> ReplayGame
        public int ReplayGameId { get; set; }
        [ForeignKey(nameof(ReplayGameId))]
        public ReplayGame? ReplayGame { get; set; }
    }

    public class ReplayGame
    {
        [Key]
        public int Id { get; set; }                  // local PK for SQLite

        public int GameId { get; set; }              // server GameId
        public int PlayerId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSec { get; set; }            // computed when saving
        public string Result { get; set; } = "Unknown"; // "Won" | "Lost" | "Draw"

        public List<ReplayMove> Moves { get; set; } = new();
    }
}
