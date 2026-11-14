namespace FolderVerseMuseum;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

internal sealed partial class MuseumGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly bool _smoke;
    private ToyOrientationRenderer[] _robots;
    private int _exhibit;
    private static readonly string[] ExhibitNames = { "旧・方向のロボット", "魚型ロボットの玩具", "猿型ロボットの玩具", "犬型ロボットの玩具" };
    private Quaternion _orientation = Quaternion.Identity;
    private MouseState _previousMouse;
    private KeyboardState _previousKeys;
    private bool _dragging;

    public MuseumGame(bool smoke)
    {
        _smoke = smoke;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 920,
            PreferredBackBufferHeight = 620
        };
        Window.AllowUserResizing = true;
        UpdateTitle();
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        Content.RootDirectory = "Content";
        LoadExhibitInterface();
        _robots = new[] { new ToyOrientationRenderer(GraphicsDevice),
            new ToyOrientationRenderer(GraphicsDevice, AnimalToy.Fish),
            new ToyOrientationRenderer(GraphicsDevice, AnimalToy.Monkey),
            new ToyOrientationRenderer(GraphicsDevice, AnimalToy.Dog) };
    }

    private void UpdateTitle() => Window.Title =
        $"FolderVerseMuseum — {ExhibitNames[_exhibit]} | 1–4：展示 / ドラッグ：回転 / X・Y・Z：軸回転 / R：リセット";

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var area = GraphicsDevice.Viewport.Bounds;
        bool Pressed(Keys key) => keys.IsKeyDown(key) && _previousKeys.IsKeyUp(key);

        if (Pressed(Keys.Escape))
        {
            if (_page == Page.Menu) Exit();
            else OpenPage(Page.Menu);
        }
        if (_page != Page.Direction || IsActive && mouse.LeftButton == ButtonState.Pressed &&
            _previousMouse.LeftButton == ButtonState.Released && ToCanvas(mouse.Position).Y < 76)
        {
            UpdateExhibitInterface(mouse, Pressed(Keys.R));
            _previousMouse = mouse;
            _previousKeys = keys;
            return;
        }
        if (Pressed(Keys.R)) _orientation = Quaternion.Identity;
        for (int i = 0; i < ExhibitNames.Length; i++)
            if (Pressed(Keys.D1 + i))
            {
                _exhibit = i;
                _orientation = Quaternion.Identity;
                UpdateTitle();
            }

        if (IsActive && mouse.LeftButton == ButtonState.Pressed)
        {
            if (_previousMouse.LeftButton == ButtonState.Released) _dragging = area.Contains(mouse.Position);
            if (_dragging && _previousMouse.LeftButton == ButtonState.Pressed)
                _orientation = Arcball.Drag(_orientation, _previousMouse.Position, mouse.Position, area);
        }
        else _dragging = false;

        float angle = keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift)
            ? -MathHelper.Pi / 6 : MathHelper.Pi / 6;
        if (Pressed(Keys.X)) Rotate(Vector3.UnitX, angle);
        if (Pressed(Keys.Y)) Rotate(Vector3.UnitY, angle);
        if (Pressed(Keys.Z)) Rotate(Vector3.UnitZ, angle);

        _previousMouse = mouse;
        _previousKeys = keys;
        base.Update(gameTime);
    }

    private void Rotate(Vector3 axis, float angle)
    {
        var pose = Matrix.CreateFromQuaternion(_orientation) * Matrix.CreateFromAxisAngle(axis, angle);
        _orientation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(pose));
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(19, 38, 51));
        if (_page == Page.Direction)
            _robots[_exhibit].Draw(0, 0, Matrix.CreateFromQuaternion(_orientation));
        DrawExhibitInterface();
        if (_smoke) AdvanceSmoke();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        if (_robots != null)
            foreach (var robot in _robots) robot.Dispose();
        _pixel?.Dispose();
        _batch?.Dispose();
        base.UnloadContent();
    }
}
