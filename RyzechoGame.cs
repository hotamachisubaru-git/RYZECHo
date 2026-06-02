using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RYZECHo.Audio;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace RYZECHo;

public class RyzechoGame : Game
{
    private readonly GameModel _game = new();
    private readonly GraphicsDeviceManager _graphics;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private SpriteBatch? _spriteBatch;
    private Graphics? _renderer;
    private AudioManager? _audioManager;
    private AudioRippleSystem? _audioRipples;

    public RyzechoGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Assets";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = GameLayout.DefaultClientWidth;
        _graphics.PreferredBackBufferHeight = GameLayout.DefaultClientHeight;
        Window.AllowUserResizing = true;
        ApplyApplicationTitle();
    }

    protected override void Initialize()
    {
        _previousKeyboard = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
        base.Initialize();
        ApplyApplicationTitle();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderer = new Graphics(GraphicsDevice, _spriteBatch);

        _audioManager = new AudioManager();
        _audioManager.MasterVolume = 0.80f;
        _audioManager.BgmVolume = 0.20f;
        _audioManager.SfxVolume = 0.80f;
        _audioManager.PreloadEffects(SoundEffectCatalog.All);
        _audioManager.PlayMusic(SoundEffectCatalog.BGM_HoloTheme);

        _audioRipples = new AudioRippleSystem(_audioManager);
        _game.AudioCueEmitted += HandleAudioCueEmitted;
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
            return;
        }

        if (IsNewKeyPress(keyboard, Keys.Escape))
        {
            _game.IsPaused = !_game.IsPaused;
            _previousKeyboard = keyboard;
            _previousMouse = mouse;
            return;
        }

        if (_game.IsPaused)
        {
            var pauseMousePosition = new Point(mouse.X, mouse.Y);
            _game.UpdatePauseMouse(pauseMousePosition);
            if (IsNewLeftClick(mouse))
            {
                _game.HandlePauseResume();
            }
            _previousKeyboard = keyboard;
            _previousMouse = mouse;
            base.Update(gameTime);
            return;
        }

        var mousePosition = new Point(mouse.X, mouse.Y);

        if (IsNewLeftClick(mouse))
        {
            _game.HandleLeftClick(mousePosition);
        }

        if (IsNewRightClick(mouse))
        {
            _game.HandleRightClick(mousePosition);
        }

        if (IsNewKeyPress(keyboard, Keys.Tab))
        {
            _game.CycleBuildTool();
        }

        if (IsNewKeyPress(keyboard, Keys.Space))
        {
            _game.ToggleBriefing();
        }

        var deltaSeconds = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0.001f, 0.05f);
        _game.Update(deltaSeconds, CaptureInput(keyboard, mouse, mousePosition));

        _previousKeyboard = keyboard;
        _previousMouse = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(XnaColor.Black);

        if (_renderer is not null)
        {
            var viewport = GraphicsDevice.Viewport;
            _renderer.BeginFrame();
            _game.Render(
                _renderer,
                new Rectangle(0, 0, viewport.Width, viewport.Height),
                new Point(Mouse.GetState().X, Mouse.GetState().Y));
            _renderer.EndFrame();
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _game.AudioCueEmitted -= HandleAudioCueEmitted;
        _audioManager?.Dispose();
        _audioManager = null;
        _audioRipples = null;
        _renderer?.Dispose();
        _renderer = null;
        base.UnloadContent();
    }

    private void HandleAudioCueEmitted(RippleKind kind, PointF sourcePosition, float strength)
    {
        if (_audioRipples is null)
        {
            return;
        }

        var listenerPosition = _game.AudioListenerPosition;
        var dx = sourcePosition.X - listenerPosition.X;
        var dy = sourcePosition.Y - listenerPosition.Y;
        var distance = MathF.Sqrt((dx * dx) + (dy * dy));
        var maxDistance = MathF.Max(1f, GameSettings.SoundMaxDistance * GameLayout.CellSize);
        var attenuation = MathF.Pow(1f - Math.Clamp(distance / maxDistance, 0f, 1f), 0.65f);
        var pan = Math.Clamp(dx / (GameLayout.CellSize * 8f), -1f, 1f);

        _audioRipples.Play(kind, strength * attenuation, pan);
    }

    private InputSnapshot CaptureInput(KeyboardState keyboard, MouseState mouse, Point mousePosition)
    {
        return new InputSnapshot(
            keyboard.IsKeyDown(Keys.W),
            keyboard.IsKeyDown(Keys.A),
            keyboard.IsKeyDown(Keys.S),
            keyboard.IsKeyDown(Keys.D),
            IsNewKeyPress(keyboard, Keys.A),
            IsNewKeyPress(keyboard, Keys.D),
            IsNewKeyPress(keyboard, Keys.Enter),
            IsNumberPressed(keyboard, Keys.D1, Keys.NumPad1),
            IsNumberPressed(keyboard, Keys.D2, Keys.NumPad2),
            IsNumberPressed(keyboard, Keys.D3, Keys.NumPad3),
            IsNumberPressed(keyboard, Keys.D4, Keys.NumPad4),
            IsNumberPressed(keyboard, Keys.D5, Keys.NumPad5),
            IsNumberPressed(keyboard, Keys.D6, Keys.NumPad6),
            IsNewKeyPress(keyboard, Keys.Q),
            IsNewKeyPress(keyboard, Keys.E),
            IsNewKeyPress(keyboard, Keys.R),
            IsNewKeyPress(keyboard, Keys.T),
            mouse.LeftButton == ButtonState.Pressed,
            keyboard.IsKeyDown(Keys.F),
            mousePosition);
    }

    private bool IsNumberPressed(KeyboardState keyboard, Keys topRow, Keys numberPad) =>
        IsNewKeyPress(keyboard, topRow) || IsNewKeyPress(keyboard, numberPad);

    private bool IsNewKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private bool IsNewLeftClick(MouseState mouse) =>
        mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

    private bool IsNewRightClick(MouseState mouse) =>
        mouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;

    private void ApplyApplicationTitle()
    {
        var title = ApplicationTitle();
        Window.Title = title;
        TrySetSdlWindowTitleUtf8(title);
    }

    private static string ApplicationTitle()
    {
        return typeof(RyzechoGame).Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "RYZECHØ";
    }

    private void TrySetSdlWindowTitleUtf8(string title)
    {
        if (Window.Handle == IntPtr.Zero)
        {
            return;
        }

        var utf8Title = Encoding.UTF8.GetBytes(title + '\0');
        var titlePointer = Marshal.AllocHGlobal(utf8Title.Length);
        try
        {
            Marshal.Copy(utf8Title, 0, titlePointer, utf8Title.Length);
            SDL_SetWindowTitle(Window.Handle, titlePointer);
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        finally
        {
            Marshal.FreeHGlobal(titlePointer);
        }
    }

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_SetWindowTitle(IntPtr window, IntPtr title);
}
