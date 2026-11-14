namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class CubeNet
{
    private Texture2D _mapleMapArt;
    private static readonly Vector2[] MapleTrees={new(0,0),new(-.85f,.48f),new(.72f,.62f)};
    private static readonly Vector2[] MapleForest={new(-.75f,-.48f),new(.2f,-.65f),new(.86f,-.12f),new(-.9f,.25f),new(0,.08f),new(.68f,.66f),new(-.38f,.76f)};

    private void DrawMapleDeposit(UiPainter ui,WorldSetup world,Node node,Func<Vector3,Vector2> project,bool globe)
    {
        int loads=world.Robots.MapleDeposit(node.Id);
        if(loads<=0 || !node.Functional || node.Purpose==NodePurpose.Relay)return;
        var cell=world.Cells[node.Cell];
        var grove=node.Points.Where(p=>p!=node.Center && world.Routes.Terrain(node.Cell,p)==TravelTerrain.Land)
            .OrderByDescending(p=>Vector2.DistanceSquared(p.ToVector2(),node.Center.ToVector2()))
            .FirstOrDefault(node.Center);
        var origin=world.Routes.Position(node.Cell,grove);
        if(globe)origin+=cell.Normal*WorldTerrain.ReliefAt(cell,origin,world.Terrain);
        var at=project(origin);
        float size=Math.Clamp(Math.Max(Vector2.Distance(at,project(origin+cell.U)),Vector2.Distance(at,project(origin+cell.V)))*.055f,4,15);
        bool forest=loads>=4;
        if(globe)
        {
            var offsets=forest?MapleForest:MapleTrees;
            int count=forest?offsets.Length:loads;
            float spacing=forest?.05f:.082f;
            foreach(var index in Enumerable.Range(0,count).OrderBy(i=>project(origin+spacing*(cell.U*offsets[i].X+cell.V*offsets[i].Y)).Y))
            {
                var offset=offsets[index];
                var root=origin+spacing*(cell.U*offset.X+cell.V*offset.Y);
                Vector2 Point(float x,float y,float z)=>project(root+.044f*(cell.U*x+cell.V*y+cell.Normal*z));
                FillProjectedFace(ui,new[]{Point(-.14f,0,0),Point(.14f,0,0),Point(.14f,0,1.05f),Point(-.14f,0,1.05f)},NodeTint(node.Id,new Color(105,60,33)));
                FillProjectedFace(ui,new[]{Point(0,-.14f,0),Point(0,.14f,0),Point(0,.14f,1.05f),Point(0,-.14f,1.05f)},NodeTint(node.Id,new Color(123,74,40)));
                var peak=Point(0,0,1.85f);
                for(int side=0;side<10;side++)
                {
                    Vector2 Edge(int n){float a=n*MathHelper.TwoPi/10;float r=n%2==0?.77f:.52f;return Point(MathF.Cos(a)*r,MathF.Sin(a)*r,1f);}
                    var leaf=index%3==0?new Color(239,108,38):index%3==1?new Color(203,63,35):new Color(255,167,46);
                    var shade=Color.Lerp(leaf,new Color(122,46,28),(side%4)*.11f);
                    FillProjectedFace(ui,new[]{peak,Edge(side),Edge(side+1)},NodeTint(node.Id,shade));
                }
            }
        }
        else
        {
            _mapleMapArt??=LoadMapleMapArt(ui.Batch.GraphicsDevice);
            int half=_mapleMapArt.Width/2;
            var source=new Rectangle(forest?half:0,0,half,_mapleMapArt.Height);
            if(forest)DrawMapleMapSprite(at,size*4.4f);
            else for(int i=0;i<loads;i++)DrawMapleMapSprite(at+MapleTrees[i]*size*2.2f,size*2.1f);
            void DrawMapleMapSprite(Vector2 center,float width)
            {
                int h=(int)(width*source.Height/source.Width);
                ui.Batch.Draw(_mapleMapArt,new Rectangle((int)(center.X-width/2),(int)(center.Y-h/2),(int)width,h),
                    source,NodeTint(node.Id,Color.White));
            }
        }
        if(forest)
        {
            var label=new Rectangle((int)at.X-27,(int)(at.Y+size*.8f),54,17);
            ui.Box(label,NodeTint(node.Id,new Color(62,37,30)));
            ui.Center($"残り{loads}荷",label,.24f,NodeTint(node.Id,new Color(255,236,181)));
        }
    }

    private static Texture2D LoadMapleMapArt(GraphicsDevice device)
    {
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","maple-map-v001.png"));
        return Texture2D.FromStream(device,stream);
    }
}
