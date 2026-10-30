namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _globeDragSlide,_netDragRotate,_mapDragListOpen;
    private bool MapDragSlide=>_statusGlobe?_globeDragSlide:!_netDragRotate;
    private string MapDragName=>(_statusGlobe?"地星儀":"展開図")+"を"+(MapDragSlide?"スライド":"回転");
    private static readonly Rectangle MapDragButton=new(350,82,270,48);
    private static Rectangle MapDragOption(int index)=>new(350,134+index*52,414,52);
    private bool MapDragClick(Point pointer)
    {
        if(_mapDragListOpen){
            _mapDragListOpen=false;
            for(int i=0;i<2;i++)if(MapDragOption(i).Contains(pointer)){
                if(_statusGlobe)_globeDragSlide=i==1;else _netDragRotate=i==0;
            }
            _dragging=_mapDragging=false;return true;
        }
        if(!MapDragButton.Contains(pointer))return false;
        _mapDragListOpen=true;_mapLayerListOpen=_trafficLayerListOpen=false;
        _dragging=_mapDragging=false;return true;
    }
    private void DrawMapDragButton()
    {
        _ui.Text("右マウスボタンドラッグ：",new(40,97),.45f,Cream);
        _ui.Button(MapDragButton,MapDragName,Accent,.5f);
        for(int i=0;i<8;i++)_ui.Box(new(MapDragButton.Right-26+i,MapDragButton.Center.Y-3+i,15-i*2,1),SelectionInk);
        if(_mapDragListOpen)for(int i=0;i<2;i++)_ui.Button(MapDragOption(i),(_statusGlobe?"地星儀":"展開図")+"を"+(i==0?"回転":"スライド"),(i==1)==MapDragSlide?Accent:Muted,.5f);
    }
    private void DragStatusMap(MouseState mouse,bool active)
    {
        bool held=active && mouse.RightButton==ButtonState.Pressed;
        if(!held || MapTransitionActive || OrientationRotating || _net.IsAnimating || _mapDragListOpen){_mapDragging=_dragging=false;return;}
        if(_previousMouse.RightButton!=ButtonState.Pressed && MapViewBounds.Contains(_pointer))_mapDragging=true;
        if(!_mapDragging || _previousMouse.RightButton!=ButtonState.Pressed)return;
        var old=CanvasPoint(_previousMouse);var delta=new Vector2(_pointer.X-old.X,_pointer.Y-old.Y);
        if(_statusGlobe){
            if(MapDragSlide)_world.GlobePan+=new Vector2(delta.X/StatusGlobeArea.Width,delta.Y/StatusGlobeArea.Height);
            else {var orientation=Quaternion.CreateFromRotationMatrix(_world.ViewOrientation(_yaw,_pitch));_world.ManualOrientation=Arcball.Drag(orientation,old,_pointer,StatusGlobeArea);}
        }else if(MapDragSlide)_net.Drag(delta);
        else _net.ScreenAngle+=delta.X*.01f;
    }
}
