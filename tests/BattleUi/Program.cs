using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var world=new WorldSetup();world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
void Clear()
{
    world.Relations.Reset();world.SelectPlayer(0);
    foreach(var n in world.Nodes.All){Array.Clear(n.Population.People);Array.Clear(n.Population.Conversion);Array.Clear(n.Population.Migration);n.Population.BirthPercent=0;}
}
void Place(int ruler,Node n){world.ConquerorLocations[ruler]=n.Cell;world.ConquerorPoints[ruler]=n.Center;n.Owner=ruler;}
var a=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Length>0);
var route=world.Routes.Neighbors(a)[0];var b=world.Nodes.At(route.Target,route.Entry);
Clear();Place(0,a);Place(1,b);a.Population.People[0]=140;b.Population.People[0]=90;
world.Campaign.Resolve(world,new[]{new March(0,b.Id,100),new March(1,a.Id,60)});
Check(world.Campaign.Battles.Count==2 && world.Campaign.Battles[0].Kind==BattleKind.Edge && world.Campaign.Battles[1].Kind==BattleKind.Node,"Edge must precede node battle");
Check(world.Campaign.Battles[0].FirstAfter==40 && world.Campaign.Battles[0].SecondAfter==0,"Edge cancellation wrong");
Check(b.Owner==0 && b.Population.People[0]==10 && world.Nodes.Current(0).Id==b.Id && world.Nodes.Current(1).Id==b.Id,"Edge winner/loser movement wrong");
Check(BattleSchedule.Create(world.Campaign.Battles).Length==2,"Edge and node battle not consecutive waves");
Clear();Place(0,a);Place(1,b);a.Population.People[0]=60;b.Population.People[0]=60;
world.Campaign.Resolve(world,new[]{new March(0,b.Id,60),new March(1,a.Id,60)});
Check(world.Campaign.Battles.Count==1 && world.Nodes.Current(0).Id==a.Id && world.Nodes.Current(1).Id==b.Id,"Edge tie incorrectly advanced");
Clear();Place(0,a);Place(1,b);a.Population.People[0]=100;
world.Campaign.Resolve(world,new[]{new March(0,b.Id,100)});
Check(world.Campaign.Battles.Count==0 && b.Owner==0,"Unopposed capture should skip battle");
Clear();Place(0,a);b.Owner=0;a.Population.People[0]=100;b.Population.People[0]=20;
world.Campaign.Resolve(world,new[]{new March(0,b.Id,100)});
Check(world.Campaign.Battles.Count==0 && b.Population.People[0]==120,"Friendly forces incorrectly fought");
var ten=Enumerable.Range(0,10).Select(i=>new BattleRecord(BattleKind.Node,i*2,i*2+1,a.Id,b.Id,(i+1)*100,50,(i+1)*100-50,0)).ToArray();
Check(BattleSchedule.Create(ten).Length==1,"Ten distinct pairs not simultaneous");
foreach(var strengths in new[]{Enumerable.Repeat(100L,10).ToArray(),new[]{long.MaxValue,1L,1L,1L,1L,1L,1L,1L,1L,1L},new[]{1L},new[]{100L,1000L,10000L}})
{
    var tiles=CharacterSelectionLayout.CreateBattles(strengths);
    Check(tiles.All(t=>t.X>=0 && t.Y>=0 && t.X+t.Side<=28 && t.Y+t.Side<=20),"Battle tile outside calendar");
    Check(tiles.SelectMany((t,i)=>tiles.Skip(i+1).Select(other=>t.Bounds.Intersects(other.Bounds))).All(overlap=>!overlap),"Battle tiles overlap");
    for(int i=0;i<strengths.Length;i++)for(int j=0;j<strengths.Length;j++)if(strengths[i]>strengths[j])Check(tiles[i].Side>=tiles[j].Side,"Larger battle got smaller area");
}
Console.WriteLine("PASS: edge victory continues to node battle, ties stop, unopposed capture skips scene, ten simultaneous battles and weighted calendar packing.");
using var game=new BattleCheck(ten);game.Run();
sealed class BattleCheck(BattleRecord[] fixtures):Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame;
    object Get(string n)=>typeof(Game1).GetField(n,Flags)!.GetValue(this)!;
    void Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,Flags)!.Invoke(this,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        var w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
        ((WorldPreview)Get("_world")).ShowSetup(w,true);
        w.Population.Advance(w);
        typeof(Game1).GetField("_screen",Flags)!.SetValue(this,Enum.Parse(typeof(Game1).GetField("_screen",Flags)!.FieldType,"WorldStatus"));
        Call("OpenBattle");Check(Get("_screen").ToString()=="WorldStatus","Empty turn opened battle screen");
        w.Campaign.Battles.AddRange(fixtures);Call("OpenBattle");
        int turn=w.Population.Turn;
        Call("HandleInput",new MouseState(700,1044,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);
        Check(w.Population.Turn==turn && Get("_screen").ToString()=="Battle","Battle allowed a new movement turn");
    }
    protected override void Update(GameTime time)
    {
        if(frame<3)Call("UpdateBattle",.8,false);
        else if(frame==3)
        {
            Call("UpdateBattle",3d,true);Check(Get("_screen").ToString()=="WorldStatus","Battle did not return to game");
            var w=(WorldSetup)Get("_setup");w.Campaign.Battles.Clear();w.Campaign.Battles.Add(fixtures[0] with{Kind=BattleKind.Edge});w.Campaign.Battles.Add(fixtures[0]);Call("OpenBattle");
            Call("UpdateBattle",0d,true);Call("UpdateBattle",0d,true);Check((int)Get("_battleWave")==1,"Consecutive battle did not start");
        }
        else if(frame==4){Console.WriteLine("PASS: battle UI draws ten portraits/soldiers/cells, skips empty scenes, advances waves and returns to game.");Exit();}
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);
        if(frame<4)
        {
            var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
            using var texture=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);texture.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"battle-{frame}.png"));texture.SaveAsPng(output,texture.Width,texture.Height);
        }
        frame++;
    }
}
