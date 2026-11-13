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
else if(args.Length==4 || args.Length==5 && args[4] is ("--tinplate" or "--tinplate-torso" or "--tinplate-legs" or "--tinplate-side" or "--tinplate-back" or "--tinplate-torso-side" or "--tinplate-legs-side" or "--tinplate-torso-back" or "--tinplate-legs-back" or "--silicone" or "--silicone-torso" or "--silicone-legs" or "--silicone-side" or "--silicone-back" or "--silicone-torso-side" or "--silicone-legs-side" or "--silicone-torso-back" or "--silicone-legs-back" or "--soft-vinyl" or "--soft-vinyl-side" or "--soft-vinyl-back" or "--soft-vinyl-torso" or "--soft-vinyl-legs" or "--soft-vinyl-torso-side" or "--soft-vinyl-legs-side" or "--soft-vinyl-torso-back" or "--soft-vinyl-legs-back" or "--paper" or "--paper-torso" or "--paper-legs" or "--paper-side" or "--paper-back" or "--paper-torso-side" or "--paper-legs-side" or "--paper-torso-back" or "--paper-legs-back" or "--cardboard" or "--cardboard-torso" or "--cardboard-legs" or "--cardboard-side" or "--cardboard-back" or "--cardboard-torso-side" or "--cardboard-legs-side" or "--cardboard-torso-back" or "--cardboard-legs-back" or "--foam" or "--foam-torso" or "--foam-legs" or "--foam-side" or "--foam-back" or "--foam-torso-side" or "--foam-legs-side" or "--foam-torso-back" or "--foam-legs-back" or "--polystyrene" or "--polystyrene-torso" or "--polystyrene-legs" or "--polystyrene-side" or "--polystyrene-back" or "--polystyrene-torso-side" or "--polystyrene-legs-side" or "--polystyrene-torso-back" or "--polystyrene-legs-back")){
 bool cardboard=args.Length==5 && args[4].StartsWith("--cardboard");
 bool foam=args.Length==5 && args[4].StartsWith("--foam");
 bool polystyrene=args.Length==5 && args[4].StartsWith("--polystyrene");
 bool softVinyl=args.Length==5 && args[4].StartsWith("--soft-vinyl", StringComparison.Ordinal);
 bool silicone=args.Length==5 && args[4].StartsWith("--silicone", StringComparison.Ordinal);
 bool tinplate=args.Length==5 && args[4].StartsWith("--tinplate", StringComparison.Ordinal);
 bool paper=args.Length==5 && !tinplate && !silicone && !softVinyl && !cardboard && !foam && !polystyrene;
 var bytes=File.ReadAllBytes(args[0]);int width=int.Parse(args[1]),height=int.Parse(args[2]);
 Check(bytes.Length==width*height*4,"RGBA input dimensions");
 var original=Enumerable.Range(0,width*height).Select(i=>new Color(bytes[i*4],bytes[i*4+1],bytes[i*4+2],bytes[i*4+3])).ToArray();
 var part=args.Length==5 && args[4].Contains("-torso")?RobotParts.Body:
  args.Length==5 && args[4].Contains("-legs")?RobotParts.Legs:RobotParts.Head;
 var facing=args.Length==5 && args[4].EndsWith("-side")?BattleFacing.East:
  args.Length==5 && args[4].EndsWith("-back")?BattleFacing.North:BattleFacing.South;
 var surface=tinplate?(part==RobotParts.Head?RobotHeadSurface.TinplateHead(original,width,height,facing):RobotHeadSurface.TinplateBody(original,width,height,part,facing)):silicone?(part==RobotParts.Head?RobotHeadSurface.SiliconeHead(original,width,height,facing):RobotHeadSurface.SiliconeBody(original,width,height,part,facing)):softVinyl?(part==RobotParts.Head?RobotHeadSurface.SoftVinylHead(original,width,height,facing):RobotHeadSurface.SoftVinylBody(original,width,height,part,facing)):polystyrene?(part==RobotParts.Head?RobotHeadSurface.PolystyreneHead(original,width,height,facing):RobotHeadSurface.PolystyreneBody(original,width,height,part,facing)):foam?RobotHeadSurface.Foam(original,width,height,part,facing):cardboard?(part==RobotParts.Head?RobotHeadSurface.CardboardHead(original,width,height,facing):
  RobotHeadSurface.CardboardBody(original,width,height,part,facing)):
  paper?RobotHeadSurface.Paper(original,width,height,part,facing):RobotHeadSurface.Polypropylene(original,width,height);
 if(tinplate){
  int AtTinplate(int x,int y)=>y*height/1024*width+x*width/1536;
  var (bare, painted, shine) = (part,facing) switch {
   (RobotParts.Body,BattleFacing.East) => (new[]{(804,416),(956,280),(20,20)}, (640,520), (700,190)),
   (RobotParts.Legs,BattleFacing.East) => (new[]{(945,267),(620,675),(20,20)}, (630,525), (650,175)),
   (RobotParts.Body,BattleFacing.North) => (new[]{(522,365),(768,292),(20,20)}, (768,520), (768,214)),
   (RobotParts.Legs,BattleFacing.North) => (new[]{(768,550),(768,342),(20,20)}, (520,550), (500,797)),
   (RobotParts.Body,_) => (new[]{(522,365),(768,292),(20,20)}, (768,520), (768,214)),
   (RobotParts.Legs,_) => (new[]{(508,274),(768,550),(768,342),(20,20)}, (520,550), (500,797)),
   (RobotParts.Head,BattleFacing.East) => (new[]{(1190,530),(1250,630),(411,368),(20,20)}, (768,520), (768,180)),
   (RobotParts.Head,BattleFacing.North) => (new[]{(429,376),(1094,771),(20,20)}, (768,520), (768,187)),
   _ => (new[]{(590,550),(930,550),(760,630),(430,355),(1094,755),(760,267),(20,20)}, (768,147), (768,187))
  };
  foreach(var (x,y) in bare)
   Check(surface[AtTinplate(x,y)].Coverage==0,"Tinplate face, trim and fasteners must be protected");
  Check(surface[AtTinplate(painted.Item1,painted.Item2)].Coverage==255 && surface[AtTinplate(shine.Item1,shine.Item2)].Reflection>220,
   "Tinplate shell must accept paint and retain bright reflections");
 }
 if(silicone){
  var (protectedPoints, shell, highlight) = (part,facing) switch {
   (RobotParts.Body,BattleFacing.East) => (new[]{(930,374),(20,20),(1050,400)}, (680,600), (700,210)),
   (RobotParts.Legs,BattleFacing.East) => (new[]{(20,20),(930,374),(1070,900)}, (575,350), (635,185)),
   (RobotParts.Body,BattleFacing.North) => (new[]{(350,380),(1180,380),(20,20)}, (768,550), (530,200)),
   (RobotParts.Legs,BattleFacing.North) => (new[]{(768,550),(768,740),(20,20)}, (520,540), (530,200)),
   (RobotParts.Body,_) => (new[]{(350,380),(1170,380),(20,20)}, (768,550), (540,236)),
   (RobotParts.Legs,_) => (new[]{(768,550),(768,740),(768,855)}, (520,540), (530,200)),
   (RobotParts.Head,BattleFacing.East) => (new[]{(1168,530),(1220,621),(1090,620),(20,20)}, (768,550), (520,200)),
   (RobotParts.Head,BattleFacing.North) => (new[]{(20,20),(768,90)}, (768,550), (475,278)),
   _ => (new[]{(600,550),(921,555),(768,630),(768,390),(768,110)}, (768,290), (470,278))
  };
  int AtSilicone((int x,int y) p)=>p.y*height/1024*width+p.x*width/1536;
  foreach(var point in protectedPoints)
   Check(surface[AtSilicone(point)].Coverage==0,$"Silicone {part} must protect {point}");
  Check(surface[AtSilicone(shell)].Coverage==255 && surface[AtSilicone(highlight)].Reflection>surface[AtSilicone(shell)].Reflection &&
   surface.Max(p=>p.Reflection)<=67,$"Silicone {part} must retain broad, muted highlights");
 }
 if(softVinyl){
  var (protectedPoints, shell, highlight) = (part,facing) switch {
   (RobotParts.Body,BattleFacing.East) => (new[]{(680,200),(680,360),(920,672),(690,870),(20,20)}, (530,540), (768,512)),
   (RobotParts.Legs,BattleFacing.East) => (new[]{(20,20),(768,80),(1070,460)}, (620,514), (635,185)),
   (RobotParts.Body,BattleFacing.North) => (new[]{(768,225),(390,325),(1140,335),(180,690),(1350,690),(768,855)}, (768,550), (552,327)),
   (RobotParts.Legs,BattleFacing.North) => (new[]{(768,550),(768,740),(768,855)}, (520,540), (940,745)),
   (RobotParts.Body,_) => (new[]{(768,220),(390,325),(1140,335),(180,690),(1350,690),(768,855)}, (768,550), (552,327)),
   (RobotParts.Legs,_) => (new[]{(768,550),(768,740),(768,855)}, (520,540), (940,736)),
   (RobotParts.Head,BattleFacing.East) => (new[]{(1170,550),(1227,620),(1100,630),(20,20)}, (768,550), (570,222)),
   (RobotParts.Head,BattleFacing.North) => (new[]{(20,20),(768,90)}, (768,550), (475,278)),
   _ => (new[]{(600,550),(921,555),(768,630),(768,384),(768,120)}, (768,290), (475,272))
  };
  int AtVinyl((int x,int y) p)=>(p.y*height/1024)*width+p.x*width/1536;
  foreach(var point in protectedPoints)
   Check(surface[AtVinyl(point)].Coverage==0,"Soft vinyl trim, face and transparent gaps must remain uncolored");
  Check(surface[AtVinyl(shell)].Coverage==255 && surface[AtVinyl(highlight)].Reflection>surface[AtVinyl(shell)].Reflection,
   "Soft vinyl shell accepts color and retains broad highlights");
 }
 if(polystyrene){
  bool sideLegs=part==RobotParts.Legs && facing==BattleFacing.East;
  int AtPolystyrene(int x,int y)=>(y*height/(sideLegs?1199:1024))*width+x*width/(sideLegs?1312:1536);
  var (protectedPoints, paintedPoint, reflectionPoint)=(part,facing) switch {
      (RobotParts.Body,BattleFacing.East)=>(new[]{(963,438),(20,20)},(545,535),(550,220)),
   (RobotParts.Legs,BattleFacing.East)=>(new[]{(20,20),(850,500)},(530,550),(490,738)),
   (RobotParts.Body,BattleFacing.North)=>(new[]{(453,446),(1083,678),(20,20)},(768,550),(768,356)),
   (RobotParts.Legs,BattleFacing.North)=>(new[]{(768,600),(768,850),(20,20)},(535,530),(510,646)),
   (RobotParts.Body,_)=>(new[]{(466,450),(1064,669),(20,20)},(768,540),(768,364)),
   (RobotParts.Legs,_)=>(new[]{(768,600),(768,850),(20,20)},(530,535),(530,632)),
   (RobotParts.Head,BattleFacing.East)=>(new[]{(1195,530),(1250,624),(1110,450),(768,169),(20,20)},(768,290),(768,322)),
   (RobotParts.Head,BattleFacing.North)=>(new[]{(310,455),(1220,700),(768,170),(20,20)},(768,550),(768,327)),
   _=>(new[]{(760,570),(430,570),(1100,570),(310,455),(1210,705),(768,170),(20,20)},(768,293),(768,333))
  };
  foreach(var (x,y) in protectedPoints)
   Check(surface[AtPolystyrene(x,y)].Coverage==0,"Polystyrene face, latches, seam and background must stay uncolored");
  Check(surface[AtPolystyrene(paintedPoint.Item1,paintedPoint.Item2)].Coverage==255 &&
   surface[AtPolystyrene(reflectionPoint.Item1,reflectionPoint.Item2)].Reflection>surface[AtPolystyrene(paintedPoint.Item1,paintedPoint.Item2)].Reflection,
   "Polystyrene shell must accept paint and keep the narrow reflection");
 }
 if(foam){
  int AtFoam(int x,int y)=>(y*height/1024)*width+x*width/1536;
  var (painted,bare)=(part,facing) switch {
   (RobotParts.Body,BattleFacing.North)=>(new[]{(731,552)},new[]{(768,300),(768,780),(290,550),(20,20)}),
   (RobotParts.Legs,BattleFacing.North)=>(new[]{(556,562),(973,562)},new[]{(768,580),(550,780),(768,300),(20,20)}),
   (RobotParts.Body,BattleFacing.East)=>(new[]{(880,560),(517,530)},new[]{(760,260),(494,700),(880,790),(20,20)}),
   (RobotParts.Legs,BattleFacing.East)=>(new[]{(602,572),(846,548)},new[]{(760,340),(730,580),(680,780),(20,20)}),
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
  if(!tinplate && !paper && !cardboard && !foam && !polystyrene && !softVinyl && !silicone)RobotFlagComposite.Sticker(output,width,height,new((int)(width*.70f),(int)(height*.59f),(int)(width*.15f),(int)(height*.16f)),tiles,WorldSetup.Colors[world.CountrySecondaryColor(0)]);
  if(paper && facing!=BattleFacing.South && part==RobotParts.Head)
   RobotFlagComposite.Sticker(output,width,height,new(width*56/100,height*55/100,width*10/100,height*13/100),
    tiles,WorldSetup.Colors[world.CountrySecondaryColor(0)]);
  for(int i=0;i<output.Length;i++){
   Check(output[i].A==original[i].A,"Rendered alpha changed");
   if(surface[i].Coverage==0)Check(output[i]==original[i],"Rendered face or transparent source changed");
  }
  if(tinplate && part==RobotParts.Head && facing==BattleFacing.South)RobotFlagComposite.Sticker(output,width,height,new(width*68/100,height*13/100,width*7/100,height*9/100),
    tiles,WorldSetup.Colors[world.CountrySecondaryColor(0)]);
  var raw=new byte[bytes.Length];for(int i=0;i<output.Length;i++){raw[i*4]=output[i].R;raw[i*4+1]=output[i].G;raw[i*4+2]=output[i].B;raw[i*4+3]=output[i].A;}
  File.WriteAllBytes(Path.Combine(args[3],$"head-{country}.rgba"),raw);
 }
 Console.WriteLine(foam?"PASS foam PNG: face, bead variation and alpha.":cardboard?$"PASS cardboard {part} PNG: two flags preserve protected pixels and alpha.":paper?"PASS paper PNG: two flags preserve face, folds, bare side and alpha.":
  "PASS actual neutral PNG pixels: two flags, shared emblem stickers, exact protected face and alpha.");
}
