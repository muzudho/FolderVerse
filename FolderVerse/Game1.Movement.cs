namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _movementOpen;
    private long _escort;
    private static readonly Rectangle MovementButton=new(328,1018,208,52);
    private static Rectangle MarchButton(int direction)=>new(84,445+direction*94,770,76);
    private long AvailableEscort
    {
        get {int cell=_setup.ConquerorLocations[_setup.PlayerSlot];return _setup.Owners[cell]==_setup.PlayerSlot && _setup.TerritoryCounts[_setup.PlayerSlot]>0?_setup.Population.Cells[cell].People[0]:0;}
    }
    private bool MovementClick(Point pointer,KeyboardState keyboard)
    {
        if(MovementButton.Contains(pointer)){_movementOpen=!_movementOpen;_populationCell=-1;_escort=Math.Min(_escort,AvailableEscort);return true;}
        if(!_movementOpen)return false;
        if(new Rectangle(803,169,58,44).Contains(pointer)){_movementOpen=false;return true;}
        long step=keyboard.IsKeyDown(Keys.LeftControl)||keyboard.IsKeyDown(Keys.RightControl)?10000:keyboard.IsKeyDown(Keys.LeftShift)||keyboard.IsKeyDown(Keys.RightShift)?1000:100;
        if(new Rectangle(84,339,94,60).Contains(pointer))_escort=Math.Max(0,_escort-step);
        else if(new Rectangle(190,339,94,60).Contains(pointer))_escort=Math.Min(AvailableEscort,_escort+step);
        else if(new Rectangle(305,339,200,60).Contains(pointer))_escort=AvailableEscort;
        else if(new Rectangle(519,339,200,60).Contains(pointer))_escort=0;
        for(int d=0;d<4;d++)if(MarchButton(d).Contains(pointer))
        {
            if(_setup.Routes.ForRuler(_setup.PlayerSlot,d)==null)return true;
            _setup.Campaign.Advance(_setup,d,_escort);_escort=Math.Min(_escort,AvailableEscort);
            _world.ShowSetup(_setup,true);
            _net.SetCenter(_setup,_setup.Cells[_setup.ConquerorLocations[_setup.PlayerSlot]].Face);
            _populationCell=-1;return true;
        }
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawMovementPanel()
    {
        int ruler=_setup.PlayerSlot,source=_setup.ConquerorLocations[ruler];
        _escort=Math.Min(_escort,AvailableEscort);
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Text("征服者の移動",new(84,177),.9f,Cream);_ui.Button(new(803,169,58,44),"×",Muted,.7f);
        string city="現在地　"+_setup.Routes.LocationLabel(ruler);
        _ui.Text(city,new(84,240),Math.Min(.6f,760/_font.MeasureString(city).X),Cream);
        _ui.Text($"同行する戦闘員　{_escort:N0} / {AvailableEscort:N0} 人",new(84,289),.72f,Color.White);
        _ui.Button(new(84,339,94,60),"−",Muted,.8f);_ui.Button(new(190,339,94,60),"＋",Muted,.8f);
        _ui.Button(new(305,339,200,60),"全員",Muted,.7f);_ui.Button(new(519,339,200,60),"０人",Muted,.7f);
        for(int d=0;d<4;d++)
        {
            int target=_setup.Population.Neighbor(_setup,source,d);bool own=_setup.Owners[target]==ruler;
            var route=_setup.Routes.ForRuler(ruler,d);bool open=route!=null;
            var rect=MarchButton(d);_ui.Button(rect,"",open?(own?Accent:Muted):new Color(37,52,60));
            string label=PopulationDirections[d]+(open?"へ移動 / "+(own?"自国":"他国")+" / 守備 "+_setup.Population.Cells[target].People[0].ToString("N0")+" 人":"：経路なし / 上陸・通行できない");
            _ui.Text(label,new(rect.X+16,rect.Y+8),.58f,Cream);
            string name=open?WorldCoordinates.Label(_setup,target)+" / "+_setup.Routes.PointName(source,route.Exit)+" → "+_setup.Routes.PointName(target,route.Entry):WorldCoordinates.Label(_setup,target);
            _ui.Text(name,new(rect.X+16,rect.Y+43),Math.Min(.43f,730/_font.MeasureString(name).X),new(186,215,223));
        }
        _ui.Text("移動するとターン終了。敵も同時に行動する。",new(84,837),.53f,Cream);
        _ui.Text("100人ずつ / Shift：1,000人 / Ctrl：10,000人",new(84,878),.49f,new(180,209,219));
        string status=_setup.TerritoryCounts[ruler]==0?"放浪者：募集・征服はできず、移動できる":"戦闘員は現在地が自国のときに連れていける";
        _ui.Text(status,new(84,924),.47f,new(180,209,219));
    }
}
