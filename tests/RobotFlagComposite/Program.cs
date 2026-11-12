using FolderVerse;
using Microsoft.Xna.Framework;
using Surface=FolderVerse.RobotFlagComposite.Surface;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var stickerArea = new Rectangle(7, 3, 29, 19);
var asymmetricTiles = new[] { new Rectangle(4, 6, 5, 12), new Rectangle(9, 15, 10, 3) };
var originalSticker = Enumerable.Repeat(Color.Black, 43 * 28).ToArray();
var mirroredSticker = (Color[])originalSticker.Clone();
RobotFlagComposite.Sticker(originalSticker, 43, 28, stickerArea, asymmetricTiles, Color.Gold);
RobotFlagComposite.Sticker(mirroredSticker, 43, 28, stickerArea, asymmetricTiles, Color.Gold, mirror: true);
Check(!originalSticker.SequenceEqual(mirroredSticker), "Asymmetric emblem must change before the sprite flip");
for (int y = 0; y < 28; y++)
for (int x = 0; x < 43; x++)
{
 int originalX = stickerArea.Contains(x, y) ? stickerArea.Left + stickerArea.Right - 1 - x : x;
 Check(mirroredSticker[y * 43 + x] == originalSticker[y * 43 + originalX], "Counter-flipped sticker pixel mismatch");
}
Console.WriteLine("PASS asymmetric sticker and white edge counter-flip inside the sticker only.");
var world=new WorldSetup();world.SetParticipantCounts(1,1);world.SetWorld(0);
var neutral=Enumerable.Repeat(new Color(132,143,151,197),36*24).ToArray();
var map=Enumerable.Range(0,neutral.Length).Select(i=>new Surface(new((i%36+.5f)/36,(i/36+.5f)/24),255,255,0)).ToArray();
int combinations=0;
for(int shape=0;shape<WorldSetup.CountryFlagShapeNames.Length;shape++)
for(int pattern=0;pattern<WorldSetup.CountryPatternNames.Length;pattern++)
for(int emblem=0;emblem<WorldSetup.CountryEmblemNames.Length;emblem++){
 world.SetCountryFlagShape(0,shape);world.SetCountryAppearance(0,6,12,pattern);world.SetCountryEmblem(0,emblem);
 var output=RobotFlagComposite.Compose(world,0,neutral,map);
 for(int i=0;i<output.Length;i++){
  Check(output[i].A==neutral[i].A,"Silhouette alpha changed");
  var expected=CountryFlagArt.Contains(shape,i%36,i/36)?CountryFlagArt.Pixel(world,0,i%36,i/36):neutral[i];
  Check(output[i].R==expected.R && output[i].G==expected.G && output[i].B==expected.B,"Shared flag raster mismatch");
 }
 combinations++;
}
var protectedMap=new[]{new Surface(new(.5f,.5f),0,255,0),new Surface(new(.5f,.5f),255,255,255),new Surface(new(float.NaN,0),255,255,0)};
var protectedPixels=new[]{new Color(230,218,181,255),new Color(100,100,100,153),new Color(10,20,30,0)};
var saved=(Color[])protectedPixels.Clone();
var first=RobotFlagComposite.Compose(world,0,protectedPixels,protectedMap);
world.SetCountryAppearance(0,2,9,1);
var second=RobotFlagComposite.Compose(world,0,protectedPixels,protectedMap);
Check(first.SequenceEqual(second),"Protected face, reflection or transparent pixel changed with country");
Check(first[0]==saved[0] && first[1]==new Color(255,255,255,153) && first[2]==saved[2],"Protected layers not preserved");
Check(protectedPixels.SequenceEqual(saved),"Source artwork mutated");
Check(neutral.All(p=>p==new Color(132,143,151,197)),"Shared neutral image mutated");
Console.WriteLine($"PASS {combinations} five-element flag designs; explicit masks, reflection, silhouette alpha and immutable source.");
// A wood finish needs an unpainted grain band and grain modulation under ink.
world.SetCountryFlagShape(0,0);
var wood = new[]{new Color(213,175,124),new Color(167,116,71),new Color(213,175,124,210),new Color(167,116,71,210)};
var woodMap = new[]{new Surface(new(.4f,.5f),0,255,0),new Surface(new(.4f,.5f),0,255,0),
 new Surface(new(.4f,.5f),200,225,0),new Surface(new(.4f,.5f),200,155,0)};
