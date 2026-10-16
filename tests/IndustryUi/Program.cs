using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CheckGame();game.Run();
sealed class CheckGame:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    WorldSetup w=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");
        w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        w.Industry.Queue(1);Call("FinishTransport");Check(Get("_screen").ToString()=="Industry","No-battle round still enters industry");
        var font=(SpriteFont)Get("_font");foreach(string text in new[]{"さあ、産業の時間です！　あなたは何台のロボットを製造できるかなー？","数年に一度の当たり年だあ！？　大穴当てたみなさん、今だ、大チャーンス！","ロボット不足のみなさん、ざんねんでした～！　工場はストップだー！　祈ってくださーい。持てる者が襲ってきまーす！","正規・安いパーツ / 産業ターン累計"}){Check(text.All(c=>font.Characters.Contains(c)),"Missing font glyph: "+text);font.MeasureString(text);}
    }
    protected override void Update(GameTime time){}
    void Save(string name)
    {
        var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,name+".png"));texture.SaveAsPng(output,texture.Width,texture.Height);
    }
    protected override void Draw(GameTime time)
    {
        var batch=(SpriteBatch)Get("_spriteBatch");
        foreach(int result in new[]{0,7,40})
        {
            w.Industry.Reset();w.Industry.Queue(1);
            int seed=Enumerable.Range(0,100000).First(s=>IndustryCycle.Sectors[IndustryCycle.SectorAt(new SeedRandom(s^7919^0x45123).Next(100))].Turns==result);w.Industry.Roll(seed);
            Call("BeginIndustry");Set("_industryAge",0d);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();Save("industry-spin");
            Call("UpdateIndustry",4.6,false);int industry=w.Robots.IndustryTurn;Call("UpdateIndustry",.1,false);Check(w.Robots.IndustryTurn==industry,"Repeated draw/update must not produce twice");
            GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();Save("industry-"+result);
            if(result==0)Check(Get("_industryPrayer")!=null,"Prayer mob on fumble");if(result==40)Check(Get("_industrySurprise")!=null,"Surprised mob on jackpot");
            Call("UpdateIndustry",0d,true);Check(Get("_screen").ToString()=="WorldStatus","Continue to domestic");
        }
        var node=w.Nodes.Current(0);var factory=w.Robots.Workshops[node.Id];for(int seed=0;factory.ProductionLines.Length!=3;seed++)factory.GenerateLines(seed);
        Set("_populationNode",node.Id);Set("_populationCell",node.Cell);Set("_factoryPlanLine",0);factory.SelectPlan(4);
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawNodeFactory");batch.End();Save("cheap-production-plans");
        var option=(Rectangle)typeof(Game1).GetMethod("FactoryPlanButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{6})!;
        Call("NodeFactoryClick",option.Center);Check(factory.SelectedPlan==6 && factory.CheapProduct,"Cheap option can be selected from actual menu");
        foreach(string mode in new[]{"_autoPlay","_demoActive"})
        {
            w.Industry.Reset();w.Industry.Queue(1);Set(mode,true);Call("BeginIndustry");Call("UpdateIndustry",10d,false);
            Check(Get("_screen").ToString()=="WorldStatus","Industry automatically proceeds during "+mode);Set(mode,false);
        }
        Console.WriteLine("PASS: industry entry without battle, font, wheel/result screens, prayer/surprise mobs, exactly-once application, return and cheap factory menu.");Exit();
    }
}
