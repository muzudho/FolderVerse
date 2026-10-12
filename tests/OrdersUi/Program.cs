using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new CheckGame();game.Run();
class CheckGame:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        var world=(WorldSetup)Get("_setup");world.SetWorld(2);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
        var home=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Select(r=>world.Nodes.At(r.Target,r.Entry).Id).Distinct().Count()>=2);
        var neighbors=world.Routes.Neighbors(home).Select(r=>world.Nodes.At(r.Target,r.Entry)).DistinctBy(n=>n.Id).ToArray();
        home.Owner=0;neighbors[0].Owner=0;neighbors[1].Owner=1;
        world.ConquerorLocations[0]=home.Cell;world.ConquerorPoints[0]=home.Center;
        Set("_screen",Enum.Parse(typeof(Game1).GetNestedType("Screen",BindingFlags.NonPublic)!,"WorldStatus"));
        Call("OpenMovement");var net=(CubeNet)Get("_net");
        if(net.MovementTargets.Contains(neighbors[1].Id) || !net.MovementTargets.Contains(neighbors[0].Id))throw new Exception("Movement target ownership filter failed");
        int turn=world.Population.Turn;Call("SelectMoveNode",neighbors[0].Id);
        Call("MovementClick",new Point(600,930),new KeyboardState());
        if(world.Population.Turn!=turn || world.Nodes.Current(0).Id!=home.Id || world.Campaign.PlannedOrders.Count!=1)throw new Exception("Movement committed before end turn");
        net.Drag(new Vector2(123,-77));net.ZoomAt(new Rectangle(0,0,960,900),new Point(350,450),240);var pan=net.Pan;var zoom=net.Zoom;Set("_statusGlobe",false);Call("OpenWarPlanning");if(net.Pan!=pan || net.Zoom!=zoom)throw new Exception("War mode changed net view");
        if(!net.MovementTargets.Contains(neighbors[1].Id) || net.MovementTargets.Contains(neighbors[0].Id))throw new Exception("Invasion target filter failed");
        Call("SelectMoveNode",neighbors[1].Id);
    }
    protected override void Update(GameTime t){}
    protected override void Draw(GameTime t){base.Draw(t);Console.WriteLine("PASS: move/war target filters, inert movement reservation and invasion panel drawing");Exit();}
}
