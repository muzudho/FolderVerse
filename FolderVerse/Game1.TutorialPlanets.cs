namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private int _tutorialPlanetSelection;
    private RenderTarget2D[] _tutorialPlanetTextures;
    private static readonly string[] TutorialPlanetNames={"プチステラ","コルトロディア","エトロブレッタ","オチョエッカ","ラルゴエディア","グランキスタ"};
    private static readonly (int X,int Y,int Z)[] TutorialPlanetSizes={(1,1,1),(1,2,1),(2,1,2),(2,2,2),(3,2,2),(3,3,3)};
    private static readonly string[] TutorialPlanetForms={"地賽","地棒","地盤","地箱","地箱","地箱"};
    private static Rectangle TutorialPlanetCard(int i)=>new(520+i%3*445,340+i/3*300,420,275);
    private void LoadTutorialPlanets()
    {
        var previous=GraphicsDevice.GetRenderTargets();var viewport=GraphicsDevice.Viewport;
        _tutorialPlanetTextures=new RenderTarget2D[6];
        try{
            for(int i=0;i<6;i++){
                var size=TutorialPlanetSizes[i];
                using var preview=new WorldPreview(GraphicsDevice);preview.Generate(size.X,size.Y,size.Z,123+i*31);
                var texture=new RenderTarget2D(GraphicsDevice,300,180,false,SurfaceFormat.Color,DepthFormat.Depth24);
                _tutorialPlanetTextures[i]=texture;GraphicsDevice.SetRenderTarget(texture);GraphicsDevice.Clear(Color.Transparent);
                preview.Draw(-.55f,.32f,0,false);
            }
        }finally{GraphicsDevice.SetRenderTargets(previous);GraphicsDevice.Viewport=viewport;}
    }
    private void DisposeTutorialPlanets(){if(_tutorialPlanetTextures==null)return;foreach(var texture in _tutorialPlanetTextures)texture?.Dispose();_tutorialPlanetTextures=null;}
    private void DrawTutorialPlanetMenu()
    {
        var gold=new Color(255,214,112);
        _ui.Box(new(0,0,1920,1080),new Color(15,28,43)*.97f);
        _ui.Text("遊び方",new(70,55),1f,Cream);
        _ui.Text("６つの星をめぐる旅",new(520,65),.8f,Cream);
        if(_tutorialHost!=null){
            float scale=Math.Min(410f/_tutorialHost.Width,690f/_tutorialHost.Height);
            _spriteBatch.Draw(_tutorialHost,new Rectangle(45,285,(int)(_tutorialHost.Width*scale),(int)(_tutorialHost.Height*scale)),Color.White);
        }
        _ui.Box(new(490,155,1365,145),Cream);_ui.Tile(new(490,245),new(34,34),MathHelper.PiOver4,Cream);
        _ui.Text("さあ、戦争の仕方を覚えましょう！",new(535,180),.65f,Muted);
        _ui.Text("最初の目的地は、小さな星［プチステラ］です！",new(535,245),.52f,Muted);
        for(int i=0;i<6;i++){
            var r=TutorialPlanetCard(i);bool enabled=i==0,selected=i==_tutorialPlanetSelection;
            if(selected){var border=r;border.Inflate(3,3);_ui.Box(border,gold);}
            _ui.Box(r,enabled?new Color(30,62,79):new Color(22,35,49));
            if(selected){for(int y=0;y<22;y++)_ui.Box(new(r.X+14,r.Y+24+y,y/2+1,1),gold);}
            float scale=.67f+i*.055f;int width=(int)(300*scale),height=(int)(180*scale);
            if(_tutorialPlanetTextures!=null)_spriteBatch.Draw(_tutorialPlanetTextures[i],new Rectangle(r.Center.X-width/2,r.Y+16+(170-height)/2,width,height),enabled?Color.White:new Color(65,74,86));
            _ui.Text((i+1).ToString("00"),new(r.X+20,r.Y+12),.42f,enabled?gold:new Color(106,117,134));
            if(i==0)DrawPetitstellaMedals(r);
            var ink=enabled?Cream:new Color(133,145,162);
            _ui.Center(TutorialPlanetNames[i],new(r.X+15,r.Y+182,r.Width-30,35),.63f,ink);
            var size=TutorialPlanetSizes[i];
            _ui.Center($"{size.X}×{size.Y}×{size.Z}　{TutorialPlanetForms[i]}",new(r.X+15,r.Y+221,r.Width-30,24),.38f,ink);
            string status=enabled?"この星へ出発":"このチュートリアルはまだ作ってません。";
            _ui.Center(status,new(r.X+15,r.Y+250,r.Width-30,22),Math.Min(.34f,(r.Width-30)/_font.MeasureString(status).X),enabled?gold:ink);
        }
        _ui.Text("方向キーで選択 / Enterで決定",new(520,945),.35f,Cream);
        _ui.Button(BackButton,"戻る",Muted,.6f);
        _ui.Button(TutorialClearMedals,"チュートリアルの勲章をクリアーする",Muted,.43f);
        if(_tutorialAchievementError.Length>0)_ui.Text(_tutorialAchievementError,new(520,1010),.4f,new Color(255,160,140));
    }
}
