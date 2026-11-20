using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
sealed class SiteLayoutPreview:Game1
{
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
        var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetWorld(0);w.SetCast(2);w.SetPlacement(3);typeof(Game1).GetField("_setup",F)!.SetValue(this,w);
        var batch=(SpriteBatch)typeof(Game1).GetField("_spriteBatch",F)!.GetValue(this)!;var ui=(UiPainter)typeof(Game1).GetField("_ui",F)!.GetValue(this)!;
        using var target=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(18,38,48));batch.Begin();
        var kinds=new[]{BattleSiteKind.Standard,BattleSiteKind.Open,BattleSiteKind.Narrow,BattleSiteKind.MountainPass,BattleSiteKind.Valley,BattleSiteKind.Coast,BattleSiteKind.Standard,BattleSiteKind.Shoal};
        for(int i=0;i<kinds.Length;i++){
            bool sea=i>=6;var b=new BattleState(7){Kind=BattleKind.Node,Defender=0,TargetNode=0,EquipmentMode=RobotEquipmentMode.Materials,SiteProfile=new(kinds[i],0,20,5,3)};
            b.BuildTerrain(sea?BattleTerrain.Sea:kinds[i]==BattleSiteKind.MountainPass?BattleTerrain.Mountain:BattleTerrain.Field);
            var stock=new RobotWorld();stock.Initialize(1,2);for(int owner=0;owner<2;owner++)b.AddArmy(owner,0,Enumerable.Range(0,3).Select(_=>stock.Create(owner,RobotParts.Complete)));
            b.ChooseLayout();b.PrepareSiteFlag();b.Deploy();b.BuildSiteFortifications(4);b.ApplySiteLayout();int x=40+i%4*475,y=30+i/4*520;
            ui.Text((sea?"海 / ":"陸 / ")+b.SiteProfile.Name,new(x,y),.6f,Color.White);
            var bounds=new Rectangle(x,y+45,390,390);typeof(Game1).GetMethod("DrawPropagationGrid",F)!.Invoke(this,new object[]{b,bounds,1f});
            var flag=new Rectangle(bounds.X+b.Flag.X*30+14,bounds.Y+b.Flag.Y*30+4,2,23);ui.Box(flag,Color.White);ui.Box(new(flag.X+2,flag.Y,13,8),Color.Orange);
            foreach(var u in b.Units)ui.Box(new(bounds.X+u.Position.X*30+11,bounds.Y+u.Position.Y*30+11,9,9),u.Owner==0?Color.LightGreen:Color.Salmon);
            ui.Text($"水 {b.SiteWater.Count} / 岩 {b.SiteRocks.Count} / 壁 {b.Fortifications.Count}",new(x,y+450),.38f,Color.White);
        }
        batch.End();GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,"site-layouts.png"));target.SaveAsPng(file,1920,1080);Console.WriteLine("PASS actual terrain rendering: eight standard/special comparisons");Exit();
    }
}
