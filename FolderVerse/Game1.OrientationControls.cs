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
    private Rectangle AxisButton(Rectangle toy,int axis,int sign)
    {
        float r=toy.Width*.39f;var c=new Vector2(toy.Center.X,toy.Center.Y);
        var p=axis switch{0=>c+new Vector2(sign*r,0),1=>c+new Vector2(0,-sign*r),_=>c+new Vector2(sign*r*.7071f,-r*.7071f)};
        return new((int)p.X-14,(int)p.Y-14,28,28);
    }
    private bool OrientationControlClick(Point pointer)
    {
        if(MapTransitionActive || OrientationRotating)return false;
        var toy=OrientationAreas(NetPanel,false).Toy;
        if(!toy.Contains(pointer))return false;
        for(int axis=_statusGlobe?0:2;axis<3;axis++)foreach(int sign in new[]{-1,1})
        {
            if(!AxisButton(toy,axis,sign).Contains(pointer))continue;
            _rotationGlobe=_statusGlobe;_orientationAge=0;_dragging=_mapDragging=false;_globePressedCell=-1;
            if(_statusGlobe){
                _orientationFrom=Quaternion.CreateFromRotationMatrix(_world.ViewOrientation(_yaw,_pitch));
                var vector=axis==0?Vector3.UnitX:axis==1?Vector3.UnitY:Vector3.UnitZ;
                // Screen axes originate at the front of the direction sphere; +Z turns clockwise.
                float angle=MathHelper.ToRadians(30)*sign*(axis==2?-1:1);
                _orientationTo=Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(_orientationFrom)*Matrix.CreateFromAxisAngle(vector,angle));
            }else{_rotationFrom=_net.ScreenAngle;_rotationTo=_rotationFrom+MathHelper.ToRadians(30)*sign;}
            return true;
        }
        return true;
    }
    private void UpdateOrientationRotation(float elapsed)
    {
        if(!OrientationRotating)return;
        _orientationAge+=elapsed;float t=MathHelper.Clamp(_orientationAge/.4f,0,1);t=t*t*(3-2*t);
        if(_rotationGlobe)_world.ManualOrientation=Quaternion.Slerp(_orientationFrom,_orientationTo,t);
        else _net.ScreenAngle=MathHelper.Lerp(_rotationFrom,_rotationTo,t);
        if(_orientationAge>=.4f)_orientationAge=-1;
    }
    private void DrawOrientationControls(Rectangle toy)
    {
        var c=new Vector2(toy.Center.X,toy.Center.Y);float r=toy.Width*.39f;
        void Line(Vector2 a,Vector2 b,Color color){var d=b-a;_ui.Tile((a+b)/2,new(d.Length(),2),MathF.Atan2(d.Y,d.X),color);}
        void Arrow(Vector2 p,Vector2 direction,Color color){var side=new Vector2(-direction.Y,direction.X);Line(p,p-direction*9+side*5,color);Line(p,p-direction*9-side*5,color);}
        // The two great circles through the front project to a cross; the third is the rim.
        if(_statusGlobe)for(int axis=0;axis<2;axis++){
            var d=axis==0?Vector2.UnitX:-Vector2.UnitY;var color=AxisColors[axis];
            Line(c-d*r,c+d*r,color);Arrow(c+d*(r-19),d,color);Arrow(c-d*(r-19),-d,color);
        }
        if(_statusGlobe){_ui.Text("X",c+new Vector2(r-28,-23),.3f,AxisColors[0]);_ui.Text("Y",c+new Vector2(18,-r+14),.3f,AxisColors[1]);}
        _ui.Text("Z",c+new Vector2(-r*.72f,r*.48f),.3f,AxisColors[2]);
        var blue=AxisColors[2];
        // Separate arcs show both directions, with each arrow ending at its sign button.
        foreach(int sign in new[]{-1,1}){
            float end=sign==1?-MathHelper.PiOver4:-3*MathHelper.PiOver4;
            float start=end-sign*MathHelper.Pi*.72f;
            Vector2 At(float a)=>c+new Vector2(MathF.Cos(a),MathF.Sin(a))*r;
            for(int i=0;i<32;i++)Line(At(MathHelper.Lerp(start,end,i/32f)),At(MathHelper.Lerp(start,end,(i+1)/32f)),blue);
            var tip=At(end-sign*.23f);Arrow(tip,new Vector2(-MathF.Sin(end-sign*.23f),MathF.Cos(end-sign*.23f))*sign,blue);
        }
        for(int axis=_statusGlobe?0:2;axis<3;axis++)foreach(int sign in new[]{-1,1})
            _ui.Button(AxisButton(toy,axis,sign),sign>0?"+":"－",AxisColors[axis],.52f);
    }
}
