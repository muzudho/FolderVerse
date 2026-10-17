namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

// Snapshot for presentation only: never changes ownership, ranking or player state.
public sealed class ConquestPresentation
{
    public sealed record Card(int Ruler,int Rank,int Depth,bool Reminder,int[] Faces);
    public Card[] Cards {get;}
    public static int ExactCellOwner(WorldSetup w,int cell){var owners=w.Nodes.InCell(cell).Select(n=>n.Owner).Distinct().ToArray();return owners.Length==1?owners[0]:-1;}
    public static int[] OwnCells(WorldSetup w,int ruler)=>w.Cells.Where(c=>ExactCellOwner(w,c.Id)==ruler).Select(c=>c.Id).ToArray();
    public static int[] FaceTour(WorldSetup w,int ruler,int startFace=4){
        var faces=OwnCells(w,ruler).Select(c=>w.Cells[c].Face).Concat(w.Nodes.All.Where(n=>n.Owner==ruler).Select(n=>w.Cells[n.Cell].Face)).Distinct().OrderBy(f=>f).ToArray();
        if(faces.Length<2)return faces;
        Vector3 Normal(int face)=>w.Cells.First(c=>c.Face==face).Normal;
        int[] best=faces;float cost=float.MaxValue;
        void Visit(List<int> path,float length){
            if(length>=cost)return;
            if(path.Count==faces.Length){best=path.ToArray();cost=length;return;}
            int previous=path.Count==0?startFace:path[^1];
            foreach(int face in faces)if(!path.Contains(face)){
                float angle=MathF.Acos(Math.Clamp(Vector3.Dot(Normal(previous),Normal(face)),-1,1));
                path.Add(face);Visit(path,length+angle);path.RemoveAt(path.Count-1);
            }
        }
        Visit(new(),0);return best;
    }
    public ConquestPresentation(WorldSetup w){
        var forest=w.Relations.RankedForest(w);var byId=forest.ToDictionary(e=>e.Ruler);var cards=new List<Card>();int lastFace=4;
        void Add(int r,bool reminder){var e=byId[r];var faces=FaceTour(w,r,lastFace);cards.Add(new(r,e.Rank,e.Depth+1,reminder,faces));if(faces.Length>0)lastFace=faces[^1];}
        foreach(var entry in forest){
            var parents=new Stack<int>();for(int r=w.Relations.Superiors[entry.Ruler];r>=0;r=w.Relations.Superiors[r])parents.Push(r);
            foreach(int r in parents)Add(r,true);Add(entry.Ruler,false);
        }
        Cards=cards.ToArray();
    }
    public static Vector3 Environment(WorldSetup w,int ruler){
        var cells=OwnCells(w,ruler).Select(id=>w.Cells[id]).ToArray();if(cells.Length==0)return Vector3.Zero;
        int sea=0,mountain=0,polar=0,total=0;var offset=WorldTerrain.Offset(w.WorldSeed);
        foreach(var c in cells)for(int y=0;y<10;y++)for(int x=0;x<10;x++){
            var kind=WorldTerrain.Kind(WorldTerrain.Elevation(c.Origin+c.U*((x+.5f)/10)+c.V*((y+.5f)/10),offset));
            if(Math.Abs(c.Normal.Y)>.5f)polar++;if(kind==TerrainKind.Sea)sea++;if(kind==TerrainKind.Mountain)mountain++;total++;
        }
        return new(polar/(float)total,sea/(float)total,mountain/(float)total);
    }
}
