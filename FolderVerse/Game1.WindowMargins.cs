namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawWindowMargins(int width,int height,Rectangle canvas)
    {
        // Paint only the unused strips, in window pixels rather than game-canvas coordinates.
        var strips=new[]{new Rectangle(0,0,canvas.Left,height),new Rectangle(canvas.Right,0,width-canvas.Right,height),
            new Rectangle(canvas.Left,0,canvas.Width,canvas.Top),new Rectangle(canvas.Left,canvas.Bottom,canvas.Width,height-canvas.Bottom)};
        foreach(var strip in strips)
        {
            if(strip.Width<=0 || strip.Height<=0)continue;
            _spriteBatch.Draw(_pixel,strip,new Color(247,238,237));
            const int check=28;
            for(int y=strip.Top;y<strip.Bottom;y+=check)for(int x=strip.Left;x<strip.Right;x+=check)
            {
                var color=((x-strip.Left)/check%2,(y-strip.Top)/check%2) switch
                {(1,1)=>new Color(221,207,224),(1,0) or (0,1)=>new Color(236,223,234),_=>new Color(250,243,240)};
                _spriteBatch.Draw(_pixel,new Rectangle(x,y,Math.Min(check,strip.Right-x),Math.Min(check,strip.Bottom-y)),color);
            }
            bool vertical=strip.Height>strip.Width;
            int shortSide=vertical?strip.Width:strip.Height,longSide=vertical?strip.Height:strip.Width;
            int size=Math.Min(260,(int)(shortSide*.8f));
            if(size<12)continue;
            int count=Math.Clamp(longSide/Math.Max(size*4,180),1,7);
            for(int i=0;i<count;i++)
            {
                var toy=i%3==1?_marginBall:i%3==2?_marginStar:!vertical && shortSide<140?_marginTrain:_marginRabbit;
                float aspect=toy.Width/(float)toy.Height;
                int toyHeight=Math.Min(size,(int)((longSide/(float)(count+1)*.85f)/aspect));
                int toyWidth=(int)(toyHeight*aspect);
                if(vertical){toyWidth=Math.Min(size,toyWidth);toyHeight=(int)(toyWidth/aspect);}
                int along=(i+1)*longSide/(count+1);
                int x=vertical?strip.Center.X-toyWidth/2:strip.Left+along-toyWidth/2;
                int y=vertical?strip.Top+along-toyHeight/2:strip.Center.Y-toyHeight/2;
                _spriteBatch.Draw(toy,new Rectangle(x,y,toyWidth,toyHeight),Color.White*.92f);
            }
        }
    }
}
