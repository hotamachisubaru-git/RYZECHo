using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace RYZECHo.Prototype;

public partial class GameForm : Form
{
    private readonly GameModel _game = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer _frameTimer = new() { Interval = 16 };
    private readonly HashSet<Keys> _keysDown = [];
    private readonly HashSet<Keys> _pressedThisFrame = [];

    private TimeSpan _lastFrameTime;
    private Point _mousePosition;
    private bool _leftMouseDown;

    public GameForm()
    {
        InitializeComponent();

        _mousePosition = new Point(ClientRectangle.Width / 2, ClientRectangle.Height / 2);
        _frameTimer.Tick += HandleFrameTick;

        MouseMove += (_, args) => _mousePosition = args.Location;
        MouseDown += HandleMouseDown;
        MouseUp += HandleMouseUp;
        KeyDown += HandleKeyDown;
        KeyUp += (_, args) => _keysDown.Remove(args.KeyCode);
        Shown += (_, _) =>
        {
            _lastFrameTime = _clock.Elapsed;
            _frameTimer.Start();
        };
    }

    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);

        var graphics = args.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        _game.Render(graphics, ClientRectangle, _mousePosition);
    }

    private void HandleFrameTick(object? sender, EventArgs args)
    {
        var now = _clock.Elapsed;
        var deltaSeconds = (float)Math.Clamp((now - _lastFrameTime).TotalSeconds, 0.001, 0.05);
        _lastFrameTime = now;

        _game.Update(deltaSeconds, CaptureInput());
        _pressedThisFrame.Clear();
        Invalidate();
    }

    private InputSnapshot CaptureInput()
    {
        return new InputSnapshot(
            _keysDown.Contains(Keys.W),
            _keysDown.Contains(Keys.A),
            _keysDown.Contains(Keys.S),
            _keysDown.Contains(Keys.D),
            _pressedThisFrame.Contains(Keys.A),
            _pressedThisFrame.Contains(Keys.D),
            _pressedThisFrame.Contains(Keys.Enter),
            _pressedThisFrame.Contains(Keys.D1),
            _pressedThisFrame.Contains(Keys.D2),
            _pressedThisFrame.Contains(Keys.D3),
            _pressedThisFrame.Contains(Keys.D4),
            _pressedThisFrame.Contains(Keys.D5),
            _pressedThisFrame.Contains(Keys.Q),
            _pressedThisFrame.Contains(Keys.E),
            _pressedThisFrame.Contains(Keys.R),
            _leftMouseDown,
            _keysDown.Contains(Keys.F),
            _mousePosition);
    }

    private void HandleKeyDown(object? sender, KeyEventArgs args)
    {
        if (_keysDown.Add(args.KeyCode))
        {
            _pressedThisFrame.Add(args.KeyCode);
        }

        if (args.KeyCode == Keys.Tab)
        {
            _game.CycleBuildTool();
            args.SuppressKeyPress = true;
        }

        if (args.KeyCode == Keys.Space)
        {
            _game.ToggleBriefing();
            args.SuppressKeyPress = true;
        }
    }

    private void HandleMouseDown(object? sender, MouseEventArgs args)
    {
        _mousePosition = args.Location;

        if (args.Button == MouseButtons.Left)
        {
            _leftMouseDown = true;
            _game.HandleLeftClick(args.Location);
        }

        if (args.Button == MouseButtons.Right)
        {
            _game.HandleRightClick(args.Location);
        }
    }

    private void HandleMouseUp(object? sender, MouseEventArgs args)
    {
        _mousePosition = args.Location;

        if (args.Button == MouseButtons.Left)
        {
            _leftMouseDown = false;
        }
    }
}
