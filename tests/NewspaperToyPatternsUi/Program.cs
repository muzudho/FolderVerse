using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new PatternGame();game.Run();
sealed class PatternGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  var atlas=(Texture2D)Call("LoadNewspaperToyAtlas","toys.png");Set("_newspaperToys",atlas);var source=new Rectangle(0,0,atlas.Width/5,atlas.Height/4);var original=new Color[source.Width*source.Height];atlas.GetData(0,source,original,0,original.Length);float scale=82f/Math.Max(source.Width,source.Height);
  for(int pattern=1;pattern<5;pattern++){
   w.SetCountryAppearance(0,1,10,pattern);var texture=(Texture2D)Call("NewspaperPatternedToy",0,0,82,atlas,source,scale);Check(ReferenceEquals(texture,Call("NewspaperPatternedToy",0,0,82,atlas,source,scale)),"Unchanged pattern reuses GPU texture");var pixels=new Color[original.Length];texture.GetData(pixels);int primary=0,secondary=0;
   for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++){
    int i=y*source.Width+x;bool second=WorldSetup.CountryPatternSecondary(pattern,(int)(x*scale/2),(int)(y*scale/2));var ink=second?WorldSetup.Colors[w.CountrySecondaryColor(0)]:w.OwnerColor(0);var c=original[i];Check(pixels[i]==new Color(c.R*ink.R/255,c.G*ink.G/255,c.B*ink.B/255,(int)c.A),"Pattern preserves alpha, shading and exact national colors");if(c.A>200){if(second)secondary++;else primary++;}
   }
   Check(primary>0 && secondary>0,"Both national colors cover opaque toy pixels");w.SetCountryAppearance(0,2,10,pattern);var changed=(Texture2D)Call("NewspaperPatternedToy",0,0,82,atlas,source,scale);Check(!ReferenceEquals(texture,changed) && !texture.IsDisposed,"Appearance change preserves queued old-page texture until a safe frame boundary");Call("BeginNewspaperToyPatternFrame");Call("BeginNewspaperToyPatternFrame");Call("BeginNewspaperToyPatternFrame");Check(texture.IsDisposed && changed.IsDisposed,"Unused appearance textures retire between frames");
  }
  w.SetCountryAppearance(0,1,10,0);Check(Call("NewspaperPatternedToy",0,0,82,atlas,source,scale)==null,"Solid country retains original tint path");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  Call("BeginNewspaperToyPatternFrame");
  if(frame<5){
   GraphicsDevice.Clear(new Color(255,244,211));var batch=(SpriteBatch)Get("_spriteBatch");var font=(SpriteFont)Get("_font");batch.Begin();batch.DrawString(font,"Newspaper toys / "+WorldSetup.CountryPatternNames[frame],new Vector2(60,25),Color.Black,0,Vector2.Zero,.8f,SpriteEffects.None,0);
   for(int index=0;index<28;index++){
    w.SetCountryAppearance(0,index%WorldSetup.Colors.Length,(index+10)%WorldSetup.Colors.Length,frame);w.ColorIndices[0]=index;Set("_newspaperEdition",0);
    int x=100+index%7*250,y=140+index/7*215;Call("DrawNewspaperToy",0,new Vector2(x,y),82);Call("DrawNewspaperToy",0,new Vector2(x+95,y+55),48);batch.DrawString(font,index.ToString(),new Vector2(x-20,y+100),Color.Black,0,Vector2.Zero,.45f,SpriteEffects.None,0);
   }
   batch.End();
  }else{
   for(int owner=0;owner<5;owner++)w.SetCountryAppearance(owner,owner+1,owner+11,owner);
   var b=new BattleState(42){Kind=BattleKind.Node,TargetNode=w.Nodes.Current(0).Id,Defender=1,Finished=true,Winner=0};for(int owner=0;owner<5;owner++){b.Armies.Add(new(){Owner=owner,Side=owner});b.Units.Add(new(){Id=owner+1,Owner=owner,Position=new(2+owner,3)});}b.Capture(BattlePhase.Result);
   Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",F)!.FieldType,"Battle"));Set("_robotScenes",new[]{b});Set("_robotPlayback",new RobotBattlePlayback(new[]{b}));Set("_robotPlaying",false);Set("_robotSummary",false);Set("_lastCutInPhase","戦闘フェーズ");Set("_cutInStarted",-10d);base.Draw(t);
  }
  var p=GraphicsDevice.PresentationParameters;var data=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(data);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(data);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"newspaper-patterns-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==5){Call("DisposeNewspaperToyPatterns");Check(((System.Collections.IDictionary)Get("_newspaperToyPatterns")).Count==0,"Cached textures dispose at cleanup");Console.WriteLine("PASS: all four patterns, exact two-color shading and alpha, cache reuse/invalidation/disposal, all 28 toys at 82/48 pixels and integrated battle newspaper rendering.");Exit();}
 }
}