world.SetCountryAppearance(0,6,12,1);
var woodFirst=RobotFlagComposite.Compose(world,0,wood,woodMap,fieldOnly:true);
world.SetCountryAppearance(0,9,3,4);
var woodSecond=RobotFlagComposite.Compose(world,0,wood,woodMap,fieldOnly:true);
foreach(var finish in new[]{woodFirst,woodSecond})
{
 Check(finish.Take(2).SequenceEqual(wood.Take(2)),"Unpainted wood grain changed");
 Check(finish.Select(p=>p.A).SequenceEqual(wood.Select(p=>p.A)),"Wood finish changed alpha");
 Check(finish[2].R+finish[2].G+finish[2].B>finish[3].R+finish[3].G+finish[3].B,
  "Paint hid the grain contrast");
}
Check(woodFirst[2]!=woodSecond[2] && woodFirst[3]!=woodSecond[3],"Wood finish did not take the country colors");
Console.WriteLine("PASS explicit wood mask keeps bare grain and diffuse detail under two country designs.");
const int size=128;
var headPixels=Enumerable.Repeat(new Color(140,140,140,255),size*size).ToArray();
var headMap=RobotHeadSurface.Polypropylene(headPixels,size,size);
int At(float x,float y)=>(int)(y*size)*size+(int)(x*size);
foreach(var point in new[]{new Vector2(.25f,.50f),new Vector2(.52f,.50f),new Vector2(.39f,.60f),new Vector2(.39f,.39f)})
 Check(headMap[At(point.X,point.Y)].Coverage==0,"Face and features must be protected even when grey");
