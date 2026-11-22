namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Color HangarWall = new(43, 67, 80);
    private static readonly Color HangarFloor = new(57, 77, 86);
    private static readonly Color HangarGuide = new(220, 173, 88);

    private void DrawHangarBay(Rectangle bay, bool hovered, bool canReserve)
    {
        var wall = hovered ? (canReserve ? new Color(54, 89, 102) : new Color(85, 54, 57)) : HangarWall;
        void Box(int x,int y,int width,int height,Color color)=>_ui.Box(HangarCellRegion(bay,x,y,width,height),color);
        _ui.Box(bay,wall);
        Box(16,32,220,128,new(30,48,60));
        for(int y=38;y<150;y+=14)Box(16,y,220,2,wall);
        Box(0,160,252,100,HangarFloor);
        Box(0,201,252,59,new(36,54,65));
        foreach(int post in new[]{0,242}){
            Box(post,12,10,148,new(70,93,103));
            Box(post==0?7:242,12,3,148,new(27,46,57));
        }
        foreach(int side in new[]{-1,1})
            _ui.Tile(new(bay.Center.X+side*75*bay.Width/252f,bay.Y+180*bay.Height/260f),
                new(47*bay.Width/252f,3*bay.Height/260f),side*-.9f,HangarGuide);
        Box(0,201,252,3,HangarGuide);
        Box(0,0,252,14,new(79,105,114));
        Box(0,14,252,6,new(32,51,62));
        Box(100,6,52,4,new(180,223,221));
    }

    private void DrawHangarRobot(Rectangle bounds,Robot robot,float opacity=1,Func<RobotParts,float> partOpacity=null)
    {
        bool materials=_setup.EquipmentMode==RobotEquipmentMode.Materials;
        DrawRobotGlyphCore(bounds,robot,true,opacity,partOpacity,showIntegratedMaterial:materials,allowCaptain:materials,captainCapOverflow:true);
    }

}
