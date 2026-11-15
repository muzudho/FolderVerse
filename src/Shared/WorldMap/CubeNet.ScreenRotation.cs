namespace FolderVerse;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    public float ScreenAngle {get;set;}
    // The robot's belly is +Z (face 4). Match that face's on-screen roll,
    // including the same eased hinge motion used by the animated net.
    public Matrix BellyOrientation {
        get {
            var belly=System.Linq.Enumerable.First(Faces,f=>f.Face==4);
            float angle=System.MathF.Atan2(-belly.Up.X,belly.Right.X)+ScreenAngle;
            if(IsAnimating && _movingFaces.Contains(4)){
                float t=_animationProgress;
                angle+=_animationAngle*t*t*(3-2*t);
            }
            return Matrix.CreateRotationZ(-angle);
        }
    }
    private bool _drawing;
    private Rectangle? _badgePanel;
    public Matrix ScreenTransform(Rectangle panel)=>Matrix.CreateTranslation(-panel.Center.X,-panel.Center.Y,0)*Matrix.CreateRotationZ(ScreenAngle)*Matrix.CreateTranslation(panel.Center.X,panel.Center.Y,0);
    public Vector2 RotateScreen(Vector2 point,Rectangle panel)=>Vector2.Transform(point,ScreenTransform(panel));
    public Point UnrotateScreen(Point point,Rectangle panel)=>Vector2.Transform(new Vector2(point.X,point.Y),Matrix.Invert(ScreenTransform(panel))).ToPoint();
}
