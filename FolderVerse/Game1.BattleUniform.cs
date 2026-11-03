namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawBattleUniformPatch(Rectangle area,Rectangle robotArea,Rectangle damageArea,Robot robot,int army,Color primary,RobotParts part,float opacity)
    {
        int pattern=_setup.CountryPattern(army);
        var secondary=WorldSetup.Colors[_setup.CountrySecondaryColor(army)];
        for(int y=area.Top;y<area.Bottom;y++){
            int start=area.Left;Color prior=default;
            for(int x=area.Left;x<=area.Right;x++){
                Color color=default;
                if(x<area.Right){
                    color=WorldSetup.CountryPatternSecondary(pattern,(x-robotArea.X)/2,(y-robotArea.Y)/2)?secondary:primary;
                    if(robot.IsCheap(part) && y%6>=3)color=Cream;
                    if(robot.IsDamaged(part))color=RobotDamageArt.Tint(damageArea,x,y,color);
                    color*=opacity;
                }
                if(x==area.Left)prior=color;
                if(x==area.Right || color!=prior){_ui.Box(new(start,y,x-start,1),prior);start=x;prior=color;}
            }
        }
    }
}
