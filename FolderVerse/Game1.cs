namespace FolderVerse;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Game1 : Game
{
    private enum Screen { Title, Rolling, Review, Confirmed }
    private readonly GraphicsDeviceManager _graphics;
    private readonly Random _random = new();
    private SpriteBatch _spriteBatch;
    private Texture2D _titleScreen, _pixel;
    private SpriteFont _font;
    private WorldPreview _world;
    private Screen _screen;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private double _rollTime;
    private float _yaw = -0.55f, _pitch = 0.3f;
    private bool _dragging;
    private static readonly Rectangle StartButton = new(740, 884, 440, 106);
    private static readonly Rectangle ActionButton = new(1360, 750, 400, 90);
    private static readonly Rectangle RetryButton = new(1360, 860, 400, 80);
    private static readonly Rectangle BackButton = new(100, 960, 320, 65);
    private static readonly Rectangle PreviewArea = new(130, 240, 1120, 680);

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 1920, PreferredBackBufferHeight = 1080, PreferredDepthStencilFormat = DepthFormat.Depth24 };
        Content.RootDirectory = "Content";
        Window.Title = "Folder Verse";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _titleScreen = Content.Load<Texture2D>("Images/title-screen");
        _font = Content.Load<SpriteFont>("UiFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _world = new WorldPreview(GraphicsDevice);
    }

    private Rectangle CanvasBounds()
    {
        var viewport = GraphicsDevice.Viewport;
        float scale = Math.Min(viewport.Width / 1920f, viewport.Height / 1080f);
        int width = (int)(1920 * scale), height = (int)(1080 * scale);
        return new Rectangle((viewport.Width - width) / 2, (viewport.Height - height) / 2, width, height);
    }

    private Point CanvasPoint(MouseState mouse)
    {
        var bounds = CanvasBounds();
        if (bounds.Width == 0 || bounds.Height == 0) return new Point(-1, -1);
        return new Point((int)((mouse.X - bounds.X) * 1920f / bounds.Width), (int)((mouse.Y - bounds.Y) * 1080f / bounds.Height));
    }

    private void BeginRolling()
    {
        _screen = Screen.Rolling;
        _rollTime = 0;
        _dragging = false;
        _world.Generate(3, 3, 3, _random.Next());
    }

    protected override void Update(GameTime gameTime)
    {
        HandleInput(Mouse.GetState(), Keyboard.GetState(), gameTime, IsActive);
        base.Update(gameTime);
    }

    private void HandleInput(MouseState mouse, KeyboardState keyboard, GameTime gameTime, bool active)
    {
        var point = CanvasPoint(mouse);
        bool click = active && mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        bool enter = active && keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter);
        bool escape = active && keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape);
        if (escape)
        {
            if (_screen == Screen.Title) Exit();
            else { _screen = Screen.Title; _dragging = false; }
        }
        else if (_screen == Screen.Title)
        {
            if ((click && StartButton.Contains(point)) || enter) BeginRolling();
        }
        else
        {
            if (click && BackButton.Contains(point)) { _screen = Screen.Title; _dragging = false; }
            else if (_screen == Screen.Rolling)
            {
                if ((click && ActionButton.Contains(point)) || enter) _screen = Screen.Review;
                else
                {
                    _rollTime += gameTime.ElapsedGameTime.TotalSeconds;
                    if (_rollTime >= 1.15)
                    {
                        _rollTime %= 1.15;
                        _world.Generate(_random.Next(1, 6), _random.Next(1, 6), _random.Next(1, 6), _random.Next());
                    }
                    _yaw += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.22f;
                }
            }
            else
            {
                if (click && RetryButton.Contains(point)) BeginRolling();
                else if (_screen == Screen.Review && ((click && ActionButton.Contains(point)) || enter)) _screen = Screen.Confirmed;
                else if (click && PreviewArea.Contains(point)) _dragging = true;
                if (!active || mouse.LeftButton == ButtonState.Released) _dragging = false;
                if (_dragging && !click)
                {
                    var previousPoint = CanvasPoint(_previousMouse);
                    _yaw += (point.X - previousPoint.X) * 0.008f;
                    _pitch = MathHelper.Clamp(_pitch + (point.Y - previousPoint.Y) * 0.008f, -MathHelper.PiOver2, MathHelper.PiOver2);
                }
            }
        }
        _previousMouse = mouse;
        _previousKeyboard = keyboard;
    }

    private void Box(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);
    private void Text(string text, Vector2 position, float scale, Color color) =>
        _spriteBatch.DrawString(_font, text, position, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    private void Button(Rectangle rect, string label, Color color)
    {
        if (rect.Contains(CanvasPoint(Mouse.GetState()))) color = Color.Lerp(color, Color.White, 0.15f);
        Box(new Rectangle(rect.X, rect.Y + 6, rect.Width, rect.Height), new Color(7, 23, 35));
        Box(rect, color);
        var size = _font.MeasureString(label) * 0.8f;
        Text(label, new Vector2(rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2), 0.8f, Color.White);
    }
    private void Parameter(string label, int value, int y)
    {
        Box(new Rectangle(1360, y, 400, 115), new Color(31, 62, 79));
        Text(label, new Vector2(1390, y + 35), 0.7f, new Color(179, 207, 215));
        Text(value.ToString(), new Vector2(1660, y + 9), 1.6f, new Color(255, 221, 145));
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(16, 35, 46));
        var viewport = GraphicsDevice.Viewport;
        var canvas = CanvasBounds();
        if (canvas.Width <= 0 || canvas.Height <= 0) { base.Draw(gameTime); return; }
        float cover = Math.Max(viewport.Width / (float)_titleScreen.Width, viewport.Height / (float)_titleScreen.Height);
        int bw = (int)Math.Ceiling(_titleScreen.Width * cover), bh = (int)Math.Ceiling(_titleScreen.Height * cover);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_titleScreen, new Rectangle((viewport.Width - bw) / 2, (viewport.Height - bh) / 2, bw, bh), new Color(65, 75, 85));
        if (_screen == Screen.Title) _spriteBatch.Draw(_titleScreen, canvas, Color.White);
        _spriteBatch.End();
        if (_screen != Screen.Title)
        {
            var transform = Matrix.CreateScale(canvas.Width / 1920f, canvas.Height / 1080f, 1) * Matrix.CreateTranslation(canvas.X, canvas.Y, 0);
            _spriteBatch.Begin(transformMatrix: transform);
            Box(new Rectangle(0, 0, 1920, 1080), new Color(16, 35, 46));
            Box(PreviewArea, new Color(23, 49, 64));
            Text("FOLDER VERSE / 世界作成", new Vector2(100, 60), 1.05f, new Color(255, 232, 184));
            Text(_screen == Screen.Rolling ? "抽選中 / STOP で確認" : _screen == Screen.Review ? "確認 / 左ドラッグで回転" : "世界確定 / 左ドラッグで回転", new Vector2(100, 140), 0.7f, new Color(170, 206, 216));
            Parameter("Width", _world.Width, 270);
            Parameter("Height", _world.Height, 405);
            Parameter("Depth", _world.Depth, 540);
            Text(_world.Kind, new Vector2(1360, 685), 0.85f, new Color(255, 221, 145));
            if (_screen != Screen.Confirmed) Button(ActionButton, _screen == Screen.Rolling ? "STOP" : "この世界を確定", new Color(45, 135, 142));
            else Text("世界を確定済み", new Vector2(1360, 780), 0.8f, new Color(126, 218, 174));
            if (_screen != Screen.Rolling) Button(RetryButton, "再抽選", new Color(68, 84, 111));
            Button(BackButton, "タイトルへ戻る", new Color(45, 65, 81));
            Text("SEED " + _world.Seed, new Vector2(520, 975), 0.55f, new Color(133, 174, 187));
            _spriteBatch.End();
            var worldViewport = new Viewport(
                canvas.X + (int)(PreviewArea.X * canvas.Width / 1920f),
                canvas.Y + (int)(PreviewArea.Y * canvas.Height / 1080f),
                Math.Max(1, (int)(PreviewArea.Width * canvas.Width / 1920f)),
                Math.Max(1, (int)(PreviewArea.Height * canvas.Height / 1080f)));
            GraphicsDevice.Viewport = worldViewport;
            _world.Draw(_yaw, _pitch);
            GraphicsDevice.Viewport = viewport;
        }
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _world?.Dispose(); _pixel?.Dispose(); _spriteBatch?.Dispose();
        base.UnloadContent();
    }
}