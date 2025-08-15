using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Services
{
    public static class GameLogic
    {
        public const int Rows = 6;
        public const int Cols = 7;

        // players: 1 = Human, 2 = Server
        public static int ApplyMove(int[][] board, int col, int player)
        {
            if (col < 0 || col >= Cols) return -1;
            for (int r = Rows - 1; r >= 0; r--)
            {
                if (board[r][col] == 0)
                {
                    board[r][col] = player;
                    return r; // row where disc landed
                }
            }
            return -1; // column full
        }

        public static bool IsBoardFull(int[][] board)
        {
            for (int c = 0; c < Cols; c++)
                if (board[0][c] == 0) return false;
            return true;
        }

        public static bool CheckWin(int[][] board, int row, int col, int player)
        {
            if (row < 0 || col < 0) return false;
            // 4 directions: horiz, vert, diag /, diag \
            int[][] dirs = new[]
            {
                new[]{0,1},   // —
                new[]{1,0},   // |
                new[]{1,1},   // \
                new[]{1,-1},  // /
            };

            foreach (var d in dirs)
            {
                int count = 1;
                count += CountDir(board, row, col, d[0], d[1], player);
                count += CountDir(board, row, col, -d[0], -d[1], player);
                if (count >= 4) return true;
            }
            return false;
        }

        private static int CountDir(int[][] b, int r, int c, int dr, int dc, int p)
        {
            int cnt = 0;
            int rr = r + dr, cc = c + dc;
            while (rr >= 0 && rr < Rows && cc >= 0 && cc < Cols && b[rr][cc] == p)
            {
                cnt++;
                rr += dr;
                cc += dc;
            }
            return cnt;
        }

        public static int PickRandomLegalMove(int[][] board, Random rng)
        {
            var legal = new List<int>();
            for (int c = 0; c < Cols; c++)
            {
                if (board[0][c] == 0) legal.Add(c);
            }
            if (legal.Count == 0) return -1;
            return legal[rng.Next(legal.Count)];
        }

        public static int[][] EmptyBoard()
        {
            return Enumerable.Range(0, Rows)
                .Select(_ => new int[Cols])
                .ToArray();
        }
    }
}
