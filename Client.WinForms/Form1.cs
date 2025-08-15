using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Client.WinForms.Services;

namespace Client.WinForms
{
    public partial class Form1 : Form
    {
        private void Form1_Load(object? sender, EventArgs e)
        {
            // Designer hook placeholder – no logic needed.
        }

        private readonly ApiClient _api = new ApiClient("http://localhost:5080");

        private int _playerId = 1;       // TODO: wire to real login later
        private int _gameId = -1;
        private int[][] _board = EmptyBoard();

        private readonly int _rows = 6;
        private readonly int _cols = 7;
        private readonly int _cell = 64;     // pixel size
        private readonly int _margin = 20;

        private readonly Button _btnNew = new Button { Text = "Create Game", AutoSize = true };

        // --- animation state ---
        private bool _animActive = false;
        private int _animCol = -1;
        private int _animRowTarget = -1;
        private int _animYpx = 0;
        private readonly System.Windows.Forms.Timer _animTimer = new() { Interval = 16 };
        private bool _animCommitNeeded = false;

        // --- server animation state ---
        private bool _srvAnimActive = false;
        private int _srvAnimCol = -1;
        private int _srvAnimRowTarget = -1;
        private int _srvAnimYpx = 0;
        private readonly System.Windows.Forms.Timer _srvAnimTimer = new() { Interval = 16 };
        private bool _srvAnimCommitNeeded = false;
        private bool _waitingServer = false;

        // --- queued server move (start after human anim finishes) ---
        private bool _srvPending = false;
        private int _srvPendingCol = -1;
        private int _srvPendingRow = -1;

        // 1s delay before starting server animation
        private readonly System.Windows.Forms.Timer _srvDelayTimer = new() { Interval = 500, Enabled = false };


        // Helper to clone the board state
        private static int[][] CloneBoard(int[][] src)
        {
            return src.Select(row => row.ToArray()).ToArray();
        }

        public Form1()
        {
            InitializeComponent();
            Text = "Connect4 Client";
            DoubleBuffered = true;
            ClientSize = new Size(_margin * 2 + _cols * _cell, _margin * 3 + _rows * _cell + 40);

            _btnNew.Location = new Point(_margin, _margin);
            _btnNew.Click += BtnNew_Click;
            Controls.Add(_btnNew);

            _animTimer.Tick += (s, e) =>
            {
                // fall speed (px per tick)
                _animYpx += 18;

                // compute pixel Y of the target cell center
                int boardTop = _margin * 2 + 40;
                int targetYpx = boardTop + _animRowTarget * _cell + (_cell / 2);

                if (_animYpx >= targetYpx)
                {
                    if (_animCommitNeeded)
                    {
                        _board[_animRowTarget][_animCol] = 1;
                        _animCommitNeeded = false;
                    }
                    _animActive = false;
                    _animTimer.Stop();
                    
                    if (_srvPending)
                    {
                        _waitingServer = true;
                        _srvDelayTimer.Stop();
                        _srvDelayTimer.Start();
                    }
                }
                Invalidate();
            };

            _srvAnimTimer.Tick += (s, e) =>
            {
                _srvAnimYpx += 18;
                int targetYpx2 = (_margin * 2 + 40) + _srvAnimRowTarget * _cell + (_cell / 2);
                if (_srvAnimYpx >= targetYpx2)
                {
                    if (_srvAnimCommitNeeded)
                    {
                        _board[_srvAnimRowTarget][_srvAnimCol] = 2; // place server disc now
                        _srvAnimCommitNeeded = false;
                    }
                    _srvAnimActive = false;
                    _srvAnimTimer.Stop();
                    _waitingServer = false;
                }
                Invalidate();
            };

            _srvDelayTimer.Tick += (s, e) =>
            {
                _srvDelayTimer.Stop();
                if (_srvPending)
                {
                    _srvPending = false;

                    _srvAnimActive = true;
                    _srvAnimCol = _srvPendingCol;
                    _srvAnimRowTarget = _srvPendingRow;
                    _srvAnimYpx = _margin * 2 + 40 - (_cell / 2);  // start above board
                    _srvAnimTimer.Start();
                    Invalidate();
                }
            };

            // Mouse click to drop a disc
            MouseDown += Form1_MouseDown;
        }

