namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    internal static void DrawPurposeIcon(UiPainter ui,NodePurpose? purpose,Vector2 at,bool sea,bool enabled)
    {
        Color Tint(Color color)=>enabled?color:Color.Lerp(color,new Color(25,34,40),.7f);
        float scale=purpose is NodePurpose.Defense or NodePurpose.Frontline?.8f:1f;
        Vector2 Project(float x,float y,float z)=>at+new Vector2(x*18+y*8,y*9-x*4-z*18)*scale;
        if(purpose==null)DrawHarborPier(ui,Project,Tint);
        else if(purpose==NodePurpose.General){
            var top=Tint(sea?new Color(164,226,242):new Color(255,232,153));
            Disc(ui,at+new Vector2(0,3),12,Tint(sea?new Color(52,110,135):new Color(152,111,53)));
            Disc(ui,at,12,top);
        }
        else DrawPurposeBuilding(ui,purpose.Value,sea,Project,Tint);
    }

    private static Vector3? PurposeFlagAnchor(Node node,int grade=0)=>!node.Functional || node.IsHarbor ? null : node.Purpose switch {
        NodePurpose.Harvest=>new Vector3(0,-.12f,.75f),
        NodePurpose.Manufacturing=>new Vector3(0,-.12f,.75f),
        NodePurpose.Relay=>new Vector3(0,-.12f,.75f),
        NodePurpose.Defense=>new Vector3(-.56f,0,.91f),
        NodePurpose.Frontline=>new Vector3(0,-.12f,.75f+grade*.4f),
        _=>null
    };

    private static void DrawPurposeBuilding(UiPainter ui,NodePurpose purpose,bool sea,Func<float,float,float,Vector2> point,Func<Color,Color> tint,int grade=0)
        =>DrawDevelopedFacility(ui,purpose,sea,grade,point,tint);
}