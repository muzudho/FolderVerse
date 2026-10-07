namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Rectangle RobotMenuItem=new(1320,182,538,52);
    private static readonly Rectangle RobotQuickButton=new(236,1018,270,52);
    private int _robotNode,_robotRoutePage;
    private int _robotReinforcementIndex;
    private bool _robotTransportDisposal;
    private RobotParts _robotPart=RobotParts.Head;
    private readonly HashSet<long> _robotSelection=new();
    private string _robotMessage="";
    private static Rectangle RobotCard(int index)=>new(54+index%6*149,220+index/6*153,137,141);
    private int[] RobotNodes=>_setup.Nodes.All.Where(n=>n.Owner==_setup.PlayerSlot || _setup.Robots.Nodes[n.Id].Robots.Any(r=>r.Owner==_setup.PlayerSlot) || n.Id==_setup.Nodes.Current(_setup.PlayerSlot).Id).Select(n=>n.Id).ToArray();
    private RobotStore SelectedRobotStore=>_setup.Robots.Nodes[_robotNode];
    private RobotEncounter[] ReinforcementChoices
    {
        get
        {
            var routes=_setup.Routes.Neighbors(_setup.Nodes.All[_robotNode]);
            return _setup.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished && (e.Kind==BattleKind.Edge?e.Source==_robotNode || e.Target==_robotNode:routes.Any(r=>_setup.Nodes.At(r.Target,r.Entry).Id==e.Target))).OrderBy(e=>e.Id).ToArray();
        }
    }
    private void OpenRobots()
    {CloseMovement();_populationCell=-1;_robotNode=_setup.Nodes.Current(_setup.PlayerSlot).Id;_robotSelection.Clear();_robotRoutePage=0;_robotReinforcementIndex=0;_robotMessage="";_screen=Screen.Robots;}
    private static string PartsLabel(RobotParts parts)=>((parts&RobotParts.Head)!=0?"頭":"")+((parts&RobotParts.Body)!=0?"胴":"")+((parts&RobotParts.Legs)!=0?"脚":"");
    private void RobotInput(bool click,bool escape)
    {
        if(escape || click && InactiveBack.Contains(_pointer)){_screen=Screen.WorldStatus;return;}
        if(!click)return;
        try{RobotClick();}catch(ArgumentException){_robotMessage="その組み合わせ・計画は設定できません";}catch(InvalidOperationException){_robotMessage="容量・所有者・戦闘中の状態を確認してください";}
    }
    private void RobotClick()
    {
        int player=_setup.PlayerSlot;var nodes=RobotNodes;var store=SelectedRobotStore;
        if(new Rectangle(54,112,90,48).Contains(_pointer) || new Rectangle(850,112,90,48).Contains(_pointer))
        {
            int index=Array.IndexOf(nodes,_robotNode),delta=_pointer.X<200?-1:1;
            _robotNode=nodes[(index+delta+nodes.Length)%nodes.Length];_robotSelection.Clear();_robotRoutePage=0;return;
        }
        for(int i=0;i<store.Count;i++)if(new Rectangle(RobotCard(i).Right-36,RobotCard(i).Bottom-25,18,18).Contains(_pointer))
        {ToggleRobotDisposal(_robotNode,store.Robots[i].Id);return;}
        for(int i=0;i<store.Count;i++)if(RobotCard(i).Contains(_pointer))
        {var robot=store.Robots[i];if(robot.Owner==player){if(!_robotSelection.Add(robot.Id))_robotSelection.Remove(robot.Id);}return;}
        bool locked=_setup.Campaign.RobotBattles.LockedNode(_robotNode);
        for(int mask=1;mask<=7;mask++)if(new Rectangle(54+(mask-1)*126,554,116,44).Contains(_pointer)){_robotPart=(RobotParts)mask;return;}
        if(locked){_robotMessage="継戦中の節点では内政できません";return;}
        if(new Rectangle(54,615,210,48).Contains(_pointer))
        {_setup.Robots.QueueAssembly(_robotNode,_robotSelection);_robotSelection.Clear();_robotMessage="次の全体ターンに組み立てます";return;}
        if(new Rectangle(276,615,210,48).Contains(_pointer))
        {if(_robotSelection.Count!=1)return;_setup.Robots.QueueSplit(_robotNode,_robotSelection.Single(),_robotPart);_robotSelection.Clear();_robotMessage="次の全体ターンに分解します";return;}
        if(new Rectangle(1400,166,210,42).Contains(_pointer)){_robotTransportDisposal=false;return;}
        if(new Rectangle(1622,166,210,42).Contains(_pointer)){_robotTransportDisposal=true;return;}
        var routes=_setup.Routes.Neighbors(_setup.Nodes.All[_robotNode]);
        if(new Rectangle(1580,864,120,44).Contains(_pointer)){_robotRoutePage=Math.Max(0,_robotRoutePage-1);return;}
        if(new Rectangle(1714,864,120,44).Contains(_pointer)){_robotRoutePage=Math.Min(Math.Max(0,(routes.Length-1)/6),_robotRoutePage+1);return;}
        for(int row=0;row<6;row++)
        {
            int index=_robotRoutePage*6+row;if(index>=routes.Length)break;
            int target=_setup.Nodes.At(routes[index].Target,routes[index].Entry).Id;int y=270+row*94;
            var weights=_setup.Robots.Transport.Plan(_robotNode,player,_robotPart,_robotTransportDisposal).ToList();int weight=weights.FirstOrDefault(w=>w.Target==target)?.Twentieths??0;
            if(new Rectangle(1640,y+34,80,44).Contains(_pointer) || new Rectangle(1732,y+34,80,44).Contains(_pointer))
            {
                int next=Math.Clamp(weight+(_pointer.X<1720?-1:1),0,20);weights.RemoveAll(w=>w.Target==target);if(next>0)weights.Add(new(target,next));
                if(weights.Sum(w=>w.Twentieths)+_setup.Robots.Transport.Plan(_robotNode,player,_robotPart,!_robotTransportDisposal).Sum(w=>w.Twentieths)>20){_robotMessage="輸送率の合計は100％以内にしてください";return;}
                _setup.Robots.Transport.SetPlan(_robotNode,player,_robotPart,weights,_robotTransportDisposal);_robotMessage="輸送計画を更新しました";return;
            }
            if(new Rectangle(1500,y+34,126,44).Contains(_pointer))
            {
                if(_setup.Nodes.All[_robotNode].Owner!=player){_robotMessage="自国の節点で受け入れ順位を設定してください";return;}
                var order=_setup.Robots.Transport.Priority(_robotNode).Concat(routes.Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id)).Distinct().ToList();
                order.Remove(target);order.Insert(0,target);_setup.Robots.Transport.SetPriority(_robotNode,order);_robotMessage="この節点からの受け入れを最優先にしました";return;
            }
        }
        var encounters=ReinforcementChoices;
        if(encounters.Length>0 && (new Rectangle(1580,910,120,28).Contains(_pointer) || new Rectangle(1714,910,120,28).Contains(_pointer)))
        {_robotReinforcementIndex=Math.Clamp(_robotReinforcementIndex+(_pointer.X<1710?-1:1),0,encounters.Length-1);return;}
        if(encounters.Length>0 && new Rectangle(1000,942,830,50).Contains(_pointer))
        {_setup.Campaign.RobotBattles.SendReinforcement(_setup,player,_robotNode,encounters[Math.Clamp(_robotReinforcementIndex,0,encounters.Length-1)].Id);_robotMessage="隣接する戦場へ完成体を増援に送りました";}
    }
    private void DrawRobotGlyph(Rectangle r,int owner,RobotParts parts,bool captain=false,float opacity=1)
    {
        var color=_setup.OwnerColor(owner)*opacity;
        if((parts&RobotParts.Head)!=0){_ui.Box(new(r.X+r.Width/4,r.Y,r.Width/2,r.Height/4),Cream*opacity);_ui.Box(new(r.X+r.Width/5,r.Y,r.Width*3/5,captain?r.Height/7:r.Height/12),color);}
        if((parts&RobotParts.Body)!=0)_ui.Box(new(r.X+r.Width/6,r.Y+r.Height/3,r.Width*2/3,r.Height/3),color);
        if((parts&RobotParts.Legs)!=0){_ui.Box(new(r.X+r.Width/6,r.Y+r.Height*3/4,r.Width/4,r.Height/4),color);_ui.Box(new(r.X+r.Width*7/12,r.Y+r.Height*3/4,r.Width/4,r.Height/4),color);}
    }
    private void DrawRobots()
    {
        int player=_setup.PlayerSlot;var node=_setup.Nodes.All[_robotNode];var store=SelectedRobotStore;var workshop=_setup.Robots.Workshops[_robotNode];
        _ui.Text("ロボット輸送計画 / 配備",new(54,30),1f,Cream);
        _ui.Button(new(54,112,90,48),"←",Muted,.6f);_ui.Button(new(850,112,90,48),"→",Muted,.6f);
        string title=_setup.Nodes.Label(node);_ui.Center(title,new(154,112,686,48),Math.Min(.56f,670/_font.MeasureString(title).X),Cream);
        _ui.Text($"配備 {store.Count}/12 / 選択 {_robotSelection.Count} / 所有国 {node.Owner+1}",new(54,176),.49f,Cream);
        for(int i=0;i<12;i++)
        {
            var card=RobotCard(i);_ui.Box(card,Muted);if(i>=store.Count)continue;var robot=store.Robots[i];
            if(_robotSelection.Contains(robot.Id))_ui.Box(new(card.X,card.Y,card.Width,4),Cream);
            DrawRobotGlyph(new(card.X+42,card.Y+12,52,70),robot.Owner,robot.Parts,robot.Role==RobotRole.Captain);
            _ui.Center(PartsLabel(robot.Parts)+$" / #{robot.Owner+1}",new(card.X,card.Y+89,card.Width,28),.4f,Cream);
            _ui.Center("ID "+robot.Id,new(card.X,card.Y+115,55,24),.27f,Cream);
            DrawDisposalCheck(card,robot);
        }
        _ui.Text("自分の兵を選択 / 以下のパーツ種類は分解・製造・輸送に共通",new(54,520),.47f,Cream);
        for(int mask=1;mask<=7;mask++)_ui.Button(new(54+(mask-1)*126,554,116,44),PartsLabel((RobotParts)mask),mask==(int)_robotPart?Accent:Muted,.45f);
        _ui.Button(new(54,615,210,48),"組み立て",Muted,.5f);_ui.Button(new(276,615,210,48),"選択部分へ分解",Muted,.46f);
        DrawRobotRoutes();
        bool failed=_setup.Robots.WorkshopFailures.Any(f=>f.Node==_robotNode);
        _ui.Text(_setup.Campaign.RobotBattles.LockedNode(_robotNode)?"継戦中：工場・組立・輸送は停止しています":failed?"組立・分解予約の一部が失敗しました。容量・部品を確認してください":_robotMessage,new(54,936),.45f,Cream);
        _ui.Button(InactiveBack,"世界へ戻る",Accent,.5f);
        int reserves=_setup.Campaign.RobotBattles.Reserves.Count(r=>r.Node==_robotNode);
        _ui.Text($"帰還待機 {reserves} / 組立・分解予約 {_setup.Robots.PendingEdits.Count(e=>e.Node==_robotNode)} / 内政は全体ターンごとに更新",new(360,1034),.44f,Cream);
    }
    private void DrawRobotRoutes()
    {
        var routes=_setup.Routes.Neighbors(_setup.Nodes.All[_robotNode]);int player=_setup.PlayerSlot;
        _ui.Text("輸送計画 / "+PartsLabel(_robotPart),new(1000,168),.7f,Cream);
        _ui.Button(new(1400,166,210,42),"ふつうの移送",_robotTransportDisposal?Muted:Accent,.35f);
        _ui.Button(new(1622,166,210,42),"廃棄としての移送",_robotTransportDisposal?Accent:Muted,.32f);
        int total=_setup.Robots.Transport.Plan(_robotNode,player,_robotPart,_robotTransportDisposal).Sum(w=>w.Twentieths);
        int combined=total+_setup.Robots.Transport.Plan(_robotNode,player,_robotPart,!_robotTransportDisposal).Sum(w=>w.Twentieths);
        _ui.Text($"選択中 {total*5}% / ふつう＋廃棄 {combined*5}% / 残り {(20-combined)*5}% は留まる",new(1000,214),.48f,Cream);
        for(int row=0;row<6;row++)
        {
            int index=_robotRoutePage*6+row;if(index>=routes.Length)break;var target=_setup.Nodes.At(routes[index].Target,routes[index].Entry);int y=270+row*94;
            string name=_setup.Nodes.Label(target);_ui.Text(name,new(1000,y),Math.Min(.45f,820/_font.MeasureString(name).X),_setup.OwnerColor(target.Owner));
            int weight=_setup.Robots.Transport.Plan(_robotNode,player,_robotPart,_robotTransportDisposal).FirstOrDefault(w=>w.Target==target.Id)?.Twentieths??0;
            _ui.Text($"輸送 {weight*5}% / 配備 {_setup.Robots.Nodes[target.Id].Count}/12",new(1000,y+44),.47f,Cream);
            _ui.Button(new(1500,y+34,126,44),"受入１位",Muted,.38f);_ui.Button(new(1640,y+34,80,44),"−",Muted,.5f);_ui.Button(new(1732,y+34,80,44),"＋",Muted,.5f);
        }
        _ui.Text($"経路 {routes.Length} / ページ {_robotRoutePage+1}/{Math.Max(1,(routes.Length+5)/6)}",new(1000,874),.48f,Cream);
        _ui.Button(new(1580,864,120,44),"←",Muted,.5f);_ui.Button(new(1714,864,120,44),"→",Muted,.5f);
        var encounters=ReinforcementChoices;
        if(encounters.Length>0)
        {
            _robotReinforcementIndex=Math.Clamp(_robotReinforcementIndex,0,encounters.Length-1);var encounter=encounters[_robotReinforcementIndex];
            _ui.Text($"増援先 {_robotReinforcementIndex+1}/{encounters.Length} / Node {encounter.Target+1}",new(1000,914),.41f,Cream);
            _ui.Button(new(1580,910,120,28),"←",Muted,.38f);_ui.Button(new(1714,910,120,28),"→",Muted,.38f);
        }
        _ui.Button(new(1000,942,830,50),encounters.Length>0?"選択した継戦へ完成体を増援":"隣接する継戦なし",Muted,.49f);
    }
}
