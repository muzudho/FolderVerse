namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle DirectNodePreviewBounds(int side)=>new(57+side*960,121,122,122);
    private void DrawDirectNodePreview(int side,int id)
    {
        var bounds=DirectNodePreviewBounds(side);var center=bounds.Center.ToVector2();
        var node=_setup.Nodes.All[id];var mark=NodeLandmarks.At(_setup,id);var cell=_setup.Cells[node.Cell];
        void Disc(Vector2 at,float radius,Color color){for(int y=-(int)radius;y<=radius;y++){int half=(int)MathF.Sqrt(Math.Max(0,radius*radius-y*y));_ui.Box(new((int)at.X-half,(int)at.Y+y,half*2+1,1),color);}}
        Disc(center,60,Accent);Disc(center,57,new Color(15,31,39));
        Disc(center,54,_setup.Routes.Land(node.Cell,node.Center)?new Color(63,94,67):new Color(36,84,111));
        // A north-up local view uses the same geographic frame as the landmark description.
        var normal=Vector3.Normalize(_setup.Geographic(cell.Normal));var north=Vector3.UnitY-normal*normal.Y;
        if(north.LengthSquared()<.001f)north=-_setup.Geographic(cell.V);
        north.Normalize();var east=Vector3.Normalize(Vector3.Cross(north,normal));
        var delta=_setup.Geographic(mark.Position-_setup.Routes.Position(node.Cell,node.Center));
        var direction=Vector2.Normalize(new Vector2(Vector3.Dot(delta,east),-Vector3.Dot(delta,north)));
        var origin=center-direction*22;var destination=center+direction*22;
        var line=destination-origin;_ui.Tile((origin+destination)/2,new(line.Length(),2),MathF.Atan2(line.Y,line.X),Cream*.6f);
        float size=Math.Min(11,36f/Math.Max(1,mark.Objects.Length)/1.9f);
        for(int i=0;i<mark.Objects.Length;i++)LandmarkArt.Draw(_ui,mark.Objects[i],destination+new Vector2((i-(mark.Objects.Length-1)/2f)*size*1.9f,0),size,_animationTime);
        Disc(origin,9,new Color(15,31,39));Disc(origin,7,_setup.OwnerColor(node.Owner));Disc(origin,2,Cream);
        _ui.Text("北",new(center.X-6,bounds.Y+5),.25f,Cream);
    }
}
