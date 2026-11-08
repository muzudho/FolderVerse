using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
if(!args.Contains("--patterns"))throw new ArgumentException("Use --patterns for the current generator. The earlier star/moon/sun review is an archived image of the previous design.");
using var game=new AuditGame(args[0]);game.Run();
sealed class AuditGame(string output):Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 int page=2;Rectangle? clip;WorldSetup world=null!;UiPainter ui=null!;SpriteBatch batch=null!;
 readonly (string Name,int Primary,int Secondary,int Emblem,string Note)[] cases={
  ("01  VIET NAM",0,2,3,"Red field + yellow star\nVery close composition"),
  ("02  SOMALIA",1,19,3,"Blue field + white star\nVery close composition"),
  ("03  MOROCCO",0,14,3,"Red field + green star\nFilled vs outline differs"),
  ("04  TURKEY",0,19,2,"Red field + white crescent\nThe real flag also has a star"),
  ("05  JAPAN",19,0,1,"White field + red sun\nRays differ; inspect at 32 px"),
  ("06  BANGLADESH",14,0,1,"Green field + red sun\nReal flag uses a plain disc"),
  ("07  PALAU",1,2,1,"Blue field + yellow sun\nReal flag uses an offset disc"),
  ("08  NORTH MACEDONIA",0,2,1,"Red field + yellow sun\nReal rays reach the edges"),
  ("09  AUSTRIA",0,19,0,"Game: diagonal repeating stripes\nReal: 3 horizontal bands"),
  ("10  GREECE",12,19,0,"Game: diagonal repeating stripes\nReal: horizontal bands + cross"),
  ("11  CROATIA",0,19,0,"Red + white checkerboard\nReal: coat-of-arms detail only"),
  ("12  FINLAND",19,12,0,"White + blue grid\nReal: one Nordic cross")
 };
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  world=new WorldSetup();world.SetParticipantCounts(0,1);world.SetWorld(0);world.SetCast(1);world.SetPlacement(3);
  ui=(UiPainter)typeof(Game1).GetField("_ui",F)!.GetValue(this)!;batch=(SpriteBatch)typeof(Game1).GetField("_spriteBatch",F)!.GetValue(this)!;
 }
 protected override void Update(GameTime time){}
 void Polygon(Vector2[] vertices,Color color){
  int top=(int)vertices.Min(v=>v.Y),bottom=(int)vertices.Max(v=>v.Y);
  if(clip.HasValue){top=Math.Max(top,clip.Value.Top);bottom=Math.Min(bottom,clip.Value.Bottom-1);}
  for(int y=top;y<=bottom;y++){
   var hits=new List<float>();for(int a=0,b=vertices.Length-1;a<vertices.Length;b=a++){
    var p=vertices[a];var q=vertices[b];float scan=y+.5f;
    if((p.Y>scan)!=(q.Y>scan))hits.Add(p.X+(scan-p.Y)*(q.X-p.X)/(q.Y-p.Y));
   }hits.Sort();for(int i=0;i+1<hits.Count;i+=2){int left=(int)hits[i],right=(int)hits[i+1];if(clip.HasValue){left=Math.Max(left,clip.Value.Left);right=Math.Min(right,clip.Value.Right);}if(right>left)ui.Box(new(left,y,right-left,1),color);}
  }
 }
 void Disc(Vector2 at,float radius,Color color){for(int y=-(int)radius;y<=radius;y++){float w=MathF.Sqrt(Math.Max(0,radius*radius-y*y));ui.Box(new((int)(at.X-w),(int)at.Y+y,Math.Max(1,(int)(w*2)),1),color);}}
 Vector2[] Star(Vector2 at,float radius)=>Enumerable.Range(0,10).Select(i=>{float a=-MathF.PI/2+i*MathF.PI/5;return at+new Vector2(MathF.Cos(a),MathF.Sin(a))*radius*(i%2==0?1:.382f);}).ToArray();
 void Reference(int index,Rectangle box){
  var red=new Color(218,37,29);var yellow=new Color(255,230,0);var blue=new Color(65,137,221);var green=new Color(0,106,78);
  var r=CountryFlagArt.Fit(box,index is 5 or 7?1:0);ui.Box(new(r.X-1,r.Y-1,r.Width+2,r.Height+2),new Color(100,119,133));
  var background=index switch{1 or 6 or 9=>blue,4 or 11=>Color.White,5=>green,2=>new Color(193,39,45),_=>red};ui.Box(r,background);
  clip=r;var c=r.Center.ToVector2();float h=r.Height;
  if(index is 0 or 1)Polygon(Star(c,h*.31f),index==0?yellow:Color.White);
  if(index==2){var points=Enumerable.Range(0,5).Select(i=>{float a=-MathF.PI/2+i*MathF.PI*2/5;return c+new Vector2(MathF.Cos(a),MathF.Sin(a))*h*.30f;}).ToArray();for(int i=0;i<5;i++){var d=points[(i+2)%5]-points[i];ui.Tile((points[i]+points[(i+2)%5])/2,new(d.Length(),h*.035f),MathF.Atan2(d.Y,d.X),new(0,98,51));}}
  if(index==3){var moon=new Vector2(r.X+r.Width*.43f,c.Y);Disc(moon,h*.25f,Color.White);Disc(moon+new Vector2(h*.065f,0),h*.20f,background);Polygon(Star(new(r.X+r.Width*.63f,c.Y),h*.13f),Color.White);}
  if(index is 4 or 5 or 6){if(index!=4)c.X-=r.Width*.065f;Disc(c,h*.30f,index==6?yellow:new Color(196,0,37));}
  if(index==7){
   foreach(var end in new[]{new Vector2(r.Left,r.Top),new Vector2(c.X,r.Top),new Vector2(r.Right,r.Top),new Vector2(r.Right,c.Y),new Vector2(r.Right,r.Bottom),new Vector2(c.X,r.Bottom),new Vector2(r.Left,r.Bottom),new Vector2(r.Left,c.Y)}){
    var delta=end-c;var side=Vector2.Normalize(new Vector2(-delta.Y,delta.X))*h*.045f;
    Polygon(new[]{c,end-side,end+side},yellow);
   }Disc(c,h*.20f,yellow);
  }
  if(index==8)ui.Box(new(r.X,r.Y+r.Height/3,r.Width,r.Height/3),Color.White);
  if(index==9){
   for(int stripe=1;stripe<9;stripe+=2)ui.Box(new(r.X,r.Y+stripe*r.Height/9,r.Width,Math.Max(1,r.Height/9)),Color.White);
   var canton=new Rectangle(r.X,r.Y,r.Height*5/9,r.Height*5/9);ui.Box(canton,blue);ui.Box(new(canton.X+canton.Width*2/5,canton.Y,canton.Width/5,canton.Height),Color.White);ui.Box(new(canton.X,canton.Y+canton.Height*2/5,canton.Width,canton.Height/5),Color.White);
  }
  if(index==10){
   ui.Box(new(r.X,r.Y+r.Height/3,r.Width,r.Height/3),Color.White);ui.Box(new(r.X,r.Y+r.Height*2/3,r.Width,r.Height/3),new Color(23,23,150));
   int size=r.Height/9;var shield=new Rectangle(r.Center.X-size*5/2,r.Y+r.Height/5,size*5,size*5);
   for(int y=0;y<5;y++)for(int x=0;x<5;x++)ui.Box(new(shield.X+x*size,shield.Y+y*size,size,size),(x+y)%2==0?red:Color.White);
   for(int i=0;i<5;i++)ui.Box(new(shield.X+i*size,shield.Y-size,size-1,size),new Color(35,119,190));
  }
  if(index==11){ui.Box(new(r.X+r.Width*5/18,r.Y,r.Width/6,r.Height),new Color(0,53,128));ui.Box(new(r.X,r.Y+r.Height*4/11,r.Width,r.Height*3/11),new Color(0,53,128));}
  clip=null;
 }
 protected override void Draw(GameTime time){
  using var target=new RenderTarget2D(GraphicsDevice,1500,1160);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(18,35,46));batch.Begin();
  ui.Text("FLAG SIMILARITY REVIEW",new(45,24),.85f,new Color(255,229,178));
  ui.Text("Current game output",new(55,88),.58f,Color.White);ui.Text("32 px",new(445,88),.5f,Color.White);
  ui.Text("Real-world reference (schematic)",new(625,88),.53f,Color.White);
  ui.Text("Visual similarity, not an exact-match or legal finding.",new(45,1130),.38f,new Color(174,194,207));
  for(int row=0;row<4;row++){
   int index=page*4+row,y=145+row*240;var item=cases[index];ui.Box(new(45,y-8,1410,1),new Color(65,89,103));
   ui.Text(item.Name,new(55,y),.52f,new Color(255,229,178));
   world.SetCountryAppearance(0,item.Primary,item.Secondary,index==10?4:index==11?3:index>=8?1:0);world.SetCountryEmblem(0,item.Emblem);world.SetCountryFlagShape(0,index is 5 or 7?1:0);
   CountryFlagArt.Draw(ui,world,0,new(55,y+44,330,160));CountryFlagArt.Draw(ui,world,0,new(445,y+105,32,22));
   Reference(index,new(625,y+44,330,160));ui.Text(item.Note,new(1020,y+70),.40f,Color.White);
  }
  batch.End();GraphicsDevice.SetRenderTarget(null);
  using(var file=File.Create(Path.Combine(output,"flag-pattern-review.png")))target.SaveAsPng(file,1500,1160);
  Console.WriteLine("Rendered four current two-colour pattern/reference comparisons. Repeat-pattern rules unchanged.");Exit();
 }
}
