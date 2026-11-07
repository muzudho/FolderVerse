namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawBattleUniformPatch(Rectangle area,Rectangle robotArea,Rectangle damageArea,Robot robot,int army,Color primary,RobotParts part,float opacity)
    {
        DrawRobotPartFill(area,robotArea,damageArea,robot,part,army,primary,opacity);
    }
}
