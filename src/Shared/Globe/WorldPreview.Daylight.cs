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
    private MapTerrainTile[] _dayTileMap;
    private int _dayTerrainVertexCount;
    private void ResetDaylightMesh(VertexPositionColorNormal[] source,int terrainVertexCount){
        _daySource=source;_dayTerrainVertexCount=terrainVertexCount;
        _dayTileMap=null;_dayWorking=null;_dayPhases=null;_dayWasEnabled=false;_lastDawn=float.NaN;
        _lastSelectedCell=-1;_lastHighlightTiles.Clear();
    }
    private readonly HashSet<MapTerrainTile> _lastHighlightTiles=new();
    private int _lastSelectedCell=-1;
    private void ApplyDaylight()
    {
        if(_daySource==null)return;
        var setup=DaylightWorld??SelectionSetup;
        bool daylight=DaylightWorld!=null;
        if(setup==null){if(_dayWasEnabled)_vertices.SetData(_daySource);_dayWasEnabled=false;return;}
        if(_dayWasEnabled && _lastDawn==(daylight?DawnMeridian:float.MinValue) && _lastSelectedCell==SelectedCell && _lastHighlightTiles.SetEquals(HighlightTiles))return;
        if(daylight){
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

        }
        if(_dayTileMap==null){
            _dayTileMap=new MapTerrainTile[_daySource.Length];
            Array.Fill(_dayTileMap,new MapTerrainTile(-1,0,0));
            // Only the terrain prefix consists of six-vertex quads; models contain arbitrary triangles.
            for(int i=0;i+6<=_dayTerrainVertexCount;i+=6){
                var center=Vector3.Zero;for(int j=0;j<6;j++)center+=_daySource[i+j].Position/6;
                var tile=new MapTerrainTile(-1,0,0);
                foreach(var cell in setup.Cells){
                    if(Vector3.Dot(cell.Normal,_daySource[i].Normal)<.99f)continue;
                    var delta=center-cell.Origin;float u=Vector3.Dot(delta,cell.U),v=Vector3.Dot(delta,cell.V);
                    if(u<0 || u>=1 || v<0 || v>=1 || Math.Abs(Vector3.Dot(delta,cell.Normal))>WorldTerrain.MaxRelief+.02f)continue;
                    tile=MapTerrainTile.At(cell,center);break;
                }
                for(int j=0;j<6;j++)_dayTileMap[i+j]=tile;
            }
        }
        _dayWorking??=new VertexPositionColorNormal[_daySource.Length];
        for(int i=0;i<_daySource.Length;i++){
            var v=_daySource[i];var tile=_dayTileMap[i];
            if(i<_dayTerrainVertexCount)v.Color=MapTerrainColors.ForTile(v.Color,tile.Cell>=0 && (HighlightTiles.Contains(tile) || tile.Cell==SelectedCell));
            if(daylight){float hour=WorldDayCycle.Wrap(DawnMeridian-_dayPhases[i]+8/24f)*24;v.Color=WorldDayCycle.Tint(v.Color,hour);}
            _dayWorking[i]=v;
        }
        _vertices.SetData(_dayWorking);_lastDawn=daylight?DawnMeridian:float.MinValue;_dayWasEnabled=true;
        _lastSelectedCell=SelectedCell;_lastHighlightTiles.Clear();_lastHighlightTiles.UnionWith(HighlightTiles);
    }
}
