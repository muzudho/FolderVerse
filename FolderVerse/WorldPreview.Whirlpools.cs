namespace FolderVerse;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    private VertexBuffer _whirlpoolMesh;
    private int _whirlpoolTriangles;
    private void BuildWhirlpoolMesh(WorldSetup world)
    {
        _whirlpoolMesh?.Dispose();_whirlpoolMesh=null;_whirlpoolTriangles=0;
        var mesh=new List<VertexPositionColorNormal>();const int detail=SeaWhirlpoolArt.Detail;
        foreach(var whirl in world.Routes.Whirlpools){
            var cell=world.Cells[whirl.Cell];
            Vector3 At(Vector2 p)=>cell.Origin+cell.U*((whirl.Center.X+.5f+p.X*whirl.Radius)/10)+cell.V*((whirl.Center.Y+.5f+p.Y*whirl.Radius)/10)+cell.Normal*(.012f+p.Length()*.035f);
            for(int y=0;y<detail;y++)for(int x=0;x<detail;x++){
                var p=new Vector2((x+.5f)*2/detail-1,(y+.5f)*2/detail-1);if(p.LengthSquared()>1)continue;
                float half=1f/detail;var a=At(p+new Vector2(-half,-half));var b=At(p+new Vector2(half,-half));var c=At(p+new Vector2(half,half));var d=At(p+new Vector2(-half,half));var color=SeaWhirlpoolArt.ColorAt(p);
                foreach(var point in new[]{a,b,c,a,c,d})mesh.Add(new(point,color,cell.Normal));
            }
        }
        if(mesh.Count==0)return;
        _whirlpoolMesh=new VertexBuffer(_device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);_whirlpoolMesh.SetData(mesh.ToArray());_whirlpoolTriangles=mesh.Count/3;
    }
    private void DrawWhirlpoolMesh()
    {
        if(_whirlpoolMesh==null)return;
        _device.SetVertexBuffer(_whirlpoolMesh);_effect.LightingEnabled=false;
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawPrimitives(PrimitiveType.TriangleList,0,_whirlpoolTriangles);}
        _effect.LightingEnabled=true;
    }
}
