using FolderVerse;using Microsoft.Xna.Framework;
var w=new WorldSetup();w.SetParticipantCounts(1,3);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.Relations.SetSuperior(1,0);w.Relations.SetSuperior(2,1);int checks=0;
void Check(bool b){checks++;if(!b)throw new Exception("flag check "+checks);}
foreach(int shape in Enumerable.Range(0,4))foreach(int childShape in Enumerable.Range(0,4)){
 w.SetCountryFlagShape(0,shape);w.SetCountryFlagShape(2,childShape);
 var pixels=new Dictionary<Point,Color>();foreach(var (b,c) in CountryFlagArt.GeographicPatches(w,2))for(int y=b.Top;y<b.Bottom;y++)for(int x=b.Left;x<b.Right;x++)pixels[new(x,y)]=c;
 for(int y=0;y<24;y++)for(int x=0;x<36;x++){
  bool inside=CountryFlagArt.Contains(shape,x,y);Check(pixels.ContainsKey(new(x,y))==inside);if(!inside)continue;
  int start=shape==3?0:18;bool inset=y>=12&&x>=start&&x<start+18&&CountryFlagArt.Contains(childShape,(x-start)*2,(y-12)*2);
  Check(pixels[new(x,y)]==(inset?CountryFlagArt.Pixel(w,2,(x-start)*2,(y-12)*2):CountryFlagArt.Pixel(w,0,x,y)));
 }
 Check(CountryFlagArt.GeographicBoss(w,2)==0);
}
w.Relations.SetSuperior(1,-1);Check(CountryFlagArt.GeographicBoss(w,2)==1);Check(CountryFlagArt.GeographicBoss(w,1)==1);
Console.WriteLine($"PASS {checks} geographic flag pixels / all 16 shape pairs / live root change");
