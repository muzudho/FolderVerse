namespace FolderVerse;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _titleScreen;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1920,
            PreferredBackBufferHeight = 1080
        };
        Content.RootDirectory = "Content";
        Window.Title = "Folder Verse";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _titleScreen = Content.Load<Texture2D>("Images/title-screen");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(16, 35, 46));

        // Keep the foreground at 16:9; fill extra space with a subdued copy of the artwork.
        var viewport = GraphicsDevice.Viewport;
        var scale = System.Math.Min(
            (float)viewport.Width / _titleScreen.Width,
            (float)viewport.Height / _titleScreen.Height);
        var width = (int)(_titleScreen.Width * scale);
        var height = (int)(_titleScreen.Height * scale);
        var destination = new Rectangle(
            (viewport.Width - width) / 2,
            (viewport.Height - height) / 2,
            width, height);

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        var backgroundScale = System.Math.Max(
            (float)viewport.Width / _titleScreen.Width,
            (float)viewport.Height / _titleScreen.Height);
        var backgroundWidth = (int)System.Math.Ceiling(_titleScreen.Width * backgroundScale);
        var backgroundHeight = (int)System.Math.Ceiling(_titleScreen.Height * backgroundScale);
        var background = new Rectangle(
            (viewport.Width - backgroundWidth) / 2,
            (viewport.Height - backgroundHeight) / 2,
            backgroundWidth, backgroundHeight);
        _spriteBatch.Draw(_titleScreen, background, new Color(65, 75, 85));
        _spriteBatch.Draw(_titleScreen, destination, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}