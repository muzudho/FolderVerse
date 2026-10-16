namespace FolderVerse;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    public float ScreenAngle {get;set;}
    private bool _drawing;
    private Rectangle? _badgePanel;
    public Matrix ScreenTransform(Rectangle panel)=>Matrix.CreateTranslation(-panel.Center.X,-panel.Center.Y,0)*Matrix.CreateRotationZ(ScreenAngle)*Matrix.CreateTranslation(panel.Center.X,panel.Center.Y,0);
    public Vector2 RotateScreen(Vector2 point,Rectangle panel)=>Vector2.Transform(point,ScreenTransform(panel));
    public Point UnrotateScreen(Point point,Rectangle panel)=>Vector2.Transform(new Vector2(point.X,point.Y),Matrix.Invert(ScreenTransform(panel))).ToPoint();
}
