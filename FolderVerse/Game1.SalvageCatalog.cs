namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _salvageCatalogOpen;
    private long _salvageMapTarget=-1;
    private int SalvageMapTargetNode=>_salvagePlan && !_nodeInspectorClosing?(_setup.Salvage.Items.FirstOrDefault(i=>i.Id==_salvageMapTarget)?.Node??-1):-1;
    private const int SalvageCatalogPageSize=24;
    private static readonly Rectangle SalvageCatalogButton=new(84,580,784,84);
    private static readonly Rectangle SalvageCatalogBack=new(1570,30,296,58),SalvageCatalogMap=new(54,986,300,54),SalvageCatalogRequest=new(370,986,300,54),SalvageCatalogPickUp=new(686,986,350,54);
    private static readonly Rectangle SalvageCatalogAll=new(54,124,290,48),SalvageCatalogPrevious=new(1570,124,140,48),SalvageCatalogNext=new(1726,124,140,48);
    private static Rectangle SalvageCatalogCard(int i)=>new(54+i%8*228,192+i/8*204,216,192);
    private void OpenSalvageCatalog(int node=-1)
    {
        _salvageNodeFilter=node;_salvagePage=0;_salvageMessage="";_mapOverlayMenu=false;
        var items=VisibleSalvageItems();
        int selected=Array.FindIndex(items,i=>i.Id==_salvageSelected);
        if(selected<0)_salvageSelected=items.FirstOrDefault()?.Id??-1;
        else _salvagePage=selected/SalvageCatalogPageSize;
        _salvageCatalogOpen=true;
    }
    private bool CanPickUpSalvage(SalvageItem item)=>item!=null && _setup.Nodes.Current(_setup.PlayerSlot)?.Id==item.Node && _setup.Robots.Retinues[_setup.PlayerSlot].HasSpace && !_setup.Campaign.RobotBattles.LockedNode(item.Node);
    private string SalvageProvenance(SalvageItem item)=>item==null?"廃品を選択してください":"放棄元："+(item.AbandonedBy<0?"初期配置（放棄元なし）":_setup.ConquerorNames[item.AbandonedBy])+" / "+(item.RequestedBy==_setup.PlayerSlot?"回収依頼済み":item.AbandonedBy==_setup.PlayerSlot?"手動の回収依頼で拾い直せます":"未所属の回収品");
    private void SalvageCatalogInput(bool click,bool escape)
    {
        if(escape || click && SalvageCatalogBack.Contains(_pointer)){_salvageCatalogOpen=false;return;}
        if(!click)return;
        if(SalvageCatalogAll.Contains(_pointer)){OpenSalvageCatalog();return;}
        var items=VisibleSalvageItems();int pages=Math.Max(1,(items.Length+SalvageCatalogPageSize-1)/SalvageCatalogPageSize);
        _salvagePage=Math.Clamp(_salvagePage,0,pages-1);
        if(SalvageCatalogPrevious.Contains(_pointer)){_salvagePage=(_salvagePage+pages-1)%pages;return;}
        if(SalvageCatalogNext.Contains(_pointer)){_salvagePage=(_salvagePage+1)%pages;return;}
        for(int i=0;i<SalvageCatalogPageSize;i++)if(SalvageCatalogCard(i).Contains(_pointer) && _salvagePage*SalvageCatalogPageSize+i<items.Length){_salvageSelected=items[_salvagePage*SalvageCatalogPageSize+i].Id;_salvageMessage="";return;}
        var selected=items.FirstOrDefault(i=>i.Id==_salvageSelected);
        if(SalvageCatalogMap.Contains(_pointer) && selected!=null){_salvageMapTarget=selected.Id;_salvageCatalogOpen=false;CenterSalvageNode(selected.Node);return;}
        if(SalvageCatalogRequest.Contains(_pointer) && selected!=null){_salvageMessage=_setup.Salvage.Request(_setup,_setup.PlayerSlot,selected.Id)?"回収を依頼しました":"回収隊が到達できません";_salvageFocusTurn=-1;}
        if(SalvageCatalogPickUp.Contains(_pointer) && selected!=null){_salvageMessage=_setup.Salvage.PickUp(_setup,_setup.PlayerSlot,selected.Id)?"ポケットに拾いました":"現地への到着・ポケットの空き・戦闘状況を確認してください";_salvageFocusTurn=-1;}
    }
    private void DrawSalvageCatalog()
    {
        _ui.Box(new(0,0,1920,1080),new Color(16,35,47));
        _ui.Text("迷子タグ / 廃品カタログ",new(54,30),.85f,Cream);
        _ui.Text("放棄されたロボットパーツの迷子タグを受信しています",new(54,87),.42f,Cream);
        _ui.Button(SalvageCatalogBack,"地図へ戻る",Muted,.5f);
        _ui.Button(SalvageCatalogAll,"世界中の廃品",_salvageNodeFilter<0?Accent:Muted,.44f);
        var items=VisibleSalvageItems();int pages=Math.Max(1,(items.Length+SalvageCatalogPageSize-1)/SalvageCatalogPageSize);_salvagePage=Math.Clamp(_salvagePage,0,pages-1);
        string scope=_salvageNodeFilter<0?"すべての地点":_setup.Nodes.DisplayName(_setup.Nodes.All[_salvageNodeFilter]);
        SalvageText(scope,new(370,124,1180,48),.42f);
        _ui.Button(SalvageCatalogPrevious,"←",Muted,.5f);_ui.Button(SalvageCatalogNext,"→",Muted,.5f);
        for(int i=0;i<SalvageCatalogPageSize;i++){
            var r=SalvageCatalogCard(i);_ui.Box(r,new Color(27,49,62));int index=_salvagePage*SalvageCatalogPageSize+i;if(index>=items.Length)continue;
            var item=items[index];var robot=item.Robot;bool selected=item.Id==_salvageSelected;
            _ui.Box(r,selected?new Color(57,81,96):Muted);
            SalvageText("#"+item.Id+(item.RequestedBy==_setup.PlayerSlot?" / 回収依頼済み":""),new(r.X+6,r.Y+4,r.Width-12,24),.32f);
            DrawRobotGlyph(new(r.Center.X-35,r.Y+34,70,78),robot,whiteOutline:true);
            SalvageText(PartsLabel(robot),new(r.X+4,r.Y+119,r.Width-8,24),.33f);
            SalvageText(robot.DamagedParts!=0?"損傷あり":robot.CheapParts!=0?"安い / 無傷":"正規 / 無傷",new(r.X+4,r.Y+144,r.Width-8,20),.3f);
            SalvageText(_setup.Nodes.Description(_setup.Nodes.All[item.Node]),new(r.X+4,r.Bottom-25,r.Width-8,22),.28f);
            if(selected)PocketOutline(r,new Color(255,220,114),3);
            else if(r.Contains(_pointer))PocketOutline(r,Cream,2);
        }
        if(items.Length==0)SalvageText("この範囲に廃品はありません",new(54,380,1812,80),.65f);
        SalvageText($"廃品 {items.Length} 件 / {_salvagePage+1} / {pages}",new(54,808,1812,32),.4f);
        var chosen=items.FirstOrDefault(i=>i.Id==_salvageSelected);
        SalvageText(chosen==null?"廃品を選択してください":"#"+chosen.Id+" / "+_setup.Nodes.DisplayName(_setup.Nodes.All[chosen.Node]),new(54,850,1812,32),.48f);
        if(chosen!=null)SalvageText(SalvagePartLabel(chosen.Robot),new(54,888,1812,32),.42f);
        SalvageText(_salvageMessage.Length>0?_salvageMessage:SalvageProvenance(chosen),new(54,933,1812,32),.4f);
        _ui.Button(SalvageCatalogMap,"地図で見る",chosen!=null?Accent:Muted,.48f);
        _ui.Button(SalvageCatalogRequest,"回収を依頼",chosen!=null && _setup.Salvage.CanRecover(_setup,_setup.PlayerSlot,chosen)?Accent:Muted,.48f);
        _ui.Button(SalvageCatalogPickUp,"現地でポケットに拾う",CanPickUpSalvage(chosen)?Accent:Muted,.44f);
    }
}
