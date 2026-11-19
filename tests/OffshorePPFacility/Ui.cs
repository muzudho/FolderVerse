using FolderVerse;using System.Reflection;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;
using var game=new Preview();game.Run();
delegate void Fill(UiPainter ui,ReadOnlySpan<Vector2> face,Color color);
sealed class Preview:Game1{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;SpriteBatch batch=null!;UiPainter ui=null!;int frame;CubeNet net=new();Color[] first=null!;
 protected override void LoadContent(){
  AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");var font=Fonts.LoadUi(Content);batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});ui=new(batch,pixel,font);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_ART_CONTENT_BASE"));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  using var rt=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(rt);GraphicsDevice.Clear(new Color(19,38,50));batch.Begin();ui.Text("Offshore PP recycling / 3D and map illustration",new(60,35),.7f,Color.White);
  var fill=typeof(CubeNet).GetMethod("FillProjectedFace",F)!.CreateDelegate<Fill>();var kind=Enum.Parse(typeof(CubeNet).GetNestedType("ResourceSiteKind",BindingFlags.NonPublic)!,"OffshoreGas");int faces=0;
  for(int view=0;view<3;view++){
   float angle=view*MathHelper.TwoPi/3;Vector2 Project(Vector3 p){float x=p.X*MathF.Cos(angle)-p.Y*MathF.Sin(angle),y=p.X*MathF.Sin(angle)+p.Y*MathF.Cos(angle);return new Vector2(355+view*610,450)+new Vector2(x*.87f-y*.55f,x*.35f+y*.4f-p.Z)*220;}
   Action<Color,Vector3[]> emit=(c,v)=>{if(v.Any(p=>!float.IsFinite(p.X+p.Y+p.Z)))throw new Exception("nonfinite vertex");faces++;fill(ui,v.Select(Project).ToArray(),frame==1?Color.Lerp(c,new Color(78,76,74),.65f):c);};
   typeof(CubeNet).GetMethod("DrawResourceSite",F)!.Invoke(null,new object[]{kind,true,emit,(Func<Vector3,Vector2>)Project});
  }
  foreach(int width in new[]{360,120,60})typeof(CubeNet).GetMethod("DrawRecycledPPMapArt",F)!.Invoke(net,new object[]{ui,new Vector2(width==360?420:width==120?1020:1500,830), (float)width,frame==1});
  if(frame==2){ui.Box(new(0,100,1920,980),new Color(19,38,50));var w=AutoSaveStore.Read<WorldSetup>(Environment.GetEnvironmentVariable("PP_FIXTURE")!);w.SelectPlayer(0);net.SetCenter(w,w.Cells[0].Face);net.Draw(ui,w,new(60,120,1800,900),-1,0);}
  batch.End();GraphicsDevice.SetRenderTarget(null);var pixels=new Color[1920*1080];rt.GetData(pixels);if(frame==0)first=pixels;else if(!pixels.Where((p,i)=>p!=first[i]).Any())throw new Exception("depletion state unchanged");
  using(var file=File.Create(Path.Combine(Environment.GetEnvironmentVariable("PP_OUTPUT")!,"facility-"+frame+".png")))rt.SaveAsPng(file,1920,1080);Console.WriteLine($"PASS projected finite faces {faces}, 3 views, map sizes 360/120/60, depleted={frame==1}, fullMap={frame==2}");if(frame++==2)Exit();
 }
}
