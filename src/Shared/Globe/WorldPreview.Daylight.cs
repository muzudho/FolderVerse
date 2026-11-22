namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    public WorldSetup DaylightWorld {get;set;}
    public float DawnMeridian {get;set;}
    private VertexPositionColorNormal[] _daySource,_dayWorking;
    private float[] _dayPhases;
    private float _lastDawn=float.NaN;
    private bool _dayWasEnabled,_dayHadSpotlight;

    private void ApplyDaylight()
    {
        if(_daySource==null)return;
        if(DaylightWorld==null){
            if(_dayWasEnabled)_vertices.SetData(_daySource);
            _dayWasEnabled=false;_dayHadSpotlight=false;return;
        }
        bool spotlight=SelectionSetup!=null && (CountryFocus>=0 || NodeFocus>=0 || AttackTargetSpotlight);
        // Focus circles follow camera movement even when the time of day is unchanged.
        if(_dayWasEnabled && _lastDawn==DawnMeridian && !spotlight && !_dayHadSpotlight)return;
        if(_dayPhases==null){
            _dayPhases=new float[_daySource.Length];
            var poles=new Dictionary<(int Side,int X,int Z),float>();
            var size=DaylightWorld.Geographic(new Vector3(Width,Height,Depth));
            int width=(int)Math.Abs(size.X),depth=(int)Math.Abs(size.Z);
            foreach(var cell in DaylightWorld.Cells){
                int face=DaylightWorld.GeographicFace(cell.Face);if(face is not (2 or 3))continue;
                var p=DaylightWorld.Geographic(cell.Center);
                poles[(face==2?1:-1,(int)MathF.Floor(p.X+width*.5f),(int)MathF.Floor(p.Z+depth*.5f))]=WorldDayCycle.Phase(DaylightWorld,cell);
            }
            for(int i=0;i<_dayPhases.Length;i++){
                var vertex=_daySource[i];var normal=DaylightWorld.Geographic(vertex.Normal);var p=DaylightWorld.Geographic(vertex.Position);
                var key=(normal.Y>0?1:-1,Math.Clamp((int)MathF.Floor(p.X+width*.5f),0,width-1),Math.Clamp((int)MathF.Floor(p.Z+depth*.5f),0,depth-1));
                _dayPhases[i]=Math.Abs(normal.Y)>.5f && poles.TryGetValue(key,out float phase)?phase:WorldDayCycle.PhaseAt(DaylightWorld,vertex.Position);
            }
        }
        _dayWorking??=new VertexPositionColorNormal[_daySource.Length];
        for(int i=0;i<_daySource.Length;i++){
            var v=_daySource[i];float hour=WorldDayCycle.Wrap(DawnMeridian-_dayPhases[i]+8/24f)*24;
            var night=WorldDayCycle.Tint(v.Color,hour);
            float outside=1;
            if(spotlight && ProjectVisible(v.Position,v.Normal,FocusCircleYaw,FocusCirclePitch,FocusCircleArea,out var at))
                foreach(var circle in FocusCircles)outside=Math.Min(outside,SelectionGlow.OutsideCircle(at,circle.Center,circle.Radius));
            v.Color=Color.Lerp(v.Color,night,outside);_dayWorking[i]=v;
        }
        _vertices.SetData(_dayWorking);_lastDawn=DawnMeridian;_dayWasEnabled=true;_dayHadSpotlight=spotlight;
    }
}
