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
        const int bottomMargin=40; // Includes the paper shadow and extrusion.
        if(_pointer.Y>=0 && _pointer.Y<canvasHeight/2)
            bounds.Y=canvasHeight-bottomMargin-bounds.Height;
        return bounds;
    }
    private void QueuePopupHint(Action draw)=>_popupHints.Add(draw);
    private void DrawPopupHints(){var pending=_popupHints.ToArray();_popupHints.Clear();foreach(var draw in pending)draw();}
}
