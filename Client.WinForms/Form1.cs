using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Client.WinForms.Data;
using Client.WinForms.Models;
using Client.WinForms.Services;

namespace Client.WinForms
{
    public partial class Form1 : Form
    {
        private void Form1_Load(object? sender, EventArgs e)
        {
            // Designer hook placeholder – no logic needed.
        }

        private readonly ApiClient _api = new ApiClient("http://localhost:5221");

        private int _playerId = 1;       // TODO: wire to real login later
        private int _gameId = -1;
        private int[][] _board = EmptyBoard();
        private Rectangle[,] _cells = new Rectangle[0, 0];

        private readonly int _rows = 6;
        private readonly int _cols = 7;
        private readonly int _cell = 64;     // pixel size
        private readonly int _margin = 20;
        private Point _lastHuman = new(-1, -1);   // (col,row) of most recent human disc
        private Point _lastServer = new(-1, -1);   // (col,row) of most recent server disc
        private int _hoverCol = -1; // -1 = none
        private string? _pendingEndStatus = null;

        private readonly Button _btnNew = new Button { Text = "Create Game", AutoSize = true };
        private readonly Button _btnReplays = new() { Text = "Replays", AutoSize = true };
        private readonly Label _lblStatus = new() { AutoSize = true, Text = "Status: —" };
        private readonly Label _lblError = new() { AutoSize = true, ForeColor = Color.Red, Text = "" };
        private readonly ComboBox _cmbReplays = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };

        private ReplayGame? _replay;      // current replay being recorded
        private int _turnIndex = 0;       // increases after each committed move

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

        private enum AppMode { Live, Replay }
        private AppMode _mode = AppMode.Live;

        private List<ReplayMove> _replayMoves = new();
        private int _replayIndex = 0;
        private readonly System.Windows.Forms.Timer _replayTimer = new() { Interval = 600 }; // ms per move

        private class ReplayChoice
        {
            public int Id { get; init; }
            public string Text { get; init; } = "";
            public override string ToString() => Text; // what the ComboBox shows
        }

        // Helper to clone the board state
        private static int[][] CloneBoard(int[][] src)
        {
            return src.Select(row => row.ToArray()).ToArray();
        }

        // Helper - Thread-safe status updates
        private void SetStatusText(string text)
        {
            if (_lblStatus.InvokeRequired)
                _lblStatus.BeginInvoke(new Action(() => { _lblStatus.Text = text; _lblStatus.Refresh(); }));
            else
            {
                _lblStatus.Text = text;
                _lblStatus.Refresh();
            }
        }

        // Helper - Map server status to human-friendly text (adjust if your API returns different strings)
        private void SetStatusFromServer(string status)
        {
            // status expected: "Playing" | "Won" | "Lost" | "Draw"
            string display = status switch
            {
                "Playing" => "Playing…",
                "Won" => "You won!",
                "Lost" => "You lost.",
                "Draw" => "Draw.",
                _ => status ?? "Status unknown"
            };
            SetStatusText(display);
        }

        // Helper - Show error message in the label
        private void ShowError(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
                _lblError.Text = "";
            else
                _lblError.Text = "Error: " + msg;
        }

