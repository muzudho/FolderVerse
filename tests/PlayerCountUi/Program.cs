using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new CheckGame();game.Run();
sealed class CheckGame:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        var font=(SpriteFont)Get("_font");
        foreach(var text in new[]{"＜大きなお子様＞の人数確認フェーズ","このゲームをお遊びになる＜大きなお子様＞の人数を選んでください。","推定プレイ時間は＜６時間４０分＞です。よろしいですか？","＜１人あたり20分＞の概算です。遊ぶ時間は自由です。","次の担当の行動を選択 / 全員の行動後にターン確定"})
        {Check(text.All(c=>font.Characters.Contains(c)),"Font glyph: "+text);font.MeasureString(text);}
        Call("BeginPlayerCount");Set("_pointer",new Point(740,410+6*54));Call("PlayerCountInput",true,false,new KeyboardState());
        Check((int)Get("_requestedPlayers")==60,"Select 60 with mouse");
        Call("PlayerCountInput",false,true,new KeyboardState());var w=(WorldSetup)Get("_setup");Check(Get("_screen").ToString()=="ComputerPlayerCount" && (int)Get("_requestedComputers")==0,"Human confirmation opens capped computer count");
        Set("_pointer",new Point(740+109,410));Call("ComputerPlayerCountInput",true,false,new KeyboardState());Check((int)Get("_requestedComputers")==0,"Unavailable computer count ignored");
        Call("ComputerPlayerCountInput",false,true,new KeyboardState());Check(w.PlayerCount==60 && w.ComputerPlayerCount==0 && Get("_screen").ToString()=="Rolling","60 humans confirmed");
        Call("BeginPlayerCount");Set("_requestedPlayers",0);Call("PlayerCountInput",false,true,new KeyboardState());
        Set("_requestedComputers",0);Call("ComputerPlayerCountInput",false,true,new KeyboardState());Check(Get("_screen").ToString()=="ComputerPlayerCount","Zero total stays on count screen");
        Set("_pointer",new Point(740,410+6*54));Call("ComputerPlayerCountInput",true,false,new KeyboardState());Check((int)Get("_requestedComputers")==60,"60 computers selectable");
        Call("ComputerPlayerCountInput",false,true,new KeyboardState());Check(w.ComputerOnly && w.ActiveCount==60,"Computer-only world setup");
        w.SetCast(123);w.SetTerritoryRights(456);Call("BeginSelection");Check(Get("_screen").ToString()=="LandConquest" && w.HumanPlayers.Count==0,"Computer-only skips human selection");
        Set("_skipOtherLandTurns",true);Call("UpdateLandSetup",0d,new MouseState(),false,false,false);
        Check(Get("_screen").ToString()=="Fortune","All 60 computer land turns finish automatically");
        Call("UpdateLandSetup",5d,new MouseState(),false,false,false);
        Check(Get("_screen").ToString()=="WorldStatus" && (bool)Get("_autoPlay"),"Computer-only begins automatic domestic play");Set("_autoPlay",false);
        Call("BeginPlayerCount");Check((int)Get("_requestedPlayers")==1,"New session default 1");
        w.SetPlayerCount(2);w.SetWorld(0);w.SetCast(123);w.SetTerritoryRights(456);Call("BeginSelection");
        w.SelectPlayer(0);Call("ConfirmHumanSelection");Check(Get("_screen").ToString()=="PlayerSelect","Second participant selection");
        w.SelectPlayer(1);Call("ConfirmHumanSelection");Check(Get("_screen").ToString()=="LandConquest" && w.HumanPlayers.SequenceEqual(new[]{0,1}),"All choices before land");
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"PlayerCount"));
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        var batch=(SpriteBatch)Get("_spriteBatch");
        foreach(int count in new[]{0,1,30,60})
        {
            Set("_requestedPlayers",count);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawPlayerCount");batch.End();
            var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
            using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"player-count-{count}.png"));tex.SaveAsPng(output,tex.Width,tex.Height);
        }
        Set("_requestedPlayers",0);Set("_requestedComputers",60);
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawComputerPlayerCount");batch.End();
        {
            var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
            using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"computer-player-count-60.png"));tex.SaveAsPng(output,tex.Width,tex.Height);
        }
        var w=(WorldSetup)Get("_setup");
        w.SetParticipantCounts(60,0);w.SetWorld(0);w.SetCast(123);w.SetTerritoryRights(456);
        foreach(var stage in new[]{"CastReview","PlayerSelect"})
        {
            if(stage=="PlayerSelect")Call("BeginSelection");else Set("_screen",Enum.Parse(Get("_screen").GetType(),stage));
            GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call(stage=="CastReview"?"DrawCastUi":"DrawSelectionUi");batch.End();
            var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
            using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"participants-60-{stage}.png"));tex.SaveAsPng(output,tex.Width,tex.Height);
        }
        w.SetParticipantCounts(1,2);w.SetWorld(0);w.SetCast(123);w.SetTerritoryRights(456);
        Check(w.ActiveCount==3 && w.Looks.Length==3,"One human plus two computers draws exactly three conquerors");
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"CastReview"));
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawCastUi");Call("DrawCastPreviewInfo");batch.End();
        foreach(int count in new[]{1,3,19,20,21,60})
        {
            w.SetParticipantCounts(0,count);w.SetWorld(0);w.SetCast(123);w.SetTerritoryRights(456);Call("BeginSelection");
            var pp=GraphicsDevice.PresentationParameters;var canvas=new Rectangle(0,0,pp.BackBufferWidth,pp.BackBufferHeight);
            var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1);
            Call("DrawLandSetupGlobe",canvas,transform,GraphicsDevice.Viewport);
            Set("_skipOtherLandTurns",true);Call("UpdateLandSetup",0d,new MouseState(),false,false,false);
            Check(Get("_screen").ToString()=="Fortune","Land completes before fortune rendering");
            Call("DrawLandSetupGlobe",canvas,transform,GraphicsDevice.Viewport);
        }
        Console.WriteLine("PASS: land and falling-robot rendering with 1/3/19/20/21/60 participants.");
        w.SetPlayerCount(2);w.SetWorld(0);w.SetCast(123);w.HumanPlayers.AddRange(new[]{0,1});w.SetPlacement(456);w.SelectPlayer(0);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
        Call("NotifyPlayerTurn");Check(Get("_turnChime")!=null,"Turn notification sound created");Call("NotifyPlayerTurn");
        w.SelectPlayer(1);Call("NotifyPlayerTurn");Check(Get("_notifiedTurn").Equals((0,1)),"Turn notification follows next participant");
        w.SetPlayerCount(20);Set("_demoActive",true);Set("_demoIntro",true);Set("_demoAge",0d);
        Call("UpdateDemo",2.3,true,false,false,false,2.3);Check(w.PlayerCount==1 && Get("_screen").ToString()=="PlayerCount","Demo starts at one participant");
        Call("UpdateDemo",3.1,true,false,false,false,5.4);Check(w.PlayerCount==1 && Get("_screen").ToString()=="Rolling","Demo confirms one participant");
        Console.WriteLine("PASS: glyphs, 60-person mouse selection, computer limits, zero-total guard, computer-only setup, participant selection, rendering, demo and turn chime.");Exit();
    }
}
