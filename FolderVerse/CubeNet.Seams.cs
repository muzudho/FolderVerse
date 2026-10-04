namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed record SeamLabel(int Face,int Side,string Name,Vector2 A,Vector2 B,Rectangle Bounds);
public sealed partial class CubeNet
{
    private static int OtherFace(NetFace face,int side)=>Array.IndexOf(Normals,side switch{0=>face.Up,1=>face.Right,2=>-face.Up,_=>-face.Right});
    public string HoveredSeam=>HoveredFace<0?"":Seam(HoveredFace,OtherFace(Faces.First(f=>f.Face==HoveredFace),HoveredSide));
    public SeamLabel[] SeamLabels(Rectangle panel)
    {
        if(IsAnimating)return Array.Empty<SeamLabel>();
        var result=new List<SeamLabel>();var fit=Fit(panel);
        foreach(var face in Faces)for(int side=0;side<4;side++)
        {
            int other=OtherFace(face,side);if(Joined(face,Faces.First(f=>f.Face==other)))continue;
            var edge=EdgeEnds(face,side);var a=edge.A*fit.Scale+fit.Origin;var b=edge.B*fit.Scale+fit.Origin;
            var position=(a+b)/2+(side switch{0=>new Vector2(0,-16),1=>new Vector2(16,0),2=>new Vector2(0,16),_=>new Vector2(-16,0)});
            result.Add(new(face.Face,side,Seam(face.Face,other),a,b,new((int)position.X-11,(int)position.Y-12,22,24)));
        }
        return result.ToArray();
    }
    private void DrawSeamLabels(UiPainter ui,Rectangle panel)
    {
        foreach(var label in SeamLabels(panel))
        {
            bool active=label.Name==HoveredSeam;
            ui.Box(label.Bounds,active?new Color(255,230,112):new Color(20,42,54));
            ui.Center(label.Name,label.Bounds,.40f,active?new Color(28,46,59):new Color(255,230,158));
        }
    }
}
