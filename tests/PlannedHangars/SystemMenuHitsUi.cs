using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

sealed class SystemMenuHitsUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame;WorldSetup w=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    Rectangle Bounds(string n)=>(Rectangle)typeof(Game1).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Click(Point p){var c=(Rectangle)Call("CanvasBounds");foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(c.X+p.X*c.Width/1920,c.Y+p.Y*c.Height/1080,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);}
    void Ready(){
        Set("_autoPlay",false);Set("_countrySettingsOpen",false);Set("_operationSettingsOpen",false);Set("_operationSettingsSlide",0f);Set("_statusMenuOpen",false);Set("_intakeLine",null!);
        Call("CloseDirectDelivery");Call("CloseFunctionTransferMap");Call("ResetInspector");Set("_primaryActionSlide",0f);Set("_orientationAge",-1f);Set("_orientationDragging",false);
        w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);w.HumanPlayers.Add(0);Set("_conquestOverview",false);
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
    }
    protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);}
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time){
        if(frame<2){
            if(frame==1){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
            foreach(string item in new[]{"InactiveMenuItem","HierarchyMenuItem","AutoMenuItem","CountrySettingsMenuItem","EndingMenuItem","QuitMenuItem"})foreach(int horizontal in new[]{0,1,2}){
                Ready();Click(Bounds("StatusMenuButton").Center);Check((bool)Get("_statusMenuOpen"),"Menu button opens normally");var r=Bounds(item);Click(new(horizontal==0?r.Left+8:horizontal==1?r.Center.X:r.Right-8,r.Center.Y));
                bool acted=item switch {"InactiveMenuItem"=>Get("_screen").ToString()=="InactiveList","HierarchyMenuItem"=>Get("_screen").ToString()=="Hierarchy","AutoMenuItem"=>(bool)Get("_autoPlay"),"CountrySettingsMenuItem"=>(bool)Get("_countrySettingsOpen"),"EndingMenuItem"=>Get("_screen").ToString()=="Ending",_=>Get("_screen").ToString()=="Title"};
                Check(acted,$"Menu item did not receive its visible click: {item}, horizontal={horizontal}, size={frame}");
            }
            Ready();Call(frame==0?"OpenDirectMap":"OpenFunctionTransferMap");Call("UpdateMapFooter",1f);Click(Bounds("StatusMenuButton").Center);
            Check(!(bool)Get("_statusMenuOpen"),"Retired menu has no hidden hit box in direct/transfer mode");
        }
        if(frame==2){Call("CloseDirectDelivery");Call("CloseFunctionTransferMap");Call("UpdateMapFooter",1f);Click(Bounds("StatusMenuButton").Center);Check((bool)Get("_statusMenuOpen"),"Menu returns with working hit box after leaving mode");}
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame*10),TimeSpan.Zero));
        var pp=GraphicsDevice.PresentationParameters;var colors=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(colors);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(colors);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"system-menu-hits-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
        if(frame++==2){Console.WriteLine("PASS: all six menu actions at left/center/right in 1080p/540p, retired menu in direct/function modes and restored hit box after mode exit.");Exit();}
    }
}
