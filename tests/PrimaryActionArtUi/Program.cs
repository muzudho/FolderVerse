using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new PrimaryArtUi(args[0]);game.Run();
sealed class PrimaryArtUi(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 WorldSetup w=null!;SpriteBatch batch=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 Rectangle Rect(string n)=>(Rectangle)typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));
  w=new(){EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.SelectPlayer(0);w.HumanPlayers.Add(0);w.Relations.Leader=0;
  w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var n in w.Nodes.All)n.Owner=0;
  Set("_setup",w);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_stratagemManual",true);Set("_stratagemTrayBlend",0f);Set("_pointer",new Point(-1,-1));
  ((CubeNet)Get("_net")).Home(w);
  batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);
  Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");var font=Content.Load<SpriteFont>("UiFont");Set("_font",font);Set("_ui",new UiPainter(batch,pixel,font));
  var guide=Rect("GuideCallBounds");var end=(Rectangle)Call("PrimaryActionBounds",1);var medal=new Rectangle(26,766,300,300);
  Check(guide.Width==96&&end.Width==144&&guide.Right<end.Left,"guide standard width and end turn one-and-a-half width");
  Check(!guide.Intersects(medal),"guide does not overlap full portrait medal");
  var all=new List<Rectangle>{guide};for(int i=0;i<10;i++){var r=(Rectangle)Call("PrimaryActionBounds",i);Check(r.Right<=1920&&!r.Intersects(medal)&&all.All(a=>!a.Intersects(r)),"primary button separated and inside canvas "+i);all.Add(r);}
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  w.SetCountryAppearance(0,frame%WorldSetup.Colors.Length,(frame+3)%WorldSetup.Colors.Length,1);w.SetCountryEmblem(0,1);
  using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(new Color(16,35,46));
  batch.Begin();Call("DrawPortraitMedallion",new Vector2(176,916),w.OwnerColor(0),0);Call("DrawPrimaryActionButtonsCore");batch.End();GraphicsDevice.SetRenderTarget(null);
  using(var file=File.Create(Path.Combine(output,"primary-actions-country-"+frame+".png")))image.SaveAsPng(file,1920,1080);
  var atlas=(Texture2D)Get("_primaryActionArt");Check(atlas.Width%4==0&&atlas.Height%3==0,"atlas grid is exact");
  var pixels=new Color[atlas.Width*atlas.Height];atlas.GetData(pixels);Check(pixels.Any(p=>p.A==0)&&pixels.Any(p=>p.A==255),"atlas has real transparency");
  Check(frame>0||Enumerable.Range(0,10).Select(i=>(int)typeof(Game1).GetMethod("PrimaryActionArtIndex",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{new[]{-1,1,2,3,7,4,5,6,8,9}[i]})!).Distinct().Count()==10,"all ten illustrations have distinct atlas cells");
  // Draw each symbol alone and inspect the exact country-suit rectangles before overlaying flags.
  for(int i=0;i<10;i++){
   using var symbol=new RenderTarget2D(GraphicsDevice,96,96);GraphicsDevice.SetRenderTarget(symbol);GraphicsDevice.Clear(Color.Transparent);batch.Begin();Call("DrawPrimaryActionArt",new[]{-1,1,2,3,7,4,5,6,8,9}[i],new Rectangle(16,8,64,64),true);batch.End();
   var sample=new Color[96*96];symbol.GetData(sample);int occupied=0;
   foreach(var corner in new[]{new Rectangle(75,5,16,16),new Rectangle(5,75,16,16)})for(int y=corner.Top;y<corner.Bottom;y++)for(int x=corner.Left;x<corner.Right;x++)if(sample[y*96+x].A>16)occupied++;
   Check(occupied==0,"country flag corners remain clear for icon "+i);GraphicsDevice.SetRenderTarget(null);
  }
  Console.WriteLine("PASS country color "+frame+" rendered");if(++frame==4){Check((bool)Call("MapOverlayClick",Rect("GuideCallBounds").Center,false)&&Get("_guideBriefing")!=null,"moved guide button hitbox recalls guide");Exit();}
 }
}
