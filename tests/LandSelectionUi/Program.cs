using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
using var game=new CheckGame();game.Run();
class CheckGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 int frame;
 protected override void LoadContent()
 {
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  var w=(WorldSetup)Get("_setup");w.SetWorld(1);w.SetCast(2);Call("SetPlacementSeed",3);
  Set("_screen",Enum.Parse(typeof(Game1).GetNestedType("Screen",BindingFlags.NonPublic)!,"PlacementReview"));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t)
 {
  if(frame==1)Call("BeginSelection");
  if(frame==2){var w=(WorldSetup)Get("_setup");((WorldPreview)Get("_world")).HighlightOwner(w,0);Set("_hoveredSlot",0);}
  if(frame==3){var w=(WorldSetup)Get("_setup");var preview=(WorldPreview)Get("_world");preview.HighlightOwner(w,-1);w.SetPlacement(3);preview.ShowSetup(w,true);preview.HighlightOwner(w,0);w.SetTerritoryRights(3);preview.HighlightOwner(w,1);if((int)typeof(WorldPreview).GetField("_highlightCount",F)!.GetValue(preview)! !=0)throw new Exception("Stale owned highlight");}
  base.Draw(t);
  if(++frame==4){Console.WriteLine("PASS: rights review, selection and unclaimed hover");Exit();}
 }
}
