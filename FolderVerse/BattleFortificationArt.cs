namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public static class BattleFortificationArt
{
    public static void Draw(UiPainter ui,Rectangle tile,BattleFortification fort)
    {
        if(fort.Kind==FortificationKind.Barrel){BattleObstacleArt.Draw(ui,tile,BattleObstacleStyle.Barrel);return;}
        Vector2 P(float x,float y){var p=new Vector2(x-.5f,y-.5f);for(int i=0;i<(int)fort.Facing;i++)p=new(-p.Y,p.X);return new Vector2(tile.Center.X,tile.Center.Y)+p*tile.Width;}
        void Line(float x,float y,float xx,float yy,float width,Color color){var a=P(x,y);var b=P(xx,yy);var d=b-a;ui.Tile((a+b)/2,new(d.Length(),width*tile.Width),MathF.Atan2(d.Y,d.X),color);}
        var dark=new Color(25,30,33);
        if(fort.Kind==FortificationKind.Moat){
            ui.Box(new(tile.X+8,tile.Y+8,tile.Width-16,tile.Height-16),new Color(83,64,44));
            ui.Box(new(tile.X+14,tile.Y+14,tile.Width-28,tile.Height-28),new Color(29,111,152));
            for(int i=0;i<3;i++)Line(.23f,.29f+i*.2f,.77f,.29f+i*.2f,.025f,new Color(102,202,220));return;
        }
        if(fort.Kind==FortificationKind.Wall){
            Line(.14f,.5f,.86f,.5f,.47f,dark);Line(.17f,.47f,.83f,.47f,.37f,new Color(143,151,149));
            Line(.16f,.29f,.84f,.29f,.06f,new Color(201,204,185));
            for(int i=0;i<3;i++)Line(.27f+i*.23f,.33f,.27f+i*.23f,.63f,.025f,new Color(75,88,88));return;
        }
        bool bent=fort.Health==1;float skew=bent?.09f:0;
        Line(.19f,.19f,.19f,.86f,.11f,dark);Line(.81f,.19f,.81f,.86f,.11f,dark);
        Line(.19f,.21f,.81f,.21f,.13f,new Color(145,91,47));
        Line(.19f,.27f,.19f,.83f,.06f,new Color(197,143,77));Line(.81f,.27f,.81f,.83f,.06f,new Color(197,143,77));
        for(int i=0;i<5;i++){
            float x=.29f+i*.105f;Line(x,.3f,x+skew,.73f,.09f,dark);Line(x,.32f,x+skew,.71f,.06f,new Color(130,83,49));
        }
        Line(.26f,.4f,.74f+skew,.44f,.055f,new Color(84,92,96));Line(.26f,.64f,.74f+skew,.68f,.055f,new Color(84,92,96));
        if(bent){Line(.44f,.35f,.56f,.51f,.028f,new Color(236,190,113));Line(.56f,.51f,.45f,.68f,.028f,new Color(236,190,113));}
        // The short front marker makes all four orientations distinguishable.
        Line(.42f,.12f,.5f,.04f,.035f,new Color(210,207,183));Line(.5f,.04f,.58f,.12f,.035f,new Color(210,207,183));
    }
}
