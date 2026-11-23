namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawPersuasionRange(BattleState battle,BattleFrame frame,int left,int top,int size)
    {
        if(frame.Phase!=BattlePhase.Persuade)return;
        foreach(var queen in frame.Units.Where(u=>u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated && battle.Contains(u.Position)))
        {
            var color=_setup.OwnerColor(queen.PersuadedByQueen??queen.Owner);
            for(int y=0;y<battle.BoardSize;y++)for(int x=0;x<battle.BoardSize;x++)
                if(Math.Abs(x-queen.Position.X)+Math.Abs(y-queen.Position.Y)<=battle.Rules.PersuasionRadius)
                {
                    var r=new Rectangle(left+x*size+3,top+y*size+3,size-6,size-6);
                    _ui.Box(r,color*.16f);
                    _ui.Box(new(r.X,r.Y,r.Width,2),color*.65f);_ui.Box(new(r.X,r.Bottom-2,r.Width,2),color*.65f);
                    _ui.Box(new(r.X,r.Y,2,r.Height),color*.65f);_ui.Box(new(r.Right-2,r.Y,2,r.Height),color*.65f);
                }
        }
    }
    private void DrawPersuasionEffects(BattleState battle,BattleFrame frame,int left,int top,int size)
    {
        if(!frame.Units.Any(u=>u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated))return;
        if(frame.Phase is not (BattlePhase.Persuade or BattlePhase.Converted))return;
        var actions=battle.Persuasions.Where(p=>p.Turn==frame.Turn).ToArray();
        foreach(var action in actions)
        {
            var from=new Vector2(left+(action.From.X+.5f)*size,top+(action.From.Y+.5f)*size);
            var to=new Vector2(left+(action.To.X+.5f)*size,top+(action.To.Y+.5f)*size);
            if(frame.Phase==BattlePhase.Persuade)
            {
                var delta=to-from;
                for(int i=0;i<7;i++)
                {
                    float t=(i/7f+_animationTime*.6f)%1;
                    _ui.Tile(Vector2.Lerp(from,to,t),new(8,8),MathHelper.PiOver4,new Color(255,229,144));
                }
                _ui.Center("説得中",new((int)from.X-45,(int)from.Y-65,90,26),.35f,Cream);
            }
            else if(action.Success)
            {
                float radius=30+10*MathF.Sin(_animationTime*8);
                for(int i=0;i<8;i++)
                {
                    float angle=i*MathHelper.TwoPi/8;
                    _ui.Tile(to+new Vector2(MathF.Cos(angle),MathF.Sin(angle))*radius,new(10,4),angle,new Color(255,234,139));
                }
                if(action.Target<0)continue; // Leave space over a queen for their new flag.
                _ui.Box(new((int)to.X-72,(int)to.Y-77,144,30),Color.Black*.7f);
                _ui.Center("寝返り成功！",new((int)to.X-72,(int)to.Y-75,144,26),.4f,new Color(255,235,145));
                _ui.Box(new((int)to.X-33,(int)to.Y+32,30,6),_setup.OwnerColor(action.PreviousOwner));
                _ui.Box(new((int)to.X+3,(int)to.Y+32,30,6),_setup.OwnerColor(action.Owner));
            }
        }
        if(frame.Phase!=BattlePhase.Converted)return;
        var queenSuccesses=actions.Where(p=>p.Success && p.Target<0).ToArray();
        foreach(var queen in frame.Units.Where(u=>u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated))
        {
            var success=queenSuccesses.FirstOrDefault(p=>p.Target==queen.Id) ??
                queenSuccesses.FirstOrDefault(p=>p.Queen==queen.Id);
            if(success==null)continue;
            int width=Math.Clamp(size*2/3,30,52),height=width*2/3;
            int x=left+queen.Position.X*size+(size-width)/2;
            int y=top+queen.Position.Y*size-height-3;
            var flag=CountryFlagArt.Fit(new(x,y,width,height),_setup.CountryFlagShape(success.Owner));
            _ui.Box(new(flag.X-2,flag.Y-2,flag.Width+4,flag.Height+4),new Color(255,235,145));
            CountryFlagArt.Draw(_ui,_setup,success.Owner,new(x,y,width,height));
        }
    }
}
