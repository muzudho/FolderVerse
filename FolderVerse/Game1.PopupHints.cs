namespace FolderVerse;
using System;
using System.Collections.Generic;

public partial class Game1
{
    // Hints share one foreground pass, after the fixed screen-name plaque.
    private readonly List<Action> _popupHints=new();
    private void QueuePopupHint(Action draw)=>_popupHints.Add(draw);
    private void DrawPopupHints(){var pending=_popupHints.ToArray();_popupHints.Clear();foreach(var draw in pending)draw();}
}