Check(headMap[At(.78f,.55f)].Coverage==255,"Right shell must accept the country design");
var beforeMap=(Color[])headPixels.Clone();
headPixels[At(.78f,.55f)]=Color.White;headPixels[At(.8f,.8f)]=Color.Transparent;
var shinyMap=RobotHeadSurface.Polypropylene(headPixels,size,size);
Check(shinyMap[At(.78f,.55f)].Reflection>240,"Authored reflection map lost white highlight");
Check(shinyMap[At(.8f,.8f)].Coverage==0,"Transparent silhouette must not be painted");
Check(headPixels[0]==beforeMap[0],"Map authoring mutated neutral artwork");
Console.WriteLine("PASS PP head map: geometric face protection independent of RGB, shell coverage, reflection and transparency.");
var bodyMap = RobotHeadSurface.PolypropyleneBody(headPixels, size, size, new(.125f, .125f, .75f, .75f));
Check(bodyMap[At(.39f, .55f)].Coverage == 255, "Body paint must cover the area protected on the head");
Check(bodyMap[At(.8f, .8f)].Coverage == 0, "Body paint must preserve transparent gaps");
Check(bodyMap[At(.78f, .55f)].Reflection > 240, "Body paint must preserve white highlights");
Check(bodyMap[0].Uv == Vector2.Zero && bodyMap[^1].Uv == Vector2.One, "Body UV must stay within the flag field");
if(args.Length==5 && args[4]=="--maple")
{
 var bytes=File.ReadAllBytes(args[0]);int width=int.Parse(args[1]),height=int.Parse(args[2]);
 Check(bytes.Length==width*height*4,"Maple RGBA input dimensions");
 var source=Enumerable.Range(0,width*height).Select(i=>new Color(bytes[i*4],bytes[i*4+1],bytes[i*4+2],bytes[i*4+3])).ToArray();
 var woodSurface=RobotHeadSurface.Maple(source,width,height);
 var renders=new List<Color[]>();
 for(int country=0;country<2;country++)
 {
  world.SetCountryAppearance(0,country==0?6:9,country==0?12:3,country==0?1:4);
  var output=RobotFlagComposite.Compose(world,0,source,woodSurface,fieldOnly:true);
  Check(output.Select(p=>p.A).SequenceEqual(source.Select(p=>p.A)),"Maple alpha changed");
  for(int i=0;i<source.Length;i++)
   if(woodSurface[i].Coverage==0) Check(output[i]==source[i],"Protected wood or face changed");
  var rgba=output.SelectMany(p=>new[]{p.R,p.G,p.B,p.A}).ToArray();
  File.WriteAllBytes(Path.Combine(args[3],$"maple-{country}.rgba"),rgba);
  renders.Add(output);
 }
 Check(renders[0].Zip(renders[1]).Count(pair=>pair.First!=pair.Second)>width*height/10,
  "Country change did not affect enough of the painted maple shell");
 Console.WriteLine("PASS real maple head preserves face, bare flank and alpha while two flags recolor the shell.");
}
else if(args.Length==4 || args.Length==5 && args[4] is ("--paper" or "--paper-torso" or "--paper-legs" or "--paper-side" or "--paper-back" or "--paper-torso-side" or "--paper-legs-side" or "--paper-torso-back" or "--paper-legs-back" or "--cardboard" or "--cardboard-torso" or "--cardboard-legs" or "--cardboard-side" or "--cardboard-back" or "--cardboard-torso-side" or "--cardboard-legs-side" or "--cardboard-torso-back" or "--cardboard-legs-back" or "--foam" or "--foam-torso" or "--foam-legs" or "--foam-side" or "--foam-back")){
 bool cardboard=args.Length==5 && args[4].StartsWith("--cardboard");
 bool foam=args.Length==5 && args[4].StartsWith("--foam");
 bool paper=args.Length==5 && !cardboard && !foam;
 var bytes=File.ReadAllBytes(args[0]);int width=int.Parse(args[1]),height=int.Parse(args[2]);
 Check(bytes.Length==width*height*4,"RGBA input dimensions");
 var original=Enumerable.Range(0,width*height).Select(i=>new Color(bytes[i*4],bytes[i*4+1],bytes[i*4+2],bytes[i*4+3])).ToArray();
 var part=args.Length==5 && args[4].Contains("-torso")?RobotParts.Body:
  args.Length==5 && args[4].Contains("-legs")?RobotParts.Legs:RobotParts.Head;
 var facing=args.Length==5 && args[4].EndsWith("-side")?BattleFacing.East:
  args.Length==5 && args[4].EndsWith("-back")?BattleFacing.North:BattleFacing.South;
 var surface=foam?RobotHeadSurface.Foam(original,width,height,part,facing):cardboard?(part==RobotParts.Head?RobotHeadSurface.CardboardHead(original,width,height,facing):
  RobotHeadSurface.CardboardBody(original,width,height,part,facing)):
  paper?RobotHeadSurface.Paper(original,width,height,part,facing):RobotHeadSurface.Polypropylene(original,width,height);
 if(foam){
  int AtFoam(int x,int y)=>(y*height/1024)*width+x*width/1536;
  var (painted,bare)=(part,facing) switch {
   (RobotParts.Body,_)=>(new[]{(700,550),(300,520),(1170,520)},new[]{(768,240),(220,700),(1250,710),(20,20)}),
   (RobotParts.Legs,_)=>(new[]{(550,550),(950,550)},new[]{(755,590),(550,780),(950,810),(20,20)}),
   (RobotParts.Head,BattleFacing.East)=>(new[]{(620,430),(900,560)},new[]{(1120,520),(1190,648),(768,110),(20,20)}),
   (RobotParts.Head,BattleFacing.North)=>(new[]{(768,500),(620,630)},new[]{(768,230),(325,550),(1190,550),(20,20)}),
   _=>(new[]{(650,290)},new[]{(491,558),(829,558),(658,638),(671,377),(20,20)})
  };
  foreach(var (x,y) in bare)
   Check(surface[AtFoam(x,y)].Coverage==0,"Foam face and background must stay uncolored");
  var shell=surface.Where(p=>p.Coverage>0).ToArray();
  Check(painted.All(point=>surface[AtFoam(point.Item1,point.Item2)].Coverage>0) && shell.All(p=>p.Reflection==0) &&
   shell.Max(p=>p.Diffuse)-shell.Min(p=>p.Diffuse)>20 &&
   shell.Max(p=>p.Coverage)-shell.Where(p=>p.Coverage>120).Min(p=>p.Coverage)>40,
   "Foam shell must retain matte bead shading and variation");
 }
 if(cardboard){
  bool sideLegs=part==RobotParts.Legs && facing==BattleFacing.East;
  int AtCardboard(int x,int y)=>(y*height/(sideLegs?1199:1024))*width+x*width/(sideLegs?1312:1536);
  var (painted,bare)=(part,facing) switch {
   (RobotParts.Body,BattleFacing.North) => (new[]{(768,520),(240,560),(1295,560)},new[]{(768,311),(768,797),(360,390)}),
   (RobotParts.Legs,BattleFacing.North) => (new[]{(550,550),(980,550)},new[]{(768,760),(550,792),(660,530),(768,310)}),
   (RobotParts.Body,BattleFacing.East) => (new[]{(550,550),(1180,530)},new[]{(750,260),(975,550),(1230,770),(140,500)}),
   (RobotParts.Legs,BattleFacing.East) => (new[]{(470,550),(698,550)},new[]{(635,590),(480,930),(450,310),(140,500)}),
   (RobotParts.Body,_) => (new[]{(700,480),(225,530),(1260,530)},new[]{(700,265),(310,540),(1270,701),(700,780)}),
   (RobotParts.Legs,_) => (new[]{(550,550),(960,550)},new[]{(750,550),(650,500),(460,780),(950,320)}),
   (_,BattleFacing.East) => (new[]{(768,520),(900,665)},new[]{(1220,510),(768,203),(302,520),(20,20)}),
   (_,BattleFacing.North) => (new[]{(768,520),(900,665)},new[]{(768,203),(323,520),(1200,520),(20,20)}),
   _ => (new[]{(680,355)},new[]{(481,537),(907,537),(694,660),(768,204),(1200,500),(20,20)})
  };
  foreach(var (x,y) in bare)Check(surface[AtCardboard(x,y)].Coverage==0,"Cardboard features and flutes must remain uncolored");
  foreach(var (x,y) in painted)Check(surface[AtCardboard(x,y)].Coverage==255,"Cardboard panel must accept paint");
  Check(surface.All(p=>p.Reflection==0),"Cardboard must remain matte");
 }
 if(paper && part==RobotParts.Head && facing!=BattleFacing.South){
  int AtOriented(int x,int y)=>(y*height/1024)*width+x*width/1536;
  Check(surface[AtOriented(768,500)].Coverage==255,"Paper panel must accept country color");
  foreach(var (x,y) in new[]{(20,20),(768,180),(1180,520)})
   Check(surface[AtOriented(x,y)].Coverage==0,"Oriented paper face and folded rim must stay uncolored");
 }
 if(paper && part==RobotParts.Head && facing==BattleFacing.South){
  int AtPaper(int x,int y)=>(y*height/1059)*width+x*width/1485;
  foreach(var (x,y) in new[]{(644,590),(644,432),(1080,600),(950,320),(20,20)})
   Check(surface[AtPaper(x,y)].Coverage==0,"Paper face, fold or side must stay uncolored");
  Check(surface[AtPaper(645,350)].Coverage==255 && surface.All(p=>p.Reflection==0),
   "Paper front must accept flat matte color");
 }
 if(paper && part!=RobotParts.Head){
  var points=facing==BattleFacing.East
   ? part==RobotParts.Body
    ? new[]{(500,500,true),(1180,550,true),(1080,280,false),(700,200,false)}
    : new[]{(550,550,true),(700,540,true),(550,850,false),(550,280,false)}
   : facing==BattleFacing.North
   ? part==RobotParts.Body
    ? new[]{(768,520,true),(253,554,true),(1283,554,true),(768,270,false),(360,400,false)}
    : new[]{(550,550,true),(980,550,true),(550,780,false),(768,760,false),(768,300,false)}
   : part==RobotParts.Body
   ? new[]{(700,520,true),(240,600,true),(1260,560,true),(768,270,false),(1080,360,false)}
   : new[]{(550,580,true),(880,590,true),(480,755,false),(920,750,false),(737,700,false)};
  foreach(var (x,y,painted) in points){
   int index=(y*height/(width==1312?1199:1024))*width+x*width/(width==1312?1312:1536);
   Check(surface[index].Coverage==(painted?255:0),$"Paper {part} mask at ({x},{y})");
  }
  Check(surface.All(p=>p.Reflection==0),"Paper must stay matte");
 }
 Directory.CreateDirectory(args[3]);
 var tilesMethod=typeof(CountryEmblemArt).GetMethod("Tiles",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
 for(int country=0;country<2;country++){
  world.SetCountryFlagShape(0,0);world.SetCountryAppearance(0,country==0?6:9,country==0?12:3,country==0?1:4);
  world.SetCountryEmblem(0,country==0?1:5);
  var output=RobotFlagComposite.Compose(world,0,original,surface,fieldOnly:true);
  var tiles=(IReadOnlyList<Rectangle>)tilesMethod.Invoke(null,new object[]{world.CountryEmblem(0)})!;
  if(!paper && !cardboard && !foam)RobotFlagComposite.Sticker(output,width,height,new((int)(width*.70f),(int)(height*.59f),(int)(width*.15f),(int)(height*.16f)),tiles,WorldSetup.Colors[world.CountrySecondaryColor(0)]);
  if(paper && facing!=BattleFacing.South && part==RobotParts.Head)
   RobotFlagComposite.Sticker(output,width,height,new(width*56/100,height*55/100,width*10/100,height*13/100),
    tiles,WorldSetup.Colors[world.CountrySecondaryColor(0)]);
  for(int i=0;i<output.Length;i++){
   Check(output[i].A==original[i].A,"Rendered alpha changed");
   if(surface[i].Coverage==0)Check(output[i]==original[i],"Rendered face or transparent source changed");
  }
  var raw=new byte[bytes.Length];for(int i=0;i<output.Length;i++){raw[i*4]=output[i].R;raw[i*4+1]=output[i].G;raw[i*4+2]=output[i].B;raw[i*4+3]=output[i].A;}
  File.WriteAllBytes(Path.Combine(args[3],$"head-{country}.rgba"),raw);
 }
 Console.WriteLine(foam?"PASS foam PNG: face, bead variation and alpha.":cardboard?$"PASS cardboard {part} PNG: two flags preserve protected pixels and alpha.":paper?"PASS paper PNG: two flags preserve face, folds, bare side and alpha.":
  "PASS actual neutral PNG pixels: two flags, shared emblem stickers, exact protected face and alpha.");
}
