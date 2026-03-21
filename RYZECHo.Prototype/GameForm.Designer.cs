#nullable enable

namespace RYZECHo.Prototype;

partial class GameForm
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _frameTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(8, 12, 18);
        ClientSize = new Size(1440, 960);
        DoubleBuffered = true;
        Font = new Font("Yu Gothic UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        KeyPreview = true;
        Margin = new Padding(4);
        MaximizeBox = false;
        MinimumSize = new Size(1456, 999);
        Name = "GameForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "RYZECHØ Prototype v0.0.2";
        ResumeLayout(false);
    }
}
