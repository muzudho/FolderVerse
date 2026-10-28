namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _salvagePlan,_hangarAbandonMode;
    private long _salvageSelected=-1;
    private int _salvagePage,_salvageNodeFilter=-1,_salvageFocusTurn=-1;
    private string _salvageMessage="";
    private bool? _salvageRoutesBefore;
    private readonly HashSet<int> _salvageFocusNodes=new();
    private readonly List<(Rectangle Bounds,int Node)> _salvageHits=new();
    private (int Node,long Robot,RobotParts Parts)? _salvageAbandonConfirmation;
    private static readonly Rectangle HangarAbandonButton=new(480,908,182,42),HangarSalvageButton=new(678,908,190,42);
    private static readonly Rectangle SalvageToggle=new(84,355,200,42),SalvageAgentFocus=new(296,355,250,42),SalvageAll=new(558,355,310,42);
    private static readonly Rectangle SalvageRequest=new(84,916,380,44),SalvagePickUp=new(478,916,390,44);
    private static readonly Rectangle AbandonYes=new(560,646,380,58),AbandonNo=new(980,646,380,58);
    private static Rectangle SalvagePolicyButton(int i)=>new(84+i%2*394,245+i/2*54,380,46);
    private static readonly string[] SalvagePolicies={"短期的に早く回収する","中期的に程よく回収する","長期的に多く回収する","任せる"};
    private SalvageAgent CurrentSalvageAgent=>_setup.Salvage.Agents.FirstOrDefault(a=>a.Owner==_setup.PlayerSlot);
    private SalvageItem[] VisibleSalvageItems()=>_setup.Salvage.Items.Where(i=>_salvageNodeFilter<0 || i.Node==_salvageNodeFilter)
        .OrderByDescending(i=>i.RequestedBy==_setup.PlayerSlot).ThenBy(i=>i.Node).ThenBy(i=>i.Id).ToArray();
    private string SalvagePartLabel(Robot r)=>(r.IntegratedParts!=0?IntegratedLabel(r.IntegratedParts)+" / ":"")+string.Join("・",new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(p=>(r.Parts&p)!=0)
        .Select(p=>PartsLabel(p)+"（"+(r.IsCheap(p)?"安い":"正規")+"・"+(r.IsDamaged(p)?"損傷":"無傷")+"）"))+(r.IntegratedParts!=0 && r.DamagedParts!=0?" / 実損傷："+PartsLabel(r.DamagedParts):"");
    private void SalvageText(string text,Rectangle r,float scale=.4f)=>_ui.Center(text,r,Math.Min(scale,(r.Width-12)/Math.Max(1,_font.MeasureString(text).X)),Cream);
    private void OpenSalvagePlan(int node=-1)
    {
        ResetInspector();_salvageRoutesBefore=_net.ShowRoutes;_net.ShowRoutes=true;_salvagePlan=true;
        _salvageNodeFilter=node;_salvagePage=0;_salvageMessage="";_salvageFocusTurn=-1;
        _setup.Salvage.EnsureAgents(_setup);if(CurrentSalvageAgent is {} a)_setup.Salvage.Preview(_setup,a);
        _salvageSelected=VisibleSalvageItems().FirstOrDefault()?.Id??-1;
        CenterSalvageNode(node>=0?node:CurrentSalvageAgent?.Node??-1);
    }
    private void CenterSalvageNode(int id)
    {
        if(id<0 || id>=_setup.Nodes.All.Length)return;
        var node=_setup.Nodes.All[id];
        if(_statusGlobe)FocusGlobePosition(_setup.Routes.Position(node.Cell,node.Center));else _net.SetCenter(_setup,_setup.Cells[node.Cell].Face);
        QueueInspectorCenter(id);
    }
    private void RefreshSalvageFocus()
    {
        if(_salvageFocusTurn==_setup.Population.Turn)return;
        _salvageFocusTurn=_setup.Population.Turn;_salvageFocusNodes.Clear();
        if(CurrentSalvageAgent is {} a){
            _setup.Salvage.Preview(_setup,a);_salvageFocusNodes.Add(a.Node);
            int home=_setup.Salvage.NearestHome(_setup,a.Owner,a.Node);if(home>=0)_salvageFocusNodes.Add(home);
            foreach(int node in a.Route)_salvageFocusNodes.Add(node);
        }
        foreach(var item in _setup.Salvage.Items.Where(i=>_setup.Salvage.CanRecover(_setup,_setup.PlayerSlot,i) || _setup.Nodes.Current(_setup.PlayerSlot)?.Id==i.Node && _setup.Robots.Retinues[_setup.PlayerSlot].HasSpace)){
            _salvageFocusNodes.Add(item.Node);int home=_setup.Salvage.NearestHome(_setup,_setup.PlayerSlot,item.Node);if(home>=0)_salvageFocusNodes.Add(home);
        }
    }
    private bool SalvageInput(Point point,bool click)
    {
        if(click && !_salvagePlan && _salvageHits.Any(h=>h.Bounds.Contains(point))){OpenSalvagePlan(_salvageHits.First(h=>h.Bounds.Contains(point)).Node);return true;}
        if(!_salvagePlan || !click)return false;
        if(MapViewBounds.Contains(point)){
            var hit=_salvageHits.FirstOrDefault(h=>h.Bounds.Contains(point));if(hit.Bounds!=Rectangle.Empty){
                OpenSalvageCatalog(hit.Node);return true;
            }
            return false;
        }
        point.X-=NodeInspectorOffset;if(!new Rectangle(54,150,850,825).Contains(point))return false;
        var agent=CurrentSalvageAgent;
        if(agent!=null){
            for(int i=0;i<4;i++)if(SalvagePolicyButton(i).Contains(point)){agent.Policy=(SalvagePolicy)i;_setup.Salvage.Preview(_setup,agent);_salvageFocusTurn=-1;return true;}
            if(SalvageToggle.Contains(point)){agent.Paused=!agent.Paused;_setup.Salvage.Preview(_setup,agent);_salvageFocusTurn=-1;return true;}
            if(SalvageAgentFocus.Contains(point)){_salvageMapTarget=-1;CenterSalvageNode(agent.Node);return true;}
        }
        if(SalvageAll.Contains(point)){OpenSalvageCatalog();return true;}
        if(SalvageCatalogButton.Contains(point)){OpenSalvageCatalog(_salvageNodeFilter);return true;}
        if(SalvageRequest.Contains(point)){
            _salvageMessage=_setup.Salvage.Request(_setup,_setup.PlayerSlot,_salvageSelected)?"回収を依頼しました":"回収隊が到達できません";_salvageFocusTurn=-1;
        }
        if(SalvagePickUp.Contains(point)){
            _salvageMessage=_setup.Salvage.PickUp(_setup,_setup.PlayerSlot,_salvageSelected)?"ポケットに拾いました":"現地への到着・ポケットの空き・戦闘状況を確認してください";_salvageFocusTurn=-1;
        }
        return true;
    }
    private void DrawSalvagePanel()
    {
        _ui.Box(new(54,150,850,825),new Color(27,49,62));
        SalvageText("廃品回収計画",new(84,162,784,46),.75f);
        _ui.Text("回収方針",new(84,216),.4f,Cream);
        _ui.Text("６部位まで / １全体ターンに１地点間",new(360,218),.32f,Cream);
        var agent=CurrentSalvageAgent;
        for(int i=0;i<4;i++)_ui.Button(SalvagePolicyButton(i),SalvagePolicies[i],agent?.Policy==(SalvagePolicy)i?Accent:Muted,.43f);
        _ui.Button(SalvageToggle,agent?.Paused==false?"回収する ✓":"休止中",agent?.Paused==false?Accent:Muted,.4f);
        _ui.Button(SalvageAgentFocus,"回収隊を表示",Muted,.4f);_ui.Button(SalvageAll,"世界中のカタログ",Muted,.38f);
        if(agent!=null){
            SalvageText("回収隊："+agent.Status+" / 積荷 "+agent.Load+" / "+SalvagePlan.Capacity+" 部位",new(84,407,784,32),.45f);
            SalvageText("現在地："+_setup.Nodes.DisplayName(_setup.Nodes.All[agent.Node]),new(84,442,784,28),.35f);
            SalvageText(agent.Cargo.Count==0?"積荷なし":string.Join(" / ",agent.Cargo.Select(SalvagePartLabel)),new(84,475,784,28),.32f);
            string destination=agent.Destination>=0?_setup.Nodes.DisplayName(_setup.Nodes.All[agent.Destination]):"未定";
            SalvageText("次の目的地："+destination,new(84,506,784,25),.32f);
            string delivery=agent.DeliveryNode<0?"搬入先未定":"搬入先："+_setup.Nodes.Description(_setup.Nodes.All[agent.DeliveryNode])+" / 最短 約 "+agent.DeliveryTurns+" 全体ターン";
            SalvageText(delivery,new(84,534,784,25),.31f);
        }else SalvageText("所有地点がないため回収隊は待機しています",new(84,407,784,70),.4f);
        _ui.Button(SalvageCatalogButton,"迷子タグを使って、廃品になったロボットパーツを一覧",Accent,.4f);
        SalvageText("放棄されたパーツの迷子タグを受信しています",new(84,678,784,30),.38f);
        SalvageText("地図のカゴをクリックすると、その地点のカタログが開きます",new(84,714,784,30),.35f);
        var selected=_setup.Salvage.Items.FirstOrDefault(i=>i.Id==_salvageSelected);
        if(selected!=null){
            DrawRobotGlyph(new(112,760,72,82),selected.Robot,whiteOutline:true);
            SalvageText("#"+selected.Id+" / "+PartsLabel(selected.Robot),new(212,758,656,30),.43f);
            SalvageText(_setup.Nodes.DisplayName(_setup.Nodes.All[selected.Node]),new(212,795,656,30),.34f);
            SalvageText(SalvagePartLabel(selected.Robot),new(84,841,784,25),.31f);
        }
        SalvageText(_salvageMessage.Length>0?_salvageMessage:SalvageProvenance(selected),new(84,875,784,30),.33f);
        bool recover=selected!=null && _setup.Salvage.CanRecover(_setup,_setup.PlayerSlot,selected);
        bool pickup=CanPickUpSalvage(selected);
        _ui.Button(SalvageRequest,"回収を依頼",recover?Accent:Muted,.45f);_ui.Button(SalvagePickUp,"現地でポケットに拾う",pickup?Accent:Muted,.4f);
    }
    private void DrawSalvageMap()
    {
        _salvageHits.Clear();if(!_salvagePlan || MapTransitionActive || _net.IsAnimating)return;
        foreach(var group in _setup.Salvage.Items.GroupBy(i=>i.Node)){
            if(!OperationPosition(group.Key,out var at))continue;at+=new Vector2(28,12);
            if(!MapViewBounds.Contains(at.ToPoint()))continue;
            var r=new Rectangle((int)at.X-14,(int)at.Y-17,28,32);
            bool dim=SalvageMapTargetNode>=0?_net.DimNode(group.Key):!_salvageFocusNodes.Contains(group.Key);
            var wire=new Color(132,146,148);if(dim)wire=SelectionGlow.Dim(wire);
            var representative=group.OrderBy(i=>i.Id).First().Robot;
            DrawRobotGlyph(new(r.X+5,r.Y,18,24),representative,opacity:dim?.65f:1f);
            // The representative part sits inside one quiet mesh basket per location.
            _ui.Box(new(r.X,r.Y+16,28,2),wire);_ui.Box(new(r.X+3,r.Bottom-2,22,2),wire);
            for(int x=3;x<=23;x+=5)_ui.Box(new(r.X+x,r.Y+18,1,12),wire);
            _ui.Box(new(r.X+2,r.Y+22,24,1),wire);_ui.Box(new(r.X+3,r.Y+27,22,1),wire);
            _salvageHits.Add((r,group.Key));
            _ui.Text(group.Count().ToString(),new(r.Right+3,r.Y+12),.3f,dim?SelectionGlow.Dim(Cream):Cream);
        }
        var agent=CurrentSalvageAgent;if(agent==null)return;
        if(_salvagePlan && SalvageMapTargetNode<0)for(int i=1;i<agent.Route.Length;i++)if(OperationPosition(agent.Route[i-1],out var a) && OperationPosition(agent.Route[i],out var b)){
            var d=b-a;_ui.Tile((a+b)/2,new Vector2(d.Length(),4),MathF.Atan2(d.Y,d.X),new Color(255,221,135));
        }
        if(!OperationPosition(agent.Node,out var center))return;center+=new Vector2(-28,-35);
        if(!MapViewBounds.Contains(center.ToPoint()))return;
        Color AgentColor(Color c)=>SalvageMapTargetNode>=0 && _net.DimNode(agent.Node)?SelectionGlow.Dim(c):c;
        var own=AgentColor(_setup.OwnerColor(agent.Owner));_ui.Box(new((int)center.X-12,(int)center.Y-13,24,31),AgentColor(new Color(10,25,34)));
        _ui.Box(new((int)center.X-4,(int)center.Y-10,8,8),AgentColor(Cream));_ui.Box(new((int)center.X-6,(int)center.Y,12,13),own);
        _ui.Box(new((int)center.X+7,(int)center.Y+5,13,9),AgentColor(new Color(220,183,112)));
        if(_salvagePlan)_ui.Text("回収隊",center+new Vector2(-22,20),.3f,AgentColor(Cream));
    }
    private bool AbandonConfirmationInput(bool click,bool escape)
    {
        if(_salvageAbandonConfirmation is not {} selection)return false;
        if(escape || click && AbandonNo.Contains(_pointer)){_salvageAbandonConfirmation=null;return true;}
        if(click && AbandonYes.Contains(_pointer)){
            try{_setup.Salvage.Abandon(_setup,_setup.PlayerSlot,selection.Node,selection.Robot,selection.Parts);_hangarMessage="停止して放棄しました / 放棄品置き場で確認できます";}
            catch(InvalidOperationException){_hangarMessage="予約・所有国・戦闘状況を確認してください";}
            _salvageAbandonConfirmation=null;_hangarAbandonMode=false;_salvageFocusTurn=-1;SyncHangarSlots();
        }
        return true;
    }
    private void DrawAbandonConfirmation()
    {
        if(_salvageAbandonConfirmation is not {} selection)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.7f);_ui.Box(new(500,350,920,390),new Color(27,49,62));
        SalvageText("放棄しますか？",new(540,385,840,65),.8f);
        var robot=_setup.Robots.Nodes[selection.Node].Robots.FirstOrDefault(r=>r.Id==selection.Robot);
        string parts=robot!=null?PartsLabel(robot.Slice(selection.Parts)):PartsLabel(selection.Parts);
        SalvageText("ID "+selection.Robot+" / "+parts,new(540,465,840,45),.6f);
        SalvageText("電源を切り、所有権を手放して地点の放棄品置き場へ出します。",new(540,530,840,35),.42f);
        SalvageText("品質・損傷は変わりません。他国に回収される可能性があります。",new(540,576,840,35),.4f);
        _ui.Button(AbandonYes,"放棄する",Accent,.6f);_ui.Button(AbandonNo,"やめる",Muted,.6f);
    }
}
