namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Rectangle PocketButton=new(84,912,190,52);
    private long? _pocketRobot;
    private bool _pocketFromNode;
    private string _pocketMessage="";
    private static readonly Point[] PocketZoomSizes={new(3,1),new(4,2),new(6,2)};
    private static readonly Vector2 PocketGap=new(12f/149,12f/153);
    private readonly int[] _pocketZoom={2,2};
    private readonly Point[] _pocketScroll=new Point[2];
    private readonly long?[][] _pocketSlots=new long?[2][];
    private (int Side,int Slot,Point Start)? _pocketDrag;
    private (int Side,Point Start,Point Origin)? _pocketPan;
    private static Rectangle PocketTransferButton(bool fromNode)=>new(fromNode?1000:54,615,300,48);
    private string PocketRobotLockReason(Robot robot,bool fromNode)
    {
        if(!new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Any(p=>(robot.Parts&p)!=0 && robot.Origin(p)==_setup.PlayerSlot))return "自国のパーツがありません";
        int node=_setup.Nodes.Current(_setup.PlayerSlot).Id;var world=_setup.Robots;
        if(_setup.Campaign.RobotBattles.LockedNode(node))return "継戦中";
        int work=world.WorkCountry(node,robot);if(work>=0 && work!=_setup.PlayerSlot)return "他国の仕事予約";
        if(world.Workshops[node].IsDisposing(robot.Id))return "廃棄処理中";
        var edit=world.PendingEdits.FirstOrDefault(e=>e.Ids.Contains(robot.Id));
        if(edit!=null)return edit.SplitPart.HasValue?"分解予約":"組立予約";
        return world.CanTransferPocket(node,_setup.PlayerSlot,robot)?"":"作業中";
    }
    private bool PocketRobotSelectable(Robot robot,bool fromNode)=>PocketRobotLockReason(robot,fromNode).Length==0;
    private bool PocketTransferEnabled(bool fromNode)
    {
        if(!_pocketRobot.HasValue || _pocketFromNode!=fromNode)return false;
        int player=_setup.PlayerSlot,node=_setup.Nodes.Current(player).Id;
        var source=fromNode?_setup.Robots.Nodes[node]:_setup.Robots.Retinues[player];
        var destination=fromNode?_setup.Robots.Retinues[player]:_setup.Robots.Nodes[node];
        var robot=source.Robots.FirstOrDefault(r=>r.Id==_pocketRobot);
        return robot!=null && PocketRobotSelectable(robot,fromNode) && destination.HasSpace;
    }
    private void PocketOutline(Rectangle box,Color color,int width=3)
    {
        _ui.Box(new(box.X,box.Y,box.Width,width),color);_ui.Box(new(box.X,box.Bottom-width,box.Width,width),color);
        _ui.Box(new(box.X,box.Y,width,box.Height),color);_ui.Box(new(box.Right-width,box.Y,width,box.Height),color);
    }
    private static Rectangle PocketCard(int index,bool fromNode)
    {var card=RobotCard(index);if(fromNode)card.X+=946;return card;}
    private static Rectangle PocketViewport(int side)=>new(PocketCard(0,side==1).X,220,882,294);
    private Vector2 PocketPitch(int side)=>HangarCellPitch(PocketViewport(side),PocketZoomSizes[_pocketZoom[side]],PocketGap);
    private Rectangle PocketViewCard(int side,int slot)=>HangarViewCell(PocketViewport(side),PocketPitch(side),PocketGap,
        slot,6,_pocketScroll[side].X,_pocketScroll[side].Y);
    private Point PocketLimit(int side)
    {
        var zoom=PocketZoomSizes[_pocketZoom[side]];
        return new(6-zoom.X,Math.Max(0,(_pocketSlots[side].Length+5)/6-zoom.Y));
    }
    private IEnumerable<int> PocketVisible(int side)
    {
        var zoom=PocketZoomSizes[_pocketZoom[side]];
        var scroll=_pocketScroll[side];
        for(int y=0;y<zoom.Y;y++)for(int x=0;x<zoom.X;x++){
            int slot=(scroll.Y+y)*6+scroll.X+x;
            if(slot<_pocketSlots[side].Length)yield return slot;
        }
    }
    private RobotStore PocketStore(int side)=>side==0?_setup.Robots.Retinues[_setup.PlayerSlot]:
        _setup.Robots.Nodes[_setup.Nodes.Current(_setup.PlayerSlot).Id];
    private void SyncPocket()
    {
        for(int side=0;side<2;side++){
            var store=PocketStore(side);
            if(_pocketSlots[side]?.Length!=store.TotalCapacity)Array.Resize(ref _pocketSlots[side],store.TotalCapacity);
            SyncHangarOccupants(_pocketSlots[side],store,_=>false);
            var limit=PocketLimit(side);
            _pocketScroll[side]=new(Math.Clamp(_pocketScroll[side].X,0,limit.X),Math.Clamp(_pocketScroll[side].Y,0,limit.Y));
        }
    }
    private int PocketHit(int side)=>PocketVisible(side).FirstOrDefault(i=>PocketViewCard(side,i).Contains(_pointer),-1);
    private Robot PocketOccupant(int side,int slot)=>slot<0?null:PocketStore(side).Robots.FirstOrDefault(r=>r.Id==_pocketSlots[side][slot]);
    private void PocketMouseInput(MouseState mouse,KeyboardState keys,bool click,bool escape,bool active)
    {
        if(!active){_pocketDrag=null;_pocketPan=null;return;}
        SyncPocket();
        if(escape || click && InactiveBack.Contains(_pointer)){_pocketDrag=null;_pocketPan=null;PocketInput(click,escape);return;}
        int side=PocketViewport(0).Contains(_pointer)?0:PocketViewport(1).Contains(_pointer)?1:-1;
        if(mouse.RightButton==ButtonState.Released)_pocketPan=null;
        if(_pocketDrag==null && side>=0 && mouse.RightButton==ButtonState.Pressed && _previousMouse.RightButton==ButtonState.Released)
            _pocketPan=(side,_pointer,_pocketScroll[side]);
        if(_pocketPan is {} pan){
            _pocketScroll[pan.Side]=HangarPanOffset(pan.Origin,pan.Start-_pointer,PocketPitch(pan.Side),PocketLimit(pan.Side));
            return;
        }
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        if(side>=0 && wheel!=0 && mouse.LeftButton==_previousMouse.LeftButton){
            if(keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift))
                _pocketScroll[side].Y=Math.Clamp(_pocketScroll[side].Y-Math.Sign(wheel),0,PocketLimit(side).Y);
            else{
                _pocketZoom[side]=Math.Clamp(_pocketZoom[side]-Math.Sign(wheel),0,PocketZoomSizes.Length-1);
                SyncPocket();
            }
        }
        if(_pocketDrag is {} drag && mouse.LeftButton==ButtonState.Released){
            _pocketDrag=null;
            if(Vector2.DistanceSquared(drag.Start.ToVector2(),_pointer.ToVector2())<=36 || side<0)return;
            int slot=PocketHit(side);
            if(slot<0)return;
            if(PocketOccupant(drag.Side,drag.Slot)?.Id!=_pocketRobot)return;
            if(side==drag.Side){
                var slots=_pocketSlots[side];
                (slots[drag.Slot],slots[slot])=(slots[slot],slots[drag.Slot]);
            }
            else if(PocketOccupant(side,slot)==null)TransferSelectedPocket(slot);
            else _pocketMessage="空き枠を選んでください";
            return;
        }
        if(!click)return;
        PocketInput(click,escape);
        if(side<0)return;
        int source=PocketHit(side);
        if(PocketOccupant(side,source) is {} robot && robot.Id==_pocketRobot && _pocketFromNode==(side==1))
            _pocketDrag=(side,source,_pointer);
    }
    private void OpenPocket()
    {
        _setup.Robots.RevalidateWorkReservations();CloseMovement();_populationCell=-1;_pocketRobot=null;_pocketMessage="";
        _pocketDrag=null;_pocketPan=null;
        Array.Clear(_pocketSlots);Array.Clear(_pocketScroll);Array.Fill(_pocketZoom,2);
        SyncPocket();_screen=Screen.Pocket;
    }
    private void PocketInput(bool click,bool escape)
    {
        if(escape || click && InactiveBack.Contains(_pointer)){_screen=Screen.WorldStatus;return;}
        if(!click)return;
        SyncPocket();
        foreach(bool fromNode in new[]{false,true})
        {
            if(PocketOccupant(fromNode?1:0,PocketHit(fromNode?1:0)) is {} robot)
            {
                if(PocketRobotSelectable(robot,fromNode)){_pocketRobot=robot.Id;_pocketFromNode=fromNode;_pocketMessage="";}
                else {_pocketRobot=null;_pocketMessage=PocketRobotLockReason(robot,fromNode)+"のため移せません";}
                return;
            }
        }
        if(_pocketRobot.HasValue && PocketTransferButton(_pocketFromNode).Contains(_pointer))TransferSelectedPocket();
    }
    private void TransferSelectedPocket(int slot=-1)
    {
        if(!_pocketRobot.HasValue)return;
        int player=_setup.PlayerSlot,node=_setup.Nodes.Current(player).Id;
        var source=PocketStore(_pocketFromNode?1:0);var destination=PocketStore(_pocketFromNode?0:1);
        long id=_pocketRobot.Value;
        if(_setup.Campaign.RobotBattles.LockedNode(node)){_pocketMessage="継戦中の地点では配備を変更できません";return;}
        if(!destination.HasSpace){_pocketMessage="移動先が満杯です（出撃中の帰還予約を含む）";return;}
        var robot=source.Robots.FirstOrDefault(r=>r.Id==id);
        if(robot==null){_pocketRobot=null;return;}
        if(!PocketRobotSelectable(robot,_pocketFromNode))
        {_pocketMessage=PocketRobotLockReason(robot,_pocketFromNode)+"のため移せません";return;}
        _setup.Robots.TransferPocket(node,player,id,_pocketFromNode);_pocketRobot=null;
        if(slot>=0)_pocketSlots[_pocketFromNode?0:1][slot]=id;
        SyncPocket();
        if(_pocketFromNode && _setup.TutorialPocketCourse && player==0)_setup.TutorialPocketed=true;
        _pocketMessage=_pocketFromNode?"自分のポケットに入れました":"現在地の地点へ配備しました";
    }
    private void DrawPocket()
    {
        SyncPocket();
        int player=_setup.PlayerSlot;var node=_setup.Nodes.Current(player);
        _ui.Text("自分のポケット",new(54,30),1f,Cream);
        _ui.Text("ポケットのロボットは自分と一緒に移動します",new(54,112),.55f,Cream);
        _ui.Text("通常のロボット輸送長期計画の対象にはなりません",new(54,160),.5f,Cream);
        string label="現在地："+_setup.Nodes.Label(node);
        _ui.Text(label,new(1000,112),System.Math.Min(.5f,830/_font.MeasureString(label).X),Cream);
        foreach(bool fromNode in new[]{false,true})
        {
            var store=fromNode?_setup.Robots.Nodes[node.Id]:_setup.Robots.Retinues[player];int x=fromNode?1000:54;
            int side=fromNode?1:0;
            _ui.Text($"{(fromNode?"地点の配備":"自分のポケット")} {store.Count}/{store.TotalCapacity}",new(x,185),.5f,Cream);
            foreach(int i in PocketVisible(side))
            {
                var card=PocketViewCard(side,i);var robot=PocketOccupant(side,i);
                _ui.Box(card,new(30,47,59));
                if(robot==null){_ui.Center((i+1).ToString(),new(card.X,card.Bottom-25,card.Width,22),.28f,Cream*.65f);continue;}
                bool selectable=PocketRobotSelectable(robot,fromNode),hover=selectable && card.Contains(_pointer),own=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Any(p=>(robot.Parts&p)!=0 && robot.Origin(p)==player);
                _ui.Box(card,hover?new Color(70,103,118):own?Muted:new Color(37,44,53));
                int work=_setup.Robots.WorkCountry(node.Id,robot);var ownerColor=_setup.OwnerColor(work>=0?work:own?player:robot.Owner);_ui.Box(new(card.X+3,card.Y+3,card.Width-6,23),ownerColor);
                _ui.Center(work>=0?$"仕事 #{work+1}":own?"自国パーツあり":"自国パーツなし",new(card.X+3,card.Y+3,card.Width-6,23),.30f,UiPainter.ContrastInk(ownerColor));
                int height=Math.Min(160,card.Height-74),width=Math.Max(48,height*2/3);
                DrawRobotGlyphCore(new(card.Center.X-width/2,card.Y+27,width,height),robot,true,selectable?1:.38f,null,true);
                _ui.Center(PartsLabel(robot),new(card.X,card.Bottom-45,card.Width,24),System.Math.Min(.34f,(card.Width-8)/System.Math.Max(1,_font.MeasureString(PartsLabel(robot)).X)),selectable?Cream:Cream*.55f);
                _ui.Center("ID "+robot.Id,new(card.X,card.Bottom-24,card.Width,18),.28f,Cream);
                string reason=PocketRobotLockReason(robot,fromNode);
                if(reason.Length>0){
                    var badge=new Rectangle(card.X+3,card.Bottom-23,card.Width-6,19);
                    _ui.Box(badge,new Color(86,54,35));
                    _ui.Center(reason,badge,System.Math.Min(.28f,(badge.Width-8)/System.Math.Max(1,_font.MeasureString(reason).X)),new Color(255,222,162));
                }
                if(_pocketRobot==robot.Id && _pocketFromNode==fromNode)PocketOutline(card,new(255,220,114),4);
                else if(hover)PocketOutline(card,Cream);
            }
            var button=PocketTransferButton(fromNode);bool enabled=PocketTransferEnabled(fromNode),buttonHover=enabled && button.Contains(_pointer);
            string action=fromNode?"ポケットへ入れる":"地点へ配備";
            if(enabled)_ui.Button(button,action,buttonHover?new Color(81,173,172):Accent,.5f);
            else{_ui.Box(button,new(53,65,75));_ui.Center(action,button,.5f,new(163,174,180));}
            if(buttonHover)PocketOutline(button,Cream);
        }
        _ui.Text("自分のロボットを選んで、現在地の地点と出し入れできます",new(54,695),.5f,Cream);
        _ui.Text("左ドラッグ：移動 / 右ドラッグ：スクロール / ホイール：拡大・縮小",new(54,741),.46f,Cream);
        foreach(bool fromNode in new[]{false,true}){
            int side=fromNode?1:0;
            if(PocketOccupant(side,PocketHit(side)) is {} robot){
                string name=$"所属 #{robot.Owner+1}　"+_setup.ConquerorNames[robot.Owner]+(robot.Owner==player?" / 自国":" / 他国");
                _ui.Text(name,new(54,793),System.Math.Min(.55f,1770/_font.MeasureString(name).X),_setup.OwnerColor(robot.Owner));
                string reason=PocketRobotLockReason(robot,fromNode);
                _ui.Text(reason.Length==0?"状態：移動可能":"状態："+reason+" / 移動不可",new(54,839),.48f,reason.Length==0?Cream:new Color(255,222,162));
            }
        }
        _ui.Text(_pocketMessage,new(54,936),.5f,Cream);
        _ui.Button(InactiveBack,"世界へ戻る",Accent,.5f);
        if(InactiveBack.Contains(_pointer))PocketOutline(InactiveBack,Cream);
        if(_pocketDrag is {} drag && PocketOccupant(drag.Side,drag.Slot) is {} carried &&
           Vector2.DistanceSquared(drag.Start.ToVector2(),_pointer.ToVector2())>36)
            DrawRobotGlyphCore(new(_pointer.X-34,_pointer.Y-40,68,102),carried,true,.88f,null,true);
    }
}