        // Starts a new game by resetting the board and animations
        private async Task StartNewGameAsync()
        {
            // --- reset any in-flight animations/queues before starting a new game ---
            _mode = AppMode.Live;
            _replayTimer.Stop();
            _replayMoves = new List<ReplayMove>();
            _replayIndex = 0;

            _animTimer.Stop();
            _srvAnimTimer.Stop();
            _srvDelayTimer.Stop();

            _animActive = false;
            _srvAnimActive = false;
            _waitingServer = false;

            _animCommitNeeded = false;
            _srvAnimCommitNeeded = false;

            _srvPending = false;
            _srvPendingCol = -1;
            _srvPendingRow = -1;

            _animCol = -1;
            _animRowTarget = -1;
            _srvAnimCol = -1;
            _srvAnimRowTarget = -1;

            // Clear highlights right away
            _lastHuman = new Point(-1, -1);
            _lastServer = new Point(-1, -1);

            try
            {
                var resp = await _api.CreateGameAsync(_playerId);
                if (resp == null)
                {
                    ShowError("No response from server.");
                    return;
                }
                _btnNew.Enabled = false;
                _gameId = resp.GameId;
                _board = resp.Board ?? EmptyBoard();
                _replay = new ReplayGame
                {
                    GameId = _gameId,
                    PlayerId = _playerId,
                    StartedAt = DateTime.Now
                };
                _turnIndex = 0;
                _lastHuman = new Point(-1, -1);
                _lastServer = new Point(-1, -1);
                Invalidate();
                SetStatusFromServer("Playing");
                MessageBox.Show($"New game created. ID = {_gameId}",
                    "Game", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Create game failed: " + ex.Message);
            }
        }

        // Refreshes the replay combo box with saved replays from the database
        private void RefreshReplayCombo()
        {
            try
            {
                using var db = new Client.WinForms.Data.ReplayDbContext();
                db.Database.EnsureCreated();

                var items = db.ReplayGames
                              .Where(g => g.PlayerId == _playerId)
                              .OrderByDescending(g => g.StartedAt)
                              .Select(g => new
                              {
                                  g.Id,
                                  g.GameId,
                                  g.StartedAt,
                                  Moves = g.Moves.Count,
                                  g.DurationSec,
                                  g.Result
                              })
                              .ToList();

                _cmbReplays.BeginUpdate();
                _cmbReplays.Items.Clear();
                foreach (var it in items)
                {
                    var dur = it.DurationSec > 0 ? $" • {it.DurationSec}s" : "";
                    var res = !string.IsNullOrWhiteSpace(it.Result) ? $" • {it.Result}" : "";
                    var text = $"#{it.GameId} • {it.StartedAt:g} • {it.Moves} moves{dur}{res}";
                    _cmbReplays.Items.Add(new ReplayChoice { Id = it.Id, Text = text });
                }
                _cmbReplays.EndUpdate();

                // keep UX simple: select newest if list not empty
                if (_cmbReplays.Items.Count > 0 && _cmbReplays.SelectedIndex < 0)
                    _cmbReplays.SelectedIndex = 0;

                _btnReplays.Enabled = _cmbReplays.SelectedIndex >= 0;
            }
            catch (Exception ex)
            {
                ShowError("Failed to refresh replays: " + ex.Message);
            }
        }

        // Saves the current replay to the database
        private void SaveReplay(string endStatus)
        {
            if (_replay == null) return;

            _replay.EndedAt = DateTime.Now;
            _replay.DurationSec = (int)Math.Max(0,
                (_replay.EndedAt.Value - _replay.StartedAt).TotalSeconds);
            _replay.Result = string.IsNullOrWhiteSpace(endStatus) ? "Unknown" : endStatus;

            using var db = new ReplayDbContext();
            db.Database.EnsureCreated();
            db.ReplayGames.Add(_replay);
            db.SaveChanges();
        }

        // Rebuilds the cell rectangles based on current board size and margins
        private void RebuildCellRects()
        {
            int boardTop = _margin * 2 + 40; // same offsets you already use
            int boardLeft = _margin;

            _cells = new Rectangle[_rows, _cols];
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    _cells[r, c] = new Rectangle(
                        boardLeft + c * _cell,
                        boardTop + r * _cell,
                        _cell,
                        _cell
                    );
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            Text = "Connect4 Client";
            DoubleBuffered = true;
            ClientSize = new Size(_margin * 2 + _cols * _cell, _margin * 3 + _rows * _cell + 40);
            RebuildCellRects();

            _btnNew.Click += BtnNew_Click;
            Controls.Add(_btnNew);

            _btnReplays.Click += (s, e) =>
            {
                if (_cmbReplays.SelectedItem is not ReplayChoice choice) return;

                try
                {
                    using var db = new Client.WinForms.Data.ReplayDbContext();
                    db.Database.EnsureCreated();

                    var game = db.ReplayGames
                                 .Where(g => g.Id == choice.Id)
                                 .Select(g => new
                                 {
                                     g.Id,
                                     g.GameId,
                                     g.PlayerId,
                                     g.StartedAt,
                                     Moves = g.Moves.OrderBy(m => m.TurnIndex).ToList()
                                 })
                                 .FirstOrDefault();

                    if (game == null) { ShowError("Replay not found."); return; }

                    _mode = AppMode.Replay;
                    _btnNew.Enabled = false;
                    ShowError("");
                    SetStatusText($"Replaying GameId {game.GameId} ({game.Moves.Count} moves)…");

                    _board = EmptyBoard();
                    _lastHuman = new Point(-1, -1);
                    _lastServer = new Point(-1, -1);
                    Invalidate();

                    _replayMoves = game.Moves;
                    _replayIndex = 0;
                    _replayTimer.Stop();
                    _replayTimer.Start();
                }
                catch (Exception ex)
                {
                    ShowError("Failed to start replay: " + ex.Message);
                }
            };

            Controls.Add(_btnReplays);

            Controls.Add(_lblStatus);

            Controls.Add(_lblError);

            Controls.Add(_cmbReplays);

            // Buttons (top row)
            _btnNew.Location = new Point(_margin, _margin);
            _btnReplays.Location = new Point(_btnNew.Right + 12, _margin);

            // Combo to the right of Replays (top row)
            _cmbReplays.Location = new Point(_btnReplays.Right + 12, _margin - 2);
            Controls.Add(_cmbReplays);

            // Status on the next row; Error below it
            _lblStatus.Location = new Point(_margin, _btnNew.Bottom + 8);
            _lblError.Location = new Point(_margin, _lblStatus.Bottom + 6);


            _cmbReplays.Items.Clear();
            try
            {
                using var db = new Client.WinForms.Data.ReplayDbContext();
                db.Database.EnsureCreated();

                var items = db.ReplayGames
                              .Where(g => g.PlayerId == _playerId)
                              .OrderByDescending(g => g.StartedAt)
                              .Select(g => new
                              {
                                  g.Id,
                                  g.GameId,
                                  g.StartedAt,
                                  Moves = g.Moves.Count
                              })
                              .ToList();

                foreach (var it in items)
                {
                    var text = $"#{it.GameId} • {it.StartedAt:g} • {it.Moves} moves";
                    _cmbReplays.Items.Add(new ReplayChoice { Id = it.Id, Text = text });
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to load replays: " + ex.Message);
            }

            // Disable Replays button if nothing to play or nothing selected
            _btnReplays.Enabled = _cmbReplays.Items.Count > 0 && _cmbReplays.SelectedIndex >= 0;

            // Enable button only when a selection exists
            _cmbReplays.SelectedIndexChanged += (s, e) =>
            {
                _btnReplays.Enabled = _cmbReplays.SelectedIndex >= 0;
            };

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
                        if (_replay != null)
                        {
                            _replay.Moves.Add(new ReplayMove
                            {
                                TurnIndex = _turnIndex++,
                                Player = ReplayPlayerKind.Human,
                                Column = _animCol,
                                Row = _animRowTarget
                            });
                        }
                        _lastHuman = new Point(_animCol, _animRowTarget);
                        _animCommitNeeded = false;
                    }
                    _animActive = false;
                    _animTimer.Stop();

                    if (!string.IsNullOrEmpty(_pendingEndStatus) && !_srvAnimActive && !_srvPending)
                    {
                        _waitingServer = true; // block input for the dialog
                        var endStatus = _pendingEndStatus;
                        _pendingEndStatus = null;

                        SaveReplay(endStatus);
                        RefreshReplayCombo();
                        var result = MessageBox.Show(
                            $"Game over: {endStatus}\n\nPlay again?",
                            "Result",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            _ = StartNewGameAsync();
                        }
                        else
                        {
                            SetStatusText("Game finished.");
                            _btnNew.Enabled = true;
                        }
                        return;
                    }

                    if (_srvPending)
                    {
                        _waitingServer = true;
                        _srvDelayTimer.Stop();
                        _srvDelayTimer.Start();
                    }
                }
                Invalidate();
            };

