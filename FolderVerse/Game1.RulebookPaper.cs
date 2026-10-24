namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Color RulebookPaper=new(235,222,187),RulebookInk=new(67,57,43);
    private void DrawRulebookPaper(Rectangle r)
    {
        _ui.Box(new(r.X+5,r.Y+6,r.Width,r.Height),Color.Black*.25f);
        _ui.Box(r,RulebookPaper);
        // Subtle paper grain, with the same single ink used for every printed mark.
        for(int y=r.Y+3;y<r.Bottom-3;y+=5)
            for(int x=r.X+3+(y%7);x<r.Right-3;x+=13)
                _ui.Box(new(x,y,2,1),RulebookInk*.035f);
        _ui.Box(new(r.X+10,r.Y+10,r.Width-20,1),RulebookInk*.65f);
        _ui.Box(new(r.X+10,r.Bottom-11,r.Width-20,1),RulebookInk*.65f);
        _ui.Box(new(r.X+10,r.Y+10,1,r.Height-20),RulebookInk*.65f);
        _ui.Box(new(r.Right-11,r.Y+10,1,r.Height-20),RulebookInk*.65f);
    }
    private static Rectangle RulebookHintArea=>new(500,4,1040,128);
    private void DrawRulebookHint(string title,string description)
    {
        var r=RulebookHintArea;DrawRulebookPaper(r);
        _ui.Text(title,new(r.X+24,r.Y+17),.46f,RulebookInk);
        _ui.Box(new(r.X+24,r.Y+51,r.Width-48,1),RulebookInk*.5f);
        _ui.Text(description,new(r.X+24,r.Y+65),System.Math.Min(.38f,(r.Width-48)/System.Math.Max(1,_font.MeasureString(description).X)),RulebookInk);
    }
}
