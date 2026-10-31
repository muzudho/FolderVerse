namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private readonly CubeNet _directPairNet=new();
    private float _directPairSlide=1;
    private readonly List<(Rectangle Bounds,int A,int B)> _directPairTrucks=new();
    private bool _directPairGlobe;
    private WorldPreview _directPairWorld;
    private float _directPairYaw,_directPairPitch;
    private void PrepareDirectPairMap(){
        _directPairNet.RestoreView(_net.CaptureView());_directPairGlobe=_statusGlobe;
        _directPairYaw=_yaw;_directPairPitch=_pitch;
        if(!_directPairGlobe)return;
        _directPairWorld??=new WorldPreview(GraphicsDevice);
        _directPairWorld.CountryColors=_world.CountryColors;_directPairWorld.ControlledCountryColors=_world.ControlledCountryColors;
        _directPairWorld.ShowSetup(_setup,true);_directPairWorld.ManualOrientation=_world.ManualOrientation;_directPairWorld.GlobeRock=_world.GlobeRock;
    }
    private Vector2 DirectPairPosition(int node){
        var panel=_directRenderPanel??DirectPairMapBounds;
        if(!_directPairGlobe)return _directPairNet.NodePosition(_setup,_setup.Nodes.All[node],panel);
        var n=_setup.Nodes.All[node];_directPairWorld.ProjectVisible(_setup.Routes.Position(n.Cell,n.Center),_setup.Cells[n.Cell].Normal,_directPairYaw,_directPairPitch,panel,out var at);return at;
    }
    private void AlignDirectPairInspectors(){
        var a=DirectPairPosition(_directNodes[0]);var b=DirectPairPosition(_directNodes[1]);
        if(a.X>b.X+.5f || Math.Abs(a.X-b.X)<=.5f && a.Y>b.Y){
            (_directNodes[0],_directNodes[1])=(_directNodes[1],_directNodes[0]);
            (_directSlots[0],_directSlots[1])=(_directSlots[1],_directSlots[0]);
        }
    }
    private static readonly Rectangle DirectPairMapBounds=new(600,200,720,690),DirectPairFitButton=new(800,912,320,44);
    private Color DirectSideColor(int side)=>side==0?_setup.OwnerColor(_setup.PlayerSlot):Color.White;
    private void FitDirectPairMap()
    {
        var panel=DirectPairMapBounds;
        _directPairNet.ShowRoutes=true;_directPairNet.ShowOtherCountries=_net.ShowOtherCountries;
        _directPairNet.ShowRobots=_net.ShowRobots;_directPairNet.ShowBatteries=_net.ShowBatteries;
        _directPairNet.ShowMapSymbols=false;_directPairNet.ShowCountryColors=_net.ShowCountryColors;
        _directPairNet.CountryFocus=_setup.PlayerSlot;
        if(_directPairGlobe){
            var first=_setup.Nodes.All[_directNodes[0]];var second=_setup.Nodes.All[_directNodes[1]];
            _directPairWorld.GlobeAnchor=(_setup.Routes.Position(first.Cell,first.Center)+_setup.Routes.Position(second.Cell,second.Center))/2;
            _directPairWorld.GlobePan=Vector2.Zero;
            var inside=panel;inside.Inflate(-110,-110);float low=.1f,high=64;
            for(int i=0;i<24;i++){float zoom=(low+high)/2;_directPairWorld.GlobeZoom=zoom;
                if(inside.Contains(DirectPairPosition(first.Id).ToPoint()) && inside.Contains(DirectPairPosition(second.Id).ToPoint()))low=zoom;else high=zoom;
            }
            _directPairWorld.GlobeZoom=low;AlignDirectPairInspectors();return;
        }
        _directPairNet.FitAll(panel);
        var a=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[0]],panel);
        var b=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[1]],panel);
        float factor=Math.Min((panel.Width-240)/Math.Max(1,Math.Abs(a.X-b.X)),(panel.Height-240)/Math.Max(1,Math.Abs(a.Y-b.Y)));
        _directPairNet.SetFitCamera(panel,Math.Min(500,_directPairNet.Fit(panel).Scale*factor),Vector2.Zero);
        a=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[0]],panel);b=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[1]],panel);
        _directPairNet.Drag(panel.Center.ToVector2()-(a+b)/2);AlignDirectPairInspectors();
    }
    private void DirectPairMapInput(MouseState mouse)
    {
        if(!_directPairOpen || _directDrag.HasValue || _directPairClosing || _directPairSlide<1)return;
        if(mouse.LeftButton==ButtonState.Pressed && _previousMouse.LeftButton==ButtonState.Released && DirectPairFitButton.Contains(_pointer)){FitDirectPairMap();return;}
        if(!DirectPairMapBounds.Contains(_pointer))return;
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        if(wheel!=0){if(_directPairGlobe)_directPairWorld.GlobeZoom=MathHelper.Clamp(_directPairWorld.GlobeZoom*MathF.Pow(1.2f,wheel/120f),.1f,64);else _directPairNet.ZoomAt(DirectPairMapBounds,_pointer,wheel);}
        if(mouse.RightButton==ButtonState.Pressed && _previousMouse.RightButton==ButtonState.Pressed){var before=CanvasPoint(_previousMouse);var delta=new Vector2(_pointer.X-before.X,_pointer.Y-before.Y);if(_directPairGlobe)_directPairWorld.GlobePan+=new Vector2(delta.X/DirectPairMapBounds.Width,delta.Y/DirectPairMapBounds.Height);else _directPairNet.Drag(delta);}
    }
    private bool DirectPairTruckClick(Point point)
    {
        if(!DirectPairMapBounds.Contains(point))return false;
        var hit=_directPairTrucks.Where(t=>t.Bounds.Contains(point)).OrderBy(t=>Vector2.DistanceSquared(t.Bounds.Center.ToVector2(),point.ToVector2())).FirstOrDefault();
        if(hit.Bounds==Rectangle.Empty)return false;
        if(!_directNodes.Contains(hit.A) || !_directNodes.Contains(hit.B))OpenDirectPair(hit.A,hit.B);
        return true;
    }
    private bool DirectPairVisible(int node,out Vector2 at)
    {
        at=DirectPairPosition(node);
        if(!_directPairGlobe)return true;
        var n=_setup.Nodes.All[node];return _directPairWorld.ProjectVisible(_setup.Routes.Position(n.Cell,n.Center),_setup.Cells[n.Cell].Normal,_directPairYaw,_directPairPitch,_directRenderPanel??DirectPairMapBounds,out at);
    }
    private void DrawDirectPairTrucks(Rectangle panel)
    {
        _directPairTrucks.Clear();
        foreach(var node in _setup.Nodes.All.Where(n=>DirectNodeAllowed(n.Id))){
            if(!DirectPairVisible(node.Id,out var a))continue;
            foreach(var route in _setup.Routes.Neighbors(node)){
                var next=_setup.Nodes.At(route.Target,route.Entry);
                if(node.Id>=next.Id || !DirectNodeAllowed(next.Id) || !DirectPairVisible(next.Id,out var b))continue;
                var at=(a+b)/2;var bounds=new Rectangle((int)at.X-22,(int)at.Y-17,44,34);
                if(!panel.Contains(bounds))continue;
                bool enabled=!_setup.Campaign.RobotBattles.LockedNode(node.Id) && !_setup.Campaign.RobotBattles.LockedNode(next.Id) && !_setup.Campaign.RobotBattles.LockedEdge(node.Id,next.Id);
                DrawDirectTruck(node.Id,next.Id,a,b,bounds,enabled);
                if(enabled && _directPairSlide>=1 && !_directPairClosing)_directPairTrucks.Add((bounds,node.Id,next.Id));
            }
        }
    }
    private void DrawDirectTail(Vector2 root,Vector2 tip,Color color)
    {
        var delta=tip-root;if(delta.LengthSquared()<1)return;
        var across=Vector2.Normalize(new Vector2(-delta.Y,delta.X))*10;
        var points=new[]{root+across,root-across,tip};
        int top=(int)MathF.Floor(points.Min(p=>p.Y)),bottom=(int)MathF.Ceiling(points.Max(p=>p.Y));
        for(int y=top;y<bottom;y++){
            float scan=y+.5f,left=float.PositiveInfinity,right=float.NegativeInfinity;
            for(int i=0;i<3;i++){var a=points[i];var b=points[(i+1)%3];if(scan<Math.Min(a.Y,b.Y) || scan>=Math.Max(a.Y,b.Y))continue;float x=a.X+(scan-a.Y)*(b.X-a.X)/(b.Y-a.Y);left=Math.Min(left,x);right=Math.Max(right,x);}
            if(float.IsFinite(left) && right>left)_ui.Box(new((int)MathF.Floor(left),y,Math.Max(1,(int)MathF.Ceiling(right)-(int)MathF.Floor(left)),1),color);
        }
    }
    private void DrawDirectPairMap()
    {
        var panel=DirectPairAnimatedPanel;var canvas=CanvasBounds();
        var finalView=_directPairNet.CaptureView();
        float finalZoom=_directPairWorld?.GlobeZoom??1;var finalPan=_directPairWorld?.GlobePan??Vector2.Zero;var finalAnchor=_directPairWorld?.GlobeAnchor;
        float targetScale=_directPairNet.Fit(DirectPairMapBounds).Scale;
        _directRenderPanel=panel;
        _directPairNet.SetFitCamera(panel,MathHelper.Lerp(_directFromScale,targetScale,DirectPairEase),Vector2.Lerp(_directFromPan,finalView.Pan,DirectPairEase));
        if(_directPairGlobe){_directPairWorld.GlobeZoom=MathHelper.Lerp(_directFromGlobeZoom,finalZoom,DirectPairEase);_directPairWorld.GlobePan=Vector2.Lerp(_directFromGlobePan,finalPan,DirectPairEase);_directPairWorld.GlobeAnchor=Vector3.Lerp(_directFromAnchor,finalAnchor??Vector3.Zero,DirectPairEase);}
        try{
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _ui.Box(panel,new Color(20,42,54));
        _ui.Center(_directPairGlobe?"地図ビュー / 地星儀":"地図ビュー / 展開図",new(panel.X,165,panel.Width,30),.48f,Cream);
        _directPairNet.FocusCircles.Clear();_directPairNet.FocusCircleNodes.Clear();
        var points=new Vector2[2];
        for(int side=0;side<2;side++){
            int id=_directNodes[side];var at=DirectPairPosition(id);points[side]=at;
            int count=_directPairNet.ShowRobots?_setup.Robots.Nodes[id].Count:0;float radius=count>0?Math.Max(48,count*30/MathHelper.TwoPi)+38:32;
            _directPairNet.FocusCircles.Add(new(at,radius));_directPairNet.FocusCircleNodes.Add(id);
        }
        foreach(var node in _setup.Nodes.All){var at=DirectPairPosition(node.Id);if(_directPairNet.FocusCircles.Any(c=>Vector2.DistanceSquared(at,c.Center)<=c.Radius*c.Radius))_directPairNet.FocusCircleNodes.Add(node.Id);}
        var old=GraphicsDevice.ScissorRectangle;_spriteBatch.End();
        using var clip=new RasterizerState{ScissorTestEnable=true};
        GraphicsDevice.ScissorRectangle=new(canvas.X+(int)(panel.X*canvas.Width/1920f),canvas.Y+(int)(panel.Y*canvas.Height/1080f),(int)(panel.Width*canvas.Width/1920f),(int)(panel.Height*canvas.Height/1080f));
        if(_directPairGlobe){
            _directPairWorld.FocusCircles.Clear();_directPairWorld.FocusCircles.AddRange(_directPairNet.FocusCircles);
            _directPairWorld.FocusCircleArea=panel;_directPairWorld.FocusCircleYaw=_directPairYaw;_directPairWorld.FocusCirclePitch=_directPairPitch;
            var viewport=GraphicsDevice.Viewport;
            GraphicsDevice.Viewport=OrientationViewport(panel,canvas);
            _directPairWorld.Draw(_directPairYaw,_directPairPitch,_animationTime,false,true);
            _directPairWorld.DrawRobotPins(_setup,_directPairNet,_directPairYaw,_directPairPitch,panel,_animationTime);
            GraphicsDevice.Viewport=viewport;
            _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
            _directPairNet.DrawGlobeLayers(_ui,_setup,_directPairWorld,panel,_directPairYaw,_directPairPitch,_animationTime);
        }else{
            _spriteBatch.Begin(transformMatrix:_directPairNet.ScreenTransform(panel)*transform,rasterizerState:clip);
            _directPairNet.Draw(_ui,_setup,panel,-1,_animationTime,false);_spriteBatch.End();
            _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);_directPairNet.DrawMapObjects(_ui,_setup,panel,_animationTime);
        }
        _spriteBatch.End();GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
        for(int side=0;side<2 && DirectPairEase>=1;side++){
            var at=points[side];var edge=new Vector2(side==0?580:1340,Math.Clamp(at.Y,250,860));
            var tip=new Vector2(Math.Clamp(at.X,panel.Left+12,panel.Right-12),Math.Clamp(at.Y,panel.Top+12,panel.Bottom-12));
            // A tapered speech-bubble tail links the fixed pane to its geographic endpoint.
            DrawDirectTail(edge,tip,DirectSideColor(side));
            string label=panel.Contains(at.ToPoint())?(side==0?"左":"右"):(side==0?"左の地点 →":"← 右の地点");
            var badge=new Rectangle((int)tip.X-44,(int)tip.Y-38,88,28);_ui.Box(badge,new Color(15,31,39));
            _ui.Center(label,badge,.38f,DirectSideColor(side));
        }
        _spriteBatch.End();GraphicsDevice.ScissorRectangle=new(canvas.X+(int)(panel.X*canvas.Width/1920f),canvas.Y+(int)(panel.Y*canvas.Height/1080f),(int)(panel.Width*canvas.Width/1920f),(int)(panel.Height*canvas.Height/1080f));
        _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);DrawDirectPairTrucks(panel);
        _spriteBatch.End();GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
        _ui.Center("右ドラッグ：地図を移動 / ホイール：拡縮",new(panel.X,879,panel.Width,28),.32f,Cream);
        _ui.Button(DirectPairFitButton,"両地点を表示",Muted,.45f);
        }finally{
            _directRenderPanel=null;_directPairNet.RestoreView(finalView);
            if(_directPairGlobe){_directPairWorld.GlobeZoom=finalZoom;_directPairWorld.GlobePan=finalPan;_directPairWorld.GlobeAnchor=finalAnchor;}
        }
    }
}
