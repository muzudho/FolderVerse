namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _movementOpen;
    private long _escort;
    private int _movementPage;
    private const int MovesPerPage=5;
    private static readonly Rectangle MovementButton=new(328,1018,208,52);
    private static Rectangle MarchButton(int row)=>new(84,430+row*76,770,68);
    private static readonly Rectangle MovesPrevious=new(570,824,130,44),MovesNext=new(714,824,130,44);
    private long AvailableEscort
    {
        get {var node=_setup.Nodes.Current(_setup.PlayerSlot);return node!=null && node.Owner==_setup.PlayerSlot?node.Population.People[0]:0;}
    }
    private bool MovementClick(Point pointer,KeyboardState keyboard)
    {
        if(MovementButton.Contains(pointer)){_movementOpen=!_movementOpen;_populationCell=-1;_escort=Math.Min(_escort,AvailableEscort);_movementPage=0;return true;}
        if(!_movementOpen)return false;
        if(new Rectangle(803,169,58,44).Contains(pointer)){_movementOpen=false;return true;}
        var choices=_setup.Routes.NodeChoices(_setup.PlayerSlot);int pages=Math.Max(1,(choices.Length+MovesPerPage-1)/MovesPerPage);
        if(MovesPrevious.Contains(pointer)){_movementPage=Math.Max(0,_movementPage-1);return true;}
        if(MovesNext.Contains(pointer)){_movementPage=Math.Min(pages-1,_movementPage+1);return true;}
        long step=keyboard.IsKeyDown(Keys.LeftControl)||keyboard.IsKeyDown(Keys.RightControl)?10000:keyboard.IsKeyDown(Keys.LeftShift)||keyboard.IsKeyDown(Keys.RightShift)?1000:100;
        if(new Rectangle(84,339,94,60).Contains(pointer))_escort=Math.Max(0,_escort-step);
        else if(new Rectangle(190,339,94,60).Contains(pointer))_escort=Math.Min(AvailableEscort,_escort+step);
        else if(new Rectangle(305,339,200,60).Contains(pointer))_escort=AvailableEscort;
        else if(new Rectangle(519,339,200,60).Contains(pointer))_escort=0;
        for(int row=0;row<MovesPerPage;row++)if(MarchButton(row).Contains(pointer))
        {
            int index=_movementPage*MovesPerPage+row;if(index>=choices.Length)return true;
            var route=choices[index];int node=_setup.Nodes.At(route.Target,route.Entry).Id;
            _setup.Campaign.Advance(_setup,node,_escort);_escort=Math.Min(_escort,AvailableEscort);_movementPage=0;
            _world.ShowSetup(_setup,true);
            _net.SetCenter(_setup,_setup.Cells[_setup.ConquerorLocations[_setup.PlayerSlot]].Face);
            _populationCell=-1;return true;
        }
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawMovementPanel()
    {
        int ruler=_setup.PlayerSlot;var current=_setup.Nodes.Current(ruler);
        _escort=Math.Min(_escort,AvailableEscort);
        var choices=_setup.Routes.NodeChoices(ruler);int pages=Math.Max(1,(choices.Length+MovesPerPage-1)/MovesPerPage);
        _movementPage=Math.Clamp(_movementPage,0,pages-1);
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Text("征服者の移動",new(84,177),.9f,Cream);_ui.Button(new(803,169,58,44),"×",Muted,.7f);
        string city="現在地　"+(current==null?_setup.Routes.LocationLabel(ruler):_setup.Nodes.Label(current));
        _ui.Text(city,new(84,240),Math.Min(.6f,760/_font.MeasureString(city).X),Cream);
        _ui.Text($"同行する戦闘員　{_escort:N0} / {AvailableEscort:N0} 人",new(84,289),.72f,Color.White);
        _ui.Button(new(84,339,94,60),"−",Muted,.8f);_ui.Button(new(190,339,94,60),"＋",Muted,.8f);
        _ui.Button(new(305,339,200,60),"全員",Muted,.7f);_ui.Button(new(519,339,200,60),"０人",Muted,.7f);
        for(int row=0;row<MovesPerPage;row++)
        {
            int index=_movementPage*MovesPerPage+row;if(index>=choices.Length)break;
            var route=choices[index];var node=_setup.Nodes.At(route.Target,route.Entry);bool own=node.Owner==ruler;
            var rect=MarchButton(row);_ui.Button(rect,"",own?Accent:Muted);
            string label=_setup.Routes.DirectionLabel(current,route)+"へ移動 / "+(node.Cell==current.Cell?"セル内":"隣接セル")+" / "+(own?"自国拠点":"他国拠点")+" / 守備 "+node.Population.People[0].ToString("N0")+" 人";
            _ui.Text(label,new(rect.X+16,rect.Y+5),Math.Min(.53f,738/_font.MeasureString(label).X),Cream);
            string name=_setup.Nodes.Label(node);
            _ui.Text(name,new(rect.X+16,rect.Y+35),Math.Min(.43f,738/_font.MeasureString(name).X),new(186,215,223));
        }
        _ui.Text($"移動先 {choices.Length} 拠点 / {_movementPage+1}/{pages}",new(84,832),.53f,Cream);
        if(pages>1){_ui.Button(MovesPrevious,"←",_movementPage>0?Accent:Muted,.7f);_ui.Button(MovesNext,"→",_movementPage<pages-1?Accent:Muted,.7f);}
        _ui.Text("100人ずつ / Shift：1,000人 / Ctrl：10,000人",new(84,878),.49f,new(180,209,219));
        string status=choices.Length==0?"交通路でつながる移動先がない":"交通路でつながる拠点へ１ターンで移動";
        _ui.Text(status,new(84,924),.47f,new(180,209,219));
    }
}
