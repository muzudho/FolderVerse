namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawBattleActivity()
    {
        foreach(var encounter in _setup.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished)){
            Vector2 at;
            if(encounter.Kind==BattleKind.Edge){if(!OperationPosition(encounter.Source,out var a) || !OperationPosition(encounter.Target,out var b))continue;at=(a+b)/2;}
            else if(!OperationPosition(encounter.Target,out at))continue;
            if(!MapViewBounds.Contains(at.ToPoint()))continue;
            float pulse=.65f+.35f*MathF.Sin(_animationTime*5+encounter.Id);
            var ink=new Color(17,28,36);var fire=new Color(255,196,82);
            foreach(float angle in new[]{MathHelper.PiOver4,-MathHelper.PiOver4}){_ui.Tile(at,new(34,8),angle,ink);_ui.Tile(at,new(30,3),angle,fire*pulse);}
            for(int i=0;i<3;i++){float t=(_animationTime*.7f+i/3f)%1;var p=at+new Vector2(MathF.Sin(i*2+t*3)*10,-12-t*28);int radius=3+(int)(t*6);for(int y=-radius;y<=radius;y++){int half=(int)MathF.Sqrt(Math.Max(0,radius*radius-y*y));_ui.Box(new((int)p.X-half,(int)p.Y+y,half*2+1,1),new Color(194,202,207)*(1-t)*.8f);}}
            string label=encounter.InterceptOwner>=0?"迎撃":encounter.Kind==BattleKind.Edge?"辺戦":"継戦";
            var badge=new Rectangle((int)at.X-26,(int)at.Y+19,52,23);_ui.Box(badge,ink);_ui.Center(label,badge,.3f,fire);
        }
    }
}