        private async void BtnNew_Click(object? sender, EventArgs e)
        {
            try
            {
                var resp = await _api.CreateGameAsync(_playerId);
                if (resp == null)
                {
                    MessageBox.Show("No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _gameId = resp.GameId;
                _board = resp.Board ?? EmptyBoard();
                Invalidate();
                MessageBox.Show($"New game created. ID = {_gameId}", "Game", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Create game failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Form1_MouseDown(object? sender, MouseEventArgs e)
        {
            // Ignore clicks if no game yet
            var boardTop = _margin * 2 + 40; // room for the button row
            var boardLeft = _margin;

            if (_gameId <= 0) return;
            if (_animActive || _srvAnimActive || _waitingServer) return;

            // Only handle clicks inside the board
            if (e.Y < boardTop || e.X < boardLeft) return;
            int col = (e.X - boardLeft) / _cell;
            int rowArea = (e.Y - boardTop) / _cell;
            if (col < 0 || col >= _cols || rowArea < 0 || rowArea >= _rows) return;

            try
            {
                var prev = CloneBoard(_board);
                var resp = await _api.SendMoveAsync(_gameId, col);
                if (resp == null)
                {
                    MessageBox.Show("No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _board = resp.Board ?? _board;
                // Find exactly which cell became 1 in this column (compare prev vs new)
                int landedRow = -1;
                for (int r = 0; r < _board.Length; r++)
                {
                    if (prev[r][col] == 0 && _board[r][col] == 1)
                    {
                        landedRow = r;
                        break;  // top-to-bottom is fine; only one new cell becomes 1 for human in this column
                    }
                }

                // If for some reason we didn't detect it, fall back to scanning
                if (landedRow < 0)
                {
                    for (int r = 0; r < _board.Length; r++)
                    {
                        if (_board[r][col] == 1)
                            landedRow = r;
                    }
                }
                if (landedRow >= 0)
                {
                    // temporarily clear the landed cell so it won't draw yet
                    _board[landedRow][col] = 0;
                    _animCommitNeeded = true;

                    _animActive = true;
                    _animCol = col;
                    _animRowTarget = landedRow;

                    _animYpx = boardTop - (_cell / 2);
                    _animTimer.Start();
                }
                // --- detect server-changed cell by diffing prev vs new (0 -> 2 anywhere) ---
                int serverRow = -1, serverCol = -1;
                for (int r = 0; r < _board.Length; r++)
                {
                    for (int c = 0; c < _board[r].Length; c++)
                    {
                        if (prev[r][c] == 0 && _board[r][c] == 2)
                        {
                            serverRow = r;
                            serverCol = c;
                            break;
                        }
                    }
                    if (serverRow >= 0) break;
                }

                // Queue server animation (start after human anim finishes + 1s delay)
                if (serverRow >= 0 && serverCol >= 0)
                {
                    // prevent the server disc from "popping in" before the animation
                    _board[serverRow][serverCol] = 0;
                    _srvAnimCommitNeeded = true;

                    _srvPending = true;
                    _srvPendingRow = serverRow;
                    _srvPendingCol = serverCol;
                }

                Invalidate();

                if (resp.Status == "Won" || resp.Status == "Lost" || resp.Status == "Draw")
                {
                    MessageBox.Show($"Game over: {resp.Status}", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Move failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            var boardTop = _margin * 2 + 40; // below the button
            var boardLeft = _margin;

            // Board frame
            using (var pen = new Pen(Color.Black, 2))
            {
                g.DrawRectangle(pen, boardLeft - 1, boardTop - 1, _cols * _cell + 2, _rows * _cell + 2);
            }

            // Grid and discs
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    var x = boardLeft + c * _cell;
                    var y = boardTop + r * _cell;

                    // Cell background
                    g.FillRectangle(Brushes.LightGray, x + 1, y + 1, _cell - 2, _cell - 2);

                    // Disc
                    int v = _board[r][c];
                    if (v == 1)
                    {
                        g.FillEllipse(Brushes.Red, x + 6, y + 6, _cell - 12, _cell - 12);
                    }
                    else if (v == 2)
                    {
                        g.FillEllipse(Brushes.Blue, x + 6, y + 6, _cell - 12, _cell - 12);
                    }
                    else
                    {
                        // empty hole effect
                        g.FillEllipse(Brushes.White, x + 6, y + 6, _cell - 12, _cell - 12);
                    }

                    // Cell border
                    g.DrawRectangle(Pens.DimGray, x, y, _cell, _cell);
                }
            }

            // overlay: falling disc animation (human = red)
            if (_animActive && _animCol >= 0 && _animRowTarget >= 0)
            {
                int x = boardLeft + _animCol * _cell + 6;
                int y = _animYpx - (_cell / 2) + 6; // center-based to ellipse top-left
                g.FillEllipse(Brushes.Red, x, y, _cell - 12, _cell - 12);
            }

            // overlay: falling disc animation (server = blue)
            if (_srvAnimActive && _srvAnimCol >= 0 && _srvAnimRowTarget >= 0)
            {
                int x2 = boardLeft + _srvAnimCol * _cell + 6;
                int y2 = _srvAnimYpx - (_cell / 2) + 6;
                g.FillEllipse(Brushes.Blue, x2, y2, _cell - 12, _cell - 12);
            }
        }

        private static int[][] EmptyBoard()
        {
            return Enumerable.Range(0, 6)
                             .Select(_ => new int[7])
                             .ToArray();
        }
    }
}
