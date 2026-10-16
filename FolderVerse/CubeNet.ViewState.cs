namespace FolderVerse;
using Microsoft.Xna.Framework;
public sealed record CubeNetViewState(int CenterFace,int Rotation,float Zoom,Vector2 Pan,NetFace[] Faces);
public sealed partial class CubeNet{
 public CubeNetViewState CaptureView()=>new(CenterFace,Rotation,Zoom,Pan,(NetFace[])Faces.Clone());
 public void RestoreView(CubeNetViewState view){CancelAnimation();CenterFace=view.CenterFace;Rotation=view.Rotation;Zoom=view.Zoom;Pan=view.Pan;Faces=(NetFace[])view.Faces.Clone();}
}
