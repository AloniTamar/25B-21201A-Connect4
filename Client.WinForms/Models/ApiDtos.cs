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
    }

    // POST /api/moves
    public class MoveRequest
    {
        public int GameId { get; set; }
        public int Column { get; set; } // 0..6
    }

    public class MoveResponse
    {
        public int[][] Board { get; set; } = [];
        public string Status { get; set; } = string.Empty; // Playing|Won|Lost|Draw
        public object? LastMove { get; set; }
        public object? ServerReplyMove { get; set; }
    }
}
