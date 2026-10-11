using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
var b=new BattleState(1){Finished=true,Winner=0};
b.Armies.Add(new BattleArmy{Owner=0});b.Armies.Add(new BattleArmy{Owner=1});
for(int i=0;i<100;i++)b.Units.Add(new BattleUnit{Id=i,Owner=1,Retreated=true});
foreach(var item in new[]{(0,"退却……"),(29,"退却……"),(30,"全滅……"),(49,"全滅……"),(50,"壊滅……"),(89,"壊滅……"),(90,"消滅……"),(100,"消滅……")})
{
 for(int i=0;i<100;i++)b.Units[i].Fallen=i<item.Item1;
 if(NewspaperBattleOutcome.Caption(b,1,false)!=item.Item2)throw new Exception("Loss threshold wrong");
}
if(NewspaperBattleOutcome.Caption(b,0,true)!="勝利！")throw new Exception("Winner wrong");
foreach(var u in b.Units)u.Fallen=false;
if(NewspaperBattleOutcome.Caption(b,1,true)!="突然の転進")throw new Exception("Biased retreat wrong");
b.Allied=(a,c)=>a==c || a==1;
if(NewspaperBattleOutcome.Caption(b,1,false)!="勝利！")throw new Exception("Allied winner wrong");
Console.WriteLine("PASS: 0/29/30/49/50/89/90/100 percent, victory, biased retreat, allied victory");
var h=new BattleState(2){Defender=1,Kind=BattleKind.Node};h.Armies.Add(new BattleArmy{Owner=0});h.Armies.Add(new BattleArmy{Owner=1});
var names=new[]{"ルナ","レイン"};
BattleFrame Frame(int own,int other,int turn)=>new(turn,BattlePhase.Attack,Enumerable.Range(0,own+other).Select(i=>new BattleUnitView(i,i<own?0:1,BattleRole.Soldier,new Point(0,0),BattleFacing.North,0,0,0,false,false)).ToArray(),true,1);
h.Frames.Add(Frame(10,10,0));
void AssertHeadline(BattleFrame f,string expected){var actual=NewspaperBattleOutcome.Headline(h,f,names,0,false,false);if(actual!=expected)throw new Exception(actual+" != "+expected);}
AssertHeadline(Frame(10,10,0),"ルナ国側から開戦！");h.Kind=BattleKind.Edge;AssertHeadline(Frame(10,10,0),"ルナ国、レイン国衝突！");
AssertHeadline(Frame(21,10,1),"ルナ国勝勢！");AssertHeadline(Frame(20,10,1),"ルナ国優勢！");AssertHeadline(Frame(11,10,1),"ルナ国優勢！");AssertHeadline(Frame(10,10,1),"戦力拮抗！");
Console.WriteLine("PASS: opening, edge clash, >2x superiority, 10% advantage, balance; no final-state spoiler");using var game=new CheckGame();game.Run();
class CheckGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 BattleState battle;BattleFrame frame;
 protected override void LoadContent()
 {
  Content.RootDirectory=Path.GetFullPath("FolderVerse/bin/NewspaperOutcomePreview/Content");
  var setup=(WorldSetup)Get("_setup");setup.SetWorld(10);setup.SetCast(1);setup.SetPlacement(1);setup.SelectPlayer(0);
  var batch=new SpriteBatch(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});var font=Content.Load<SpriteFont>("UiFont");
  Set("_spriteBatch",batch);Set("_pixel",pixel);Set("_font",font);Set("_ui",new UiPainter(batch,pixel,font));
  battle=new BattleState(1){Finished=true,Winner=1,Defender=0,TargetNode=0};
  battle.Armies.Add(new BattleArmy{Owner=0});battle.Armies.Add(new BattleArmy{Owner=1});
  for(int i=0;i<10;i++){battle.Units.Add(new BattleUnit{Id=i,Owner=0,Fallen=i<2,Retreated=i>=2});battle.Units.Add(new BattleUnit{Id=100+i,Owner=1});}
  frame=new BattleFrame(1,BattlePhase.Result,Array.Empty<BattleUnitView>(),true,0);battle.Frames.Add(frame);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t)
 {
  foreach(int edition in new[]{0,3,6})
  {
   GraphicsDevice.Clear(new Color(24,43,51));Set("_newspaperEdition",edition);var batch=(SpriteBatch)Get("_spriteBatch");batch.Begin();Set("_newspaperSlideAge",edition==6?.35:1.0);Set("_newspaperPrevious",0);Call("DrawBattleNewspaper",battle,frame);batch.End();
   var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
   using var texture=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);texture.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"edition-{edition}.png"));texture.SaveAsPng(output,texture.Width,texture.Height);
  }
  Console.WriteLine("PASS: partisan, objective and WANTED newspapers rendered, including headline glyphs");Exit();
 }
}
