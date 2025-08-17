using System;
using System.Windows.Forms;

namespace Client.WinForms
{
    // Extends your existing Form1 (must be 'partial' in Form1.cs)
    public partial class Form1 : Form
    {
        // Set by Program.cs when launched via connect4://
        public int? StartupPlayerId { get; set; }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (StartupPlayerId is not int pid) return;

            try
            {
                // Your Form1 already has _playerId / _lblStatus / _btnNew
                _playerId = pid;
                this.Text = $"Connect Four — Player #{_playerId}";

                _lblStatus!.Text = $"Ready. Player #{_playerId}. Click 'Create Game' to begin.";
                _btnNew!.Enabled = true;
            }
            catch
            {
                // If your field names differ, no crash — adjust the names here if needed.
            }
        }
    }
}
