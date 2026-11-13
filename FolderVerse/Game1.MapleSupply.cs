namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _maplePackageArt;
    private long? _mapleSelected;
    private int _mapleDestination;
    private static readonly Rectangle MaplePanel=new(164,422,630,252);
    private static readonly Rectangle MaplePrevious=new(186,497,64,46),MapleNext=new(708,497,64,46);
    private static readonly Rectangle MapleSend=new(186,592,184,50),MapleCancel=new(387,592,184,50),MapleClose=new(588,592,184,50);

    private void DrawMaplePackage(Rectangle cell,MaplePackage package,string status=null,float opacity=1)
    {
        if(_maplePackageArt==null){
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","maple-package-v001.png"));
            _maplePackageArt=Texture2D.FromStream(GraphicsDevice,stream);
        }
        int width=Math.Min(cell.Width*4/5,cell.Height*3/4),height=width*243/288;
        if(width<1 || height<1)return;
        _spriteBatch.Draw(_maplePackageArt,new Rectangle(cell.Center.X-width/2,cell.Center.Y-height/2,width,height),Color.White*opacity);
        if(cell.Height<65)return;
        bool shipping=_setup.Robots.MapleShipments.Any(o=>o.PackageId==package.Id);
        status??=shipping?"出荷予定":"";
        _ui.Center($"楓の木 {package.Units}/16"+(status.Length>0?" / "+status:""),new(cell.X,cell.Bottom-30,cell.Width,28),Math.Min(.34f,cell.Width/410f),Cream);
    }

    private int[] MapleTargets()=>FunctionalDeliveryRoutes.Targets(_setup,_populationNode).Where(n=>_setup.Nodes.All[n].Owner>=0).ToArray();
    private bool MapleHangarInput(bool click,bool escape)
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
                    _hangarMessage=reserved?"楓の木を出荷予約しました":"楓の木を出荷予約できません";
                    _mapleSelected=null;
                }
            }
            return true;
        }
        if(!click || !DomesticOperationsAllowed || _hangarReservation!=HangarReservation.None || _hangarDrag.HasValue ||
            HangarPlanningNode!=_populationNode || !_setup.Relations.Controls(_setup.Nodes.All[_populationNode].Owner))return false;
        var store=_setup.Robots.Nodes[_populationNode];
        if(store.MaplePackages.Count==0)return false;
        SyncHangarSlots();
        int slot=VisibleAssemblySlots().FirstOrDefault(i=>AssemblyCell(i).Contains(_pointer),-1);
        var package=slot<0?null:store.MaplePackages.FirstOrDefault(p=>p.Id==_hangarSlots[slot]);
        if(package==null)return false;
        _mapleSelected=package.Id;_mapleDestination=0;
        return true;
    }

    private void DrawMapleShippingPanel()
    {
        if(_mapleSelected is not long id)return;
        var targets=MapleTargets();
        _ui.Box(MaplePanel,new Color(21,41,51));
        _ui.Center("楓の木 / 出荷先",new(186,440,586,40),.56f,Cream);
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
