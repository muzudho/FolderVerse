namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _warCancellationsOpen;
    private int _warCancellationsPage;
    private Rectangle WarCancellationButton=>new(620+NodeInspectorOffset,177,224,42);
    private static readonly Rectangle WarCancellationClose=new(1440,825,160,50),WarCancellationPrevious=new(350,825,100,50),WarCancellationNext=new(470,825,100,50);
    private CancelledWar[] VisibleWarCancellations=>_setup.Campaign.LastCancelledWars.Where(c=>_setup.ControllerOf(c.Ruler)==_setup.TurnPlayer).ToArray();
    private bool WarCancellationInput(bool click,bool escape)
    {
        if(!_warCancellationsOpen){
            if(!click || !_warPlanning || !InspectorVisible || !WarCancellationButton.Contains(_pointer))return false;
            _warCancellationsOpen=true;_warCancellationsPage=0;return true;
        }
        if(escape || click && WarCancellationClose.Contains(_pointer))_warCancellationsOpen=false;
        else if(click && WarCancellationPrevious.Contains(_pointer))_warCancellationsPage=Math.Max(0,_warCancellationsPage-1);
        else if(click && WarCancellationNext.Contains(_pointer))_warCancellationsPage=Math.Min(Math.Max(0,(VisibleWarCancellations.Length-1)/6),_warCancellationsPage+1);
        return true;
    }
    private void DrawWarCancellations()
    {
        if(!_warCancellationsOpen)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.75f);_ui.Box(new(310,185,1330,720),new Color(27,49,62));
        _ui.Text("前回の全体ターン / 開戦中止の記録",new(350,215),.65f,Cream);
        var entries=VisibleWarCancellations;
        if(entries.Length==0)_ui.Text("前回の全体ターンに開戦中止の記録はありません",new(350,320),.5f,Cream);
        for(int i=0;i<6;i++){
            int index=_warCancellationsPage*6+i;if(index>=entries.Length)break;
            var entry=entries[index];int y=295+i*82;
            string title=$"全体ターン {entry.Turn} / 国 #{entry.Ruler+1} / 目標：{_setup.Nodes.DisplayName(_setup.Nodes.All[entry.Target])}";
            _ui.Text(title,new(350,y),Math.Min(.44f,1200/_font.MeasureString(title).X),Accent);
            _ui.Text(entry.Reason,new(350,y+31),Math.Min(.40f,1200/_font.MeasureString(entry.Reason).X),Cream);
        }
        _ui.Button(WarCancellationPrevious,"←",Muted,.5f);_ui.Button(WarCancellationNext,"→",Muted,.5f);
        _ui.Center($"{_warCancellationsPage+1} / {Math.Max(1,(entries.Length+5)/6)}",new(590,825,140,50),.45f,Cream);
        _ui.Button(WarCancellationClose,"閉じる",Accent,.5f);
    }
}
