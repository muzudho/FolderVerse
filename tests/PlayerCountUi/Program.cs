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
        Call("BeginPlayerCount");Set("_pointer",new Point(740+4*218,410+3*90));Call("PlayerCountInput",true,false,new KeyboardState());
        Check((int)Get("_requestedPlayers")==20,"Select 60 with mouse");
        Call("PlayerCountInput",false,true,new KeyboardState());var w=(WorldSetup)Get("_setup");Check(w.PlayerCount==20 && Get("_screen").ToString()=="Rolling","Confirm to world setup");
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
        foreach(int count in new[]{1,10,20})
        {
            Set("_requestedPlayers",count);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawPlayerCount");batch.End();
            var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
            using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"player-count-{count}.png"));tex.SaveAsPng(output,tex.Width,tex.Height);
        }
        var w=(WorldSetup)Get("_setup");w.SetPlacement(456);w.SelectPlayer(0);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
        Call("NotifyPlayerTurn");Check(Get("_turnChime")!=null,"Turn notification sound created");Call("NotifyPlayerTurn");
        w.SelectPlayer(1);Call("NotifyPlayerTurn");Check(Get("_notifiedTurn").Equals((0,1)),"Turn notification follows next participant");
        w.SetPlayerCount(20);Set("_demoActive",true);Set("_demoIntro",true);Set("_demoAge",0d);
        Call("UpdateDemo",2.3,true,false,false,false,2.3);Check(w.PlayerCount==1 && Get("_screen").ToString()=="PlayerCount","Demo starts at one participant");
        Call("UpdateDemo",3.1,true,false,false,false,5.4);Check(w.PlayerCount==1 && Get("_screen").ToString()=="Rolling","Demo confirms one participant");
        Console.WriteLine("PASS: glyphs, 20-person mouse selection, confirmation, participant selection, default, rendering and turn chime.");Exit();
    }
}
