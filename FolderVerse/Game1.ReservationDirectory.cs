namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _reservationDirectory;
    private (bool Symbols,bool Equator,bool Robots,bool Batteries,bool Countries,bool Routes,bool Others,bool GlobeCountries) _directoryBefore;
    private readonly List<(Rectangle Bounds,int Node)> _directoryHits=new();
    private static readonly Rectangle ReservationDirectoryButton=new(632,70,60,60),ReservationDirectoryClose=new(700,82,40,40);
    private static readonly Color[] DirectoryColors={new(187,43,48),new(39,128,69),new(193,145,12),new(174,180,187)};
    private static readonly string[] DirectoryLabels={"出撃予約","守備専用予約","工場作業員予約","廃棄予約"};
    private int[] ReservationDirectoryCounts(int node)
    {
        var robots=_setup.Robots.Nodes[node].Robots;
        return new[]{robots.Count(r=>_setup.Robots.CombatMarchReserved(node,r)),robots.Count(r=>r.DefenseOnly),robots.Count(r=>r.FactoryWorker),robots.Count(r=>r.Disposal)};
    }
    private void OpenReservationDirectory()
    {
        if(_reservationDirectory)return;
        ResetInspector();_mapLayerListOpen=_trafficLayerListOpen=_mapDragListOpen=false;
        _directoryBefore=(_net.ShowMapSymbols,_net.ShowEquator,_net.ShowRobots,_net.ShowBatteries,_net.ShowCountryColors,_net.ShowRoutes,_net.ShowOtherCountries,_world.CountryColors);
        _reservationDirectory=_net.DirectoryOnly=true;
        _net.ShowMapSymbols=_net.ShowEquator=_net.ShowRobots=_net.ShowBatteries=_net.ShowCountryColors=_world.CountryColors=false;
        _net.ShowRoutes=_net.ShowOtherCountries=true;
        _net.ClearGlobeRobotBadges();_world.ShowSetup(_setup,true);
    }
    private void CloseReservationDirectory()
    {
        if(!_reservationDirectory)return;
        _reservationDirectory=_net.DirectoryOnly=false;_directoryHits.Clear();
        (_net.ShowMapSymbols,_net.ShowEquator,_net.ShowRobots,_net.ShowBatteries,_net.ShowCountryColors,_net.ShowRoutes,_net.ShowOtherCountries,_world.CountryColors)=_directoryBefore;
        _world.ShowSetup(_setup,true);
    }
    private bool ReservationDirectoryInput(Point p,bool escape)
    {
        if(!MapMarkerMenuAvailable || MapTransitionActive || _net.IsAnimating)return false;
        if(escape){if(!_reservationDirectory)return false;CloseReservationDirectory();return true;}
        if(ReservationDirectoryButton.Contains(p)){
            if(!_reservationDirectory)OpenReservationDirectory();
            return true;
        }
        if(!_reservationDirectory)return false;
        if(ReservationDirectoryClose.Contains(p)){CloseReservationDirectory();return true;}
        foreach(var hit in _directoryHits.AsEnumerable().Reverse())if(hit.Bounds.Contains(p)){OpenNodeInspector(hit.Node);return true;}
        return false;
    }
    private float DirectoryButtonPulse=>_reservationDirectory ? .16f+.12f*(1+MathF.Sin(_animationTime*MathHelper.TwoPi/2.4f)):0;
    private void DrawReservationDirectoryButton()
    {
        var r=ReservationDirectoryButton;
        var color=Color.Lerp(Accent,Color.White,DirectoryButtonPulse);
        if(r.Contains(_pointer))color=Color.Lerp(color,Color.White,.12f);
        var shadow=r;shadow.Y+=4;PrimaryRoundBox(shadow,new Color(7,23,35));PrimaryRoundBox(r,color);
        var ink=UiPainter.ContrastInk(color);
        _ui.Box(new(r.X+27,r.Y+10,5,41),ink);
        for(int i=0;i<3;i++){
            int y=r.Y+12+i*11;_ui.Box(new(r.X+10,y,35,7),ink);
            for(int x=0;x<6;x++)_ui.Box(new(r.X+45+x,y+x/2,1,7-x),ink);
        }
        if(_reservationDirectory)_ui.Button(ReservationDirectoryClose,"×",Muted,.55f);
        if(r.Contains(_pointer)){
            _ui.Box(new(390,136,360,30),new Color(7,23,35));
            _ui.Center("予約の掲示板 / 地点のロボット数",new(390,136,360,30),.32f,Cream);
        }
    }
    private void DrawDirectoryRoleIcon(int kind,Rectangle r,Color ink)
    {
        Vector2 P(float x,float y)=>new(r.X+x*r.Width,r.Y+y*r.Height);
        void Line(float x,float y,float xx,float yy,float width=2){var a=P(x,y);var b=P(xx,yy);var d=b-a;_ui.Tile((a+b)/2,new(d.Length(),width),MathF.Atan2(d.Y,d.X),ink);}
        if(kind==0){
            Line(.1f,.9f,.88f,.08f,3);Line(.9f,.9f,.12f,.08f,3);
            Line(.04f,.64f,.36f,.94f);Line(.64f,.94f,.96f,.64f);
        }else if(kind==1){
            Line(.12f,.08f,.88f,.08f);Line(.12f,.08f,.18f,.62f);Line(.88f,.08f,.82f,.62f);
            Line(.18f,.62f,.5f,.96f);Line(.82f,.62f,.5f,.96f);Line(.5f,.20f,.5f,.74f);
            Line(.30f,.43f,.70f,.43f);
        }else if(kind==2){
            Line(.13f,.88f,.65f,.35f,4);Line(.65f,.35f,.48f,.16f,3);Line(.48f,.16f,.63f,.02f,3);
            Line(.65f,.35f,.86f,.50f,3);Line(.86f,.50f,.99f,.32f,3);
        }else{
            Line(.06f,.28f,.94f,.28f,3);Line(.12f,.28f,.24f,.90f);Line(.88f,.28f,.76f,.90f);Line(.24f,.90f,.76f,.90f);
            Line(.29f,.28f,.40f,.06f);Line(.40f,.06f,.60f,.06f);Line(.60f,.06f,.71f,.28f);
            Line(.36f,.43f,.40f,.77f);Line(.64f,.43f,.60f,.77f);Line(.24f,.62f,.76f,.62f,1);
        }
    }
    private void DrawReservationDirectory()
    {
        _directoryHits.Clear();
        if(MapTransitionActive || _net.IsAnimating)return;
        var map=MapViewBounds;
        foreach(var node in _setup.Nodes.All){
            var counts=ReservationDirectoryCounts(node.Id);int rows=counts.Count(n=>n>0);
            if(rows==0 || !OperationPosition(node.Id,out var at))continue;
            const int width=70,height=24;
            int total=rows*(height+3)+8;
            int x=Math.Clamp((int)at.X+9,map.Left+2,map.Right-width-10);
            int y=Math.Clamp((int)at.Y-total/2,map.Top+2,map.Bottom-total-2);
            var whole=new Rectangle(x-3,y-3,width+11,total+6);
            // A short connector keeps the stack anchored to its geographic node.
            var foot=new Vector2(x+width/2,y+total);var d=foot-at;
            _ui.Tile((at+foot)/2,new(d.Length(),2),MathF.Atan2(d.Y,d.X),new Color(12,25,32));
            _ui.Box(new(x+width/2-2,y,4,total),new Color(73,51,35));
            int row=0;
            for(int kind=0;kind<4;kind++)if(counts[kind]>0){
                var board=new Rectangle(x,y+row++*(height+3),width,height);var shadow=board;shadow.Offset(2,2);
                _ui.Box(shadow,new Color(7,23,35));_ui.Box(board,DirectoryColors[kind]);
                for(int tip=0;tip<8;tip++)_ui.Box(new(board.Right+tip,board.Top+tip,1,Math.Max(1,height-tip*2)),DirectoryColors[kind]);
                var ink=kind==3?new Color(178,20,30):Color.White;
                DrawDirectoryRoleIcon(kind,new(board.X+5,board.Y+3,20,18),ink);
                _ui.Center(counts[kind].ToString(),new(board.X+29,board.Y,board.Width-29,board.Height),.43f,ink);
            }
            _directoryHits.Add((whole,node.Id));
        }
        // Fixed legend keeps color meaning visible even when a category is zero everywhere.
        _ui.Box(new(990,242,710,88),new Color(7,23,35,240));
        for(int i=0;i<4;i++){
            var r=new Rectangle(1000+i*170,250,162,30);_ui.Box(r,DirectoryColors[i]);
            var ink=i==3?new Color(178,20,30):Color.White;
            DrawDirectoryRoleIcon(i,new(r.X+5,r.Y+5,20,20),ink);
            _ui.Center(DirectoryLabels[i],new(r.X+28,r.Y,r.Width-28,r.Height),.30f,ink);
        }
        _ui.Text("地点のロボット数 / ０は非表示 / 平和移動・ポケット・迎撃専用は集計対象外",new(1000,292),.25f,Cream);
        foreach(var hit in _directoryHits.AsEnumerable().Reverse())if(hit.Bounds.Contains(_pointer)){
            string label=_setup.Nodes.Label(_setup.Nodes.All[hit.Node])+" / クリックで地点を開く";
            _ui.Box(new(990,342,850,36),new Color(7,23,35));
            _ui.Center(label,new(990,342,850,36),Math.Min(.32f,830/_font.MeasureString(label).X),Cream);break;
        }
    }
}
