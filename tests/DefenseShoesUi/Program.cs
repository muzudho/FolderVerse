using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new ShoesGame();game.Run();
sealed class ShoesGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup world=null!;int node,frame;long id;Robot bare=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 Rectangle StaticRect(string n,int i)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i})!;
 void Click(Point p){Set("_pointer",p);Call("NodeAssemblyLocalInput",new MouseState(p.X,p.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),true,false);}
 Robot Current=>world.Robots.Nodes[node].Robots.Single(r=>r.Id==id);
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);world.Relations.SetSuperior(1,0);world.SelectControlledRuler(1);
  node=world.Nodes.All.First(n=>n.Terrain!=TravelTerrain.Sea).Id;world.Nodes.All[node].Owner=0;world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);world.Robots.ConfigureFactories(world);world.Robots.Training[1].DeploymentPolicy=1;
  bare=world.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,BodyOrigin=1,LegsOrigin=0};id=bare.Id;world.Robots.Nodes[node].Add(bare);world.Robots.Nodes[node].Add(world.Robots.Create(1,RobotParts.Legs));world.Robots.Nodes[node].Add(world.Robots.Create(0,RobotParts.Complete));
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_animationTime",0f);
  ((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);Call("OpenNodeInspector",node);Call("OpenNodeAssembly");Call("UpdateDomesticSlide",1f);Click(StaticRect("HangarReservationButton",1).Center);
 }
 protected override void Update(GameTime time){}
 void CheckShoePixels(){
  var batch=(SpriteBatch)Get("_spriteBatch");var view=GraphicsDevice.Viewport;
  var method=typeof(Game1).GetMethod("DrawRobotGlyph",F,null,new[]{typeof(Rectangle),typeof(Robot),typeof(bool),typeof(float),typeof(Func<RobotParts,float>)},null)!;
  Color[] Render(Robot robot){using var target=new RenderTarget2D(GraphicsDevice,100,130);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Transparent);batch.Begin();method.Invoke(this,new object[]{new Rectangle(10,10,68,102),robot,true,1f,null});batch.End();GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Viewport=view;var pixels=new Color[13000];target.GetData(pixels);return pixels;}
  var before=Render(bare);var after=Render(Current);Check(before.Take(9000).SequenceEqual(after.Take(9000)),"Only the footwear changes; head and body pixels stay intact");
  Check(!before.Skip(9000).SequenceEqual(after.Skip(9000)),"Work shoes are visible below the legs");Check(after[102*100+24]==world.OwnerColor(1),"Work shoes have the reserving country's color");
  Check(Render(bare with {Parts=RobotParts.Legs}).SequenceEqual(Render(bare with {Parts=RobotParts.Legs,DefenseOnly=true})),"Incomplete robot remains barefoot even with legacy flag");
 }
 protected override void Draw(GameTime time){
  if(frame==1){Click(StaticRect("HangarCell",0).Center);Check(Current.ShoeOwner==1 && Current.LegsOrigin==0,"Reservation puts on shoes without recoloring leg parts");Set("_animationTime",.12f);Check((float)Call("DefenseShoeProgress",Current)==0,"Flag waves before shoes appear");}
  if(frame==2){Set("_animationTime",.5f);float progress=(float)Call("DefenseShoeProgress",Current);Check(progress>0 && progress<1,"Shoes animate into place");}
  if(frame==3){Set("_animationTime",1.3f);CheckShoePixels();}
  if(frame==4){world.SelectControlledRuler(0);Click(StaticRect("HangarCell",0).Center);Check(Current.ShoeOwner==1,"Another country cannot remove work shoes");}
  if(frame==5){world.SelectControlledRuler(1);Click(StaticRect("HangarCell",0).Center);Check(Current.ShoeOwner<0 && !Current.DefenseOnly,"Own-country cancellation removes shoes");}
  if(frame==6){Click(StaticRect("HangarCell",1).Center);Click(StaticRect("HangarCell",2).Center);Check(((System.Collections.IDictionary)Get("_defenseReservationEffects")).Count==0,"Failed reservations trigger no animation");}
  base.Draw(time);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"defense-shoes-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(++frame==7){Console.WriteLine("PASS: flag-before-shoes sequence, animated footwear, country colors, bare partials, own-country cancellation and failed-reservation silence.");Exit();}
 }
}
