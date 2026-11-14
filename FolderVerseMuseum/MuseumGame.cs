namespace FolderVerseMuseum;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

internal sealed class MuseumGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly bool _smoke;
    private ToyOrientationRenderer _robot;
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
        Window.Title = "FolderVerseMuseum — 旧・方向のロボット | ドラッグ：回転 / X・Y・Z：軸回転 / R：リセット / Esc：閉じる";
        IsMouseVisible = true;
    }

    protected override void LoadContent() => _robot = new ToyOrientationRenderer(GraphicsDevice);

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var area = GraphicsDevice.Viewport.Bounds;
        bool Pressed(Keys key) => keys.IsKeyDown(key) && _previousKeys.IsKeyUp(key);

        if (Pressed(Keys.Escape)) Exit();
        if (Pressed(Keys.R)) _orientation = Quaternion.Identity;

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
        _robot.Draw(0, 0, Matrix.CreateFromQuaternion(_orientation));
        if (_smoke) Exit();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _robot?.Dispose();
        base.UnloadContent();
    }
}
