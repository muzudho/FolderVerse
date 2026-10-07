using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static ConquerorLook Personality(int personality){var k=ConquerorCatalog.Keywords.First(k=>k.Personality==personality);return new(k.BaseId,k.VariantId);}
WorldSetup Create()
{
    var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    w.Campaign.UseRobotCombat=false;
    foreach(var n in w.Nodes.All){Array.Clear(n.Population.People);Array.Clear(n.Population.Conversion);Array.Clear(n.Population.Migration);n.Population.BirthPercent=0;}
    for(int i=2;i<w.ActiveCount;i++)w.Relations.Released[i]=true;
    return w;
}
void Place(WorldSetup w,int ruler,Node n){w.ConquerorLocations[ruler]=n.Cell;w.ConquerorPoints[ruler]=n.Center;n.Owner=ruler;}
var world=Create();var a=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Length>0);var path=world.Routes.Neighbors(a)[0];var b=world.Nodes.At(path.Target,path.Entry);
Place(world,0,a);Place(world,1,b);world.Looks[1]=Personality(13);a.Population.People[0]=100;b.Population.People[0]=20;
world.Campaign.Resolve(world,new[]{new March(0,b.Id,100)});
Check(world.Relations.Pending.Any(c=>c.Ruler==1),"Present defeated conqueror not captured");
var captive=world.Relations.Pending.Single(c=>c.Ruler==1);Check(world.Relations.Choose(world,captive,DefeatedChoice.Recruit),"Recruit failed");
Check(world.Relations.Party(world).SequenceEqual(new[]{0,1}),"Recruit did not become controllable ally");
int turn=world.Population.Turn;world.Campaign.Advance(world,a.Id,30);
Check(world.PlayerSlot==1 && world.Population.Turn==turn && world.Nodes.Current(0).Id==b.Id,"Ally planning advanced world prematurely");
world.Campaign.Advance(world,a.Id,20);
Check(world.PlayerSlot==0 && world.Population.Turn==turn+1 && world.Nodes.Current(0).Id==a.Id && world.Nodes.Current(1).Id==a.Id && world.Campaign.Battles.Count==0,"Party round or friendly combat wrong");
var migration=world.Nodes.All.SelectMany(n=>Enumerable.Range(0,4).Select(d=>(n,d))).First(pair=>pair.n.Population.Land && world.Routes.Find(pair.n.Cell,pair.n.Center,pair.d,true) is {} route && world.Nodes.At(route.Target,route.Entry).Population.Land);
var migrationRoute=world.Routes.Find(migration.n.Cell,migration.n.Center,migration.d,true);var destination=world.Nodes.At(migrationRoute.Target,migrationRoute.Entry);
migration.n.Owner=0;destination.Owner=1;Check(world.Population.CanMigrateNode(world,migration.n.Id,migration.d),"Allied migration unavailable");
migration.n.Population.People[2]=100;destination.Population.People[2]=0;world.Population.AdjustNodeMigration(world,migration.n.Id,2,migration.d,10);
world.Population.Advance(world);Check(destination.Population.People[2]==10,"Allied migration did not transfer residents");
world.Relations.Pending.Add(new(1,0,a.Id));captive=world.Relations.Pending[0];world.Relations.Choose(world,captive,DefeatedChoice.RemoveBattery);
Check(!world.Relations.Powered[1] && world.Nodes.All.All(n=>n.Owner!=1) && world.TerritoryCounts[1]==0,"Removal did not neutralize all owned nodes");
Check(world.Nodes.Shares(destination.Cell).Contains("未征服") && world.OwnerColor(-1)!=Color.Transparent,"Neutral display invalid");
Place(world,0,migration.n);int residents=(int)destination.Population.Total;
world.Campaign.Resolve(world,new[]{new March(0,destination.Id,0)});
Check(destination.Owner==0 && destination.Population.Total>=residents,"Neutral node not acquired by entering with zero fighters");
world.Looks[1]=Personality(9);captive=world.Relations.InsertBattery(world,1);
Check(!world.Relations.Choose(world,captive,DefeatedChoice.Recruit) && world.Relations.Pending.Contains(captive),"Refusal did not retain remaining choices");
world.Relations.Choose(world,captive,DefeatedChoice.Release);var stayed=world.Nodes.Current(1).Id;
world.Campaign.Advance(world,-1,0);if(world.PlayerSlot==1)world.Campaign.Advance(world,-1,0);
Check(world.Nodes.Current(1).Id==stayed && world.Relations.Released[1],"Released conqueror did not stay put");
Console.WriteLine("PASS: present conqueror capture, recruitment, allied turn planning/combat/migration, neutral territory acquisition, battery reinsertion, refusal and stationary release.");
var tree=Create();tree.Relations.Released[2]=false;tree.Relations.Released[4]=false;tree.Relations.Released[5]=false;
tree.Relations.SetSuperior(1,0);tree.Relations.SetSuperior(2,1);tree.Relations.SetSuperior(5,4);
Check(!tree.Relations.SetSuperior(0,2) && tree.Relations.Superiors[0]==-1,"Hierarchy allowed cycle");
tree.Looks[4]=Personality(13);var subtree=new Captive(4,2,tree.Nodes.Current(4).Id);tree.Relations.Pending.Add(subtree);tree.Relations.Choose(tree,subtree,DefeatedChoice.Recruit);
Check(tree.Relations.Superiors[4]==2 && tree.Relations.Superiors[5]==4 && tree.Relations.Allied(0,5),"Recruit did not retain subordinate subtree");
Check(tree.Relations.Party(tree).SequenceEqual(new[]{0,1,2,4,5}),"Hierarchy turn order is not preorder");
tree.Relations.Pending.Add(new(2,0,tree.Nodes.Current(2).Id));tree.Relations.Choose(tree,tree.Relations.Pending[0],DefeatedChoice.RemoveBattery);
Check(tree.Relations.Superiors[4]==1 && tree.Relations.Superiors[5]==4 && tree.Relations.Allied(0,5),"Battery removal did not promote immediate subordinates");
Check(tree.Relations.Forest(tree).Select(e=>e.Ruler).Distinct().Count()==tree.ActiveCount,"Forest omitted or duplicated conquerors");
Console.WriteLine("PASS: nested hierarchy, retained subtree recruitment, preorder control, cycle prevention and subordinate promotion.");
using var game=new RelationsCheck();game.Run();
sealed class RelationsCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,updated=-1;WorldSetup w;
    object Get(string n)=>typeof(Game1).GetField(n,Flags)!.GetValue(this)!;
    void Set(string n,object v)=>typeof(Game1).GetField(n,Flags)!.SetValue(this,v);
    void Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,Flags)!.Invoke(this,args);
    void Click(int x,int y){var mouse=new MouseState(x,y,0,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);Call("HandleInput",mouse,new KeyboardState(),new GameTime(),true);Call("HandleInput",new MouseState(x,y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);Call("HandleInput",mouse,new KeyboardState(),new GameTime(),true);}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
        w.Campaign.UseRobotCombat=false;
        var k=ConquerorCatalog.Keywords.First(k=>k.Personality==13);w.Looks[1]=new(k.BaseId,k.VariantId);
        ((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
        Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Flags)!.FieldType,"WorldStatus"));w.Relations.Pending.Add(new(1,0,w.Nodes.Current(1).Id));Call("OpenDisposition");
    }
    protected override void Update(GameTime time)
    {
        if(updated==frame)return;updated=frame;
        switch(frame)
        {
            case 1:Click(1350,895);Check(!w.Relations.Powered[1] && Get("_screen").ToString()=="WorldStatus","Battery removal UI failed");Click(1740,40);Click(1550,95);Check(Get("_screen").ToString()=="InactiveList","Menu did not open inactive list");break;
            case 2:Click(220,270);Check(w.Relations.Powered[1] && Get("_screen").ToString()=="Disposition","Reinsertion UI failed");break;
            case 3:Click(550,895);Check(w.Relations.Allied(0,1) && Get("_screen").ToString()=="InactiveList","Recruit UI failed");Click(180,1040);Check(Get("_screen").ToString()=="WorldStatus","List back failed");Click(700,1040);Check(w.PlayerSlot==1 && w.Population.Turn==0,"Ally-turn button did not switch control");break;
            case 4:
                var k=ConquerorCatalog.Keywords.First(k=>k.Personality==9);w.Looks[2]=new(k.BaseId,k.VariantId);
                var captive=new Captive(2,0,w.Nodes.Current(2).Id);w.Relations.Pending.Add(captive);w.Relations.Choose(w,captive,DefeatedChoice.RemoveBattery);
                w.Relations.InsertBattery(w,2);Call("OpenDisposition");break;
            case 5:Click(550,895);Check(Get("_screen").ToString()=="Disposition" && w.Relations.Pending.Any(p=>p.Ruler==2) && w.Relations.Message.Contains("拒"),"Refusal UI discarded remaining choices");break;
            case 6:Click(950,895);Check(w.Relations.Released[2] && Get("_screen").ToString()=="WorldStatus","Release after refusal failed");foreach(var node in w.Nodes.All)node.Owner=-1;w.Nodes.Recount();((WorldPreview)Get("_world")).ShowSetup(w,true);break;
            case 7:
                w.Relations.SetSuperior(2,1);w.Relations.SetSuperior(3,2);w.Relations.SetSuperior(4,1);w.Relations.SetSuperior(5,4);
                Click(1740,40);Click(1550,150);Check(Get("_screen").ToString()=="Hierarchy","Hierarchy menu did not open tree");break;
            case 8:Click(180,1040);Check(Get("_screen").ToString()=="WorldStatus","Hierarchy back failed");Click(1740,40);Click(1550,150);break;
            case 9:
                Call("HandleInput",new MouseState(),new KeyboardState(Keys.Escape),new GameTime(),true);Check(Get("_screen").ToString()=="WorldStatus","Hierarchy Escape failed");
                Console.WriteLine("PASS: disposition/list/turn control and hierarchy drawing/menu/back/Escape.");Exit();break;
        }
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);
        if(frame<9)
        {
            var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
            using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"relations-{frame}.png"));texture.SaveAsPng(output,texture.Width,texture.Height);
        }
        frame++;
    }
}
