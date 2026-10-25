namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private readonly CubeNet _directPairNet=new();
    private float _directPairSlide=1;
    private static readonly Rectangle DirectPairMapBounds=new(600,200,720,690),DirectPairFitButton=new(800,912,320,44);
    private Color DirectSideColor(int side)=>side==0?_setup.OwnerColor(_setup.PlayerSlot):Color.White;
    private void FitDirectPairMap()
    {
        var panel=DirectPairMapBounds;
        _directPairNet.Home(_setup);_directPairNet.SetCenter(_setup,_setup.Cells[_setup.Nodes.All[_directNodes[0]].Cell].Face);
        _directPairNet.ShowRoutes=true;_directPairNet.ShowOtherCountries=_net.ShowOtherCountries;
        _directPairNet.ShowRobots=_net.ShowRobots;_directPairNet.ShowBatteries=_net.ShowBatteries;
        _directPairNet.ShowMapSymbols=false;_directPairNet.ShowCountryColors=_net.ShowCountryColors;
        _directPairNet.CountryFocus=_setup.PlayerSlot;
        _directPairNet.FitAll(panel);
        var a=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[0]],panel);
        var b=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[1]],panel);
        float factor=Math.Min((panel.Width-240)/Math.Max(1,Math.Abs(a.X-b.X)),(panel.Height-240)/Math.Max(1,Math.Abs(a.Y-b.Y)));
        _directPairNet.SetFitCamera(panel,Math.Min(500,_directPairNet.Fit(panel).Scale*factor),Vector2.Zero);
        a=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[0]],panel);b=_directPairNet.NodePosition(_setup,_setup.Nodes.All[_directNodes[1]],panel);
        _directPairNet.Drag(panel.Center.ToVector2()-(a+b)/2);
    }
    private void DirectPairMapInput(MouseState mouse)
    {
        if(!_directPairOpen || _directDrag.HasValue)return;
        if(mouse.LeftButton==ButtonState.Pressed && _previousMouse.LeftButton==ButtonState.Released && DirectPairFitButton.Contains(_pointer)){FitDirectPairMap();return;}
        if(!DirectPairMapBounds.Contains(_pointer))return;
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        if(wheel!=0)_directPairNet.ZoomAt(DirectPairMapBounds,_pointer,wheel);
        if(mouse.RightButton==ButtonState.Pressed && _previousMouse.RightButton==ButtonState.Pressed){var before=CanvasPoint(_previousMouse);_directPairNet.Drag(new Vector2(_pointer.X-before.X,_pointer.Y-before.Y));}
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
        var panel=DirectPairMapBounds;var canvas=CanvasBounds();
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _ui.Box(panel,new Color(20,42,54));
        _ui.Center("地図ビュー / 展開図",new(panel.X,165,panel.Width,30),.48f,Cream);
        _directPairNet.FocusCircles.Clear();_directPairNet.FocusCircleNodes.Clear();
        var points=new Vector2[2];
        for(int side=0;side<2;side++){
            int id=_directNodes[side];var at=_directPairNet.NodePosition(_setup,_setup.Nodes.All[id],panel);points[side]=at;
            int count=_directPairNet.ShowRobots?_setup.Robots.Nodes[id].Count:0;float radius=count>0?Math.Max(48,count*30/MathHelper.TwoPi)+38:32;
            _directPairNet.FocusCircles.Add(new(at,radius));_directPairNet.FocusCircleNodes.Add(id);
        }
        foreach(var node in _setup.Nodes.All){var at=_directPairNet.NodePosition(_setup,node,panel);if(_directPairNet.FocusCircles.Any(c=>Vector2.DistanceSquared(at,c.Center)<=c.Radius*c.Radius))_directPairNet.FocusCircleNodes.Add(node.Id);}
        var old=GraphicsDevice.ScissorRectangle;_spriteBatch.End();
        using var clip=new RasterizerState{ScissorTestEnable=true};
        GraphicsDevice.ScissorRectangle=new(canvas.X+(int)(panel.X*canvas.Width/1920f),canvas.Y+(int)(panel.Y*canvas.Height/1080f),(int)(panel.Width*canvas.Width/1920f),(int)(panel.Height*canvas.Height/1080f));
        _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
        _directPairNet.Draw(_ui,_setup,panel,-1,_animationTime,false);_directPairNet.DrawMapObjects(_ui,_setup,panel,_animationTime);
        _spriteBatch.End();GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
        for(int side=0;side<2;side++){
            var at=points[side];var edge=new Vector2(side==0?580:1340,Math.Clamp(at.Y,250,860));
            var tip=new Vector2(Math.Clamp(at.X,panel.Left+12,panel.Right-12),Math.Clamp(at.Y,panel.Top+12,panel.Bottom-12));
            // A tapered speech-bubble tail links the fixed pane to its geographic endpoint.
            DrawDirectTail(edge,tip,DirectSideColor(side));
            string label=panel.Contains(at.ToPoint())?(side==0?"左":"右"):(side==0?"左の地点 →":"← 右の地点");
            var badge=new Rectangle((int)tip.X-44,(int)tip.Y-38,88,28);_ui.Box(badge,new Color(15,31,39));
            _ui.Center(label,badge,.38f,DirectSideColor(side));
        }
        _ui.Center("右ドラッグ：地図を移動 / ホイール：拡縮",new(panel.X,879,panel.Width,28),.32f,Cream);
        _ui.Button(DirectPairFitButton,"両地点を表示",Muted,.45f);
    }
}
