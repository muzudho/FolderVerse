using FolderVerse;
using Microsoft.Xna.Framework;
var ui=new UiPainter();var original=new Color(220,40,70);var area=new Rectangle(0,0,100,100);
RobotDamageArt.Draw(ui,area,area,original);
if(ui.Pixels[10,10]!=original || ui.Pixels[90,90]!=RobotDamageArt.Gray(original))throw new Exception("Expected original upper-left and 80% gray lower-right");
int gray=0;foreach(var pixel in ui.Pixels)if(pixel==RobotDamageArt.Gray(original))gray++;
int clean=0,soft=0;foreach(var pixel in ui.Pixels){if(pixel==original)clean++;else if(pixel!=RobotDamageArt.Gray(original))soft++;}
if(Math.Abs(gray-clean)>700 || gray<3500 || clean<3500 || soft<200)throw new Exception("Expected roughly half dirty, half clean with a soft edge");
var first=(Color[,])ui.Pixels.Clone();RobotDamageArt.Draw(ui,area,area,original);
for(int y=0;y<100;y++)for(int x=0;x<100;x++)if(first[x,y]!=ui.Pixels[x,y])throw new Exception("Dirt must not flicker between frames");
int reversals=0;for(int y=1;y<100;y++)if(RobotDamageArt.SplitX(area,y)>RobotDamageArt.SplitX(area,y-1))reversals++;
if(reversals<3)throw new Exception("Boundary must be visibly irregular rather than a straight diagonal");
using(var file=new BinaryWriter(File.Create(Path.Combine(AppContext.BaseDirectory,"damage-edge.bmp")))){
file.Write((ushort)0x4d42);file.Write(54+30000);file.Write(0);file.Write(54);file.Write(40);file.Write(100);file.Write(100);file.Write((ushort)1);file.Write((ushort)24);file.Write(0);file.Write(30000);file.Write(0);file.Write(0);file.Write(0);file.Write(0);
for(int y=99;y>=0;y--)for(int x=0;x<100;x++){var c=ui.Pixels[x,y];file.Write(c.B);file.Write(c.G);file.Write(c.R);}
}
RobotDamageArt.Draw(ui,area,area,original,.4f);
if(ui.Pixels[10,10]!=original*.4f || ui.Pixels[90,90]!=RobotDamageArt.Gray(original)*.4f)throw new Exception("Opacity must affect both halves equally");
Console.WriteLine("PASS: country color, 80% gray, rough feathered half coverage, stable dirt and disabled opacity.");
namespace FolderVerse { public sealed class UiPainter {public Color[,] Pixels=new Color[100,100]; public void Box(Rectangle r,Color c){for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++)Pixels[x,y]=c;}} }
