namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _movementOpen;
    private long _escort;
    private int _movementPage;
    private bool _routesBeforeMovement;
    private int _selectedMoveNode=-1;
    private const int MovesPerPage=5;
    private static readonly Rectangle MovementButton=new(328,1018,208,52);
    private static Rectangle MarchButton(int row)=>new(84,430+row*76,770,68);
    private static readonly Rectangle MovesPrevious=new(570,824,130,44),MovesNext=new(714,824,130,44);
    private static readonly Rectangle ConfirmMoveButton=new(570,914,274,48);
    private void OpenMovement()
    {
        _dragging=false;
        _routesBeforeMovement=_net.ShowRoutes;_net.ShowRoutes=true;_movementOpen=true;
        _populationCell=-1;_escort=Math.Min(_escort,AvailableEscort);_movementPage=0;_selectedMoveNode=-1;
        _net.MovementTargets.Clear();
        _net.MovementTargets.UnionWith(_setup.Routes.NodeChoices(_setup.PlayerSlot).Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id));
        _net.SelectedTarget=-1;
    }
    private void CloseMovement()
    {
        if(!_movementOpen)return;
        _movementOpen=false;_net.ShowRoutes=_routesBeforeMovement;
        _selectedMoveNode=-1;_net.SelectedTarget=-1;_net.MovementTargets.Clear();
    }
    private void SelectMoveNode(int node)
    {
        if(!_net.MovementTargets.Contains(node))return;
        _selectedMoveNode=node;_net.SelectedTarget=node;
        var choices=_setup.Routes.NodeChoices(_setup.PlayerSlot);
        _movementPage=Array.FindIndex(choices,r=>_setup.Nodes.At(r.Target,r.Entry).Id==node)/MovesPerPage;
    }
    private long AvailableEscort
    {
        get {var node=_setup.Nodes.Current(_setup.PlayerSlot);return node!=null && _setup.Relations.Allied(node.Owner,_setup.PlayerSlot)?node.Population.People[0]:0;}
    }
    private bool MovementClick(Point pointer,KeyboardState keyboard)
    {
        if(MovementButton.Contains(pointer)){if(_movementOpen)CloseMovement();else OpenMovement();return true;}
        if(!_movementOpen)return false;
        if(new Rectangle(803,169,58,44).Contains(pointer)){CloseMovement();return true;}
        if(PopulationTurnButton.Contains(pointer))return true;
        if(ConfirmMoveButton.Contains(pointer))
        {
            if(_selectedMoveNode<0 || _setup.Routes.ToNode(_setup.PlayerSlot,_selectedMoveNode)==null)return true;
            _setup.Campaign.Advance(_setup,_selectedMoveNode,_escort);_escort=Math.Min(_escort,AvailableEscort);
            CloseMovement();_world.ShowSetup(_setup,true);
            _net.SetCenter(_setup,_setup.Cells[_setup.ConquerorLocations[_setup.PlayerSlot]].Face);
            _populationCell=-1;OpenBattle();return true;
        }
        if(NetPanel.Contains(pointer) && !keyboard.IsKeyDown(Keys.Space))
        {
            int node=_statusGlobe?HitGlobeNode(pointer,_net.MovementTargets):_net.HitNode(_setup,NetPanel,pointer,_net.MovementTargets);
            if(node>=0){SelectMoveNode(node);return true;}
        }
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
            var route=choices[index];SelectMoveNode(_setup.Nodes.At(route.Target,route.Entry).Id);return true;
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
            var route=choices[index];var node=_setup.Nodes.At(route.Target,route.Entry);
            string relationship=node.Owner<0?"未征服の節点":node.Owner==ruler?"自国節点":_setup.Relations.Allied(node.Owner,ruler)?"手下の節点":"他国節点";
            var rect=MarchButton(row);_ui.Button(rect,"",node.Id==_selectedMoveNode?Accent:Muted);
            string label=(node.Id==_selectedMoveNode?"選択：":"")+_setup.Routes.DirectionLabel(current,route)+" / "+(node.Cell==current.Cell?"セル内":"隣接セル")+" / "+relationship+" / 守備 "+node.Population.People[0].ToString("N0")+" 人";
            _ui.Text(label,new(rect.X+16,rect.Y+5),Math.Min(.53f,738/_font.MeasureString(label).X),Cream);
            string name=_setup.Nodes.Label(node);
            _ui.Text(name,new(rect.X+16,rect.Y+35),Math.Min(.43f,738/_font.MeasureString(name).X),new(186,215,223));
        }
        _ui.Text($"移動先 {choices.Length} 節点 / {_movementPage+1}/{pages}",new(84,832),.53f,Cream);
        if(pages>1){_ui.Button(MovesPrevious,"←",_movementPage>0?Accent:Muted,.7f);_ui.Button(MovesNext,"→",_movementPage<pages-1?Accent:Muted,.7f);}
        _ui.Text("100人ずつ / Shift：1,000人 / Ctrl：10,000人",new(84,878),.49f,new(180,209,219));
        string status=choices.Length==0?"交通路でつながる移動先がない":_selectedMoveNode<0?"点滅する節点をクリックして選択":$"選択：Node {_selectedMoveNode+1} / 確定で移動";
        _ui.Text(status,new(84,926),Math.Min(.47f,465/_font.MeasureString(status).X),new(180,209,219));
        _ui.Button(ConfirmMoveButton,"確定",_selectedMoveNode>=0?Accent:Muted,.7f);
    }
}
