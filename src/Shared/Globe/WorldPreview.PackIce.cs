namespace FolderVerse;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    private void AppendPackIce(WorldSetup world,List<VertexPositionColorNormal> mesh,System.Func<int,float> brightness)
    {
        Color Tint(Color color,int cell)=>brightness==null?color:Color.Lerp(Color.Black,color,brightness(cell));
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Color color){foreach(var p in new[]{a,b,c,a,c,d})mesh.Add(new(p,color,normal));}
        foreach(var cell in world.Cells)foreach(var floe in SeaIceArt.Floes(world,cell.Id)){
            Vector3 Position(Vector2 p)=>cell.Origin+cell.U*(p.X/10)+cell.V*(p.Y/10)+cell.Normal*.015f;
            var center=Vector2.Zero;foreach(var p in floe.Points)center+=p;center/=floe.Points.Length;
            var color=Tint(floe.Color,cell.Id);
            for(int i=0;i<floe.Points.Length;i++)foreach(var p in new[]{center,floe.Points[i],floe.Points[(i+1)%floe.Points.Length]})mesh.Add(new(Position(p),color,cell.Normal));
        }
        foreach(var wall in world.Routes.IceWalls){
            var normal=world.Cells[wall.Cell].Normal;var direction=Vector3.Normalize(wall.B-wall.A);var width=Vector3.Cross(normal,direction)*.016f;var lift=normal*.018f;
            Quad(wall.A-width+lift,wall.B-width+lift,wall.B+width+lift,wall.A+width+lift,normal,Tint(new Color(230,249,253),wall.Cell));
        }
    }
}
