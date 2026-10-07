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
    private int _nodeTransportPage;
    private bool _nodeTransportDisposal;
    private string _nodeTransportMessage="";
    private static readonly Rectangle NodeTransportTile=new(280,875,182,84);
    private void OpenNodeTransport()
    {_nodeAssemblyOpen=false;_nodeDisposalOpen=false;_nodeFactoryOpen=false;_nodeTransportOpen=true;_nodeTransportPage=0;_nodeTransportMessage="";}
    private bool NodeTransportClick(Point pointer)
    {
        if(new Rectangle(803,169,58,44).Contains(pointer) || NodeTransportTile.Contains(pointer)){_nodeTransportOpen=false;return true;}
        if(NodeFactoryTile.Contains(pointer)){OpenNodeFactory();return true;}
        if(new Rectangle(84,270,374,46).Contains(pointer)){_nodeTransportDisposal=false;return true;}
        if(new Rectangle(480,270,374,46).Contains(pointer)){_nodeTransportDisposal=true;return true;}
        for(int mask=1;mask<=7;mask++)if(new Rectangle(84+(mask-1)%4*196,322+(mask-1)/4*56,182,46).Contains(pointer))
        {_nodeTransportPart=(RobotParts)mask;return true;}
        var routes=_setup.Routes.Neighbors(_setup.Nodes.All[_populationNode]);
        if(new Rectangle(630,810,100,40).Contains(pointer)){_nodeTransportPage=Math.Max(0,_nodeTransportPage-1);return true;}
        if(new Rectangle(744,810,100,40).Contains(pointer)){_nodeTransportPage=Math.Min(Math.Max(0,(routes.Length-1)/4),_nodeTransportPage+1);return true;}
        for(int row=0;row<4;row++)
        {
            int index=_nodeTransportPage*4+row;if(index>=routes.Length)break;int y=462+row*78;
            bool minus=new Rectangle(630,y,100,42).Contains(pointer),plus=new Rectangle(744,y,100,42).Contains(pointer);
            if(!minus && !plus)continue;
            int target=_setup.Nodes.At(routes[index].Target,routes[index].Entry).Id;
            var weights=_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).ToList();
            int value=Math.Clamp((weights.FirstOrDefault(w=>w.Target==target)?.Twentieths??0)+(plus?1:-1),0,20);
            weights.RemoveAll(w=>w.Target==target);if(value>0)weights.Add(new(target,value));
            if(weights.Sum(w=>w.Twentieths)+_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,!_nodeTransportDisposal).Sum(w=>w.Twentieths)>20){_nodeTransportMessage="輸送率の合計は100％以内にしてください";return true;}
            _setup.Robots.Transport.SetPlan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,weights,_nodeTransportDisposal);
            _nodeTransportMessage="自分の輸送計画を保存しました";return true;
        }
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawNodeTransportTile()
    {
        _ui.Box(NodeTransportTile,_nodeTransportOpen || NodeTransportTile.Contains(_pointer)?Accent:Muted);
        _spriteBatch.Draw(_robotTransportIcon,new Rectangle(290,891,52,52),Color.White);
        _ui.Center("ロボット輸送計画",new(344,885,112,60),.24f,Cream);
        if(_nodeTransportOpen)_ui.Box(new(NodeTransportTile.X,NodeTransportTile.Y,NodeTransportTile.Width,4),Cream);
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
        int total=_setup.Robots.Transport.Plan(node.Id,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).Sum(w=>w.Twentieths)*5;
        int combined=total+_setup.Robots.Transport.Plan(node.Id,_setup.PlayerSlot,_nodeTransportPart,!_nodeTransportDisposal).Sum(w=>w.Twentieths)*5;
        _ui.Text($"選択中 {total}% / ふつう＋廃棄 {combined}% / 残り {100-combined}% は留まる",new(84,437),.43f,Cream);
        for(int row=0;row<4;row++)
        {
            int index=_nodeTransportPage*4+row;if(index>=routes.Length)break;int y=462+row*78;
            var target=_setup.Nodes.At(routes[index].Target,routes[index].Entry);string name=_setup.Nodes.Label(target);
            _ui.Text(name,new(84,y),Math.Min(.37f,530/_font.MeasureString(name).X),Cream);
            int rate=(_setup.Robots.Transport.Plan(node.Id,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).FirstOrDefault(w=>w.Target==target.Id)?.Twentieths??0)*5;
            bool enemy=target.Owner>=0 && !_setup.Relations.Allied(target.Owner,_setup.PlayerSlot);
            _ui.Text($"{rate}% / {(enemy?"敵国：計画のみ":"輸送可能")}",new(84,y+29),.43f,Cream);
            _ui.Button(new(630,y,100,42),"−",Muted,.5f);_ui.Button(new(744,y,100,42),"＋",Muted,.5f);
        }
        _ui.Text(_nodeTransportMessage,new(84,810),.38f,Cream);
        _ui.Button(new(630,810,100,40),"←",Muted,.5f);_ui.Button(new(744,810,100,40),"→",Muted,.5f);
        DrawNodeFactoryTile(false);DrawNodeTransportTile();DrawNodeDisposalTile();DrawNodeAssemblyTile();
    }
}