            _srvAnimTimer.Tick += async (s, e) =>
            {
                _srvAnimYpx += 18;
                int targetYpx2 = (_margin * 2 + 40) + _srvAnimRowTarget * _cell + (_cell / 2);
                if (_srvAnimYpx >= targetYpx2)
                {
                    if (_srvAnimCommitNeeded)
                    {
                        _board[_srvAnimRowTarget][_srvAnimCol] = 2;
                        if (_replay != null)
                        {
                            _replay.Moves.Add(new ReplayMove
                            {
                                TurnIndex = _turnIndex++,
                                Player = ReplayPlayerKind.Server,
                                Column = _srvAnimCol,
                                Row = _srvAnimRowTarget
                            });
                        }
                        _lastServer = new Point(_srvAnimCol, _srvAnimRowTarget);
                        _srvAnimCommitNeeded = false;
                    }
                    _srvAnimActive = false;
                    _srvAnimTimer.Stop();
                    _waitingServer = false;
                }
                // If we deferred a terminal popup, show it now (after server disc lands)
                if (!string.IsNullOrEmpty(_pendingEndStatus))
                {
                    _waitingServer = true; // block input for the dialog
                    var endStatus = _pendingEndStatus;
                    _pendingEndStatus = null;

                    SaveReplay(endStatus);
                    RefreshReplayCombo();
                    var result = MessageBox.Show(
                        $"Game over: {endStatus}\n\nPlay again?",
                        "Result",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                        await StartNewGameAsync();
                    else
                    {
                        SetStatusText("Game finished.");
                        _btnNew.Enabled = true;
                    }

                    return;
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

            _replayTimer.Tick += (s, e) =>
{
    // stop when finished
    if (_replayIndex >= _replayMoves.Count)
    {
        _replayTimer.Stop();
        SetStatusText("Replay finished.");
        _btnNew.Enabled = true; // allow starting a new live game after replay
        return;
    }

    // apply next move instantly (no falling yet)
    var m = _replayMoves[_replayIndex++];
    _board[m.Row][m.Column] = (m.Player == ReplayPlayerKind.Human) ? 1 : 2;
    Invalidate();
};

            // Mouse click to drop a disc
            MouseDown += Form1_MouseDown;
            MouseMove += (s, e) =>
            {
                var boardTop = _margin * 2 + 40;
                var boardLeft = _margin;

                // ignore if outside board or input is blocked
                if (_animActive || _srvAnimActive || _waitingServer || _gameId <= 0 || e.Y < boardTop || e.X < boardLeft)
                {
                    if (_hoverCol != -1) { _hoverCol = -1; Invalidate(); }
                    return;
                }

                int col = (e.X - boardLeft) / _cell;
                if (col < 0 || col >= _cols) col = -1;

                if (_hoverCol != col) { _hoverCol = col; Invalidate(); }
            };

            MouseLeave += (s, e) =>
            {
                if (_hoverCol != -1) { _hoverCol = -1; Invalidate(); }
            };

        }

        private async void BtnNew_Click(object? sender, EventArgs e)
        {
            await StartNewGameAsync();
        }

        private async void Form1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_mode == AppMode.Replay) return;
            if (_gameId <= 0) return;

            // block input while animations or server turn are in progress
            if (_animActive || _srvAnimActive || _waitingServer)
            {
                SetStatusText("Wait for your turn…");
                return;
            }

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
                    ShowError("No response from server.");
                    return;
                }
                SetStatusFromServer(resp.Status);
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
                    // Defer popup + saving if ANY animation is still in progress or queued
                    if (_animActive || _srvAnimActive || _srvPending)
                    {
                        _pendingEndStatus = resp.Status; // will be handled by timers after commit
                        return;
                    }

