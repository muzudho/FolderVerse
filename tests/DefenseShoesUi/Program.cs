using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new ShoesGame();game.Run();
sealed class ShoesGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup world=null!;int node,frame;long id;Robot bare=null!;Robot[] workers=Array.Empty<Robot>();
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
 void CheckCompactGlyph(){
  var batch=(SpriteBatch)Get("_spriteBatch");var viewport=GraphicsDevice.Viewport;
  var method=typeof(Game1).GetMethod("DrawRobotGlyph",F,null,new[]{typeof(Rectangle),typeof(Robot),typeof(bool),typeof(float),typeof(Func<RobotParts,float>)},null)!;
  var worker=world.Robots.Nodes[node].Robots.Single(r=>r.Id==workers[1].Id);
  using var image=new RenderTarget2D(GraphicsDevice,100,60);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Transparent);batch.Begin();
  method.Invoke(this,new object[]{new Rectangle(10,10,26,31),worker,true,1f,null});
  method.Invoke(this,new object[]{new Rectangle(60,10,26,31),worker with {Parts=RobotParts.Head},true,1f,null});batch.End();
  GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Viewport=viewport;
  var pixels=new Color[6000];image.GetData(pixels);
  Check(Enumerable.Range(0,6000).Any(i=>i%100<40 && i/100>=35 && pixels[i].A>0),"Compact preview retains visible shoes");
  Check(!Enumerable.Range(0,6000).Any(i=>i%100>=60 && i%100<90 && i/100>=34 && pixels[i].A>0),"Head-only transport preview gets a flag without phantom legs or shoes");
  using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"work-compact.png"));image.SaveAsPng(output,100,60);
 }
 protected override void Draw(GameTime time){
  if(frame==1){Click(StaticRect("HangarCell",0).Center);Check(Current.ShoeOwner==1 && Current.LegsOrigin==0,"Reservation puts on shoes without recoloring leg parts");Set("_animationTime",.12f);Check((float)Call("DefenseShoeProgress",Current)==0,"Flag waves before shoes appear");}
  if(frame==2){Set("_animationTime",.5f);float progress=(float)Call("DefenseShoeProgress",Current);Check(progress>0 && progress<1,"Shoes animate into place");}
  if(frame==3){Set("_animationTime",1.3f);CheckShoePixels();}
  if(frame==4){world.SelectControlledRuler(0);Click(StaticRect("HangarCell",0).Center);Check(Current.ShoeOwner==1,"Another country cannot remove work shoes");}
  if(frame==5){world.SelectControlledRuler(1);Click(StaticRect("HangarCell",0).Center);Check(Current.ShoeOwner<0 && !Current.DefenseOnly,"Own-country cancellation removes shoes");}
  if(frame==6){Click(StaticRect("HangarCell",1).Center);Click(StaticRect("HangarCell",2).Center);Check(((System.Collections.IDictionary)Get("_defenseReservationEffects")).Count==0,"Failed reservations trigger no animation");}
  if(frame==7){
   world.SelectControlledRuler(0);world.Robots.Training[0].DeploymentPolicy=1;Set("_animationTime",10f);
   workers=Enumerable.Range(0,3).Select(_=>world.Robots.Create(0,RobotParts.Complete)).ToArray();foreach(var r in workers)world.Robots.Nodes[node].Add(r);Call("SyncHangarSlots");
   Click(StaticRect("HangarReservationButton",3).Center);Click(StaticRect("HangarCell",3).Center);
   Click(StaticRect("HangarReservationButton",2).Center);Click(StaticRect("HangarCell",4).Center);
   Click(StaticRect("HangarReservationButton",0).Center);Click(StaticRect("HangarCell",5).Center);
   var robots=world.Robots.Nodes[node].Robots;
   Check(robots.Single(r=>r.Id==workers[0].Id).InterceptOnly && robots.Single(r=>r.Id==workers[1].Id).FactoryWorker && robots.Single(r=>r.Id==workers[2].Id).Disposal,"Three different jobs are accepted at the same animation time");
   Check(((System.Collections.IDictionary)Get("_workReservationEffects")).Count==2,"Independent work flags coexist; disposal waiting is not a shoe job");
   Click(StaticRect("HangarReservationButton",3).Center);Click(StaticRect("HangarCell",3).Center);
   Check(!world.Robots.Nodes[node].Robots.Single(r=>r.Id==workers[0].Id).InterceptOnly && ((System.Collections.IDictionary)Get("_workReservationEffects")).Count==1,"Immediate cancellation works while flag animation is running");
  }
  if(frame==8){Set("_animationTime",10.12f);Check((float)Call("WorkShoeProgress",world.Robots.Nodes[node].Robots.Single(r=>r.Id==workers[1].Id))==0,"Worker flag precedes shoes");}
  if(frame==9){Set("_animationTime",10.5f);float p=(float)Call("WorkShoeProgress",world.Robots.Nodes[node].Robots.Single(r=>r.Id==workers[1].Id));Check(p>0 && p<1,"Worker shoes animate without stopping input");CheckCompactGlyph();}
  if(frame==10){
   int target=world.Routes.Neighbors(world.Nodes.All[node]).Select(r=>world.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=node);
   Call("ResetInspector");Call("OpenTransportOverview");Set("_populationNode",node);Call("UpdateDomesticSlide",1f);
   var rule=world.Robots.Transport.AddInstruction(node,0,target);Call("ConfigureTransport",rule,null,null,null,2);
   Check(((System.Collections.IDictionary)Get("_transportPlanEffects")).Count==1,"Long-term plan animates its instruction marker");
   Check(world.Robots.Nodes[node].Robots.Single(r=>r.Id==workers[1].Id).FactoryWorker,"Plan animation does not assign existing workers to transport");
  }
  base.Draw(time);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"defense-shoes-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(++frame==11){Console.WriteLine("PASS: defense, worker/intercept/disposal, independent simultaneous flags, immediate cancellation, plan marker and footwear rendering.");Exit();}
 }
}
