namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    // Hints share one foreground pass, after the fixed screen-name plaque.
    private readonly List<Action> _popupHints=new();
    private Rectangle PlacePopupHint(Rectangle bounds)
    {
        const int canvasHeight=1080;
        const int bottomMargin=40;
        if(_pointer.Y>=0 && _pointer.Y<canvasHeight/2)
            bounds.Y=canvasHeight-bottomMargin-bounds.Height;
        return bounds;
    }
    private void QueuePopupHint(Action draw)=>_popupHints.Add(draw);
    private void DrawPopupHints(){var pending=_popupHints.ToArray();_popupHints.Clear();foreach(var draw in pending)draw();}

    private static Rectangle RulebookHintArea=>new(500,4,1040,128);
    private void DrawRulebookHint(string title,string description)
    {
        var r=PlacePopupHint(RulebookHintArea);
        QueuePopupHint(()=>{
            DrawPopupHintHeading(r,title);
            DrawPopupHintLine(description,r,65,.38f);
        });
    }

    private void DrawPopupHintHeading(Rectangle paper,string title)
    {
        DrawPopupHintPaper(paper);
        DrawPopupHintLine(title,paper,20,.46f);
        var content=PopupHintTextBounds(paper);
        _ui.Box(new(content.X,paper.Y+51,content.Width,1),RulebookInk*.5f);
    }
}
