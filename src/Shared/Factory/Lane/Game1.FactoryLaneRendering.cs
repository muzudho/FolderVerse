namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _factoryConveyor;
    private Texture2D _factorySlimConveyor;
    private Texture2D _factoryWorkerGear;
    private const int FactoryVisibleLanes=2;
    private static int FactoryRowY(int row)=>284+row*290;
    private static Rectangle FactoryLaneBounds(int row)=>new(78,FactoryRowY(row)-24,782,286);
    private static Rectangle FactoryRobotButton(int row,int period)=>new(301+FactoryConveyorTravel(period),FactoryRowY(row)+67,72,88);
    private static Rectangle HarvestLoadButton(int row,int loads)=>new(400+(loads-1)*105,FactoryRowY(row)+132,95,35);

    private void DrawFactoryLaneFrame(int row,int laneNumber,int stripeIndex)
    {
        _ui.Box(FactoryLaneBounds(row),stripeIndex%2==0?new Color(37,63,77):new Color(46,67,79));
        _ui.Center(laneNumber.ToString(),new(84,FactoryRowY(row)-12,66,40),.85f,new Color(167,190,194));
    }
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
            var robot=completed??line.ActiveWorkpiece??line.CurrentProductionPlan.Preview(owner);
            var product=progress is float fraction?FactoryConveyorProduct(row,period,robot.Parts,fraction):FactoryConveyorProduct(row,period,line);
            var bounds=new Rectangle(product.X,product.Y,product.Width,122);
            if(completed!=null)DrawManufacturingProduct(bounds,completed);
            else DrawRobotGlyphCore(bounds,robot,true,1,null,showIntegratedMaterial:true);
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
        int turns=Math.Clamp(line.RestTurns,1,9);
        int length=turns*30;
        _ui.Box(new(track.X,track.Y+4,length,14),new Color(47,67,78));
        int duration=IndustryTime.Whole(turns);
        int elapsed=line.RestRemaining>0?Math.Clamp(duration-line.RestRemaining,0,duration):0;
        if(elapsed>0)
            _ui.Box(new(track.X,track.Y+4,length*elapsed/duration,14),new Color(85,206,126));
        for(int i=1;i<=9;i++){
            int x=track.X+i*30;
            _ui.Box(new(x-1,track.Y+4,1,14),Cream*.65f);
        }
        _ui.Box(new(track.X+length-6,track.Y,12,22),Cream);
    }


    private void DrawFactoryHarvestLane(RobotWorkshop line,int node,int row,int y,Robot worker,bool planned=false,int available=-1)
    {
        var product=FactoryRobotButton(row,3);
        if(worker!=null)
        {
            DrawFactoryWorker(y,worker,harvesting:true);
            _ui.Text("採取員",new(91,y+58),.27f,Cream);
        }
        if(line.MapleRemaining==0)
        {
            _ui.Text(planned?"採取レーン / 予約中":"採取レーン",new(250,y),.48f,Cream);
            _ui.Button(product,"採取",Muted,.34f);
            return;
        }
        _ui.Box(product,Accent);
        DrawMaplePackage(product,new MaplePackage(0,line.MapleRemaining*16){Material=line.HarvestMaterial});
        if(worker!=null)DrawMaplePackageArt(new Rectangle(219,y+100,66,45),1,material:line.HarvestMaterial);
        int deposit=_setup.Robots.RawDeposit(node,line.HarvestMaterial);
        bool waiting=!_setup.Robots.CanHarvestMaple(node,line.MapleRemaining,line.HarvestMaterial);
        string status=line.Paused?"一時停止":deposit==0?"採取できる埋蔵量がありません":waiting?"格納枠の空きを待っています":"採取中";
        _ui.Text($"採取レーン / {status} / 予約残り {line.MapleRemaining} 荷",new(160,y-19),.33f,Cream);
        _ui.Text($"{RobotWorld.RawName(line.HarvestMaterial)} / 残り埋蔵量 {deposit} 荷",new(400,y+46),.45f,Cream);
        _ui.Text($"次の入荷まで {IndustryTime.Format(Math.Max(0,line.EffectiveHarvestPeriod(_setup.Nodes.All[node].Purpose)-line.MapleProgress))} 産業ターン",new(400,y+90),.33f,Cream);
        for(int loads=1;available>=0 && loads<=3;loads++)
            _ui.Button(HarvestLoadButton(row,loads),$"{loads} 荷",loads>available?Muted*.55f:loads==line.MapleRemaining?Accent:Muted,.34f);
        DrawFactoryLaneStatus(y,!waiting && !line.Paused,status);
    }

    private void DrawManufacturingProduct(Rectangle bounds,Robot product)
    {
        bool materials=_setup.EquipmentMode==RobotEquipmentMode.Materials;
        DrawRobotGlyphCore(bounds,product,true,1,null,showIntegratedMaterial:materials,allowCaptain:materials);
    }
}
