namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _robotTransportIcon;
    private bool _nodeTransportOpen;
    private RobotParts _nodeTransportPart=RobotParts.Complete;
    private bool _nodeTransportDisposal;
    private int _nodeReinforcementIndex;
    private RobotEncounter[] NodeReinforcementChoices
    {
        get
        {
            var routes=_setup.Routes.Neighbors(_setup.Nodes.All[_populationNode]);
            return _setup.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished && (e.Kind==BattleKind.Edge?
                e.Source==_populationNode || e.Target==_populationNode:routes.Any(r=>_setup.Nodes.At(r.Target,r.Entry).Id==e.Target))).OrderBy(e=>e.Id).ToArray();
        }
    }
    private string _nodeTransportMessage="";
    private static readonly Rectangle NodeTransportTile=new(280,875,182,84);
    private void OpenNodeTransport()
    {_nodeAssemblyOpen=false;_nodeDisposalOpen=false;_nodeFactoryOpen=false;_nodeTransportOpen=true;_rateTarget=-1;_nodeReinforcementIndex=0;_nodeTransportMessage="";FitDestinationView();}
    private bool NodeTransportClick(Point pointer)
    {
        if(new Rectangle(803,169,58,44).Contains(pointer) || NodeTransportTile.Contains(pointer)){_nodeTransportOpen=false;return true;}
        if(NodeFactoryTile.Contains(pointer)){OpenNodeFactory();return true;}
        if(new Rectangle(84,270,374,46).Contains(pointer)){_nodeTransportDisposal=false;return true;}
        if(new Rectangle(480,270,374,46).Contains(pointer)){_nodeTransportDisposal=true;return true;}
        for(int mask=1;mask<=7;mask++)if(new Rectangle(84+(mask-1)%4*196,322+(mask-1)/4*56,182,46).Contains(pointer))
        {_nodeTransportPart=(RobotParts)mask;return true;}
        var routes=_setup.Routes.Neighbors(_setup.Nodes.All[_populationNode]);
        var encounters=NodeReinforcementChoices;
        if(encounters.Length>0 && (new Rectangle(630,770,100,34).Contains(pointer) || new Rectangle(744,770,100,34).Contains(pointer)))
        {_nodeReinforcementIndex=Math.Clamp(_nodeReinforcementIndex+(pointer.X<740?-1:1),0,encounters.Length-1);return true;}
        if(new Rectangle(84,770,526,34).Contains(pointer))
        {
            if(encounters.Length>0)
            {
                try{_setup.Campaign.RobotBattles.SendReinforcement(_setup,_setup.PlayerSlot,_populationNode,encounters[Math.Clamp(_nodeReinforcementIndex,0,encounters.Length-1)].Id);_nodeTransportMessage="完成体を継戦へ増援に送りました";}
                catch(InvalidOperationException){_nodeTransportMessage="自国の節点・完成体・戦場への経路を確認してください";}
            }
            return true;
        }
        if(DestinationMapClick(pointer))return true;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeTransportTile()
    {
    }
    private void DrawNodeTransport()
    {
        var node=_setup.Nodes.All[_populationNode];var routes=_setup.Routes.Neighbors(node);
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));
        _ui.Text("ロボット輸送計画",new(84,178),.8f,Cream);_ui.Button(new(803,169,58,44),"×",Muted,.7f);
        string label=_setup.Nodes.Label(node);_ui.Text(label,new(84,230),Math.Min(.45f,770/_font.MeasureString(label).X),Cream);
        _ui.Button(new(84,270,374,46),"ふつうの移送",_nodeTransportDisposal?Muted:Accent,.5f);
        _ui.Button(new(480,270,374,46),"廃棄としての移送",_nodeTransportDisposal?Accent:Muted,.5f);
        for(int mask=1;mask<=7;mask++)_ui.Button(new(84+(mask-1)%4*196,322+(mask-1)/4*56,182,46),PartsLabel((RobotParts)mask),_nodeTransportPart==(RobotParts)mask?Accent:Muted,.48f);
        int total=_setup.Robots.Transport.Plan(node.Id,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).Sum(w=>w.Degrees);
        int combined=total+_setup.Robots.Transport.Plan(node.Id,_setup.PlayerSlot,_nodeTransportPart,!_nodeTransportDisposal).Sum(w=>w.Degrees);
        _ui.Text($"選択中 {RateLabel(total)} / ふつう＋廃棄 {RateLabel(combined)} / 残り {RateLabel(360-combined)} は留まる",new(84,437),.43f,Cream);
        _ui.Text("地図上の移送先と割合をクリック",new(84,490),.65f,Cream);
        var encounters=NodeReinforcementChoices;
        string reinforcement=encounters.Length==0?"増援先の継戦なし":$"継戦 #{encounters[Math.Clamp(_nodeReinforcementIndex,0,encounters.Length-1)].Id} へ完成体を増援";
        _ui.Button(new(84,770,526,34),reinforcement,encounters.Length>0 && node.Owner==_setup.PlayerSlot?Accent:Muted,.38f);
        _ui.Button(new(630,770,100,34),"←",Muted,.4f);_ui.Button(new(744,770,100,34),"→",Muted,.4f);
        _ui.Text(_nodeTransportMessage,new(84,810),.38f,Cream);
        DrawNodeFactoryTile(false);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
    }
}
