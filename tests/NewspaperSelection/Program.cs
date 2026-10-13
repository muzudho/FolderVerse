using FolderVerse;
var battles=Enumerable.Range(0,14).Select(i=>new BattleState(i){Finished=true,Winner=0}).ToArray();
foreach(var b in battles){for(int i=0;i<8;i++)b.NewspaperRanks[i]=i+1;b.Armies.Add(new(){Owner=0});b.Armies.Add(new(){Owner=7});}
battles[13].Winner=7;
var picked=NewspaperSelection.Select(battles,1,-1,9);
if(picked.Length!=9 || picked.Distinct().Count()!=9 || picked[0]!=13)throw new Exception("Upset priority or capacity failed");
if(!picked.SequenceEqual(NewspaperSelection.Select(battles,1,-1,9)))throw new Exception("Unstable selection");
if(NewspaperSelection.Select(battles.Take(2).ToArray(),1,-1).Length!=2)throw new Exception("Small issue omitted a battle");
if(battles[0].Winner!=0 || battles[13].Winner!=7)throw new Exception("Selection changed source");
Console.WriteLine("PASS: upset priority, capacity, all small results, stable order and non-destructive selection");
using(var game=new Game1()){
 const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var type=typeof(Game1);
 type.GetField("_robotScenes",flags)!.SetValue(game,battles);
 type.GetMethod("UpdateAutomaticSummary",flags)!.Invoke(game,new object[]{6d,true});
 if((int)type.GetField("_summaryReadPapers",flags)!.GetValue(game)!=1)throw new Exception("DEMO must advance after one issue regardless of battle count");
 Console.WriteLine("PASS: fourteen battles still advance the newspaper in six seconds");
}
var root=Directory.GetCurrentDirectory();
var font=System.Xml.Linq.XDocument.Load(Path.Combine(root,"FolderVerse/Content/UiFont.spritefont"),System.Xml.Linq.LoadOptions.PreserveWhitespace);
var glyphs=new HashSet<char>();
foreach(var region in font.Descendants("CharacterRegion"))for(int c=region.Element("Start")!.Value[0];c<=region.Element("End")!.Value[0];c++)glyphs.Add((char)c);
var summary=File.ReadAllText(Path.Combine(root,"FolderVerse/Game1.BattleSummary.cs"));
var missing=summary.Where(c=>c>127 && !glyphs.Contains(c)).Distinct().ToArray();
if(missing.Length>0)throw new Exception("Missing newspaper glyphs: "+new string(missing));
Console.WriteLine("PASS: all newspaper summary Japanese glyphs are included");
