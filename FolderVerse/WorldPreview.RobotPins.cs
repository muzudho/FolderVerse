namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    private readonly List<VertexPositionColorNormal> _robotPinMesh=new();
    private VertexPositionColorNormal[] _robotPinVertices=Array.Empty<VertexPositionColorNormal>();
    private const float RobotPinScale=.5f, BatteryPinScale=1f;
    private static readonly int[][] RobotBoxQuads={new[]{0,1,3,2},new[]{4,6,7,5},new[]{0,4,5,1},new[]{2,3,7,6},new[]{0,2,6,4},new[]{1,5,7,3}};
    public float GlobeZoom {get;set;}=1;
    public Vector3? GlobeAnchor {get;set;}
    public Vector2 GlobePan {get;set;}
    private Matrix GlobeProjection(float aspect)
    {
        var projection=Matrix.CreatePerspectiveFieldOfView(GlobeFieldOfView,aspect,.1f,100);
        projection.M31-=GlobePan.X*2;projection.M32+=GlobePan.Y*2;
        return projection;
    }
    private float GlobeFieldOfView=>2*MathF.Atan(MathF.Tan(MathHelper.ToRadians(21))/GlobeZoom);
    private Matrix GlobeOrientation(float yaw,float pitch)
    {
        var rotation=DisplayOrientation(yaw,pitch);
        var anchor=Vector3.Transform(GlobeAnchor??Vector3.Zero,rotation);
        return rotation*Matrix.CreateTranslation(-anchor.X,-anchor.Y,0);
    }
    public static float BatteryTilt(double seconds,int ruler)=>(float)((Math.Sin(seconds*MathHelper.TwoPi/8+ruler*.7)+1)*MathHelper.PiOver4);
    public void DrawRobotPins(WorldSetup setup,CubeNet net,float yaw,float pitch,Rectangle panel,double seconds=0)
    {
        net.ClearGlobeRobotBadges();net.ClearGlobeBatteries();
        if(!setup.Campaign.UseRobotCombat)return;
        var mesh=_robotPinMesh;mesh.Clear();
        var corners=new Vector3[8];
        var orientation=GlobeOrientation(yaw,pitch);
        float cameraRadius=new Vector3(Width,Height,Depth).Length()*.5f+(_markers==null?0:.4f);
        var camera=new Vector3(0,0,cameraRadius*3.3f);
        var projection=orientation*Matrix.CreateLookAt(camera,Vector3.Zero,Vector3.Up)*GlobeProjection(panel.Width/(float)panel.Height);
        Vector3 Screen(Vector3 p){var clip=Vector4.Transform(new Vector4(p,1),projection);return new(panel.X+(clip.X/clip.W+1)*panel.Width*.5f,panel.Y+(1-clip.Y/clip.W)*panel.Height*.5f,clip.Z/clip.W);}
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color tint)
        {
            var normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));
            mesh.Add(new(a,tint,normal));mesh.Add(new(b,tint,normal));mesh.Add(new(c,tint,normal));
        }
        var groups=setup.Robots.FunctionDisplayGroups(setup);
        var terrainOffset=setup.Terrain;
        foreach(var node in setup.Nodes.All)
        {
            bool nodeVisible=net.NodeVisible(setup,node);
            bool ownBattery=Enumerable.Range(0,setup.ActiveCount).Any(r=>net.ShowBatteries && setup.Relations.Powered[r] && setup.Nodes.Current(r)?.Id==node.Id && (net.ShowOtherCountries || setup.Relations.Controls(r)) && (net.VisibleOperationBatteries==null || net.VisibleOperationBatteries.Contains(r)));
            if(!nodeVisible && !ownBattery)continue;
            var cell=setup.Cells[node.Cell];var robots=groups.GetValueOrDefault(node.Id,new List<Robot>()).Where(r=>nodeVisible && net.ShowRobots && (net.ShowOtherCountries || setup.Relations.Controls(r.Owner))).ToList();
            if(net.ShowRobots && net.ShowBatteries && net.HoveredBattery>=0 && setup.Relations.Powered[net.HoveredBattery] && setup.Nodes.Current(net.HoveredBattery)?.Id==node.Id && (net.ShowOtherCountries || setup.Relations.Controls(net.HoveredBattery)))robots.AddRange(setup.Robots.Retinues[net.HoveredBattery].Robots);
            var center=setup.Routes.Position(node.Cell,node.Center);
            center+=cell.Normal*WorldTerrain.ReliefAt(cell,center,terrainOffset);
            var projected=Screen(center);
            if(Vector3.Dot(Vector3.TransformNormal(cell.Normal,orientation),camera-Vector3.Transform(center,orientation))<=0 || projected.Z<0 || projected.Z>1)continue;
            Rectangle bounds=Rectangle.Empty;
            bool emphasized=net.ShowRoutes && net.EmphasizedNode(node.Id);
            float hop=0;
            Vector3 Point(float u,float v,float height)=>center+cell.U*u+cell.V*v+cell.Normal*(height+hop);
            void Track(Vector3 p)
            {
                var at=Screen(p);
                var box=new Rectangle((int)at.X-2,(int)at.Y-2,5,5);bounds=bounds==Rectangle.Empty?box:Rectangle.Union(bounds,box);
                if(emphasized || hop>0){
                    for(int height=0;height<2;height++){
                        var rest=Screen(p+cell.Normal*(height*.018f-hop));
                        bounds=Rectangle.Union(bounds,new Rectangle((int)rest.X-2,(int)rest.Y-2,5,5));
                    }
                }
            }
            float batteryTilt=0,batteryV=0;
            void Box(float u,float v,float z,float width,float depth,float height,Color color,bool damaged=false,int patternOwner=-1)
            {
                color=net.NodeTint(node.Id,color);

                for(int i=0;i<8;i++){
                    float x=u+((i&1)==0?-1:1)*width/2,y=v+((i&2)==0?-1:1)*depth/2,h=z+((i&4)==0?0:height);
                    if(batteryTilt!=0){float local=y-batteryV,up=h-.005f;y=batteryV+local*MathF.Cos(batteryTilt)-up*MathF.Sin(batteryTilt);h=.005f+local*MathF.Sin(batteryTilt)+up*MathF.Cos(batteryTilt)+.04f*MathF.Sin(batteryTilt);}
                    corners[i]=Point(x,y,h);Track(corners[i]);
                }
                Span<Color> inks=stackalloc Color[144];Span<bool> used=stackalloc bool[144];
                foreach(var q in RobotBoxQuads){
                    int pattern=patternOwner>=0?setup.CountryPattern(patternOwner):0;
                    if(!damaged && pattern==0){Triangle(corners[q[0]],corners[q[1]],corners[q[2]],color);Triangle(corners[q[0]],corners[q[2]],corners[q[3]],color);continue;}
                    Vector3 Face(float x,float y)=>Vector3.Lerp(Vector3.Lerp(corners[q[0]],corners[q[1]],x),Vector3.Lerp(corners[q[3]],corners[q[2]],x),y);
                    int resolution=pattern==0?12:4;
                    // Merge equal-colour tiles into rectangles without changing the pattern.
                    // A striped face needs a few bands instead of 32 tiny triangles.
                    bool upright=MathF.Abs(Vector3.Dot(corners[q[1]]-corners[q[0]],cell.Normal))>height*.5f;
                    var secondary=pattern==0?color:net.NodeTint(node.Id,WorldSetup.Colors[setup.CountrySecondaryColor(patternOwner)]);
                    used.Clear();
                    for(int row=0;row<resolution;row++)for(int column=0;column<resolution;column++){
                        var ink=pattern!=0 && WorldSetup.CountryPatternSecondary(pattern,upright?row*2:column*2,upright?column*2:row*2)?secondary:color;
                        if(damaged)ink=RobotDamageArt.Tint(new(0,0,100,100),column*100/resolution,row*100/resolution,ink);
                        inks[row*resolution+column]=ink;
                    }
                    for(int row=0;row<resolution;row++)for(int column=0;column<resolution;column++){
                        int at=row*resolution+column;if(used[at])continue;var ink=inks[at];int right=column+1,bottom=row+1;
                        while(right<resolution && !used[row*resolution+right] && inks[row*resolution+right]==ink)right++;
                        while(bottom<resolution){bool same=true;for(int x=column;x<right;x++)if(used[bottom*resolution+x] || inks[bottom*resolution+x]!=ink){same=false;break;}if(!same)break;bottom++;}
                        for(int y=row;y<bottom;y++)for(int x=column;x<right;x++)used[y*resolution+x]=true;
                        var a=Face(column/(float)resolution,row/(float)resolution);var b=Face(right/(float)resolution,row/(float)resolution);
                        var c=Face(right/(float)resolution,bottom/(float)resolution);var d=Face(column/(float)resolution,bottom/(float)resolution);
                        Triangle(a,b,c,ink);Triangle(a,c,d,ink);
                    }
                }
            }
            for(int slot=0;slot<robots.Count;slot++)
            {
                hop=emphasized?CubeNet.MapObjectHop(seconds,slot)*.003f:0;
                bounds=Rectangle.Empty;
                float angle=-MathHelper.PiOver2+slot*MathHelper.TwoPi/robots.Count,radius=Math.Max(.18f,robots.Count*.12f/MathHelper.TwoPi);
                float u=MathF.Cos(angle)*radius,v=MathF.Sin(angle)*radius,z=.008f;var robot=robots[slot];
                foreach(var whirl in setup.Routes.Whirlpools){
                    if(Vector3.Dot(cell.Normal,setup.Cells[whirl.Cell].Normal)<.99f)continue;
                    var delta=center+cell.U*u+cell.V*v-setup.Routes.WhirlpoolCenter(whirl);float clearance=whirl.Radius/10+.10f;
                    if(delta.LengthSquared()>=clearance*clearance)continue;
                    var away=delta.LengthSquared()<.0001f?cell.U:Vector3.Normalize(delta);
                    var safe=setup.Routes.WhirlpoolCenter(whirl)+away*clearance-center;u=Vector3.Dot(safe,cell.U);v=Vector3.Dot(safe,cell.V);
                }
                void RobotBox(float x,float y,float height,float width,float depth,float extent,Color tint,bool damaged=false,int patternOwner=-1)=>
                    Box(u+(x-u)*RobotPinScale,v+(y-v)*RobotPinScale,.008f+(height-.008f)*RobotPinScale,
                        width*RobotPinScale,depth*RobotPinScale,extent*RobotPinScale,tint,damaged,patternOwner);
                void Part(RobotParts part,float x,float y,float height,float width,float depth,float extent){
                    var tint=Color.Lerp(Color.White,setup.OwnerColor(robot.Origin(part)),.48f);
                    bool cheap=robot.IsCheap(part),premium=robot.IsPremium(part);
                    if(!cheap && !premium){RobotBox(x,y,height,width,depth,extent,tint,robot.IsDamaged(part),robot.Origin(part));return;}
                    for(int layer=0;layer<6;layer++){
                        float t=(layer+.5f)/6;
                        float rounded=.8f+.2f*MathF.Sin(MathF.PI*t);
                        float waist=premium?1-.35f*MathF.Pow(MathF.Sin(MathF.PI*t),2):.76f;
                        var ink=premium && layer is 0 or 5?new Color(255,215,100):tint;
                        RobotBox(x,y,height+extent*layer/6,width*waist*rounded,depth*(cheap?.75f:1)*rounded,extent/6,ink,robot.IsDamaged(part),premium && layer is 0 or 5?-1:robot.Origin(part));
                    }
                }
                void Emblem(RobotParts part,float baseHeight,float depth,float extent){
                    int owner=robot.Origin(part),emblem=setup.CountryEmblem(owner);
                    var ink=net.NodeTint(node.Id,WorldSetup.Colors[setup.CountrySecondaryColor(owner)]);
                    float side=.084f*RobotPinScale,front=v+(depth/2+.004f)*RobotPinScale,
                        h=.008f+(baseHeight-.008f+extent/2)*RobotPinScale;
                    Vector3 P(float px,float py)=>Point(u+(px-.5f)*side,front,h+(.5f-py)*side);
                    foreach(var tile in CountryEmblemArt.Tiles(emblem)){
                        var a=P(tile.X/32f,tile.Y/32f);var b=P(tile.Right/32f,tile.Y/32f);
                        var c=P(tile.Right/32f,tile.Bottom/32f);var d=P(tile.X/32f,tile.Bottom/32f);
                        Triangle(a,b,c,ink);Triangle(a,c,d,ink);
                    }
                }
                if((robot.Parts&RobotParts.Legs)!=0){foreach(float side in new[]{-1f,1f})Part(RobotParts.Legs,u+side*.027f,v,z,.038f,.065f,.09f);z+=.1f;}
                if((robot.Parts&RobotParts.Body)!=0){Part(RobotParts.Body,u,v,z,.10f,.065f,.11f);Emblem(RobotParts.Body,z,robot.IsCheap(RobotParts.Body)?.065f*.75f:.065f,.11f);foreach(float side in new[]{-1f,1f})Part(RobotParts.Body,u+side*.068f,v,z+.01f,.026f,.045f,.09f);z+=.12f;}
                if((robot.Parts&RobotParts.Head)!=0)
                {
                    Part(RobotParts.Head,u,v,z,.115f,.08f,.09f);
                    if((robot.Parts&RobotParts.Body)==0)Emblem(RobotParts.Head,z,.08f,.09f);
                    foreach(float side in new[]{-1f,1f})RobotBox(u+side*.025f,v+.043f,z+.05f,.018f,.009f,.018f,new(25,41,49));
                    RobotBox(u,v+.044f,z+.023f,.04f,.008f,.009f,new(230,102,60));RobotBox(u,v,z+.09f,.01f,.01f,.025f,new(83,112,122));
                }
                if(robot.ShoeOwner>=0)foreach(float side in new[]{-1f,1f})RobotBox(u+side*.027f,v+.012f,.008f,.055f,.085f,.025f,setup.OwnerColor(robot.ShoeOwner));
                if(bounds!=Rectangle.Empty && bounds.Intersects(panel))net.AddGlobeRobotBadge(node.Functional?node.Id:node.FunctionHost,bounds);
            }
            foreach(int ruler in Enumerable.Range(0,setup.ActiveCount).Where(r=>net.ShowBatteries && setup.Relations.Powered[r] && setup.Nodes.Current(r)?.Id==node.Id && (net.ShowOtherCountries || setup.Relations.Controls(r)) && (net.VisibleOperationBatteries==null || net.VisibleOperationBatteries.Contains(r)))){
                hop=emphasized || net.HoveredBattery==ruler?CubeNet.MapObjectHop(seconds,ruler)*.003f:0;
                bounds=Rectangle.Empty;var point=setup.Routes.Position(setup.ConquerorLocations[ruler],setup.ConquerorPoints[ruler]);
                var delta=point-center;int batterySlot=Enumerable.Range(0,ruler).Count(r=>setup.Relations.Powered[r] && setup.Nodes.Current(r)?.Id==node.Id);
                float u=Vector3.Dot(delta,cell.U)+.32f,v=Vector3.Dot(delta,cell.V)+batterySlot*.13f;
                batteryV=v;batteryTilt=BatteryTilt(seconds,ruler);
                void BatteryBox(float offset,float height,float width,float depth,float extent,Color tint)=>
                    Box(u,v+offset*BatteryPinScale,.005f+height*BatteryPinScale,
                        width*BatteryPinScale,depth*BatteryPinScale,extent*BatteryPinScale,tint);
                BatteryBox(0,0,.09f,.07f,.18f,setup.OwnerColor(ruler));
                BatteryBox(0,.18f,.035f,.035f,.018f,new(240,216,147));
                for(int i=0;i<3;i++)BatteryBox(.038f,.02f+i*.05f,.06f,.009f,.035f,new(45,112,70));
                if(bounds.Intersects(panel))net.AddGlobeBatteryBadge(ruler,bounds);
                batteryTilt=0;
            }
        }
        if(mesh.Count==0)return;
        _device.BlendState=BlendState.Opaque;_device.DepthStencilState=DepthStencilState.Default;_device.RasterizerState=RasterizerState.CullNone;
        _effect.LightingEnabled=true;_effect.World=GlobeOrientation(yaw,pitch);
        if(_robotPinVertices.Length<mesh.Count)Array.Resize(ref _robotPinVertices,Math.Max(mesh.Count,_robotPinVertices.Length*2));
        mesh.CopyTo(_robotPinVertices);
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawUserPrimitives(PrimitiveType.TriangleList,_robotPinVertices,0,mesh.Count/3);}
    }
}
