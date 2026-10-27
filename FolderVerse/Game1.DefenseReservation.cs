namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly Dictionary<long,(float Start,int Country)> _defenseReservationEffects=new();
    private void BeginDefenseReservationEffect(Robot robot)
    {
        foreach(var id in _defenseReservationEffects.Where(e=>_animationTime-e.Value.Start>1.3f).Select(e=>e.Key).ToArray())_defenseReservationEffects.Remove(id);
        _defenseReservationEffects[robot.Id]=(_animationTime,robot.ShoeOwner);
    }
    private float DefenseShoeProgress(Robot robot)
        =>_defenseReservationEffects.TryGetValue(robot.Id,out var effect) && effect.Country==robot.ShoeOwner?Math.Clamp((_animationTime-effect.Start-.28f)/.45f,0,1):1;
    private float DefenseFlagWave()
    {
        float age=_defenseReservationEffects.Where(e=>e.Value.Country==_setup.PlayerSlot).Select(e=>_animationTime-e.Value.Start).DefaultIfEmpty(2).Min();
        return age>=0 && age<1.15f?MathF.Sin(age*18)*(1-age/1.15f):0;
    }
    private void DrawDefenseReservationEffect(Rectangle cell,Robot robot)
    {
        if(robot.ShoeOwner<0 || !_defenseReservationEffects.TryGetValue(robot.Id,out var effect))return;
        float age=_animationTime-effect.Start;if(age<0 || age>1.15f)return;
        float wave=MathF.Sin(age*18)*Math.Clamp(1-age/1.15f,0,1);
        RobotWorkArt.DrawFlag(_ui,new Rectangle(cell.Right-49,cell.Top+18,40,64),_setup.OwnerColor(robot.ShoeOwner),wave);
    }
    private void DrawDefenseReservationLabel(Rectangle bounds,int country)
    {
        foreach(var offset in new[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)}){
            var shadow=bounds;shadow.Offset(offset);_ui.Center("守備専用予約",shadow,.36f,new Color(225,232,235));
        }
        _ui.Center("守備専用予約",bounds,.36f,_setup.OwnerColor(country));
    }
}
