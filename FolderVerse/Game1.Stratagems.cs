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
    private bool _stratagemManual,_stratagemPanning,_stratagemReservationsOpen;
    private int _stratagemSelected=-1,_stratagemHover=-1,_stratagemOffset,_stratagemPage,_stratagemGroup=-1;
    private string _stratagemMessage="";
    private StratagemMatch _stratagemPreview;
    private StratagemMatch[] _stratagemMatches=Array.Empty<StratagemMatch>();
    private double _stratagemRefresh;
    private int _stratagemController=-1;
    private int _stratagemTurn=-1;
    private WorldSetup _stratagemWorld;
    private readonly Dictionary<StratagemKind,Texture2D> _stratagemArt=new();
    private bool StratagemInterface=>IsStatusScreen && DomesticOperationsAllowed && _setup.TutorialStep==0 &&
        !_demoActive && !_autoPlay && !_setup.ComputerOnly && _setup.ControlledParty(_setup.TurnPlayer).Length>0;
    private bool StratagemManualActions=>_stratagemManual || _stratagemMatches.Length==0 && !StratagemPending.Any();
    private bool StratagemPanelActive=>StratagemInterface && _stratagemSelected>=0 && !InspectorVisible;
    private IEnumerable<StratagemReservation> StratagemPending=>_setup.Campaign.Stratagems.Pending.Where(p=>p.Controller==_setup.TurnPlayer);
    private static readonly Rectangle StratagemTray=new(54,820,1115,146),StratagemPanel=new(54,150,850,680);
    private static readonly Rectangle StratagemEnd=new(1700,984,180,78),StratagemClose=new(845,165,42,42);
    private static readonly Rectangle StratagemReserve=new(550,760,325,48),StratagemPrevious=new(82,705,115,38),StratagemNext=new(745,705,115,38);
    private StratagemKind[] StratagemCards=>Enum.GetValues<StratagemKind>().Where(k=>_stratagemMatches.Any(m=>m.Kind==k) || StratagemPending.Any(p=>p.Match.Kind==k)).ToArray();
    private static Rectangle StratagemCard(int slot,bool expanded)=>new(265+slot*194,expanded?724:854,180,expanded?240:110);
    private static Color StratagemColor(StratagemKind kind)=>StratagemPlans.Category(kind) switch{
        StratagemCategory.Defense=>new(34,118,72),StratagemCategory.Transport=>new(38,126,195),
        StratagemCategory.Harvest=>new(111,208,174),StratagemCategory.Manufacturing=>new(36,49,100),_=>new(172,48,45)};
    private Texture2D StratagemArt(StratagemKind kind)
    {
        if(_stratagemArt.TryGetValue(kind,out var image))return image;
        string file=kind switch{StratagemKind.WallSortie=>"attack-through-wall-v001.png",StratagemKind.WallRelief=>"relieve-wall-v001.png",_=>"concentrated-attack-v001.png"};
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Stratagems",file));
        image=Texture2D.FromStream(GraphicsDevice,stream);_stratagemArt.Add(kind,image);return image;
    }
    private void RefreshStratagems()
    {
        _stratagemMatches=_setup.Campaign.Stratagems.Find(_setup,_setup.TurnPlayer);
        _stratagemRefresh=0;
        if(_stratagemController!=_setup.TurnPlayer || _stratagemTurn!=_setup.Population.Turn || !ReferenceEquals(_stratagemWorld,_setup)){
            _stratagemSelected=-1;_stratagemPreview=null;_stratagemGroup=-1;_stratagemController=_setup.TurnPlayer;_stratagemManual=false;
            _stratagemTurn=_setup.Population.Turn;_stratagemWorld=_setup;
        }
        if(_stratagemPreview!=null && !_stratagemMatches.Any(m=>m.Key==_stratagemPreview.Key))_stratagemPreview=null;
    }
    private StratagemMatch[] SelectedStratagemMatches=>_stratagemMatches.Where(m=>(int)m.Kind==_stratagemSelected).ToArray();
    // Group many combinations by the enemy objective, then choose the exact rear/front pairing.
    private int[] StratagemGroups=>SelectedStratagemMatches.Select(m=>m.Target).Distinct().ToArray();
    private bool StratagemInput(MouseState mouse,bool click,bool escape,bool active,double elapsed)
    {
        if(!StratagemInterface){_stratagemSelected=-1;_stratagemController=-1;_stratagemPreview=null;return false;}
        _stratagemRefresh+=elapsed;
        if(_stratagemController!=_setup.TurnPlayer || _stratagemRefresh>.8)RefreshStratagems();
        if(!active || InspectorVisible || MapFooterAction>=0 || !MapMarkerMenuAvailable || _transferChoice!=null)return false;
        if(escape && _stratagemSelected>=0){_stratagemSelected=-1;_stratagemPreview=null;return true;}
        if(click && !StratagemManualActions && StratagemEnd.Contains(_pointer)){EndPlannedTurn();RefreshStratagems();return true;}
        var cards=StratagemCards;int max=Math.Max(0,cards.Length-4);
        bool overTray=StratagemTray.Contains(_pointer) || _stratagemHover>=0 && StratagemCard(_stratagemHover,true).Contains(_pointer);
        if(overTray && mouse.RightButton==ButtonState.Pressed && _previousMouse.RightButton==ButtonState.Released)_stratagemPanning=true;
        if(_stratagemPanning){
            if(mouse.RightButton==ButtonState.Released)_stratagemPanning=false;
            else if(Math.Abs(mouse.X-_previousMouse.X)>3)_stratagemOffset=Math.Clamp(_stratagemOffset+Math.Sign(_previousMouse.X-mouse.X),0,max);
            return true;
        }
        if(overTray){
            int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
            if(wheel!=0)_stratagemOffset=Math.Clamp(_stratagemOffset-Math.Sign(wheel),0,max);
            if(click){
                if(new Rectangle(1080,854,32,110).Contains(_pointer)){_stratagemOffset=Math.Max(0,_stratagemOffset-1);return true;}
                if(new Rectangle(1120,854,32,110).Contains(_pointer)){_stratagemOffset=Math.Min(max,_stratagemOffset+1);return true;}
                if(new Rectangle(64,854,180,110).Contains(_pointer)){_stratagemManual=true;_stratagemSelected=-1;_stratagemPreview=null;return true;}
                for(int slot=0;slot<4 && slot+_stratagemOffset<cards.Length;slot++)if(StratagemCard(slot,slot==_stratagemHover).Contains(_pointer)){
                    ResetInspector();_stratagemSelected=(int)cards[slot+_stratagemOffset];_stratagemManual=false;_stratagemPage=0;_stratagemGroup=-1;_stratagemPreview=null;_stratagemMessage="";_stratagemReservationsOpen=false;return true;
                }
            }
            return true; // Never pass a card click/wheel to the map behind it.
        }
        if(_stratagemSelected<0)return false;
        if(!StratagemPanel.Contains(_pointer))return false;
        if(!click)return true;
        if(StratagemClose.Contains(_pointer)){_stratagemSelected=-1;_stratagemPreview=null;return true;}
        var pending=StratagemPending.Where(p=>(int)p.Match.Kind==_stratagemSelected).ToArray();
        if(pending.Length>0 && new Rectangle(82,235,780,42).Contains(_pointer)){
            if(_stratagemReservationsOpen || pending.Length>1){_stratagemReservationsOpen=!_stratagemReservationsOpen;_stratagemPage=0;_stratagemPreview=null;return true;}
            _setup.Campaign.Stratagems.Cancel(pending[^1].Id,_setup.TurnPlayer);_stratagemMessage="計略予約を取り消しました";RefreshStratagems();return true;
        }
        if(_stratagemReservationsOpen){
            if(StratagemPrevious.Contains(_pointer)){if(_stratagemPage>0)_stratagemPage--;else _stratagemReservationsOpen=false;return true;}
            if(StratagemNext.Contains(_pointer)){_stratagemPage=Math.Min(Math.Max(0,(pending.Length-1)/6),_stratagemPage+1);return true;}
            for(int row=0;row<6;row++)if(new Rectangle(82,315+row*60,780,52).Contains(_pointer)){
                int index=_stratagemPage*6+row;
                if(index<pending.Length){_setup.Campaign.Stratagems.Cancel(pending[index].Id,_setup.TurnPlayer);RefreshStratagems();}
                return true;
            }
            return true;
        }
        var groups=StratagemGroups;
        var choices=_stratagemGroup<0?Array.Empty<StratagemMatch>():SelectedStratagemMatches.Where(m=>m.Target==_stratagemGroup).ToArray();
        int count=_stratagemGroup<0?groups.Length:choices.Length;
        if(StratagemPrevious.Contains(_pointer)){
            if(_stratagemPage>0)_stratagemPage--;else if(_stratagemGroup>=0){_stratagemGroup=-1;_stratagemPreview=null;}
            return true;
        }
        if(StratagemNext.Contains(_pointer)){_stratagemPage=Math.Min(Math.Max(0,(count-1)/6),_stratagemPage+1);return true;}
        for(int row=0;row<6;row++)if(new Rectangle(82,315+row*60,780,52).Contains(_pointer)){
            int index=_stratagemPage*6+row;if(index>=count)return true;
            if(_stratagemGroup<0){_stratagemGroup=groups[index];_stratagemPage=0;_stratagemPreview=null;}
            else{
                _stratagemPreview=choices[index];_net.SelectedTarget=_stratagemPreview.Target;
                var target=_setup.Nodes.All[_stratagemPreview.Target];
                if(_statusGlobe)FocusGlobePosition(_setup.Routes.Position(target.Cell,target.Center));else _net.SetCenter(_setup,_setup.Cells[target.Cell].Face);
                QueueInspectorCenter(target.Id);
            }
            return true;
        }
        if(StratagemReserve.Contains(_pointer) && _stratagemPreview!=null){
            if(_setup.Campaign.Stratagems.Reserve(_setup,_setup.TurnPlayer,_stratagemPreview,out string reason)){
                _stratagemMessage="計略を予約しました。ターン終了まで取消できます";_stratagemPreview=null;
            }else _stratagemMessage=reason;
            RefreshStratagems();return true;
        }
        return true;
    }
    private void DrawStratagemInterface()
    {
        if(!StratagemInterface || InspectorVisible || MapFooterAction>=0 || !MapMarkerMenuAvailable)return;
        var ink=new Color(57,42,32);
        if(_stratagemSelected>=0){
            var kind=(StratagemKind)_stratagemSelected;var image=StratagemArt(kind);
            _ui.Box(StratagemPanel,new Color(246,232,201));
            DrawStratagemImage(image,StratagemPanel,.23f);
            _ui.Box(new(70,163,810,652),new Color(255,248,225)*.68f);
            _ui.Text(StratagemPlans.Title(kind),new(84,178),.72f,ink);_ui.Button(StratagemClose,"×",Muted,.55f);
            _ui.Text("予約のみ / 陣営のターン終了で確定",new(84,212),.36f,ink);
            var pending=StratagemPending.Where(p=>p.Match.Kind==kind).ToArray();
            if(pending.Length>0)_ui.Button(new(82,235,780,42),$"この計略の予約 {pending.Length}件 / "+(_stratagemReservationsOpen?"候補へ戻る":pending.Length==1?"予約取消":"予約一覧・取消"),StratagemColor(kind),.4f);
            var groups=StratagemGroups;
            var choices=_stratagemGroup<0?Array.Empty<StratagemMatch>():SelectedStratagemMatches.Where(m=>m.Target==_stratagemGroup).ToArray();
            int count=_stratagemGroup<0?groups.Length:choices.Length;
            _ui.Text(_stratagemGroup<0?$"使える配置 {SelectedStratagemMatches.Length}件 / まず目標を選択":$"目標：{StratagemNodeName(_stratagemGroup)} / 組合せ {choices.Length}件",new(84,285),.4f,ink);
            if(!_stratagemReservationsOpen)_stratagemPage=Math.Clamp(_stratagemPage,0,Math.Max(0,(count-1)/6));
            for(int row=0;row<6;row++){
                int index=_stratagemPage*6+row;if(index>=count)break;
                var bounds=new Rectangle(82,315+row*60,780,52);
                string label;
                if(_stratagemGroup<0)label=StratagemNodeName(groups[index])+$" / {SelectedStratagemMatches.Count(m=>m.Target==groups[index])}通り";
                else{var m=choices[index];label=string.Join("・",m.Sites.Select(s=>$"地点{s.Node+1}"))+$" / ロボット{m.Troops.Length}体";}
                bool selected=_stratagemPreview!=null && _stratagemGroup>=0 && choices[index].Key==_stratagemPreview.Key;
                _ui.Button(bounds,label,selected?StratagemColor(kind):Muted,.4f);
            }
            if(count==0)_ui.Text("今使える配置はありません",new(110,340),.48f,ink);
            if(_stratagemPreview is {} previewChoice){
                string details=string.Join(" / ",previewChoice.Sites.Select(s=>$"地点{s.Node+1}：{(s.After==NodePurpose.Frontline?"詰所":s.After==NodePurpose.Defense?"城壁":"出撃")}"))+" / 使用ID "+string.Join(",",previewChoice.Troops.Select(t=>t.Before.Id));
                _ui.Text(details,new(84,680),Math.Min(.30f,775/Math.Max(1,_font.MeasureString(details).X)),ink);
            }
            _ui.Button(StratagemPrevious,_stratagemPage==0 && _stratagemGroup>=0?"目標へ戻る":"前",Muted,.36f);
            _ui.Button(StratagemNext,"次",Muted,.36f);
            _ui.Center($"{(count==0?0:_stratagemPage+1)} / {Math.Max(1,(count+5)/6)}",new(350,705,220,38),.4f,ink);
            _ui.Button(StratagemReserve,"この計略を予約",_stratagemPreview==null?Muted:StratagemColor(kind),.45f);
            string message=_stratagemMessage.Length>0?_stratagemMessage:_stratagemPreview==null?"組合せを選ぶと、地図で対象を確認できます":"用途変更・守備・出撃をまとめて予約します";
            _ui.Text(message,new(84,777),Math.Min(.31f,450/Math.Max(1,_font.MeasureString(message).X)),ink);
            if(_stratagemReservationsOpen){
                _ui.Box(new(70,280,810,530),new Color(251,241,215));
                _ui.Text("予約一覧 / 選んだ行の計略だけを取り消します",new(84,285),.4f,ink);
                _stratagemPage=Math.Clamp(_stratagemPage,0,Math.Max(0,(pending.Length-1)/6));
                for(int row=0;row<6;row++){
                    int index=_stratagemPage*6+row;if(index>=pending.Length)break;
                    var plan=pending[index];_ui.Button(new(82,315+row*60,780,52),$"予約{plan.Id} / 目標 地点{plan.Match.Target+1} / 取消",StratagemColor(kind),.4f);
                }
                _ui.Button(StratagemPrevious,_stratagemPage==0?"候補へ戻る":"前",Muted,.36f);_ui.Button(StratagemNext,"次",Muted,.36f);
            }
        }
        DrawStratagemMapPreview();
        PrimaryRoundBox(StratagemTray,new Color(43,34,28),12);
        _ui.Text($"計略 / 予約 {StratagemPending.Count()}件",new(65,823),.37f,Cream);
        _ui.Button(new(64,854,180,110),"マニュアル操作",_stratagemManual?Accent:Muted,.4f);
        var cards=StratagemCards;_stratagemOffset=Math.Clamp(_stratagemOffset,0,Math.Max(0,cards.Length-4));
        if(cards.Length==0)_ui.Text("計略がありません",new(300,880),.55f,Cream);
        int hover=-1;
        for(int slot=0;slot<4 && slot+_stratagemOffset<cards.Length;slot++){
            bool expanded=!StratagemPanelActive && (StratagemCard(slot,false).Contains(_pointer) || _stratagemHover==slot && StratagemCard(slot,true).Contains(_pointer));
            if(expanded)hover=slot;
            var kind=cards[slot+_stratagemOffset];var bounds=StratagemCard(slot,expanded);
            PrimaryRoundBox(new(bounds.X+3,bounds.Y+4,bounds.Width,bounds.Height),new Color(8,18,25),10);
            PrimaryRoundBox(bounds,new Color(216,173,92),10);PrimaryRoundBox(new(bounds.X+3,bounds.Y+3,bounds.Width-6,bounds.Height-6),new Color(250,236,205),8);
            _ui.Box(new(bounds.X+6,bounds.Y+7,bounds.Width-12,27),StratagemColor(kind));
            _ui.Center(StratagemPlans.Title(kind),new(bounds.X+8,bounds.Y+8,bounds.Width-16,25),.32f,Color.White);
            var picture=new Rectangle(bounds.X+7,bounds.Y+38,bounds.Width-14,bounds.Height-66);
            if(expanded){int width=picture.Height*3/4;picture.X+=(picture.Width-width)/2;picture.Width=width;}
            DrawStratagemImage(StratagemArt(kind),picture,1);
            int matches=_stratagemMatches.Count(m=>m.Kind==kind);
            _ui.Center($"使える配置 {matches}件",new(bounds.X+7,bounds.Bottom-26,bounds.Width-14,22),.31f,ink);
        }
        _stratagemHover=hover;
        _ui.Button(new(1080,854,32,110),"<",_stratagemOffset>0?Accent:Muted,.4f);
        _ui.Button(new(1120,854,32,110),">",_stratagemOffset+4<cards.Length?Accent:Muted,.4f);
        _ui.Text(cards.Length==0?"":$"計略 {_stratagemOffset+1}～{Math.Min(cards.Length,_stratagemOffset+4)} / {cards.Length}種類",new(970,824),.3f,Cream);
        if(!StratagemManualActions)_ui.Button(StratagemEnd,"この陣営のターン終了",Accent,.4f);
    }
    private string StratagemNodeName(int node)=>$"地点{node+1} "+_setup.Nodes.All[node].Name;
    private void DrawStratagemImage(Texture2D image,Rectangle area,float opacity)
    {
        // Cover without stretching. The original portrait can also be reused as the left backdrop.
        float scale=Math.Max(area.Width/(float)image.Width,area.Height/(float)image.Height);
        int width=(int)(area.Width/scale),height=(int)(area.Height/scale);
        var source=new Rectangle((image.Width-width)/2,(image.Height-height)/2,width,height);
        _spriteBatch.Draw(image,area,source,Color.White*opacity);
    }
    private void DrawStratagemMapPreview()
    {
        if(_stratagemPreview is not {} match)return;
        var color=StratagemColor(match.Kind);
        var nodes=match.Sites.Select(s=>s.Node).Append(match.Target).Distinct();
        foreach(int node in nodes)if(OperationPosition(node,out var at) && MapViewBounds.Contains(at.ToPoint())){
            PrimaryRoundBox(new((int)at.X-22,(int)at.Y-22,44,44),color*.85f,12);
            _ui.Center($"{node+1}",new((int)at.X-22,(int)at.Y-17,44,32),.35f,Color.White);
        }
        foreach(var order in match.Marches){
            int[] path=match.Wall>=0?new[]{order.SourceNode,match.Wall,match.Target}:new[]{order.SourceNode,match.Target};
            for(int i=1;i<path.Length;i++)if(OperationPosition(path[i-1],out var from) && OperationPosition(path[i],out var to) && MapViewBounds.Contains(from.ToPoint()) && MapViewBounds.Contains(to.ToPoint()))DrawFunctionTransferLine(from,to,color,5);
        }
        if(match.Kind==StratagemKind.WallRelief && OperationPosition(match.Sites[0].Node,out var rear) && OperationPosition(match.Wall,out var wall))DrawFunctionTransferLine(rear,wall,color,5);
    }
}
