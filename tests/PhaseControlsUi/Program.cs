using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

using var game=new PhaseControlsCheck();game.Run();
sealed class PhaseControlsCheck:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,industryBefore,result,target;WorldSetup w=null!;int[] snapshot=null!;
    object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
    object Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,args)!;
    void Screen(string name)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),name));
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    void Click(Point point)
    {
        var canvas=(Rectangle)Call("CanvasBounds");
        foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})
            Call("HandleInput",new MouseState(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);
    }
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
        w=(WorldSetup)Get("_setup");w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.HumanPlayers.Add(0);
        w.Industry.Queue(1);Call("BeginIndustry");industryBefore=w.Robots.IndustryTurn;result=w.Industry.Result;
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        if(frame==1){
            Click(new(1775,64));
            Check(Get("_screen").ToString()=="Industry" && (bool)Get("_industryApplied") && (double)Get("_industryIdleAge")==0,"Industry skip stops at result and waits");
            Check(w.Industry.Result==result && w.Robots.IndustryTurn==industryBefore+result,"Skip preserves roulette result and applies once");
            Click(new(1775,64));Call("UpdateIndustry",0d,false);
            Check(w.Robots.IndustryTurn==industryBefore+result && Get("_screen").ToString()=="Industry","Repeated skip never rerolls or applies twice");
        }
        if(frame==2){
            w.Industry.Queue(2);w.Lessons.Reset();
            w.Lessons.Pending.AddRange(new[]{new LessonAward(0,BattleLesson.FlagCourtesy),new LessonAward(0,BattleLesson.RetreatBackwards)});
            Call("BeginBattleResearch");Check(Get("_screen").ToString()=="BattleResearch","Research has multiple unread cards");
        }
        if(frame==3){
            Click(new(1775,64));
            Check(Get("_screen").ToString()=="Industry" && (w.Lessons.Owned[0]&(BattleLesson.FlagCourtesy|BattleLesson.RetreatBackwards))==(BattleLesson.FlagCourtesy|BattleLesson.RetreatBackwards),"Research skip skips every remaining card, retaining awards");
        }
        if(frame==4){
            var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();
            Click(new(1775,64));Check((bool)Get("_industryApplied") && Get("_screen").ToString()=="Industry","Scaled industry skip reaches result");
        }
        if(frame==5){
            CheckOwnershipPixels();Screen("Transport");Set("_militaryTransportPlayback",true);Call("BeginPropagation");
        }
        if(frame==6){
            var net=(CubeNet)Get("_propagationNet");Check(ReferenceEquals(net.NodeOwnerSnapshot,w.Campaign.PreviousNodeOwners) && net.DisplayedNodeOwner(w.Nodes.All[target])==0 && w.Nodes.All[target].Owner==1,"Actual sortie drawing uses pre-battle ownership without changing resolved model");
            Screen("WorldStatus");((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
        }
        if(frame==7){
            var node=w.Nodes.All.First(n=>n.Functional && w.Robots.Workshops[n.Id].ProductionLines.Length>0);node.Owner=0;
            Call("OpenNodeInspector",node.Id);Call("UpdateDomesticSlide",1f);Call("OpenNodeFactory");Set("_factoryPlanLine",0);
        }
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame*10),TimeSpan.Zero));
        var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);
        using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"phase-controls-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
        if(frame++==7){Console.WriteLine("PASS: real skip input, preserved roulette/awards, exact pre-battle marker pixels, resolved ownership preserved, 1080p/540p rendering and factory labels.");Exit();}
    }
    void CheckOwnershipPixels()
    {
        var node=w.Nodes.All.First(n=>!n.IsHarbor);int cell=node.Cell;target=node.Id;
        foreach(var n in w.Nodes.InCell(cell))n.Owner=0;
        snapshot=w.Nodes.All.Select(n=>n.Owner).ToArray();typeof(ConquestCampaign).GetProperty("PreviousNodeOwners")!.SetValue(w.Campaign,snapshot);
        var net=new CubeNet();var batch=(SpriteBatch)Get("_spriteBatch");var ui=(UiPainter)Get("_ui");var viewport=GraphicsDevice.Viewport;
        Color[] Render(){
            using var image=new RenderTarget2D(GraphicsDevice,600,600);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Black);batch.Begin();
            Func<Vector3,Vector2> project=p=>new(300+Vector3.Dot(p,w.Cells[cell].U)*45,300+Vector3.Dot(p,w.Cells[cell].V)*45);
            typeof(CubeNet).GetMethod("DrawNodeMarkers",F)!.Invoke(net,new object[]{ui,w,cell,project,0f});batch.End();
            var colors=new Color[600*600];GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Viewport=viewport;image.GetData(colors);return colors;
        }
        var before=Render();node.Owner=1;net.NodeOwnerSnapshot=snapshot;var sortie=Render();
        Check(before.SequenceEqual(sortie),"Sortie snapshot must produce identical marker pixels to pre-battle state");
        net.NodeOwnerSnapshot=null;Check(!sortie.SequenceEqual(Render()),"Normal result drawing still reveals ownership change");
    }
}
