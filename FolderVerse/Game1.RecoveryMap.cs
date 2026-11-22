namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private bool RecoveryDraftActive=>_recoveryStage is 0 or 1 or 2 or 3 or 6;
    private bool RecoveryMapVisible=>(_recoveryStage is 0 or 6)&&!_recoveryLocation||_recoveryStage==2&&_recoveryMap;
    private IEnumerable<int> RecoveryRecipientNodes=>_setup.Nodes.All.Where(n=>n.Functional&&n.Owner==_setup.PlayerSlot).Select(n=>n.Id);
    private void CancelRecoveryDraft(){
        _recoveryStage=4;_recoveryPage=0;_recoveryMap=_recoveryLocation=false;_recoveryDestination=_recoverySource=-1;
        _recoveryWorker=-1;_recoveryWorkers.Clear();_recoveryTargets.Clear();_recoveryFilter=new();_recoveryMessage="";
    }
    private void SelectRecoveryDestination(int id){
        if(!RecoveryRecipientNodes.Contains(id))return;
        _recoveryDestination=id;_recoveryLocation=true;_recoveryPage=0;_recoveryWorkers.Clear();_recoveryWorker=-1;
    }
    private void DrawRecoveryReceivingHangar(){
        var robots=RecoveryHangar;var store=_setup.Robots.Nodes[_recoveryDestination];
        _recoveryPage=Math.Min(_recoveryPage,Math.Max(0,(robots.Length-1)/12));
        for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=robots.Length)break;var r=robots[index];var card=RecoveryCard(i);
            _ui.Box(card,Muted);DrawHangarRobot(new(card.X+100,card.Y+5,80,110),r);
            _ui.Center("ID "+r.Id+" / "+PartsLabel(r),new(card.X+8,card.Y+120,card.Width-16,24),.3f,Cream);
            _ui.Center("確認用 / 回収員はまだ選びません",new(card.X+8,card.Bottom-27,card.Width-16,24),.27f,Cream);
        }
        if(robots.Length==0)_ui.Text("この地点の格納庫は空です。",new(690,400),.45f,Cream);
        NewsLine(_setup.Nodes.DisplayName(_setup.Nodes.All[_recoveryDestination])+" / 格納庫 "+store.Count+" / "+store.StorageLimit,980,940,880,.35f,Cream);
    }
    private Vector3 RecoverySurfacePosition(int id){
        var n=_setup.Nodes.All[id];var cell=_setup.Cells[n.Cell];
        var point=_recoveryNet.SiteDisplayPosition(_setup,n,_recoveryNet.ResourceClearance(_setup,n.Cell));
        return point+cell.Normal*(2*WorldTerrain.ReliefAt(_setup,cell,point,_setup.Terrain));
    }
    private bool RecoveryMapPosition(int id,out Vector2 at){
        var n=_setup.Nodes.All[id];
        if(!_recoveryGlobe){at=_recoveryNet.AnimatedNodePosition(_setup,n,RecoveryPanel);return true;}
        return _recoveryWorld.ProjectVisible(RecoverySurfacePosition(id),_setup.Cells[n.Cell].Normal,RecoveryYaw,RecoveryPitch,RecoveryPanel,out at);
    }
    private float RecoverySpotlightRadius(int id,Vector2 center){
        if(!_recoveryGlobe)return Math.Max(32,_recoveryNet.Fit(RecoveryPanel).Scale*.55f);
        var n=_setup.Nodes.All[id];var cell=_setup.Cells[n.Cell];var point=RecoverySurfacePosition(id);float radius=32;
        for(int i=0;i<12;i++)foreach(float height in new[]{0f,.25f}){
            float angle=i*MathHelper.TwoPi/12;var sample=point+(cell.U*MathF.Cos(angle)+cell.V*MathF.Sin(angle))*.4f+cell.Normal*height;
            _recoveryWorld.ProjectVisible(sample,cell.Normal,RecoveryYaw,RecoveryPitch,RecoveryPanel,out var at);radius=Math.Max(radius,Vector2.Distance(center,at)+8);
        }
        return radius;
    }
    private int HitRecoveryMap(IEnumerable<int> candidates){
        if(!RecoveryPanel.Contains(_pointer))return -1;
        if(!_recoveryGlobe)return _recoveryNet.HitNode(_setup,RecoveryPanel,_pointer,candidates);
        int nearest=-1;float distance=float.MaxValue;
        foreach(var n in _setup.Nodes.All){if(!RecoveryMapPosition(n.Id,out var at))continue;float d=Vector2.DistanceSquared(at,_pointer.ToVector2());
            if(d<=MathF.Pow(Math.Min(40,RecoverySpotlightRadius(n.Id,at)),2)&&d<distance){nearest=n.Id;distance=d;}}
        return candidates.Contains(nearest)?nearest:-1;
    }
    private void DrawRecoveryNetMask(){
        var spans=new List<(int Left,int Right)>();
        var ink=new Color(16,35,46)*(1-_recoveryNet.FocusBrightness);
        for(int y=RecoveryPanel.Top;y<RecoveryPanel.Bottom;y++){
            spans.Clear();
            foreach(var circle in _recoveryNet.FocusCircles){
                float dy=y+.5f-circle.Center.Y;
                if(Math.Abs(dy)>=circle.Radius)continue;
                float half=MathF.Sqrt(circle.Radius*circle.Radius-dy*dy);
                int left=Math.Max(RecoveryPanel.Left,(int)MathF.Floor(circle.Center.X-half));
                int right=Math.Min(RecoveryPanel.Right,(int)MathF.Ceiling(circle.Center.X+half));
                if(right>left)spans.Add((left,right));
            }
            spans.Sort((a,b)=>a.Left.CompareTo(b.Left));int x=RecoveryPanel.Left;
            foreach(var span in spans){
                if(span.Right<=x)continue;
                if(span.Left>x)_ui.Box(new(x,y,span.Left-x,1),ink);
                x=Math.Max(x,span.Right);
            }
            if(x<RecoveryPanel.Right)_ui.Box(new(x,y,RecoveryPanel.Right-x,1),ink);
        }
    }
    private void DrawRecoveryMapSurface(Rectangle canvas,Matrix transform,RasterizerState clip){
        if(_recoveryGlobe){
            _recoveryWorld.SelectionSetup=_setup;_recoveryWorld.AttackTargetSpotlight=true;_recoveryWorld.FocusBrightness=_recoveryNet.FocusBrightness;
            _recoveryWorld.FocusCircles.Clear();_recoveryWorld.FocusCircles.AddRange(_recoveryNet.FocusCircles);
            _recoveryWorld.FocusCircleArea=RecoveryPanel;_recoveryWorld.FocusCircleYaw=RecoveryYaw;_recoveryWorld.FocusCirclePitch=RecoveryPitch;
            var viewport=GraphicsDevice.Viewport;GraphicsDevice.Viewport=OrientationViewport(RecoveryPanel,canvas);
            _recoveryWorld.Draw(RecoveryYaw,RecoveryPitch,_animationTime,false,true,deferAtmosphere:true);GraphicsDevice.Viewport=viewport;
            _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
            _recoveryNet.DrawGlobeLayers(_ui,_setup,_recoveryWorld,RecoveryPanel,RecoveryYaw,RecoveryPitch,_animationTime);_spriteBatch.End();
            GraphicsDevice.Viewport=OrientationViewport(RecoveryPanel,canvas);_recoveryWorld.DrawAtmosphere(_animationTime);GraphicsDevice.Viewport=viewport;
        }else{
            // Draw the complete map first; the selection mask is a HUD overlay, not terrain lighting.
            bool spotlight=_recoveryNet.AttackTargetSpotlight;
            _recoveryNet.AttackTargetSpotlight=false;
            _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
            try{_recoveryNet.Draw(_ui,_setup,RecoveryPanel,-1,_animationTime,objects:false);}
            finally{_recoveryNet.AttackTargetSpotlight=spotlight;}
            DrawRecoveryNetMask();
            _recoveryNet.DrawMapObjects(_ui,_setup,RecoveryPanel,_animationTime);_spriteBatch.End();
        }
    }
}