                    // No animations pending -> safe to save now
                    _waitingServer = true; // block input
                    SaveReplay(resp.Status);
                    RefreshReplayCombo();
                    var result = MessageBox.Show(
                        $"Game over: {resp.Status}\n\nPlay again?",
                        "Result",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                        await StartNewGameAsync();
                    else
                    {
                        SetStatusText("Game finished.");
                        _btnNew.Enabled = true;
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowError("Move failed: " + ex.Message);
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

            // Grid and discs (use prebuilt rectangles)
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    Rectangle rect = _cells[r, c];

                    // Cell background
                    e.Graphics.FillRectangle(Brushes.LightGray, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);

                    // Disc
                    int v = _board[r][c];
                    int dx = rect.X + 6;
                    int dy = rect.Y + 6;
                    int ds = rect.Width - 12;

                    if (v == 1)
                        e.Graphics.FillEllipse(Brushes.Red, dx, dy, ds, ds);
                    else if (v == 2)
                        e.Graphics.FillEllipse(Brushes.Blue, dx, dy, ds, ds);
                    else
                        e.Graphics.FillEllipse(Brushes.White, dx, dy, ds, ds);

                    // Cell border
                    e.Graphics.DrawRectangle(Pens.DimGray, rect);
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

            // Human last-move ring (light red / pink)
            if (_lastHuman.X >= 0 && _lastHuman.Y >= 0)
            {
                int hx = boardLeft + _lastHuman.X * _cell + 6;
                int hy = boardTop + _lastHuman.Y * _cell + 6;
                using (var penHuman = new Pen(Color.LightPink, 4))
                    e.Graphics.DrawEllipse(penHuman, hx - 3, hy - 3, _cell - 12 + 6, _cell - 12 + 6);
            }

            // Server last-move ring (light blue)
            if (_lastServer.X >= 0 && _lastServer.Y >= 0)
            {
                int sx = boardLeft + _lastServer.X * _cell + 6;
                int sy = boardTop + _lastServer.Y * _cell + 6;
                using (var penServer = new Pen(Color.LightBlue, 4))
                    e.Graphics.DrawEllipse(penServer, sx - 3, sy - 3, _cell - 12 + 6, _cell - 12 + 6);
            }

            // Hover preview: light pink circle where the disc would land
            if (_hoverCol >= 0 && !_animActive && !_srvAnimActive && !_waitingServer && _gameId > 0)
            {
                // find lowest empty row in that column
                int row = -1;
                for (int r = _rows - 1; r >= 0; r--)
                {
                    if (_board[r][_hoverCol] == 0)
                    {
                        row = r;
                        break;
                    }
                }

                if (row >= 0)
                {
                    int x = boardLeft + _hoverCol * _cell + 6;
                    int y = boardTop + row * _cell + 6;
                    using (var brush = new SolidBrush(Color.FromArgb(100, Color.LightPink))) // semi-transparent
                    {
                        g.FillEllipse(brush, x, y, _cell - 12, _cell - 12);
                    }
                    using (var pen = new Pen(Color.LightPink, 2))
                    {
                        g.DrawEllipse(pen, x, y, _cell - 12, _cell - 12);
                    }
                }
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
