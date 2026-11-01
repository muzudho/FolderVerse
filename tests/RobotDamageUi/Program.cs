using FolderVerse;
using Microsoft.Xna.Framework;
var ui=new UiPainter();var original=new Color(220,40,70);var area=new Rectangle(0,0,100,100);
RobotDamageArt.Draw(ui,area,area,original);
if(ui.Pixels[10,10]!=original || ui.Pixels[90,90]!=RobotDamageArt.Gray(original))throw new Exception("Expected original upper-left and 80% gray lower-right");
int gray=0;foreach(var pixel in ui.Pixels)if(pixel==RobotDamageArt.Gray(original))gray++;
if(Math.Abs(gray-5000)>100)throw new Exception("Expected half coverage");
RobotDamageArt.Draw(ui,area,area,original,.4f);
if(ui.Pixels[10,10]!=original*.4f || ui.Pixels[90,90]!=RobotDamageArt.Gray(original)*.4f)throw new Exception("Opacity must affect both halves equally");
Console.WriteLine("PASS: country color, 80% gray, diagonal half coverage and disabled opacity.");
namespace FolderVerse { public sealed class UiPainter {public Color[,] Pixels=new Color[100,100]; public void Box(Rectangle r,Color c){for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++)Pixels[x,y]=c;}} }
