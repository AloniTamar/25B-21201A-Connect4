namespace Client.WinForms.Models
{
    // POST /api/games
    public class CreateGameRequest
    {
        public int PlayerId { get; set; }
    }

    public class CreateGameResponse
    {
        public int GameId { get; set; }
        public int[][] Board { get; set; } = [];
        public string Status { get; set; } = string.Empty;
        public int PlayerUniqueNumber { get; set; }
        public string PlayerFirstName { get; set; } = "";
        public string? PlayerPhone { get; set; }
        public string? PlayerCountry { get; set; }
    }

    // POST /api/moves
    public class MoveRequest
    {
        public int GameId { get; set; }
        public int Column { get; set; } // 0..6
    }

    public class MoveInfo
    {
        public string player { get; set; } = string.Empty; // "Human" or "Server"
        public int col { get; set; }
        public int row { get; set; }
    }

    public class MoveResponse
    {
        public int[][] Board { get; set; } = [];
        public string Status { get; set; } = string.Empty; // Playing|Won|Lost|Draw
        public MoveInfo? LastMove { get; set; }
        public MoveInfo? ServerReplyMove { get; set; }
    }
}
