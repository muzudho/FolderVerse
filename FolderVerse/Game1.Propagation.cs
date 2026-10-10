namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly CubeNet _propagationNet=new();
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
        int target=progress<.2f && !movement?_battleWave:focus;
        if(target>=0)
        {
            _propagationNet.FitAll(panel,MathF.Pow(1.2f,zoom*9));
            var after=_propagationNet.NodePosition(_setup,_setup.Nodes.All[_robotScenes[target].TargetNode],panel);
            _propagationNet.Drag((new Vector2(panel.Center.X,panel.Center.Y)-after)*zoom);
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
        for(int i=0;i<_robotScenes.Length;i++)
        {
            var battle=_robotScenes[i];var at=NodeAt(battle.TargetNode);
            if(battle.Kind==BattleKind.Edge)at=(at+NodeAt(battle.SourceNode))/2;
            // Stack split boards sharing a battlefield so every number remains readable.
            int offset=_robotScenes.Take(i).Count(b=>b.TargetNode==battle.TargetNode && b.SourceNode==battle.SourceNode);
            at+=new Vector2(offset*34,-32);
            var badge=new Rectangle((int)at.X-15,(int)at.Y-15,30,30);
            _ui.Box(badge,i==focus?Accent:new Color(228,154,180));
            _counter.DrawNumber(_spriteBatch,i+1,badge,Color.White);
        }
        _ui.Box(new(0,0,1920,85),new Color(16,35,46));
        _ui.Text(movement?"伝播フェーズ":"戦場を切り替えています",new(54,24),.8f,Cream);
        _ui.Box(RobotResultBar,Muted);
        _ui.Box(new(RobotResultBar.X,RobotResultBar.Y,(int)(RobotResultBar.Width*progress),RobotResultBar.Height),Accent);
        _ui.Button(BattleContinue,movement?"戦闘へ進む":"戦場を切り替える",Accent,.55f);
        if(!movement)_ui.Button(RobotResultPause,_robotPlaying?"一時停止":"再開",Muted,.5f);
    }
}
