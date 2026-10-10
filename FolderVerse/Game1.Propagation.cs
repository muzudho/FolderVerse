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
        var at=_propagationNet.NodePosition(_setup,_setup.Nodes.All[battle.TargetNode],panel);
        if(battle.Kind==BattleKind.Edge)at=(at+_propagationNet.NodePosition(_setup,_setup.Nodes.All[battle.SourceNode],panel))/2;
        int offset=_robotScenes.Take(scene).Count(b=>b.TargetNode==battle.TargetNode && b.SourceNode==battle.SourceNode);
        return at+new Vector2(offset*34,-32);
    }
    private void DrawPropagation(int focus,float progress,bool movement)
    {
        var panel=new Rectangle(40,100,1840,860);
        _propagationNet.SetCenter(_setup,_net.CenterFace);
        _propagationNet.FitAll(panel);
        _propagationNet.ShowBatteries=false;
        _propagationNet.ShowMapSymbols=_net.ShowMapSymbols;
        // Return from the previous board first; zoom into the next only at the end.
        float zoom=movement?Math.Clamp((progress-.6f)/.4f,0,1):
            progress<.2f?1-progress/.2f:Math.Clamp((progress-.8f)/.2f,0,1);
        if(_robotPendingSummary)zoom=Math.Max(0,1-progress/.2f);
        zoom=MathHelper.SmoothStep(0,1,zoom);
        int target=progress<.2f && !movement?_battleWave:focus;
        float magnification=MathHelper.Lerp(1,PropagationBattleBounds.Width/30f,zoom);
        if(target>=0)
        {
            var before=PropagationBadgeCenter(target,panel);
            _propagationNet.FitAll(panel,magnification);
            var after=PropagationBadgeCenter(target,panel);
            // Badge offsets are in canvas pixels, so include them when aligning the camera.
            var destination=Vector2.Lerp(before,new Vector2(PropagationBattleBounds.Center.X,PropagationBattleBounds.Center.Y),zoom);
            _propagationNet.Drag(destination-after);
        }
        _propagationNet.Draw(_ui,_setup,panel,-1,_animationTime);
        Vector2 NodeAt(int id)=>_propagationNet.NodePosition(_setup,_setup.Nodes.All[id],panel);
        float travel=movement?MathHelper.SmoothStep(0,1,Math.Clamp(progress/.6f,0,1)):1;
        foreach(var shipment in movement?_setup.Robots.Transport.LastTransfers:Array.Empty<RobotShipment>())
        {
            var at=Vector2.Lerp(NodeAt(shipment.Source),NodeAt(shipment.Target),travel);
            int owner=_setup.Robots.Transport.LastOwners.GetValueOrDefault(shipment.RobotId);
            DrawRobotGlyph(new((int)at.X-12,(int)at.Y-32,24,36),owner,shipment.Parts);
        }
        foreach(var march in movement?_setup.Campaign.RobotBattles.LastMarches:Enumerable.Empty<(int Owner,int Source,int Target)>())
        {
            var at=Vector2.Lerp(NodeAt(march.Source),NodeAt(march.Target),travel);
            DrawToySoldier(new((int)at.X-10,(int)at.Y-30,20,30),_setup.OwnerColor(march.Owner),false);
        }
        for(int owner=0;owner<_setup.ActiveCount;owner++)
        {
            if(!_setup.Relations.Powered[owner])continue;
            var node=_setup.Nodes.Current(owner);if(node==null)continue;
            var at=NodeAt(node.Id);
            var march=_setup.Campaign.RobotBattles.LastMarches.FirstOrDefault(m=>m.Owner==owner);
            if(movement && _setup.Campaign.RobotBattles.LastMarches.Any(m=>m.Owner==owner))at=Vector2.Lerp(NodeAt(march.Source),NodeAt(march.Target),travel);
            _ui.Box(new((int)at.X-12,(int)at.Y-9,24,16),_setup.OwnerColor(owner));
            _ui.Box(new((int)at.X+12,(int)at.Y-5,4,8),Cream);
        }
        foreach(int i in Enumerable.Range(0,_robotScenes.Length).OrderBy(i=>i==target?1:0))
        {
            var battle=_robotScenes[i];var at=PropagationBadgeCenter(i,panel);
            int side=(int)MathF.Round(30*magnification);
            var badge=new Rectangle((int)MathF.Round(at.X-side/2f),(int)MathF.Round(at.Y-side/2f),side,side);
            _ui.Box(badge,i==focus?Accent:new Color(228,154,180));
            float blend=i==target?Math.Clamp((zoom-.45f)/.55f,0,1):0;
            if(blend>0)DrawPropagationGrid(battle,badge,blend);
            if(blend<1)_counter.DrawNumber(_spriteBatch,i+1,badge,Color.White*(1-blend));
        }

        _ui.Box(RobotResultBar,Muted);
        _ui.Box(new(RobotResultBar.X,RobotResultBar.Y,(int)(RobotResultBar.Width*progress),RobotResultBar.Height),Accent);
        _ui.Button(BattleContinue,movement?"戦闘へ進む":"戦場を切り替える",Accent,.55f);
        if(!movement)_ui.Button(RobotResultPause,_robotPlaying?"一時停止":"再開",Muted,.5f);
    }
    private void DrawPropagationGrid(BattleState battle,Rectangle bounds,float opacity)
    {
        _ui.Box(bounds,new Color(16,35,46)*opacity);
        var node=_setup.Nodes.All[battle.TargetNode];var cell=_setup.Cells[node.Cell];
        float elevation=WorldTerrain.Elevation(_setup.Routes.Position(node.Cell,node.Center),WorldTerrain.Offset(_setup.WorldSeed));
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
        {
            int left=bounds.X+x*bounds.Width/10,top=bounds.Y+y*bounds.Height/10;
            var rect=new Rectangle(left,top,bounds.X+(x+1)*bounds.Width/10-left-2,bounds.Y+(y+1)*bounds.Height/10-top-2);
            bool wall=battle.Walls.Contains(new(x,y));bool bridge=x is 4 or 5 || y is 4 or 5;
            Color color=battle.Terrain==BattleTerrain.Sea?(wall?new Color(28,69,101):bridge?new Color(162,121,73):new Color(107,79,55)):
                Math.Abs(cell.Normal.Y)>.5f?new Color(161,192,203):elevation<.55f?new Color(163,137,91):new Color(35,64,58);
            _ui.Box(rect,color*opacity);
        }
    }
}
