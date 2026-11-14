namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private readonly Texture2D[] _maplePackageArt=new Texture2D[15];
    private long? _mapleSelected;
    private long? _mapleDrag;
    private Point _mapleGrab;
    private bool _mapleMoved,_mapleSplit;
    private readonly Dictionary<long,int> _maplePositions=new();
    private int _mapleDestination;
    private static readonly Rectangle MaplePanel=new(164,422,630,252);
    private static readonly Rectangle MaplePrevious=new(186,497,64,46),MapleNext=new(708,497,64,46);
    private static readonly Rectangle MapleSend=new(186,592,184,50),MapleCancel=new(387,592,184,50),MapleClose=new(588,592,184,50);

    private void DrawMaplePackageArt(Rectangle cell,int sections,float opacity=1,bool alignBottom=false,RawMaterial material=RawMaterial.Maple)
    {
        int index=(int)material*3+sections-1;
        if(_maplePackageArt[index]==null){
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots",$"raw-{material.ToString().ToLowerInvariant()}-{sections}-v001.png"));
            _maplePackageArt[index]=Texture2D.FromStream(GraphicsDevice,stream);
        }
        var art=_maplePackageArt[index];
        int width=Math.Min(cell.Width*4/5,cell.Height*3/4*art.Width/art.Height),height=width*art.Height/art.Width;
        if(width<1 || height<1)return;
        int y=alignBottom?cell.Bottom-height:cell.Center.Y-height/2;
        _spriteBatch.Draw(art,new Rectangle(cell.Center.X-width/2,y,width,height),Color.White*opacity);
    }

    private void DrawMaplePackage(Rectangle cell,MaplePackage package,string status=null,float opacity=1,bool compact=false)
    {
        DrawMaplePackageArt(cell,package.Sections,opacity,material:package.Material);
        if(compact){
            if(cell.Height>=28)_ui.Center($"{package.Units}/48",new(cell.X,cell.Bottom-16,cell.Width,16),Math.Min(.27f,cell.Width/210f),Cream);
            return;
        }
        if(cell.Height<65)return;
        bool shipping=_setup.Robots.MapleShipments.Any(o=>o.PackageId==package.Id);
        status??=shipping?"出荷予定":"";
        _ui.Center($"{RobotWorld.RawName(package.Material)} {package.Units}/48"+(status.Length>0?" / "+status:""),new(cell.X,cell.Bottom-30,cell.Width,28),Math.Min(.34f,cell.Width/410f),Cream);
    }

    private int[] MapleTargets()=>FunctionalDeliveryRoutes.Targets(_setup,_populationNode).Where(n=>_setup.Nodes.All[n].Owner>=0).ToArray();
    private Dictionary<int,MaplePackage> AssemblyMaterials(RobotStore store)
    {
        var stored=HangarMaterials(_hangarSlots,store,store.MaplePackages,positions:_maplePositions);
        var incoming=_setup.Robots.MapleShipments.Where(o=>o.Target==HangarPlanningNode)
            .Select(o=>_setup.Robots.Nodes[o.Source].MaplePackages.FirstOrDefault(p=>p.Id==o.PackageId))
            .OfType<MaplePackage>();
        foreach(var pair in HangarMaterials(_hangarSlots,store,incoming,occupied:stored))
            stored.Add(pair.Key,pair.Value);
        return stored;
    }
    private bool MapleArrival(MaplePackage package)=>_setup.Robots.MapleShipments.Any(o=>o.Target==HangarPlanningNode && o.PackageId==package.Id);
    private (string Label,Dictionary<long,int> Merged) MapleArrivalLabel(int node,RobotStore store)
    {
        var merged=new Dictionary<long,int>();
        int count=_setup.Robots.MapleShipments.Count(o=>o.Target==node);
        if(count==0)return ("",merged);
        var arrival=_setup.Robots.PreviewMapleArrivals(_setup,node,store);
        var current=store.MaplePackages.ToDictionary(p=>p.Id,p=>p.Units);
        foreach(var package in arrival.Packages)
            if(current.TryGetValue(package.Id,out int units) && package.Units>units)
                merged.Add(package.Id,package.Units-units);
        int total=merged.Values.Sum();
        return ($"原材料 / 入荷予定 {count} 件"+(total>0?$" / 合算 +{total}":"")+
            (arrival.Blocked>0?$" / 待機 {arrival.Blocked} 件":""),merged);
    }
    private bool MapleHangarInput(MouseState mouse,bool click,bool escape)
    {
        if(_mapleSelected is long id)
        {
            if(escape || !DomesticOperationsAllowed){_mapleSelected=null;return escape;}
            if(!click)return true;
            var targets=MapleTargets();
            if(MapleClose.Contains(_pointer) || !MaplePanel.Contains(_pointer))_mapleSelected=null;
            else if(MapleCancel.Contains(_pointer)){
                _setup.Robots.CancelMapleShipment(_populationNode,id);_mapleSelected=null;
            }else if(targets.Length>0){
                _mapleDestination=Math.Clamp(_mapleDestination,0,targets.Length-1);
                if(MaplePrevious.Contains(_pointer))_mapleDestination=Math.Max(0,_mapleDestination-1);
                if(MapleNext.Contains(_pointer))_mapleDestination=Math.Min(targets.Length-1,_mapleDestination+1);
                if(MapleSend.Contains(_pointer)){
                    bool reserved=_setup.Robots.ReserveMapleShipment(_setup,_populationNode,targets[_mapleDestination],id);
                    _hangarMessage=reserved?"原材料を出荷予約しました":"原材料を出荷予約できません";
                    _mapleSelected=null;
                }
            }
            return true;
        }
        if(escape || !DomesticOperationsAllowed){
            bool active=_mapleDrag.HasValue;
            _mapleDrag=null;
            return active && escape;
        }
        if(!click && !_mapleDrag.HasValue || _hangarReservation!=HangarReservation.None || _hangarDrag.HasValue ||
            HangarPlanningNode!=_populationNode || !_setup.Relations.Controls(_setup.Nodes.All[_populationNode].Owner))return false;
        var store=_setup.Robots.Nodes[_populationNode];
        SyncHangarSlots();
        var forecast=PlanningStock.Nodes[HangarPlanningNode];
        var materials=AssemblyMaterials(forecast);
        var hit=materials.FirstOrDefault(p=>VisibleAssemblySlots().Contains(p.Key/3) && Rectangle.Union(AssemblyPartBounds(p.Key/3,p.Key%3),AssemblyPartBounds(p.Key/3,p.Key%3+p.Value.Sections-1)).Contains(_pointer));
        if(_mapleDrag is not long sourceId){
            if(hit.Value==null || !store.MaplePackages.Any(p=>p.Id==hit.Value.Id))return false;
            _mapleDrag=hit.Value.Id;_mapleGrab=_pointer;_mapleMoved=false;
            var keyboard=Keyboard.GetState();
            _mapleSplit=keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            return true;
        }
        _mapleMoved|=Vector2.DistanceSquared(_pointer.ToVector2(),_mapleGrab.ToVector2())>36;
        UpdateHangarDragScroll(mouse.LeftButton==ButtonState.Pressed);
        if(mouse.LeftButton==ButtonState.Pressed)return true;
        _mapleDrag=null;
        if(!_mapleMoved){_mapleSelected=sourceId;_mapleDestination=0;return true;}
        var source=store.MaplePackages.FirstOrDefault(p=>p.Id==sourceId);
        if(source==null || _setup.Robots.MapleShipments.Any(o=>o.PackageId==sourceId) ||
            _setup.Campaign.RobotBattles.LockedNode(_populationNode))return true;
        if(hit.Value!=null){
            if(hit.Value.Id!=sourceId)
                _hangarMessage=!_mapleSplit && _setup.Robots.RearrangeMaple(_setup,_populationNode,sourceId,hit.Value.Id,false)
                    ?"原材料をまとめました":"この原材料はまとめられません";
            return true;
        }
        foreach(int slot in VisibleAssemblySlots())
        for(int row=0;row<3;row++){
            if(!AssemblyPartBounds(slot,row).Contains(_pointer))continue;
            int sections=_mapleSplit?1:source.Sections;
            var robot=forecast.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);
            if(row+sections>3 || Enumerable.Range(row,sections).Any(r=>MaterialOccupies(materials,slot,r) ||
                robot!=null && (robot.Parts&HangarPart(r))!=0))return true;
            long placedId=sourceId;
            if(_mapleSplit){
                if(!_setup.Robots.RearrangeMaple(_setup,_populationNode,sourceId,null,true)){
                    _hangarMessage="この原材料は分けられません";return true;
                }
                placedId=store.MaplePackages.Last().Id;
            }
            _maplePositions[placedId]=slot*3+row;
            _hangarMessage=_mapleSplit?"原材料を分けました":"原材料を移動しました";
            return true;
        }
        return true;
    }

    private void DrawMapleShippingPanel()
    {
        if(_mapleSelected is not long id)return;
        var targets=MapleTargets();
        _ui.Box(MaplePanel,new Color(21,41,51));
        _ui.Center(RobotWorld.RawName(_setup.Robots.Nodes[_populationNode].MaplePackages.FirstOrDefault(p=>p.Id==id)?.Material??RawMaterial.Maple)+" / 出荷先",new(186,440,586,40),.56f,Cream);
        if(targets.Length>0){
            int index=Math.Clamp(_mapleDestination,0,targets.Length-1);
            _ui.Center(_setup.Nodes.DisplayName(_setup.Nodes.All[targets[index]]),new(250,497,458,46),.44f,Cream);
            _ui.Center($"{index+1}/{targets.Length}",new(250,548,458,26),.32f,Cream);
            _ui.Button(MaplePrevious,"◀",index>0?Accent:Muted,.48f);
            _ui.Button(MapleNext,"▶",index+1<targets.Length?Accent:Muted,.48f);
        }else _ui.Center("出荷できる地点がありません",new(186,497,586,46),.44f,Cream);
        _ui.Button(MapleSend,"出荷予約",targets.Length>0?Accent:Muted,.4f);
        _ui.Button(MapleCancel,"予約取消",Muted,.4f);
        _ui.Button(MapleClose,"閉じる",Muted,.4f);
    }
}
