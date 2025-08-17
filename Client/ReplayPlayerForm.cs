using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Client.WinForms.Data;
using Client.WinForms.Models;

namespace Client.WinForms
{
    public class ReplayPlayerForm : Form
    {
        private readonly int _rows = 6;
        private readonly int _cols = 7;
        private readonly int _cell = 64;
        private readonly int _margin = 20;

        private List<ReplayMove> _moves = new();
        private int[][] _board = EmptyBoard();

        private readonly Label _lbl = new() { AutoSize = true };
        private readonly Button _btnClose = new() { Text = "Close", AutoSize = true };

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 }; // 60 FPS
        private int _playIndex = 0;
        private bool _animActive = false;
        private int _animCol = -1;
        private int _animRowTarget = -1;
        private int _animYpx = 0;
        private bool _animCommitNeeded = false;

        private int _pauseTicks = 0; // ~16ms per tick

        private void StartNextMove()
        {
            if (_playIndex >= _moves.Count) return;

            var m = _moves[_playIndex++];

            // find lowest empty row in that column (based on the current replay board)
            int landedRow = -1;
            for (int r = _rows - 1; r >= 0; r--)
            {
                if (_board[r][m.Column] == 0)
                {
                    landedRow = r;
                    break;
                }
            }
            if (landedRow < 0) { StartNextMove(); return; } // column full (shouldn't happen)

            // set animation state
            int boardTop = _margin * 2 + 40;
            _animActive = true;
            _animCol = m.Column;
            _animRowTarget = landedRow;
            _animYpx = boardTop - (_cell / 2); // start above board
            _animCommitNeeded = true;

            Invalidate();
        }

        public ReplayPlayerForm(int replayId)
        {
            Text = $"Replay #{replayId}";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(_margin * 2 + _cols * _cell, _margin * 3 + _rows * _cell + 40);
            DoubleBuffered = true;

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(8) };
            top.Controls.Add(_lbl);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8), FlowDirection = FlowDirection.RightToLeft };
            bottom.Controls.Add(_btnClose);

            Controls.Add(bottom);
            Controls.Add(top);

            _timer.Tick += (s, e) =>
            {
                int boardTop = _margin * 2 + 40;

                // If we are pausing between moves
                if (_pauseTicks > 0)
                {
                    _pauseTicks--;
                    if (_pauseTicks == 0) StartNextMove();
                    return;
                }

                if (_animActive)
                {
                    // fall speed
                    _animYpx += 18;

                    // target pixel center of the destination cell
                    int targetYpx = boardTop + _animRowTarget * _cell + (_cell / 2);

                    if (_animYpx >= targetYpx)
                    {
                        if (_animCommitNeeded)
                        {
                            // commit this replay move to the board
                            _board[_animRowTarget][_animCol] =
                                (_moves[_playIndex - 1].Player == ReplayPlayerKind.Human) ? 1 : 2;
                            _animCommitNeeded = false;
                        }
                        _animActive = false;

                        // brief pause before next move
                        _pauseTicks = 30; // ~0.5s (30 * 16ms)
                    }

                    Invalidate();
                    return;
                }

                // No animation and no pause: if nothing left, stop.
                if (_playIndex >= _moves.Count)
                {
                    _timer.Stop();
                    _lbl.Text += "  •  Finished";
                    return;
                }

                // Start the first move
                StartNextMove();
            };

            Load += (s, e) => LoadReplay(replayId);
            _btnClose.Click += (s, e) => Close();
        }

        private void LoadReplay(int replayId)
        {
            using var db = new ReplayDbContext();
            var game = db.ReplayGames
                         .Where(g => g.Id == replayId)
                         .Select(g => new
                         {
                             g.Id,
                             g.GameId,
                             g.PlayerId,
                             g.StartedAt,
                             Moves = g.Moves.OrderBy(m => m.TurnIndex).ToList()
                         })
                         .FirstOrDefault();

            if (game == null)
            {
                MessageBox.Show("Replay not found.", "Replays", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }

            _moves = game.Moves;
            _board = EmptyBoard();
            _lbl.Text = $"GameId: {game.GameId} • Moves: {_moves.Count} • {game.StartedAt:g}";

            // Draw empty board initially
            Invalidate();

            _playIndex = 0;
            _pauseTicks = 10;
            _timer.Start(); 
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var boardTop = _margin * 2 + 40;
            var boardLeft = _margin;

            // frame
            using (var pen = new Pen(Color.Black, 2))
                g.DrawRectangle(pen, boardLeft - 1, boardTop - 1, _cols * _cell + 2, _rows * _cell + 2);

            // grid + discs
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    var x = boardLeft + c * _cell;
                    var y = boardTop + r * _cell;

                    g.FillRectangle(Brushes.LightGray, x + 1, y + 1, _cell - 2, _cell - 2);

                    int v = _board[r][c];
                    if (v == 1)
                        g.FillEllipse(Brushes.Red, x + 6, y + 6, _cell - 12, _cell - 12);
                    else if (v == 2)
                        g.FillEllipse(Brushes.Blue, x + 6, y + 6, _cell - 12, _cell - 12);
                    else
                        g.FillEllipse(Brushes.White, x + 6, y + 6, _cell - 12, _cell - 12);

                    g.DrawRectangle(Pens.DimGray, x, y, _cell, _cell);
                }
            }

            // overlay: falling disc (color by player of the *current* move)
            if (_animActive && _animCol >= 0 && _animRowTarget >= 0 && _playIndex > 0)
            {
                var current = _moves[_playIndex - 1];
                int x = _margin + _animCol * _cell + 6;
                int y = _animYpx - (_cell / 2) + 6;

                var brush = (current.Player == ReplayPlayerKind.Human) ? Brushes.Red : Brushes.Blue;
                e.Graphics.FillEllipse(brush, x, y, _cell - 12, _cell - 12);
            }
        }

        private static int[][] EmptyBoard()
        {
            return Enumerable.Range(0, 6).Select(_ => new int[7]).ToArray();
        }
    }
}
