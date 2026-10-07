using System.Reflection;
using System.Text.Json;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using var game=new OperationCheck();game.Run();
sealed class OperationCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,updated=-1;CubeNet net;
    object Get(string n)=>typeof(Game1).GetField(n,Flags)!.GetValue(this)!;
    void Set(string n,object v)=>typeof(Game1).GetField(n,Flags)!.SetValue(this,v);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Input(Point p,ButtonState b,bool active=true,params Keys[] keys)=>typeof(Game1).GetMethod("HandleInput",Flags)!.Invoke(this,new object[]{new MouseState(p.X,p.Y,0,b,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(keys),new GameTime(),active});
    void Click(Point p){Input(p,ButtonState.Released);Input(p,ButtonState.Pressed);Input(p,ButtonState.Released);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        // Logging before player selection must be safe, including Title and Setup.
        Input(new(0,0),ButtonState.Released);
        var setup=(WorldSetup)Get("_setup");setup.SetWorld(0);setup.SetCast(123);setup.SetPlacement(456);
        Input(new(0,0),ButtonState.Released,false);Input(new(0,0),ButtonState.Released);
        setup.SelectPlayer(0);((WorldPreview)Get("_world")).ShowSetup(setup,true);net=(CubeNet)Get("_net");net.Home(setup);
        Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Flags)!.FieldType,"WorldStatus"));
    }
    protected override void Update(GameTime time)
    {
        if(updated==frame)return;updated=frame;
        switch(frame)
        {
            case 0:
                for(int i=0;i<8;i++)
                {
                    Click(new(1550,104));Check(net.ShowRoutes,"Repeated right-side click did not select traffic");
                    Click(new(1550,104));Check(!net.ShowRoutes,"Repeated right-side click did not restore grid");
                    Click(new(1400,104));Check(net.ShowRoutes,"Left-side click did not select traffic");
                    Click(new(1509,104));Check(!net.ShowRoutes,"Center click did not restore grid");
                }
                break;
            case 1:
                Click(new(600,1040));Check((bool)Get("_movementOpen") && net.ShowRoutes,"Movement did not open");
                Click(new(1400,104));Check(!(bool)Get("_movementOpen") && !net.ShowRoutes && net.MovementTargets.Count==0,"Grid was silently ignored during movement");break;
            case 2:
                for(int i=0;i<8;i++){Click(new(1710,104));Check((bool)Get("_statusGlobe"),"Globe switch ignored");Click(new(1810,104));Check(!(bool)Get("_statusGlobe"),"Net switch ignored");}
                Click(new(1710,104));Check((bool)Get("_statusGlobe"),"View toggle failed");
                Click(new(1759,104));Check(!(bool)Get("_statusGlobe"),"Repeated view-button click did not toggle");
                Click(new(1759,104));Check((bool)Get("_statusGlobe"),"Repeated view-button click did not restore globe");break;
            case 3:
                Input(new(1810,104),ButtonState.Released,false);Input(new(1810,104),ButtonState.Pressed,false);Input(new(1810,104),ButtonState.Released,false);
                Check((bool)Get("_statusGlobe"),"Inactive click changed view");Click(new(1810,104));Check(!(bool)Get("_statusGlobe"),"Active click after focus loss was ignored");break;
            case 4:
                var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=1280;g.PreferredBackBufferHeight=800;g.ApplyChanges();
                Click(new(1140,109));Check((bool)Get("_statusGlobe"),"Letterboxed click missed globe button");break;
            case 5:
            {
                RecordFailure(new InvalidOperationException("test_failure"),"test");
                using var logReader=new StreamReader(new FileStream(OperationLogPath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite));
                var lines=logReader.ReadToEnd().Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(s=>JsonDocument.Parse(s)).ToArray();
                try
                {
                    var events=lines.Select(d=>d.RootElement).ToArray();
                    Check(events.All(e=>DateTimeOffset.TryParse(e.GetProperty("time").GetString(),out _)),"Event timestamp missing");
                    Check(events.Select(e=>e.GetProperty("sequence").GetInt64()).SequenceEqual(Enumerable.Range(1,events.Length).Select(i=>(long)i)),"Log order wrong");
                    var ends=events.Where(e=>e.GetProperty("kind").GetString()=="input_end").Select(e=>e.GetProperty("data")).ToArray();
                    Check(ends.Any(e=>e.GetProperty("outcome").GetString()=="movement_cancelled_for_grid"),"Grid cancel cause not recorded");
                    Check(ends.Any(e=>e.GetProperty("outcome").GetString()=="layer_changed" && e.GetProperty("input").GetProperty("target").GetString()=="toggle_grid_traffic"),"Layer toggle result missing");
                    Check(ends.Any(e=>e.GetProperty("outcome").GetString()=="ignored_inactive_window"),"Inactive cause not recorded");
                    Check(ends.Any(e=>e.GetProperty("input").GetProperty("client").GetProperty("x").GetInt32()==1140 && e.GetProperty("input").GetProperty("canvas").GetProperty("x").GetInt32()==1710),"Raw/logical coordinates wrong");
                    Check(ends.All(e=>e.GetProperty("state").TryGetProperty("screen",out _) && e.GetProperty("state").TryGetProperty("worldSeed",out _)),"Screen or seed context missing");
                    Check(events.Any(e=>e.GetProperty("kind").GetString()=="screen_displayed") && events.Any(e=>e.GetProperty("kind").GetString()=="exception"),"Displayed screen or failure missing");
                    Console.WriteLine("PASS: logs include time, raw/canvas coordinates, before/after states, results, displayed screens and exceptions; repeated switches, movement grid cancellation, inactive and letterboxed input.");
                }
                finally{foreach(var line in lines)line.Dispose();}
                Exit();break;
            }
        }
    }
    protected override void Draw(GameTime time){base.Draw(time);frame++;}
}
