namespace FolderVerseMuseum;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

internal sealed partial class MuseumGame
{
    private enum Page { Menu, Direction, Parts }
    private Page _page;
    private static readonly Color Paper = new(249, 229, 190);
    private static readonly Color Panel = new(39, 66, 80);
    private static readonly Color Accent = new(106, 191, 174);
    private static readonly Rectangle Back = new(34, 24, 150, 42);
    private static readonly Rectangle DirectionDoor = new(100, 247, 340, 130);
    private static readonly Rectangle PartsDoor = new(480, 247, 340, 130);
    private static readonly Rectangle Reset = new(734, 526, 150, 42);
    private readonly RobotPartsExhibit _parts = new();
    private SpriteBatch _batch;
    private SpriteFont _font;
    private Texture2D _pixel;
    private int _smokeFrame;

    private float CanvasScale => Math.Min(GraphicsDevice.Viewport.Width / 920f, GraphicsDevice.Viewport.Height / 620f);
    private Vector2 CanvasOffset => (new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) -
        new Vector2(920, 620) * CanvasScale) / 2;
    private Point ToCanvas(Point point) => ((point.ToVector2() - CanvasOffset) / CanvasScale).ToPoint();

    private void LoadExhibitInterface()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("MuseumFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        OpenPage(Page.Menu);
    }

    private void OpenPage(Page page)
    {
        _page = page;
        _dragging = false;
        _parts.CancelDrag();
        if (page == Page.Direction) UpdateTitle();
        else Window.Title = page == Page.Menu ? "FolderVerseMuseum" : "FolderVerseMuseum — ロボットパーツ";
    }

    private void UpdateExhibitInterface(MouseState mouse, bool reset)
    {
        var at = ToCanvas(mouse.Position);
        bool pressed = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        bool released = mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed;
        if (!IsActive) { _parts.CancelDrag(); return; }
        if (_page == Page.Menu)
        {
            if (pressed && DirectionDoor.Contains(at)) OpenPage(Page.Direction);
            else if (pressed && PartsDoor.Contains(at)) OpenPage(Page.Parts);
        }
        else if (pressed && Back.Contains(at)) OpenPage(Page.Menu);
        else if (_page == Page.Parts)
        {
            if (reset || pressed && Reset.Contains(at)) _parts.Reset();
            else _parts.Input(at, pressed, released);
        }
    }

    private void DrawExhibitInterface()
    {
        var transform = Matrix.CreateScale(CanvasScale) * Matrix.CreateTranslation(CanvasOffset.X, CanvasOffset.Y, 0);
        _batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transform);
        if (_page == Page.Menu)
        {
            Center("FolderVerseMuseum", new(0, 124, 920, 76), Paper, 1.8f);
            Button(DirectionDoor, "方向のロボット");
            Button(PartsDoor, "ロボットパーツ");
        }
        else
        {
            Button(Back, "戻る / Esc");
            if (_page == Page.Direction)
                Center("1 旧型   2 魚   3 猿   4 犬   /   ドラッグで回す", new(184, 24, 700, 42), Paper, .9f);
            else
            {
                Center("ロボットパーツ格納庫", new(184, 24, 552, 48), Paper, 1.3f);
                _parts.Draw(Box, Center, ToCanvas(Mouse.GetState().Position));
                Center("頭・胴・脚を同じ段へドラッグして入れ替え", new(40, 526, 660, 42), Paper, .95f);
                Button(Reset, "リセット / R");
            }
        }
        _batch.End();
    }

    private void Box(Rectangle bounds, Color color) => _batch.Draw(_pixel, bounds, color);
    private void Center(string text, Rectangle bounds, Color color, float scale)
        => _batch.DrawString(_font, text, bounds.Center.ToVector2() - _font.MeasureString(text) * scale / 2,
            color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

    private void Button(Rectangle bounds, string text)
    {
        bool hovered = bounds.Contains(ToCanvas(Mouse.GetState().Position));
        Box(new Rectangle(bounds.X, bounds.Y + 4, bounds.Width, bounds.Height), Color.Black * .5f);
        Box(bounds, hovered ? Accent : Panel);
        Center(text, bounds, hovered ? new Color(19, 38, 51) : Paper, 1);
    }

    private void AdvanceSmoke()
    {
        if (_smokeFrame++ == 0) { OpenPage(Page.Direction); return; }
        if (_page == Page.Direction)
        {
            if (_exhibit + 1 < _robots.Length) _exhibit++;
            else OpenPage(Page.Parts);
            return;
        }
        _parts.CheckSwap();
        Exit();
    }
}
