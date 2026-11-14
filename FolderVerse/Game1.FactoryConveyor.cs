namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private Texture2D _factoryConveyor;
    private Texture2D _factorySlimConveyor;
    private Texture2D _factoryWorkerGear;
    private int _factoryHolidayLane=-1;
    private static Rectangle FactoryHolidayTrack(int row)=>new(420,FactoryRowY(row)+222,270,22);
    private static int FactoryConveyorTravel(int period)=>Math.Min(period,36)*12;
    private const int FactoryConveyorStart=327;
    private const int FactoryBeltY=166;

    private static Rectangle FactoryConveyorProduct(int row,int period,RobotWorkshop line)
    {
        float progress=line.Manufacturing?Math.Clamp((float)line.ProductionAge/period,0,1):0;
        return FactoryConveyorProduct(row,period,line.Manufacturing?line.CurrentProductionPlan.Product:0,progress);
    }

    private static Rectangle FactoryConveyorProduct(int row,int period,RobotParts parts,float progress)
    {
        int x=FactoryConveyorStart+(int)(FactoryConveyorTravel(period)*progress);
        int height=122;
        if(parts!=0){
            if((parts&RobotParts.Legs)==0)height=(parts&RobotParts.Body)==0?44:84;
        }
        return new(x-43,FactoryRowY(row)+FactoryBeltY-height,86,height);
    }

    private static Rectangle FactoryConveyorArrow(Rectangle product)=>new(product.Right+4,product.Center.Y-14,28,28);

    private void DrawFactoryConveyor(RobotWorkshop line,int owner,int row,int period,float? progress=null,Robot completed=null,bool inFlight=false)
    {
        if(_factoryConveyor==null){
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","factory-conveyor-v001.png"));
            _factoryConveyor=Texture2D.FromStream(GraphicsDevice,stream);
        }
        if(_factorySlimConveyor==null){
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","factory-conveyor-v002.png"));
            _factorySlimConveyor=Texture2D.FromStream(GraphicsDevice,stream);
        }
        var art=_factoryConveyor;
        int y=FactoryRowY(row);
        int travel=FactoryConveyorTravel(period);
        int start=FactoryConveyorStart,end=start+travel;
        _spriteBatch.Draw(art,new Rectangle(start-78,y-20,156,9),new Rectangle(154,14,635,34),Color.White);
        _spriteBatch.Draw(art,new Rectangle(start-28,y-12,56,23),new Rectangle(369,254,230,88),Color.White);
        _spriteBatch.Draw(_factorySlimConveyor,new Rectangle(262,y+145,55,48),new Rectangle(823,90,510,393),
            Color.White,-.16f,new Vector2(255,196),SpriteEffects.FlipHorizontally,0);
        _spriteBatch.Draw(_factorySlimConveyor,new Rectangle(start-22,y+FactoryBeltY,22,34),new Rectangle(20,542,80,112),Color.White);
        for(int offset=0;offset<travel;offset+=36){
            int width=Math.Min(36,travel-offset);
            _spriteBatch.Draw(_factorySlimConveyor,new Rectangle(start+offset,y+FactoryBeltY,width,34),new Rectangle(104,542,width*153/36,112),Color.White);
        }
        _spriteBatch.Draw(_factorySlimConveyor,new Rectangle(end,y+FactoryBeltY,18,34),new Rectangle(2074,542,82,112),Color.White);
        for(int tick=0;tick<=Math.Min(period,36);tick++){
            int x=end-tick*12;
            var ink=new Color(37,42,47);
            _ui.Box(new(x,y+FactoryBeltY+13,1,tick%3==0?5:3),ink);
            if(tick%3==0)_ui.Center((tick/3).ToString(),new(x-12,y+FactoryBeltY+16,24,15),.29f,ink);
        }
        if(!inFlight && (completed!=null || line.Manufacturing)){
            var robot=completed??line.CurrentProductionPlan.Preview(owner);
            var product=progress is float fraction?FactoryConveyorProduct(row,period,robot.Parts,fraction):FactoryConveyorProduct(row,period,line);
            DrawRobotGlyphCore(new(product.X,product.Y,product.Width,122),robot,true,1,null,showIntegratedMaterial:true);
        }
    }

    private void DrawFactoryWorker(int y,Robot worker,bool harvesting=false)
    {
        if(_factoryWorkerGear==null){
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","factory-worker-gear-v001.png"));
            _factoryWorkerGear=Texture2D.FromStream(GraphicsDevice,stream);
        }
        var bounds=new Rectangle(166,y+54,87,125);
        if(_setup.EquipmentMode!=RobotEquipmentMode.Materials ||
            !DrawOrientedRobotArt(bounds,worker,rear:false,west:false,opacity:1))
            DrawRobotGlyph(bounds,worker);

        _spriteBatch.Draw(_factoryWorkerGear,new Rectangle(177,y+38,66,33),new Rectangle(46,139,622,426),Color.White);
        if(harvesting)
        {
            var orange=new Color(248,132,42);
            _ui.Tile(new Vector2(207,y+102),new Vector2(29,7),-.83f,orange);
            _ui.Tile(new Vector2(207,y+102),new Vector2(25,2),-.83f,new Color(255,215,146));
            _spriteBatch.Draw(_factoryWorkerGear,new Rectangle(222,y+112,32,18),new Rectangle(766,205,644,344),Color.White);
            return;
        }
        _spriteBatch.Draw(_factoryWorkerGear,new Rectangle(182,y+128,15,22),new Rectangle(1616,94,409,556),Color.White);
        _spriteBatch.Draw(_factoryWorkerGear,new Rectangle(222,y+116,43,24),new Rectangle(766,205,644,344),Color.White);
    }

    private void DrawFactoryLaneStatus(int y,bool running,string status)
    {
        var lamp=running?new Color(91,240,158):new Color(255,113,105);
        _ui.Box(new(91,y+199,18,18),new Color(13,27,35));
        _ui.Box(new(94,y+202,12,12),lamp);
        _ui.Box(new(96,y+203,4,3),Color.Lerp(lamp,Color.White,.55f));
        _ui.Text(status,new(116,y+200),Math.Min(.32f,570/_font.MeasureString(status).X),Cream);
    }

    private void DrawFactoryHoliday(RobotWorkshop line,int row)
    {
        var track=FactoryHolidayTrack(row);
        _ui.Text("休暇",new(track.X-46,track.Y+2),.28f,Cream);
        _ui.Box(track,new Color(17,34,45));
        for(int i=1;i<=9;i++){
            int x=track.X+i*30;
            _ui.Box(new(x-1,track.Y+4,1,14),Cream*.65f);
        }
        int length=Math.Clamp(line.RestTurns,1,9)*30;
        _ui.Box(new(track.X,track.Y+8,length,6),Accent);
        _ui.Box(new(track.X+length-6,track.Y,12,22),Cream);
        string time=IndustryTime.Format(line.RestRemaining);
        _ui.Center(time,new(track.Right+12,track.Y-1,76,24),.39f,new Color(255,201,90));
    }

    private bool FactoryHolidayInput(MouseState mouse)
    {
        if(!_nodeFactoryOpen || mouse.LeftButton==ButtonState.Released){
            _factoryHolidayLane=-1;return false;
        }
        if(_factoryHolidayLane<0)return false;
        var factory=PlanningWorkshop(_populationNode);
        var lines=FactoryScreenLines(factory,out var planned);
        if(_factoryHolidayLane>=lines.Length || _setup.Campaign.RobotBattles.LockedNode(_populationNode)){
            _factoryHolidayLane=-1;return true;
        }
        var line=lines[_factoryHolidayLane];
        var track=FactoryHolidayTrack(_factoryHolidayLane-_factoryPage);
        int turns=Math.Clamp((int)MathF.Round((_pointer.X-track.X)/30f),1,9);
        if(turns!=line.RestTurns){
            line.SetRest(line.RestEvery,turns);
            if(planned.TryGetValue(_factoryHolidayLane,out long worker))_setup.Robots.PartPlanning.SetWorkerLane(worker,line);
        }
        return true;
    }
}
