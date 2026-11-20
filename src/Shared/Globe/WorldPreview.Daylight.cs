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
    private bool _dayWasEnabled;

    private void ApplyDaylight()
    {
        if(_daySource==null)return;
        if(DaylightWorld==null){
            if(_dayWasEnabled)_vertices.SetData(_daySource);
            _dayWasEnabled=false;return;
        }
        if(_dayWasEnabled && _lastDawn==DawnMeridian)return;
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
            v.Color=WorldDayCycle.Tint(v.Color,hour);_dayWorking[i]=v;
        }
        _vertices.SetData(_dayWorking);_lastDawn=DawnMeridian;_dayWasEnabled=true;
    }
}
