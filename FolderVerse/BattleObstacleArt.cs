namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public enum BattleObstacleStyle { Tree,Rock,Ice,Reef,Barrel }
public static class BattleObstacleArt
{
    public static BattleObstacleStyle Style(bool sea,bool bridge,float height,float latitude)
        =>sea?bridge?BattleObstacleStyle.Barrel:BattleObstacleStyle.Reef:
          Math.Abs(latitude)>.5f?BattleObstacleStyle.Ice:
          height<.55f || height>=WorldTerrain.MountainHeight?BattleObstacleStyle.Rock:BattleObstacleStyle.Tree;
    public static Rectangle HomeBorder(int side,Rectangle board,int thickness=8)=>side switch
    {
        0=>new(board.Left,board.Top-thickness,board.Width,thickness),
        1=>new(board.Right,board.Top,thickness,board.Height),
        2=>new(board.Left,board.Bottom,board.Width,thickness),
        _=>new(board.Left-thickness,board.Top,thickness,board.Height)
    };
    public static void Draw(UiPainter ui,Rectangle tile,BattleObstacleStyle style)
    {
        float scale=tile.Width/66f;var center=new Vector2(tile.Center.X,tile.Center.Y);
        void Box(int x,int y,int w,int h,Color c)=>ui.Box(new(tile.X+(int)(x*scale),tile.Y+(int)(y*scale),Math.Max(1,(int)(w*scale)),Math.Max(1,(int)(h*scale))),c);
        void Diamond(int x,int y,int w,int h,Color c)=>ui.Tile(center+new Vector2(x,y)*scale,new Vector2(w,h)*scale,MathHelper.PiOver4,c);
        if(style==BattleObstacleStyle.Tree)
        {
            Box(29,34,9,27,new(120,74,41));Diamond(0,-9,29,29,new(22,94,56));
            Diamond(-10,0,24,24,new(45,139,68));Diamond(11,1,25,25,new(68,166,82));
            Box(30,40,3,15,new(169,109,55));
        }
        else if(style==BattleObstacleStyle.Ice)
        {
            Diamond(-8,0,24,36,new(72,166,206));Diamond(9,-6,21,35,new(167,232,248));
            Box(31,13,3,24,new(236,253,255));Box(20,47,29,5,new(200,242,253));
        }
        else if(style==BattleObstacleStyle.Barrel)
        {
            Box(18,13,30,43,new(130,79,39));Box(22,16,9,36,new(178,113,57));
            Box(17,20,32,5,new(66,69,73));Box(17,43,32,5,new(66,69,73));
            Box(20,11,26,5,new(204,151,87));
        }
        else
        {
            bool reef=style==BattleObstacleStyle.Reef;
            Diamond(-9,7,23,24,reef?new(56,82,93):new(86,83,78));
            Diamond(9,1,30,27,reef?new(96,123,128):new(137,128,111));
            Diamond(9,-6,16,13,reef?new(143,169,170):new(184,175,152));
            if(reef){Box(8,51,20,2,new(136,206,225));Box(34,54,22,2,new(136,206,225));}
        }
    }
}
