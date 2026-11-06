namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly CubeNet _propagationNet=new();
    private static readonly Rectangle PropagationBattleBounds=new(40,40,1000,1000);
    private Vector2 PropagationBadgeCenter(int scene,Rectangle panel)
    {
        var battle=_robotScenes[scene];
        var at=_propagationNet.AnimatedNodePosition(_setup,_setup.Nodes.All[battle.TargetNode],panel);
        if(battle.Kind==BattleKind.Edge)at=(at+_propagationNet.AnimatedNodePosition(_setup,_setup.Nodes.All[battle.SourceNode],panel))/2;
        int offset=_robotScenes.Take(scene).Count(b=>b.TargetNode==battle.TargetNode && b.SourceNode==battle.SourceNode);
        return at+new Vector2(offset*34,-32);
    }
    private void DrawPropagation(int focus,float progress,bool movement)
    {
        var panel=PropagationMapPanel;
        if(_propagationNet.Faces.Length==0)_propagationNet.SetCenter(_setup,_net.CenterFace);
        if(movement && _propagationCentering)ApplyPropagationCentering();else _propagationNet.FitAll(panel);
        _propagationNet.ShowBatteries=false;
        _propagationNet.ShowMapSymbols=_net.ShowMapSymbols;
        _propagationNet.ShowEquator=_net.ShowEquator;
        _propagationNet.ShowRobots=_net.ShowRobots;_propagationNet.ShowOtherCountries=_net.ShowOtherCountries;
        _propagationNet.NodeOwnerSnapshot=movement && !DeliveryPlayback && _setup.Campaign.PreviousNodeOwners.Length==_setup.Nodes.All.Length?_setup.Campaign.PreviousNodeOwners:null;
        // Return from the previous board first; zoom into the next only at the end.
        float zoom=movement?Math.Clamp((progress-.6f)/.4f,0,1):
            progress<.2f?1-progress/.2f:Math.Clamp((progress-.8f)/.2f,0,1);
        if(_robotPendingSummary)zoom=Math.Max(0,1-progress/.2f);
        zoom=MathHelper.SmoothStep(0,1,zoom);
        int target=progress<.2f && !movement?_battleWave:focus;
        float boardBlend=movement && focus>=0 && PropagationComplete?Math.Clamp((float)(_transportAge/.5),0,1):-1;
        if(boardBlend>=0)zoom=MathHelper.SmoothStep(0,1,Math.Clamp((float)((_transportAge-.5)/1.5),0,1));
        float magnification=MathHelper.Lerp(1,PropagationBattleBounds.Width/30f,zoom);
        if(target>=0 && zoom>0)
        {
            var before=PropagationBadgeCenter(target,panel);
            _propagationNet.FitAll(panel,magnification);
            var after=PropagationBadgeCenter(target,panel);
            // Badge offsets are in canvas pixels, so include them when aligning the camera.
            var destination=Vector2.Lerp(before,new Vector2(PropagationBattleBounds.Center.X,PropagationBattleBounds.Center.Y),zoom);
            _propagationNet.Drag(destination-after);
        }
        _propagationNet.Draw(_ui,_setup,panel,-1,_animationTime);
        Vector2 NodeAt(int id)=>_propagationNet.AnimatedNodePosition(_setup,_setup.Nodes.All[id],panel);

        foreach(var shipment in movement?PropagationShipments:Array.Empty<RobotShipment>())
        {
            var at=Vector2.Lerp(NodeAt(shipment.Source),NodeAt(shipment.Target),PropagationTravel(shipment.Source,shipment.Target));
            int owner=_setup.Robots.Transport.LastOwners.GetValueOrDefault(shipment.RobotId);
            _net.DrawMapRobot(_ui,new((int)at.X-12,(int)at.Y-32,24,36),_setup.OwnerColor(owner),shipment.Parts);
        }
        if(movement && !DeliveryPlayback)foreach(var agent in _setup.Salvage.Agents.Where(a=>a.PreviousNode!=a.Node && _setup.Relations.Controls(a.Owner))){
            var at=Vector2.Lerp(NodeAt(agent.PreviousNode),NodeAt(agent.Node),PropagationTravel(agent.PreviousNode,agent.Node));
            _ui.Box(new((int)at.X-5,(int)at.Y-18,10,9),Cream);_ui.Box(new((int)at.X-7,(int)at.Y-7,14,17),_setup.OwnerColor(agent.Owner));
            _ui.Text("回収隊",at+new Vector2(10,-18),.27f,Cream);
        }
        foreach(var march in movement && !DeliveryPlayback?_setup.Campaign.RobotBattles.LastMarches:Enumerable.Empty<(int Owner,int Source,int Target)>())
        {
            var at=Vector2.Lerp(NodeAt(march.Source),NodeAt(march.Target),PropagationMarchTravel(march.Owner,march.Source,march.Target));
            DrawToySoldier(new((int)at.X-10,(int)at.Y-30,20,30),_setup.OwnerColor(march.Owner),false);
        }
        for(int owner=0;owner<_setup.ActiveCount;owner++)
        {
            if(!_setup.Relations.Powered[owner])continue;
            var node=_setup.Nodes.Current(owner);if(node==null)continue;
            var at=NodeAt(node.Id);
            var march=_setup.Campaign.RobotBattles.LastQueenMarches.FirstOrDefault(m=>m.Owner==owner);
            if(movement && !DeliveryPlayback && _setup.Campaign.RobotBattles.LastQueenMarches.Any(m=>m.Owner==owner))at=Vector2.Lerp(NodeAt(march.Source),NodeAt(march.Target),PropagationMarchTravel(march.Owner,march.Source,march.Target));
            _ui.Box(new((int)at.X-12,(int)at.Y-9,24,16),_setup.OwnerColor(owner));
            _ui.Box(new((int)at.X+12,(int)at.Y-5,4,8),Cream);
        }
        foreach(int i in Enumerable.Range(0,_robotScenes.Length).Where(i=>!movement || PropagationBattleArrived(_robotScenes[i])).OrderBy(i=>i==target?1:0))
        {
            var battle=_robotScenes[i];var at=PropagationBadgeCenter(i,panel);
            int side=(int)MathF.Round(30*magnification);
            var badge=new Rectangle((int)MathF.Round(at.X-side/2f),(int)MathF.Round(at.Y-side/2f),side,side);
            _ui.Box(badge,i==focus?Accent:new Color(228,154,180));
            float blend=i==target?(boardBlend>=0?boardBlend:Math.Clamp((zoom-.45f)/.55f,0,1)):0;
            if(blend>0){DrawPropagationGrid(battle,badge,blend);if(movement && i==target)_ui.Box(badge,new Color(105,110,116)*(blend*.9f));}
            if(blend<1)_counter.DrawNumber(_spriteBatch,i+1,badge,Color.White*(1-blend));
        }

        _ui.Box(RobotResultBar,Muted);
        _ui.Box(new(RobotResultBar.X,RobotResultBar.Y,(int)(RobotResultBar.Width*progress),RobotResultBar.Height),Accent);
        if(!movement)_ui.Button(BattleContinue,"戦場を切り替える",Accent,.55f);
        if(!movement)_ui.Button(RobotResultPause,_robotPlaying?"一時停止":"再開",Muted,.5f);
    }
    private bool PropagationBattleArrived(BattleState battle)
    {
        var marches=_setup.Campaign.RobotBattles.LastMarches.Where(m=>
            battle.Kind==BattleKind.Edge
                ? (m.Source==battle.SourceNode && m.Target==battle.TargetNode) || (m.Source==battle.TargetNode && m.Target==battle.SourceNode)
                : m.Target==battle.TargetNode && battle.Armies.Any(a=>a.Owner==m.Owner));
        // Reveal this battlefield as soon as its first attacking force arrives.
        return !marches.Any() || marches.Any(m=>PropagationTravel(m.Source,m.Target)>=1);
    }
    private void DrawPropagationGrid(BattleState battle,Rectangle bounds,float opacity)
    {
        DrawBattleTerrain(battle,bounds,opacity);
        foreach(var fort in battle.Fortifications)
            if(fort.Kind!=FortificationKind.Gate || fort.Health>0)
                BattleFortificationArt.Draw(_ui,BattleTerrainTile(bounds,battle.BoardSize,fort.Position),fort,opacity);
    }

    private static Rectangle BattleTerrainTile(Rectangle bounds,int size,Point p)
    {
        int left=bounds.X+p.X*bounds.Width/size,top=bounds.Y+p.Y*bounds.Height/size;
        int width=bounds.X+(p.X+1)*bounds.Width/size-left,height=bounds.Y+(p.Y+1)*bounds.Height/size-top;
        int gap=width>=50?2:width>=15?1:0;
        return new(left,top,Math.Max(1,width-gap),Math.Max(1,height-gap));
    }

    private void DrawBattleTerrain(BattleState battle,Rectangle bounds,float opacity)
    {
        _ui.Box(bounds,new Color(24,43,51)*opacity);
        var node=_setup.Nodes.All[battle.TargetNode];var cell=_setup.Cells[node.Cell];
        float elevation=WorldTerrain.Elevation(_setup.Routes.Position(node.Cell,node.Center),WorldTerrain.Offset(_setup.WorldSeed));
        float latitude=_setup.Geographic(cell.Normal).Y;
        var ground=Math.Abs(latitude)>.5f?new Color(161,192,203):elevation<.55f?new Color(163,137,91):new Color(35,64,58);
        for(int y=0;y<battle.BoardSize;y++)for(int x=0;x<battle.BoardSize;x++)
        {
            var p=new Point(x,y);
            var tile=BattleTerrainTile(bounds,battle.BoardSize,p);
            bool wall=battle.Walls.Contains(p) && !battle.Fortifications.Any(f=>f.Position==p);
            if(battle.Terrain!=BattleTerrain.Sea)
            {
                _ui.Box(tile,ground*opacity);
                if(wall)BattleObstacleArt.Draw(_ui,tile,battle.Terrain==BattleTerrain.Mountain
                    ?BattleObstacleStyle.Rock:BattleObstacleArt.Style(false,false,elevation,latitude),opacity);
                continue;
            }
            _ui.Box(tile,new Color(28,88,125)*opacity);
            if(wall)
            {
                if(!battle.SeaClearWater.Contains(p))
                    BattleObstacleArt.Draw(_ui,tile,BattleObstacleArt.Style(true,false,elevation,latitude),opacity);
                continue;
            }
            bool bridge=battle.SeaLogs.Contains(p);
            bool horizontal=battle.BoardSize==10
                ?battle.SeaLogs.Contains(p+new Point(1,0)) || battle.SeaLogs.Contains(p-new Point(1,0))
                :battle.Walkable(p+new Point(1,0)) && battle.Walkable(p-new Point(1,0));
            Rectangle Mark(int x,int y,int width,int height)=>new(tile.X+x*tile.Width/100,tile.Y+y*tile.Height/100,
                Math.Max(1,width*tile.Width/100),Math.Max(1,height*tile.Height/100));
            if(bridge)
            {
                _ui.Box(horizontal?Mark(0,28,100,44):Mark(28,0,44,100),new Color(151,101,52)*opacity);
                if(tile.Width>=12)for(int stripe=36;stripe<=60;stripe+=12)
                    _ui.Box(horizontal?Mark(0,stripe,100,3):Mark(stripe,0,3,100),new Color(212,165,91)*opacity);
            }
            else
            {
                _ui.Box(tile,new Color(112,75,47)*opacity);
                if(tile.Width>=12)for(int plank=18;plank<=72;plank+=18)
                    _ui.Box(Mark(12,plank,76,2),new Color(193,145,88)*opacity);
            }
        }
    }
}
