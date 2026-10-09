using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using var game = new PreviewBoundsCheck();
game.Run();

sealed class PreviewBoundsCheck : Game
{
    readonly GraphicsDeviceManager graphics;
    public PreviewBoundsCheck()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = Path.Combine(AppContext.BaseDirectory, "Content");
    }
    protected override void LoadContent()
    {
        using var preview = new WorldPreview(GraphicsDevice);
        var font = Content.Load<SpriteFont>("UiFont");
        using var batch = new SpriteBatch(GraphicsDevice);
        var portraits = new PortraitRenderer(Content);
        batch.Begin();
        for(int id=0;id<60;id++)for(int variant=0;variant<6;variant++)
        {
            var look=new ConquerorLook(id,variant);
            portraits.Draw(batch,look,new Rectangle(0,0,244,166),forCreation:true);
            portraits.Draw(batch,look,new Rectangle(0,0,256,192));
        }
        batch.End();
        foreach (var size in new[] { (1,1,1), (9,1,1), (1,9,1), (1,1,9), (1,7,7), (7,1,7), (7,7,1), (5,5,5) })
        {
            preview.Generate(size.Item1, size.Item2, size.Item3, 0);
            string expected = size == (1,1,1) ? "地賽" : size.Item1 == 9 || size.Item2 == 9 || size.Item3 == 9 ? "地棒" : size == (5,5,5) ? "地箱" : "地盤";
            if (preview.Kind != expected || preview.GlobeName != expected + "儀") throw new Exception("Incorrect shape name");
            // Cover the UI path, including the newly introduced kanji in 地賽.
            foreach(char character in preview.GlobeName)
                if(!font.Characters.Contains(character)) throw new Exception($"Missing UI glyph: {character}");
            font.MeasureString(preview.GlobeName);
            batch.Begin();
            batch.DrawString(font, preview.GlobeName, Vector2.Zero, Color.White);
            batch.End();
        }
        foreach (var invalid in new[] { (0,1,1,"width"), (10,1,1,"width"), (1,0,1,"height"), (1,10,1,"height"), (1,1,0,"depth"), (1,1,10,"depth") })
        {
            try { preview.Generate(invalid.Item1, invalid.Item2, invalid.Item3, 0); }
            catch (ArgumentOutOfRangeException ex) when (ex.ParamName == invalid.Item4) { continue; }
            throw new Exception("Invalid dimension was not rejected correctly");
        }
        var world = new WorldSetup();
        for (int seed = 0; seed < 128; seed++) { world.SetWorld(seed); preview.ShowSetup(world, false); }
        Console.WriteLine("PASS: preview rendering and UI font drawing for all shape names, maxima and orientations, correct bounds exceptions, and 128 generated worlds.");
        Console.WriteLine("PASS: all 360 creation portraits and all 360 domestic portraits loaded and drawn through their separate paths.");
        Exit();
    }
}
