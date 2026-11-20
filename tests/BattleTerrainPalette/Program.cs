using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new PaletteGame();game.Run();
sealed class PaletteGame:Game1
{
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        var palette=typeof(WorldTerrain).GetProperty("EarthPalette",BindingFlags.NonPublic|BindingFlags.Static)!;
        var mapColor=typeof(WorldTerrain).GetMethod("MapColorAt",BindingFlags.NonPublic|BindingFlags.Static)!;
        bool previous=(bool)palette.GetValue(null)!;
        using var sheet=new RenderTarget2D(GraphicsDevice,512,1024);
        var batch=(SpriteBatch)typeof(Game1).GetField("_spriteBatch",F)!.GetValue(this)!;
        var expected=new List<(Point At,Color Color)>();var seaColors=new HashSet<Color>();
        try{
            GraphicsDevice.SetRenderTarget(sheet);GraphicsDevice.Clear(Color.Black);
            for(int seed=0;seed<8;seed++){
                var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetWorld(seed);w.SetCast(8);w.SetPlacement(3);
                typeof(Game1).GetField("_setup",F)!.SetValue(this,w);
                var node=w.Nodes.All.First(n=>Math.Abs(w.Geographic(w.Cells[n.Cell].Normal).Y)<.5f);var cell=w.Cells[node.Cell];
                float elevation=w.Terrain.Elevation(w.Routes.Position(node.Cell,node.Center));float latitude=w.Geographic(cell.Normal).Y;
                batch.Begin();
                for(int column=0;column<4;column++){
                    bool sea=column>=2,earth=column%2==0;palette.SetValue(null,earth);
                    var battle=new BattleState(seed){TargetNode=node.Id,Terrain=sea?BattleTerrain.Sea:BattleTerrain.Field};
                    if(sea)for(int y=0;y<battle.BoardSize;y++)for(int x=0;x<battle.BoardSize;x++){battle.Walls.Add(new(x,y));battle.SeaClearWater.Add(new(x,y));}
                    var bounds=new Rectangle(column*128,seed*128,128,128);
                    typeof(Game1).GetMethod("DrawBattleTerrain",F)!.Invoke(this,new object[]{battle,bounds,1f});
                    Color color=earth?(sea?new Color(28,88,125):Math.Abs(latitude)>.5f?new Color(161,192,203):elevation<.55f?new Color(163,137,91):new Color(35,64,58)):
                        Color.Lerp(Color.Black,(Color)mapColor.Invoke(null,new object[]{sea?.46f:elevation,cell.Normal,w.PolarAxis,w.WorldSeed})!,sea?.7f:.55f);
                    expected.Add((new(bounds.X+4,bounds.Y+4),color));if(sea&&!earth)seaColors.Add(color);
                }
                batch.End();
            }
            GraphicsDevice.SetRenderTarget(null);var pixels=new Color[sheet.Width*sheet.Height];sheet.GetData(pixels);
            foreach(var sample in expected)if(pixels[sample.At.Y*sheet.Width+sample.At.X]!=sample.Color)throw new Exception("Battle palette differs from shared map palette / Earth compatibility");
            if(seaColors.Count!=8)throw new Exception("Planet sea palettes collapsed");
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"battle-terrain-palettes.png"));sheet.SaveAsPng(output,sheet.Width,sheet.Height);
            Console.WriteLine("PASS 32 rendered terrain samples: eight planet palettes, Earth compatibility, field and sea.");
        }finally{palette.SetValue(null,previous);GraphicsDevice.SetRenderTarget(null);}
        Exit();
    }
}
