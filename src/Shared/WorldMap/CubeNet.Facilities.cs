namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private static bool HasCivilianSettlement(Node node)=>!node.Functional && node.FunctionHost>=0;

    private static int RelocatedFacilityGroups(Node node)=>node.Functional
        ? Math.Min(3,node.FunctionUnits-1+node.IncomingFactories) : 0;

    private static void DrawFacilities(UiPainter ui,WorldSetup world,Node node,
        Func<float,float,float,Vector2> project,Func<Color,Color> tint)
    {
        var ground=world.Routes.Position(node.Cell,node.Center);
        float latitude=WorldTerrain.RelativeLatitude(world,ground);
        bool sea=node.Terrain==TravelTerrain.Sea;
        float elevation=world.Terrain.Elevation(ground);
        var terrain=WorldTerrain.Kind(elevation);
        bool valley=WorldTerrain.IsValley(world,world.Cells[node.Cell],ground,elevation);
        bool tropical=latitude<.25f, cold=latitude>=.65f, polar=latitude>=.98f;
        var warmWall=new Color(255,222,161);
        var brightRoof=new Color(230,104,64);
        var glass=new Color(48,101,119);
        var timber=new Color(171,111,66);
        var hull=new Color(49,104,128);
        var snow=new Color(222,236,240);
        var amber=new Color(255,188,80);
        var metal=new Color(137,170,181);
        var roof=polar?snow:cold?new Color(111,145,173):tropical?brightRoof:new Color(103,153,114);
        var wall=cold?snow:warmWall;

        void Face(Color color,params Vector2[] vertices)=>FillProjectedFace(ui,vertices,tint(color));
        void Prism(float back,float front,Color color,params Vector2[] profile)
        {
            var a=Array.ConvertAll(profile,p=>project(p.X,back,p.Y));
            var b=Array.ConvertAll(profile,p=>project(p.X,front,p.Y));
            var winding=Cross(b[1]-b[0],b[2]-b[1]);
            for(int i=0;i<profile.Length;i++){
                int j=(i+1)%profile.Length;
                if(Cross(b[j]-b[i],a[i]-b[i])*winding<0)
                    Face((color*(i%2==0?.84f:1f)) with {A=255},b[i],b[j],a[j],a[i]);
            }
            if(winding<0)Face((color*.72f) with {A=255},b);
            else if(winding>0)Face((color*.72f) with {A=255},a);
        }
        void Box(float x,float y,float width,float depth,float z,float height,Color color)
            =>Prism(y,y+depth,color,new(x,z),new(x+width,z),new(x+width,z+height),new(x,z+height));
        void Roof(float x,float y,float width,float depth,float z,float peak,Color color)
            =>Prism(y,y+depth,color,new(x,z),new(x+width,z),new(x+width/2,z+peak));
        void House(float x,float y,float width,float floor)
        {
            const float depth=.43f;
            Box(x,y,width,depth,floor,.34f,wall);
            Roof(x-.055f,y-.05f,width+.11f,depth+.1f,floor+.34f,tropical?.14f:.23f,roof);
            Box(x+.08f,y+depth,.13f,.018f,floor+.13f,.095f,cold?amber:glass);
            Box(x+width-.14f,y+depth,.09f,.018f,floor,.17f,timber);
        }
        void Boat(float x,float y,float length,bool sail,bool cargo)
        {
            const float width=.37f;
            var bow=project(x,y-.18f,.15f);
            Face(hull,project(x-width/2,y,.02f),project(x+width/2,y,.02f),bow);
            Box(x-width/2,y,width,length,.03f,.12f,hull);
            Face(wall,project(x-width/2,y,.15f),project(x+width/2,y,.15f),bow);
            if(sail){
                Box(x-.016f,y+length*.45f,.032f,.032f,.15f,.69f,timber);
                var mastTop=project(x,y+length*.45f,.84f);
                var tack=project(x,y-.10f,.25f);
                var boom=project(x,y+length*.45f,.25f);
                var belly=project(x+.15f,y+length*.14f,.32f);
                Face(warmWall,mastTop,tack,belly);
                Face(new Color(255,243,208),mastTop,belly,boom);
                Face(brightRoof,project(x,y+length*.45f,.75f),project(x+.08f,y+length*.86f,.27f),project(x,y+length*.45f,.27f));
            }else{
                Box(x-.12f,y+.08f,.24f,.22f,.15f,.19f,wall);
                Box(x-.081f,y+.062f,.162f,.018f,.25f,.062f,glass);
                float cargoHeight=cold?.19f:.12f;
                Box(x-.13f,y+.34f,.26f,length-.36f,.15f,cargoHeight,polar?wall:cargo?brightRoof:metal);
            }
        }
        void Lighthouse(float x,float y,float floor)
        {
            Box(x,y,.17f,.18f,floor,.46f,wall);
            Box(x-.02f,y-.02f,.21f,.22f,floor+.46f,.10f,amber);
            Roof(x-.04f,y-.04f,.25f,.26f,floor+.56f,.12f,brightRoof);
        }

        if(HasCivilianSettlement(node)){
            if(sea){
                Box(-.86f,-.36f,1.65f,.70f,.02f,.12f,hull);
                for(int i=0;i<9;i++)Box(-.86f+i*.185f,-.36f,.174f,.70f,.14f,.035f,timber);
                House(-.75f,-.32f,.55f,.175f);
                House(.06f,-.34f,.54f,.175f);
                if(polar)Box(-.20f,-.22f,.26f,.22f,.175f,.28f,wall);
                else if(cold)Box(-.20f,-.22f,.26f,.22f,.175f,.19f,metal);
                else if(!tropical)Lighthouse(.65f,.07f,.175f);
                Boat(.43f,.42f,.56f,tropical,false);
                return;
            }
            Box(-.84f,-.54f,1.62f,1.08f,0,.045f,new(167,154,102));
            House(-.69f,-.41f,.53f,.045f);
            House(.12f,valley?-.34f:-.43f,.54f,.045f);
            if(polar || valley)Box(-.16f,-.30f,.28f,.24f,.045f,.26f,wall);
            if(terrain==TerrainKind.Mountain){
                Box(.43f,.17f,.27f,.24f,.045f,.26f,wall);
                Prism(.145f,.435f,metal,new(.39f,.305f),new(.74f,.305f),new(.72f,.39f),
                    new(.65f,.47f),new(.56f,.50f),new(.47f,.47f),new(.41f,.39f));
                Box(.55f,.20f,.035f,.05f,.48f,.08f,glass);
            }else if(node.IsHarbor || terrain==TerrainKind.Coast)Lighthouse(.53f,.13f,.045f);
            else if(tropical){
                for(int i=0;i<3;i++)Box(-.57f+i*.22f,.19f,.18f,.23f,.045f,.15f,i%2==0?amber:brightRoof);
            }else if(!valley && !polar){
                for(int i=0;i<4;i++)Box(-.63f+i*.27f,.19f,.18f,.24f,.045f,.04f,new Color(108,154,74));
            }
            return;
        }

        int groups=RelocatedFacilityGroups(node);
        for(int i=0;i<groups;i++){
            float x=(i-(groups-1)/2f)*.54f;
            bool hasWarehouse=i<node.FunctionUnits-1;
            if(sea){
                Boat(x,-.41f,hasWarehouse?.83f:.70f,tropical && i==0,hasWarehouse);
                continue;
            }
            Box(x-.23f,-.39f,.46f,.78f,0,.055f,timber);
            Box(x-.21f,-.37f,.42f,.73f,.055f,.32f,wall);
            Roof(x-.24f,-.40f,.48f,.79f,.375f,.16f,hasWarehouse?roof:metal);
            Box(x+.09f,-.31f,.093f,.11f,.45f,.26f,brightRoof);
            Box(x-.11f,.36f,.22f,.018f,.055f,.20f,cold?amber:glass);
            if(hasWarehouse)
                Box(x-.16f,.40f,.32f,.10f,.055f,.10f,amber);
        }
    }
}
