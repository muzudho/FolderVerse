namespace FolderVerse;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    public static Rectangle MapViewport(Rectangle panel)=>new(panel.Left-50,panel.Top-14,panel.Width+90,panel.Height+18);
}
