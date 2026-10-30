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
    private static readonly Color[] DirectoryColors={new(187,43,48),new(174,218,76),new(39,128,69),new(193,145,12),new(135,218,242),new(174,180,187)};
    private static readonly string[] DirectoryLabels={"出撃予約","迎撃予約","守備専用予約","工場作業員予約","輸送予約","廃棄予約"};
    private int[] ReservationDirectoryCounts(int node)
    {
        var robots=_setup.Robots.Nodes[node].Robots;
        // Transport is measured in thirds: union per robot avoids counting one part twice.
        int parts=_setup.Robots.DirectDelivery.Orders.Where(o=>o.Source==node).GroupBy(o=>o.RobotId)
            .Sum(g=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(p=>(g.Aggregate((RobotParts)0,(mask,o)=>mask|o.Part)&p)!=0));
        return new[]{robots.Count(r=>_setup.Robots.CombatMarchReserved(node,r)),robots.Count(r=>r.InterceptOnly),robots.Count(r=>r.DefenseOnly),robots.Count(r=>r.FactoryWorker),parts,robots.Count(r=>r.Disposal)};
    }
    private static string DirectoryTransportAmount(int thirds)=>thirds%3==0?(thirds/3).ToString():thirds<3?$"{thirds}/3":$"{thirds/3} + {thirds%3}/3";
    private static Color DirectoryInk(int kind)=>kind==5?new Color(178,20,30):kind is 1 or 4?Color.Black:Color.White;
    private bool DirectorySeaTransport(int node)=>!_setup.Routes.Land(_setup.Nodes.All[node].Cell,_setup.Nodes.All[node].Center);
    private string DirectoryTurnStatus(int owner)
    {
        if(owner<0 || owner>=_setup.ActiveCount || !_setup.Relations.CanAct(owner))return "";
        int controller=_setup.ControllerOf(owner);
        if(controller>=0 && _setup.Campaign.Submitted.Contains(controller))return "DONE";
        return controller>=0 && controller==_setup.TurnPlayer?"TURN":"WAIT";
    }
    private static string DirectoryTurnHint(string status)=>status switch {"TURN"=>"現在の手番","DONE"=>"行動の入力完了","WAIT"=>"手番待ち / 行動未確定",_=>""};
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
    private void DrawDirectoryRoleIcon(int kind,Rectangle r,Color ink,bool sea=false)
    {
        Vector2 P(float x,float y)=>new(r.X+x*r.Width,r.Y+y*r.Height);
        void Line(float x,float y,float xx,float yy,float width=2){var a=P(x,y);var b=P(xx,yy);var d=b-a;_ui.Tile((a+b)/2,new(d.Length(),width),MathF.Atan2(d.Y,d.X),ink);}
        if(kind==0){
            Line(.1f,.9f,.88f,.08f,3);Line(.9f,.9f,.12f,.08f,3);
            Line(.04f,.64f,.36f,.94f);Line(.64f,.94f,.96f,.64f);
        }else if(kind is 1 or 2){
            Line(.12f,.08f,.88f,.08f);Line(.12f,.08f,.18f,.62f);Line(.88f,.08f,.82f,.62f);
            Line(.18f,.62f,.5f,.96f);Line(.82f,.62f,.5f,.96f);Line(.5f,.20f,.5f,.74f);
            if(kind==2)Line(.30f,.43f,.70f,.43f);
            else {Line(.61f,.86f,.96f,.25f,3);Line(.63f,.62f,.94f,.80f,2);}
        }else if(kind==3){
            Line(.13f,.88f,.65f,.35f,4);Line(.65f,.35f,.48f,.16f,3);Line(.48f,.16f,.63f,.02f,3);
            Line(.65f,.35f,.86f,.50f,3);Line(.86f,.50f,.99f,.32f,3);
        }else if(kind==4){
            if(sea){Line(.05f,.58f,.95f,.58f,3);Line(.05f,.58f,.25f,.86f);Line(.95f,.58f,.75f,.86f);Line(.25f,.86f,.75f,.86f);Line(.25f,.48f,.25f,.25f);Line(.25f,.25f,.72f,.25f);Line(.72f,.25f,.82f,.48f);Line(.02f,.98f,.98f,.98f,1);}
            else {Line(.05f,.20f,.55f,.20f);Line(.05f,.20f,.05f,.70f);Line(.05f,.70f,.95f,.70f);Line(.55f,.20f,.55f,.70f);Line(.55f,.40f,.85f,.40f);Line(.85f,.40f,.95f,.70f);Line(.20f,.78f,.20f,.95f,4);Line(.80f,.78f,.80f,.95f,4);}
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
            const int height=24;
            string[] amounts=counts.Select((n,i)=>i==4?DirectoryTransportAmount(n):n.ToString()).ToArray();
            int width=Math.Max(70,32+(int)MathF.Ceiling(amounts.Max(t=>_font.MeasureString(t).X)*.43f));
            string status=DirectoryTurnStatus(node.Owner);
            int total=rows*(height+3)+8+(status.Length>0?18:0);
            int x=Math.Clamp((int)at.X+9,map.Left+2,map.Right-width-10);
            int y=Math.Clamp((int)at.Y-total/2,map.Top+2,map.Bottom-total-2);
            var whole=new Rectangle(x-3,y-3,width+11,total+6);
            // Nearby sites can share a screen position: move their signs aside, preserving a connector.
            if(_directoryHits.Any(h=>h.Bounds.Intersects(whole))){
                var origin=whole;
                var offsets=Enumerable.Range(-3,7).SelectMany(dx=>Enumerable.Range(-3,7).Select(dy=>new Point(dx*(width+18),dy*(total+12))))
                    .OrderBy(p=>p.X*p.X+p.Y*p.Y);
                foreach(var offset in offsets){
                    var candidate=origin;candidate.Offset(offset);
                    if(!map.Contains(candidate) || _directoryHits.Any(h=>h.Bounds.Intersects(candidate)))continue;
                    whole=candidate;x=whole.X+3;y=whole.Y+3;break;
                }
            }
            // A short connector keeps the stack anchored to its geographic node.
            var foot=new Vector2(x+width/2,y+total);var d=foot-at;
            _ui.Tile((at+foot)/2,new(d.Length(),2),MathF.Atan2(d.Y,d.X),new Color(12,25,32));
            _ui.Box(new(x+width/2-2,y,4,total),new Color(73,51,35));
            int row=0;
            for(int kind=0;kind<DirectoryLabels.Length;kind++)if(counts[kind]>0){
                var board=new Rectangle(x,y+row++*(height+3),width,height);var shadow=board;shadow.Offset(2,2);
                _ui.Box(shadow,new Color(7,23,35));_ui.Box(board,DirectoryColors[kind]);
                for(int tip=0;tip<8;tip++)_ui.Box(new(board.Right+tip,board.Top+tip,1,Math.Max(1,height-tip*2)),DirectoryColors[kind]);
                var ink=DirectoryInk(kind);
                DrawDirectoryRoleIcon(kind,new(board.X+5,board.Y+3,20,18),ink,kind==4 && DirectorySeaTransport(node.Id));
                _ui.Center(amounts[kind],new(board.X+29,board.Y,board.Width-29,board.Height),.43f,ink);
            }
            if(status.Length>0){
                var note=new Rectangle(x,y+rows*(height+3)+3,48,15);
                _ui.Box(note,new Color(27,39,47));
                _ui.Center(status,note,.24f,new Color(156,166,173));
            }
            _directoryHits.Add((whole,node.Id));
        }
        // Fixed legend keeps color meaning visible even when a category is zero everywhere.
        _ui.Box(new(990,242,710,122),new Color(7,23,35,240));
        for(int i=0;i<DirectoryLabels.Length;i++){
            var r=new Rectangle(1000+i%3*226,250+i/3*34,218,30);_ui.Box(r,DirectoryColors[i]);
            var ink=DirectoryInk(i);
            DrawDirectoryRoleIcon(i,new(r.X+5,r.Y+5,20,20),ink);
            _ui.Center(DirectoryLabels[i],new(r.X+28,r.Y,r.Width-28,r.Height),.30f,ink);
        }
        _ui.Text("全地域 / ０は非表示 / 輸送は送り元の直送便予約：頭・胴・脚を各1/3台",new(1000,326),.25f,Cream);
        foreach(var hit in _directoryHits.AsEnumerable().Reverse())if(hit.Bounds.Contains(_pointer)){
            var node=_setup.Nodes.All[hit.Node];
            string hint=DirectoryTurnHint(DirectoryTurnStatus(node.Owner));
            string label=_setup.Nodes.Label(node)+(hint.Length>0?" / "+hint:"")+" / クリックで地点を開く";
            _ui.Box(new(990,376,850,36),new Color(7,23,35));
            _ui.Center(label,new(990,376,850,36),Math.Min(.32f,830/_font.MeasureString(label).X),Cream);break;
        }
    }
}
