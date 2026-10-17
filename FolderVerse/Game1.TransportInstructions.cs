namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private int _transportPage,_transportEditor,_transportEditNumber; // editor: 1 variants, 2 quantity
    private bool _transportDestinationPicking,_transportNewInstruction;
    private string _transportQuantity="1";
    private bool _transportQuantityReplace;
    private int TransportOwner=>_populationNode>=0 && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner)?_setup.Nodes.All[_populationNode].Owner:_setup.PlayerSlot;
    private bool CanEditTransport=>_populationNode>=0 && (CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || _setup.Robots.Nodes[_populationNode].Robots.Any(r=>r.Owner==_setup.PlayerSlot)) && !_setup.Campaign.RobotBattles.LockedNode(_populationNode);
    private IReadOnlyList<TransportInstruction> TransportRules=>_populationNode<0?Array.Empty<TransportInstruction>():_setup.Robots.Transport.Instructions(_populationNode,TransportOwner);
    private TransportInstruction EditingTransport=>TransportRules.FirstOrDefault(r=>r.Number==_transportEditNumber);
    private static string TransportPartsName(int mask)=>mask switch{7=>"頭胴脚",6=>"頭胴",5=>"頭脚",3=>"胴脚",4=>"頭",2=>"胴",_=>"脚"};
    private static readonly int[] TransportMasks={7,6,5,3,4,2,1};
    private static Rectangle InstructionField(int row,int field){int y=310+row*106;return field switch{0=>new(128,y,94,34),1=>new(230,y,142,34),2=>new(380,y,100,34),3=>new(488,y,318,34),4=>new(110,y+40,366,34),5=>new(518,y+40,80,34),_=>new(814,y,34,34)};}
    private void ConfigureTransport(TransportInstruction rule,int? target=null,int? variants=null,bool? cheap=null,bool? disposal=null,int? limit=null)=>_setup.Robots.Transport.ConfigureInstruction(_populationNode,TransportOwner,rule.Number,target??rule.Target,variants??rule.Variants,cheap??rule.Cheap,disposal??rule.Disposal,limit??rule.Limit);
    private int[] TransportAdjacent()=>_populationNode<0?Array.Empty<int>():_setup.Routes.Neighbors(_setup.Nodes.All[_populationNode]).Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id).Where(id=>id!=_populationNode).Distinct().ToArray();
    private bool TransportInstructionsClick(Point p)
    {
        if(_populationNode<0 || !CanEditTransport)return false;
        var transport=_setup.Robots.Transport;
        if(new Rectangle(84,264,540,36).Contains(p)){transport.UseInstructions(_populationNode,TransportOwner);return true;}
        if(!transport.HasInstructions(_populationNode,TransportOwner))return false;
        var page=TransportRules.Skip(_transportPage*4).Take(4).ToArray();
        for(int row=0;row<page.Length;row++)for(int field=0;field<7;field++)if(InstructionField(row,field).Contains(p)){
            var rule=page[row];_transportEditNumber=rule.Number;
            if(field==0)_rateTarget=rule.Number;
            if(field==1)ConfigureTransport(rule,disposal:!rule.Disposal);
            if(field==2)ConfigureTransport(rule,cheap:!rule.Cheap);
            if(field==3)_transportEditor=1;
            if(field==4){_transportDestinationPicking=true;_transportNewInstruction=false;}
            if(field==5){_transportEditor=2;_transportQuantity=rule.Limit.ToString();_transportQuantityReplace=true;}
            if(field==6){transport.DeleteInstruction(_populationNode,TransportOwner,rule.Number);_transportPage=Math.Min(_transportPage,Math.Max(0,(TransportRules.Count-1)/4));}
            return true;
        }
        if(new Rectangle(84,752,170,36).Contains(p))_transportPage=Math.Max(0,_transportPage-1);
        if(new Rectangle(264,752,170,36).Contains(p))_transportPage=Math.Min(Math.Max(0,(TransportRules.Count-1)/4),_transportPage+1);
        if(new Rectangle(444,752,220,36).Contains(p) && TransportRules.Count<16){var targets=TransportAdjacent();if(targets.Length>0){var rule=transport.AddInstruction(_populationNode,TransportOwner,targets[0]);_transportEditNumber=rule.Number;_transportPage=TransportRules.ToList().FindIndex(r=>r.Number==rule.Number)/4;_transportDestinationPicking=true;_transportNewInstruction=true;}}
        if(new Rectangle(674,752,174,36).Contains(p))foreach(var rule in page.Where(r=>r.Complete))transport.RestartInstruction(_populationNode,TransportOwner,rule.Number);
        return new Rectangle(54,150,850,680).Contains(p);
    }
    private void DrawTransportInstructions()
    {
        if(_populationNode<0)return;
        bool explicitPlan=_setup.Robots.Transport.HasInstructions(_populationNode,TransportOwner);
        _ui.Button(new(84,264,540,36),explicitPlan?"具体的に指示する":"具体的に指示する（新規）",CanEditTransport?Accent:Muted,.5f);
        if(!CanEditTransport){_ui.Text("他国の輸送計画は閲覧のみ",new(84,800),.4f,Cream);}
        if(!explicitPlan){_ui.Text("指示０個から設定します。従来の自動配分を置き換えます。",new(84,330),.4f,Cream);return;}
        var page=TransportRules.Skip(_transportPage*4).Take(4).ToArray();
        for(int row=0;row<page.Length;row++){
            var rule=page[row];int y=310+row*106;_ui.Box(new(80,y-3,774,100),new Color(21,39,51));
            _ui.Text("#"+rule.Number,new(84,y+7),.4f,Cream);
            _ui.Button(InstructionField(row,0),(rule.Degrees*100/360f).ToString("0.#")+"%",Accent,.43f);
            _ui.Button(InstructionField(row,1),rule.Disposal?"廃棄用として":"納品物として",rule.Disposal?new Color(177,60,73):Muted,.4f);
            _ui.Button(InstructionField(row,2),rule.Cheap?"安物の":"正規の",Muted,.43f);
            string variants=string.Join("／",TransportMasks.Where(m=>(rule.Variants&(1<<m))!=0).Select(TransportPartsName));
            _ui.Button(InstructionField(row,3),variants,Muted,Math.Min(.4f,300/Math.Max(1,_font.MeasureString(variants).X)));
            _ui.Button(InstructionField(row,6),"×",Muted,.5f);
            string target=_setup.Nodes.Label(_setup.Nodes.All[rule.Target]);
            string compact=System.Text.RegularExpressions.Regex.Replace(target.Split(" [Node")[0],@"[（(]-?\d+,-?\d+[）)]","");
            while(_font.MeasureString(compact).X*.4f>342 && compact.Length>1)compact=compact[..^2]+"…";
            _ui.Button(InstructionField(row,4),compact,Muted,.4f);
            if(InstructionField(row,4).Contains(_pointer))_ui.Text(target,new(84,820),Math.Min(.4f,770/Math.Max(1,_font.MeasureString(target).X)),Cream);
            _ui.Text("へ",new(484,y+47),.4f,Cream);_ui.Button(InstructionField(row,5),rule.Limit.ToString(),Muted,.46f);
            _ui.Text("個送ったら終了。",new(610,y+47),.4f,Cream);
            _ui.Text("いずれか１つ / "+rule.Sent+" / "+rule.Limit+(rule.Complete?" 完了":""),new(110,y+80),.3f,rule.Complete?new Color(121,235,161):Cream);
        }
        if(page.Length==0)_ui.Text("指示０個：この節点に留まります。",new(84,330),.5f,Cream);
        _ui.Button(new(84,752,170,36),"前へ",Muted,.4f);_ui.Button(new(264,752,170,36),"次へ",Muted,.4f);
        _ui.Button(new(444,752,220,36),"指示を追加 "+TransportRules.Count+" / 16",TransportRules.Count<16 && CanEditTransport?Accent:Muted,.4f);
        _ui.Button(new(674,752,174,36),"完了分を再開",Muted,.4f);
        _ui.Text("実行できる指示から採用 / 採用率の合計100%",new(84,798),.35f,Cream);
        if(_transportEditor!=0)DrawTransportEditor();
    }
    private static Rectangle TransportVariantButton(int i)=>new(104+i%4*182,370+i/4*130,172,116);
    private static Rectangle TransportQuantityKey(int i)=>new(220+i%3*142,400+i/3*66,132,56);
    private void DrawTransportEditor()
    {
        var rule=EditingTransport;if(rule==null)return;_ui.Box(new(84,305,770,433),new Color(17,33,46));
        if(_transportEditor==1){
            _ui.Text("いずれか１つ（複数選択可）",new(110,325),.55f,Cream);
            for(int i=0;i<7;i++){int mask=TransportMasks[i];var r=TransportVariantButton(i);bool on=(rule.Variants&(1<<mask))!=0;_ui.Button(r,"",on?Color.Lerp(Muted,Accent,.25f):Muted,.4f);if(on)_ui.Box(new(r.X,r.Y,r.Width,3),Accent);DrawRobotGlyph(new(r.Center.X-24,r.Y+5,48,70),TransportOwner,(RobotParts)mask,whiteOutline:true);_ui.Center((on?"ON ":"OFF ")+TransportPartsName(mask),new(r.X,r.Bottom-35,r.Width,30),.4f,Cream);}
            _ui.Button(new(644,665,180,46),"閉じる",Accent,.5f);
        }else{
            _ui.Text("何個送ったら終了？（1 ～ 9999）",new(110,325),.5f,Cream);_ui.Center(_transportQuantity,new(220,350,416,42),.7f,Cream);
            string[] keys={"7","8","9","4","5","6","1","2","3","C","0","<"};for(int i=0;i<12;i++)_ui.Button(TransportQuantityKey(i),keys[i],Muted,.6f);
            _ui.Button(new(660,465,160,56),"決定",Accent,.5f);_ui.Button(new(660,535,160,56),"取消",Muted,.5f);
        }
    }
    private void TransportQuantityPress(string key)
    {
        if(key=="OK"){if(int.TryParse(_transportQuantity,out int n) && n>=1 && n<=9999 && EditingTransport!=null){ConfigureTransport(EditingTransport,limit:n);_transportEditor=0;}return;}
        if(key=="C"){_transportQuantity="";_transportQuantityReplace=false;}
        else if(key=="<"){if(_transportQuantity.Length>0)_transportQuantity=_transportQuantity[..^1];_transportQuantityReplace=false;}
        else if(key.Length==1 && char.IsDigit(key[0])){if(_transportQuantityReplace){_transportQuantity="";_transportQuantityReplace=false;}if(_transportQuantity.Length<4)_transportQuantity+=key;}
    }
    private bool TransportModalInput(MouseState mouse,KeyboardState keyboard,bool click,bool escape,bool active)
    {
        if(!(_transportOverview || _nodeTransportOpen) || _populationNode<0)return false;
        if(_transportEditor==0 && !_transportDestinationPicking && _rateTarget<0)return false;
        if(!active)return true;
        if(!CanEditTransport){_transportEditor=0;_transportDestinationPicking=false;_rateTarget=-1;return true;}
        if(escape){if(_transportDestinationPicking && _transportNewInstruction)_setup.Robots.Transport.DeleteInstruction(_populationNode,TransportOwner,_transportEditNumber);_transportNewInstruction=false;_transportEditor=0;_transportDestinationPicking=false;_rateTarget=-1;_rateDragging=false;return true;}
        if(_rateTarget>=0){if(click){if(RateDialogArea.Contains(_pointer))RatePieClick(_pointer);else _rateTarget=-1;}return true;}
        var p=_pointer;p.X-=NodeInspectorOffset;
        if(_transportEditor==1){
            if(click){for(int i=0;i<7;i++)if(TransportVariantButton(i).Contains(p) && EditingTransport!=null){int variants=EditingTransport.Variants^(1<<TransportMasks[i]);if(variants!=0)ConfigureTransport(EditingTransport,variants:variants);}if(new Rectangle(644,665,180,46).Contains(p))_transportEditor=0;}return true;
        }
        if(_transportEditor==2){
            if(click){string[] keys={"7","8","9","4","5","6","1","2","3","C","0","<"};for(int i=0;i<12;i++)if(TransportQuantityKey(i).Contains(p))TransportQuantityPress(keys[i]);if(new Rectangle(660,465,160,56).Contains(p))TransportQuantityPress("OK");if(new Rectangle(660,535,160,56).Contains(p))_transportEditor=0;}
            foreach(var key in keyboard.GetPressedKeys())if(!_previousKeyboard.IsKeyDown(key)){if(key>=Keys.D0 && key<=Keys.D9)TransportQuantityPress(((int)key-(int)Keys.D0).ToString());else if(key>=Keys.NumPad0 && key<=Keys.NumPad9)TransportQuantityPress(((int)key-(int)Keys.NumPad0).ToString());else if(key==Keys.Back)TransportQuantityPress("<");else if(key==Keys.Enter)TransportQuantityPress("OK");}return true;
        }
        // Destination selection is modal, while retaining camera drag/zoom to reach adjacent nodes.
        if(MapViewBounds.Contains(_pointer)){
            int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
            if(_statusGlobe){if(wheel!=0)_world.GlobeZoom=MathHelper.Clamp(_world.GlobeZoom*MathF.Pow(1.2f,wheel/120f),.5f,16);Rotate(mouse,false,active,MapViewBounds);}
            else {if(wheel!=0)_net.ZoomAt(NetPanel,_pointer,wheel);if(mouse.RightButton==ButtonState.Pressed && _previousMouse.RightButton==ButtonState.Pressed){var old=CanvasPoint(_previousMouse);_net.Drag(new(_pointer.X-old.X,_pointer.Y-old.Y));}}
            if(click){var targets=TransportAdjacent();int id=_statusGlobe?HitGlobeNode(_pointer,targets):_net.HitNode(_setup,NetPanel,_pointer,targets);if(id>=0 && EditingTransport!=null){ConfigureTransport(EditingTransport,target:id);_transportNewInstruction=false;_transportDestinationPicking=false;}}
        }
        return true;
    }
}
