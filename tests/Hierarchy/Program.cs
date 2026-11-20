using FolderVerse;using Microsoft.Xna.Framework;
int checks=0;
void Check(bool b){checks++;if(!b)throw new Exception("layout check "+checks);}
foreach(var pair in new[]{(1,"1st"),(2,"2nd"),(3,"3rd"),(11,"11th"),(12,"12th"),(13,"13th"),(21,"21st"),(60,"60th")})Check(HierarchyLayout.Ordinal(pair.Item1)==pair.Item2);
foreach(int count in new[]{1,20,60})foreach(int mode in new[]{0,1,2,3}){
 var parents=Enumerable.Range(0,count).Select(i=>i==0||mode==0?-1:mode==1?i-1:mode==2?0:(i-1)/3).ToArray();
 var ranks=Enumerable.Range(1,count).Reverse().ToArray();var area=new Rectangle(54,130,1806,840);
 var boxes=HierarchyLayout.Arrange(parents,ranks,area);Check(boxes.Count==count);
 foreach(int i in Enumerable.Range(0,count)){var b=boxes[i];Check(b.Width==b.Height&&b.Width>6&&area.Contains(b));if(parents[i]>=0)Check(boxes[parents[i]].Bottom<b.Top);else Check(b.Top==area.Top);}
}
Console.WriteLine($"PASS {checks} hierarchy geometry / ordinals");
