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

        public Form1()
        {
            InitializeComponent();
            Text = "Connect4 Client";
            DoubleBuffered = true;
            ClientSize = new Size(_margin * 2 + _cols * _cell, _margin * 3 + _rows * _cell + 40);

            _btnNew.Location = new Point(_margin, _margin);
            _btnNew.Click += BtnNew_Click;
            Controls.Add(_btnNew);

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

            // Only handle clicks inside the board
            if (e.Y < boardTop || e.X < boardLeft) return;
            int col = (e.X - boardLeft) / _cell;
            int rowArea = (e.Y - boardTop) / _cell;
            if (col < 0 || col >= _cols || rowArea < 0 || rowArea >= _rows) return;

            try
            {
                var resp = await _api.SendMoveAsync(_gameId, col);
                if (resp == null)
                {
                    MessageBox.Show("No response from server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _board = resp.Board ?? _board;
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
        }

        private static int[][] EmptyBoard()
        {
            return Enumerable.Range(0, 6)
                             .Select(_ => new int[7])
                             .ToArray();
        }
    }
}
