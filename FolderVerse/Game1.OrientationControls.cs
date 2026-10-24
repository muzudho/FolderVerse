namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private float _orientationAge=-1,_rotationFrom,_rotationTo;
    private Quaternion _orientationFrom,_orientationTo;
    private bool _rotationGlobe;
    private bool OrientationRotating=>_orientationAge>=0;
    private static readonly Color[] AxisColors={new(244,91,91),new(89,216,126),new(95,156,255)};
    private bool OrientationGlobe=>!IsStatusScreen || _statusGlobe;
    private bool OrientationControlsVisible=>!IsRolling && (IsStatusScreen || _screen>=Screen.Rolling && _screen<=Screen.PlayerReady);
    private Rectangle OrientationControlArea {
        get {
            if(IsStatusScreen)return MapViewBounds;
            var globe=SelectionGlobe;
            var area=IsSelectionScreen?new Rectangle(globe.X+8,globe.Y+8,globe.Width-16,globe.Height-48):IsCastScreen?CastPreview:PreviewArea;
            return OrientationAreas(area,IsCastScreen && !IsSelectionScreen).Globe;
        }
    }
    private Rectangle OrientationToyArea=>OrientationAreas(OrientationControlArea,false).Toy;
    private float _automaticBoostAge=-1;
    private int _automaticBoostAxis,_automaticBoostSign;
    private readonly System.Collections.Generic.Queue<(int Axis,int Sign)> _automaticBoostQueue=new();
    private const float AutomaticBoostSeconds=3;
    private bool AutomaticOrientation=>!IsStatusScreen && (IsRolling || IsSelectionScreen || IsCastScreen) && !_dragging;
    private Rectangle AxisButton(Rectangle toy,int axis,int sign)
    {
        float r=toy.Width/2f+23;var c=new Vector2(toy.Center.X,toy.Center.Y);
        Vector2 p;
        if(OrientationGlobe){
            p=axis switch{0=>c+new Vector2(r,sign>0?17:-17),1=>c+new Vector2(sign*17,-r),_=>c+new Vector2(r*.7071f,-r*.7071f)+new Vector2(sign*18,sign*18)};
        }else{
            float a=sign>0?MathHelper.PiOver4:3*MathHelper.PiOver4;
            p=c+new Vector2(MathF.Cos(a),MathF.Sin(a))*r;
        }
        return new((int)p.X-14,(int)p.Y-14,28,28);
    }
    private bool OrientationControlClick(Point pointer)
    {
        if(!OrientationControlsVisible || MapTransitionActive || OrientationRotating)return false;
        var toy=OrientationToyArea;
        if(OrientationResetButton.Contains(pointer)){_automaticBoostAge=-1;_automaticBoostQueue.Clear();if(OrientationGlobe)ResetStatusOrientation();else {_net.ScreenAngle=-MathHelper.ToRadians(_net.Rotation*90);RecordTutorialMapRotation(2);}return true;}
        for(int axis=OrientationGlobe?0:2;axis<3;axis++)foreach(int sign in new[]{-1,1})
        {
            if(!AxisButton(toy,axis,sign).Contains(pointer))continue;
            if(AutomaticOrientation){if(_automaticBoostAge>=0){_automaticBoostQueue.Enqueue((axis,sign));return true;}_automaticBoostAge=0;_automaticBoostAxis=axis;_automaticBoostSign=sign;return true;}
            _rotationGlobe=OrientationGlobe;_orientationAge=0;_dragging=_mapDragging=false;_globePressedCell=-1;
            if(OrientationGlobe){
                _orientationFrom=Quaternion.CreateFromRotationMatrix(_world.ViewOrientation(_yaw,_pitch));
                var vector=axis==0?Vector3.UnitX:axis==1?Vector3.UnitY:Vector3.UnitZ;
                float angle=MathHelper.ToRadians(30)*sign*(axis==2?-1:1);
                _orientationTo=Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(_orientationFrom)*Matrix.CreateFromAxisAngle(vector,angle));
            }else{_rotationFrom=_net.ScreenAngle;_rotationTo=_rotationFrom+MathHelper.ToRadians(30)*sign;RecordTutorialMapRotation(sign>0?1:4);}
            return true;
        }
        return toy.Contains(pointer);
    }
    private void AdvanceAutomaticOrientation(float elapsed)
    {
        var orientation=_world.ViewOrientation(_yaw,_pitch);
        float angle=elapsed*.3f;_yawAngle+=angle;
        orientation*=Matrix.CreateRotationY(angle);
        if(_automaticBoostAge>=0){
            // Integral of a smooth velocity pulse: exactly 30 degrees, then the normal speed.
            float Ease(float t){t=MathHelper.Clamp(t/AutomaticBoostSeconds,0,1);return t*t*(3-2*t);}
            float extra=(Ease(_automaticBoostAge+elapsed)-Ease(_automaticBoostAge))*MathHelper.ToRadians(30)*_automaticBoostSign;
            var axis=_automaticBoostAxis==0?Vector3.UnitX:_automaticBoostAxis==1?Vector3.UnitY:Vector3.UnitZ;
            orientation*=Matrix.CreateFromAxisAngle(axis,extra*(_automaticBoostAxis==2?-1:1));
            _automaticBoostAge+=elapsed;if(_automaticBoostAge>=AutomaticBoostSeconds){_automaticBoostAge=-1;if(_automaticBoostQueue.Count>0){var next=_automaticBoostQueue.Dequeue();_automaticBoostAxis=next.Axis;_automaticBoostSign=next.Sign;_automaticBoostAge=0;}}
        }
        _world.ManualOrientation=Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(orientation));
    }
    private void UpdateOrientationRotation(float elapsed)
    {
        if(!AutomaticOrientation){_automaticBoostAge=-1;_automaticBoostQueue.Clear();}
        if(!OrientationRotating)return;
        _orientationAge+=elapsed;float t=MathHelper.Clamp(_orientationAge/.4f,0,1);t=t*t*(3-2*t);
        if(_rotationGlobe)_world.ManualOrientation=Quaternion.Slerp(_orientationFrom,_orientationTo,t);
        else _net.ScreenAngle=MathHelper.Lerp(_rotationFrom,_rotationTo,t);
        if(_orientationAge>=.4f)_orientationAge=-1;
    }
    private void DrawOrientationControls(Rectangle toy)
    {
        if(!OrientationControlsVisible)return;
        var c=new Vector2(toy.Center.X,toy.Center.Y);float r=toy.Width/2f+23;
        void Line(Vector2 a,Vector2 b,Color color){var d=b-a;_ui.Tile((a+b)/2,new(d.Length(),2),MathF.Atan2(d.Y,d.X),color);}
        void Arrow(Vector2 p,Vector2 direction,Color color){var side=new Vector2(-direction.Y,direction.X);Line(p,p-direction*9+side*5,color);Line(p,p-direction*9-side*5,color);}
        if(OrientationGlobe){
            float rim=toy.Width/2f,tip=rim+23;
            foreach(int axis in new[]{0,1}){
                var d=axis==0?Vector2.UnitX:-Vector2.UnitY;
                Line(c+d*(rim+2),c+d*(tip+17),AxisColors[axis]);
                _ui.Text(axis==0?"X":"Y",c+d*(tip+21)-new Vector2(5,7),.3f,AxisColors[axis]);
            }
            var dot=c+Vector2.Normalize(new Vector2(1,-1))*r;
            for(int y=-4;y<4;y++){float half=MathF.Sqrt(16-(y+.5f)*(y+.5f));_ui.Box(new((int)MathF.Ceiling(dot.X-half),(int)dot.Y+y,(int)(2*half),1),AxisColors[2]);}
        }else{
        var blue=AxisColors[2];
        // Separate arcs show both directions, with each arrow ending at its sign button.
        foreach(int sign in new[]{-1,1}){
            float end=sign==1?MathHelper.PiOver4:-5*MathHelper.PiOver4;
            float start=-MathHelper.PiOver2+sign*.25f;
            end-=sign*.18f;
            Vector2 At(float a)=>c+new Vector2(MathF.Cos(a),MathF.Sin(a))*r;
            for(int i=0;i<32;i++)Line(At(MathHelper.Lerp(start,end,i/32f)),At(MathHelper.Lerp(start,end,(i+1)/32f)),blue);
            var tip=At(end-sign*.23f);Arrow(tip,new Vector2(-MathF.Sin(end-sign*.23f),MathF.Cos(end-sign*.23f))*sign,blue);
        }
        }
        for(int axis=OrientationGlobe?0:2;axis<3;axis++)foreach(int sign in new[]{-1,1})
            _ui.Button(AxisButton(toy,axis,sign),sign>0?"+":"－",AxisColors[axis],.52f);
        if(_automaticBoostAge>=0){float t=MathHelper.Clamp(_automaticBoostAge/AutomaticBoostSeconds,0,1);int degrees=(int)MathF.Round(30*t*t*(3-2*t));_ui.Text("XYZ"[_automaticBoostAxis]+(_automaticBoostSign>0?" +":" -")+degrees+" / 30°",new(toy.Center.X-65,toy.Bottom+48),.3f,AxisColors[_automaticBoostAxis]);}
        var reset=OrientationResetButton;
        _ui.Button(reset,OrientationGlobe?"":"0",OrientationGlobe?Muted:AxisColors[2],.5f);
        if(OrientationGlobe){
            for(int axis=0;axis<3;axis++){_ui.Text("0",new(reset.X+8+axis*25,reset.Y+4),.5f,AxisColors[axis]);if(axis<2)_ui.Text(",",new(reset.X+23+axis*25,reset.Y+4),.5f,Cream);}
        }
        if(OrientationGlobe && OrientationResetButton.Contains(_pointer))_ui.Text("頭を上へ、腹を手前へ",new Vector2(toy.Center.X-130,toy.Bottom+66),.3f,Cream);
    }
}
