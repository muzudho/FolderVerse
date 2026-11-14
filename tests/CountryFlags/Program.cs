using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
var shapes=new int[4];var kinds=new int[3];
for(int seed=0;seed<100;seed++){
 var w=new WorldSetup();w.SetParticipantCounts(0,60);w.SetWorld(0);w.SetCast(seed);
 Check(Enumerable.Range(0,60).Count(i=>w.CountryPattern(i)==0 && w.CountryEmblem(i)==0)==7,"Exactly seven unpatterned countries in a large cast");
 var signatures=new HashSet<string>();
 for(int i=0;i<60;i++){
  shapes[w.CountryFlagShape(i)]++;int p=w.CountryPattern(i),e=w.CountryEmblem(i),a=w.ColorIndices[i]%WorldSetup.Colors.Length,b=w.CountrySecondaryColor(i);
  Check(a!=b,"Secondary colour equals primary");
  string signature=p==1?$"stripe:{Math.Min(a,b)}:{Math.Max(a,b)}:{e}":$"{a}:{b}:{p}:{e}";
  Check(signatures.Add(signature),"Flags repeat even if their shapes are ignored");
  if(p!=0 || e!=0)kinds[p==0?1:e==0?0:2]++;
 }
 w.SetCountryFlagShape(0,3);var save=AutoSaveStore.Capture(w);var loaded=AutoSaveStore.Restore<WorldSetup>(save);
 Check(loaded.CountryFlagShape(0)==3 && loaded.CountryPattern(25)==w.CountryPattern(25),"Flag roundtrip");
 foreach(var entry in save.Objects)entry.Fields?.Remove("_countryFlagShapes");
 Check(AutoSaveStore.Restore<WorldSetup>(save).CountryFlagShape(0)==0,"Legacy flags default to 3:2 without rerolling");
}
Check(shapes[0]>2600 && shapes[1]>2000 && shapes[2]>400 && shapes[3]>30 && shapes[3]<100,"Shape weighting");
Check(kinds[0]>1800 && kinds[1]>1300 && kinds[2]>1300,"Design weighting");
Check(CountryFlagArt.Fit(new(0,0,120,120),0).Height==80 && CountryFlagArt.Fit(new(0,0,120,120),1).Height==60 && CountryFlagArt.Fit(new(0,0,120,120),2).Height==120,"Flag proportions");
Check(!CountryFlagArt.Contains(3,35,0) && CountryFlagArt.Contains(3,0,0) && CountryFlagArt.Contains(3,32,12),"Triangle mask points right");
var emptyBattle=new BattleState(9){Kind=BattleKind.Node,Defender=1,FlagStanding=false};
emptyBattle.Units.Add(new(){Id=99,Owner=0,Robot=new(99,0,RobotParts.Complete)});
emptyBattle.Events.Add(new(1,BattlePhase.Flag,99,"国旗を倒した"));var emptyLessons=new BattleLessons();emptyLessons.ObserveEmptyFlag(emptyBattle);
Check(emptyLessons.Pending.Single().Owner==0,"New national-flag wording still awards the empty-home lesson to the attacker");
Console.WriteLine($"PASS: seeded unique five-element flags, distribution [{string.Join(',',shapes)}], kinds [{string.Join(',',kinds)}], proportions and legacy saves.");
using var game=new FlagGame();game.Run();
sealed class FlagGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Screen(string n)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),n));
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetParticipantCounts(1,19);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Screen("WorldStatus");((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-100d);}
 protected override void Update(GameTime time){}
 void CheckTerrain(){
  using var preview=new WorldPreview(GraphicsDevice);preview.ShowLandConquest(w);
  Color[] Colours(float time=0,IEnumerable<SurfaceCell> selected=null){var viewport=GraphicsDevice.Viewport;using var target=new RenderTarget2D(GraphicsDevice,320,320,false,SurfaceFormat.Color,DepthFormat.Depth24);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Black);preview.Draw(.4f,.3f,time,false,landSelection:selected);GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Viewport=viewport;var pixels=new Color[320*320];target.GetData(pixels);return pixels;}
  var before=Colours();var owners=(int[])w.Owners.Clone();Array.Fill(w.Owners,-1);preview.ShowLandConquest(w);
  Check(before.SequenceEqual(Colours()),"Claimed and unclaimed territory must retain identical terrain colours");Array.Copy(owners,w.Owners,owners.Length);
  Array.Fill(w.Owners,0);preview.ShowLandConquest(w,dimClaimed:true);
  var dimmed=Colours();
  Array.Fill(w.Owners,-1);preview.ShowLandConquest(w,dimClaimed:true);
  Check(before.SequenceEqual(Colours()),"Unclaimed land retains its normal brightness in land conquest");
  Check(before.Zip(dimmed).Any(pair=>pair.First.R>pair.Second.R+20),"Claimed terrain is visibly darker");
  Check(!Colours(.6f,w.Cells).SequenceEqual(Colours(1.8f,w.Cells)),"Selected territory backlight pulses");
  Array.Copy(owners,w.Owners,owners.Length);
 }
 protected override void Draw(GameTime time){
  if(frame==0){
   CheckTerrain();
   GraphicsDevice.Clear(new Color(18,35,46));var batch=(SpriteBatch)Get("_spriteBatch");var ui=(UiPainter)Get("_ui");batch.Begin();
   for(int shape=0;shape<4;shape++)for(int kind=0;kind<4;kind++){
    w.SetCountryFlagShape(0,shape);w.SetCountryAppearance(0,6,12,kind is 1 or 3?1:0);w.SetCountryEmblem(0,kind>=2?3:0);
    var r=new Rectangle(80+kind*430,75+shape*235,260,170);CountryFlagArt.Draw(ui,w,0,r);ui.Text(WorldSetup.CountryFlagShapeNames[shape],new(r.X,r.Bottom+8),.4f,Color.White);
   }batch.End();
  }else{
   if(frame==1){Call("OpenCountrySettings");Set("_pointer",new Point(1700,580));Call("CountrySettingsInput",true,false);Check(w.CountryFlagShape((int)Get("_countrySettingsRuler"))==3,"Shape button hit target");}
   if(frame==2){Set("_countrySettingsOpen",false);Set("_statusGlobe",true);}
   if(frame==3){Call("BeginLandConquest");for(int i=0;i<w.Owners.Length;i++)w.Owners[i]=i%3==0?i%w.ActiveCount:-1;((WorldPreview)Get("_world")).ShowLandConquest(w,dimClaimed:true);}
   if(frame==4){var battle=new BattleState(9){TargetNode=w.Nodes.Current(0)!.Id,Defender=0};battle.Armies.Add(new(){Owner=0,Side=4});battle.Capture(BattlePhase.Choose);Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Call("FinishTransport");Set("_battleIntroAge",10d);Set("_lastCutInPhase","戦闘フェーズ");}
   base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame),TimeSpan.Zero));
  }
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var f=File.Create(Path.Combine(AppContext.BaseDirectory,$"flags-{frame}.png")))image.SaveAsPng(f,image.Width,image.Height);
  if(frame++==4){Console.WriteLine("PASS: flag atlas, settings hit target, globe, land-conquest and battle render.");Exit();}
 }
}
