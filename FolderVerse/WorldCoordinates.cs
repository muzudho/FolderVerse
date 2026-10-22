namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public static class WorldCoordinates
{
    public static readonly Color LongitudeColor=new(255,143,151),LatitudeColor=new(128,232,166);
    public const float MapFontScale=.42f;
    // The -X/+Z (chopsticks/belly) corner is the meridian. Belly-side columns are positive.
    public static Point At(WorldSetup world,SurfaceCell cell)
    {
        int w=world.PolarAxis==0?world.Height:world.Width,d=world.Depth,h=world.PolarAxis==0?world.Width:world.Height,p=2*(w+d);
        var center=world.Geographic(cell.Center);int face=world.GeographicFace(cell.Face);
        int x=(int)MathF.Floor(center.X+w/2f),z=(int)MathF.Floor(center.Z+d/2f);
        int longitude,latitude;
        if(face==2 || face==3)
        {
            int ring=Math.Min(Math.Min(x,w-1-x),Math.Min(z,d-1-z));
            var rim=world.Cells.Where(c=>c.Face==cell.Face).Select(c=>new {
                Cell=c,X=(int)MathF.Floor(world.Geographic(c.Center).X+w/2f),Z=(int)MathF.Floor(world.Geographic(c.Center).Z+d/2f)})
                .Where(c=>Math.Min(Math.Min(c.X,w-1-c.X),Math.Min(c.Z,d-1-c.Z))==ring)
                .OrderBy(c=>c.X==w-1-ring?0:c.Z==ring?1:c.X==ring?2:3)
                .ThenBy(c=>c.X==w-1-ring?-c.Z:c.Z==ring?-c.X:c.X==ring?c.Z:c.X).ToArray();
            int start=Array.FindIndex(rim,c=>c.X==ring && c.Z==d-1-ring);
            int index=Array.FindIndex(rim,c=>c.Cell.Id==cell.Id);
            longitude=((index-start+rim.Length)%rim.Length)*p/rim.Length;
            latitude=face==2?h-h/2+ring:-h/2-1-ring;
        }
        else
        {
            longitude=((face switch {0=>d-1-z,5=>d+w-1-x,1=>d+w+z,_=>2*d+w+x})+w)%p;
            latitude=(int)MathF.Floor(center.Y+h/2f)-h/2;
        }
        if(longitude>=p/2)longitude-=p;
        return new(longitude,latitude);
    }
    public static string Label(WorldSetup world,int cellId)
    {
        var at=At(world,world.Cells[cellId]);return world.CityNames[cellId]+$"（{at.X},{at.Y}）";
    }
    public static Dictionary<int,(bool Longitude,bool Latitude)> GlobeLabelComponents(WorldSetup world,IEnumerable<int> visible)
    {
        var coordinates=visible.Distinct().ToDictionary(id=>id,id=>At(world,world.Cells[id]));
        bool Interior(int id,bool longitude)
        {
            var cell=world.Cells[id];var coordinate=coordinates[id];
            var neighbors=cell.Neighbors.Where(n=>coordinates.ContainsKey(n) && world.Cells[n].Face==cell.Face)
                .Where(n=>longitude?coordinates[n].X==coordinate.X:coordinates[n].Y==coordinate.Y).ToArray();
            for(int i=0;i<neighbors.Length;i++)for(int j=i+1;j<neighbors.Length;j++)
            {
                var a=Vector3.Normalize(world.Cells[neighbors[i]].Center-cell.Center);
                var b=Vector3.Normalize(world.Cells[neighbors[j]].Center-cell.Center);
                if(Vector3.Dot(a,b)<-.99f)return true;
            }
            return false;
        }
        return coordinates.Keys.ToDictionary(id=>id,id=>
        {
            bool longitude=!Interior(id,true),latitude=!Interior(id,false);
            return (longitude,latitude);
        });
    }
    public static Dictionary<int,(bool Longitude,bool Latitude)> GlobeScreenLabelComponents(WorldSetup world,IReadOnlyDictionary<int,Vector2> positions)
    {
        var result=GlobeLabelComponents(world,positions.Keys);
        var sideCells=positions.Keys.Where(id=>!world.IsPolarFace(world.Cells[id].Face))
            .Select(id=>(Id:id,Face:world.Cells[id].Face,Coordinate:At(world,world.Cells[id]))).ToArray();
        foreach(var cell in sideCells)result[cell.Id]=(false,false);
        foreach(var column in sideCells.GroupBy(c=>(c.Face,c.Coordinate.X)))
        {
            int id=column.OrderBy(c=>positions[c.Id].Y).ThenBy(c=>positions[c.Id].X).ThenBy(c=>c.Id).First().Id;
            result[id]=(true,result[id].Latitude);
        }
        foreach(var row in sideCells.GroupBy(c=>(c.Face,c.Coordinate.Y)))
        {
            int id=row.OrderBy(c=>positions[c.Id].X).ThenBy(c=>positions[c.Id].Y).ThenBy(c=>c.Id).First().Id;
            result[id]=(result[id].Longitude,true);
        }
        return result;
    }
}
