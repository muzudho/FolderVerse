namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _mercyInput,_mercyOfferInput,_mercyTyping;
    private Captive _mercyCaptive;
    private string _mercyDigits="0",_mercyMessage="";
    private MercyOutcome _mercyResult;
    private static readonly Rectangle MercyMinus=new(640,850,85,58),MercyPlus=new(1195,850,85,58),MercyConfirm=new(1000,955,440,62),MercyBack=new(480,955,440,62);
    private bool HumanMercyVictor(Captive captive)=>_setup.HumanPlayers.Any(h=>_setup.Relations.Allied(captive.Victor,h));
    private Point AutomaticDispositionPoint=>_mercyResult!=null?MercyConfirm.Center:_mercyInput?MercyConfirm.Center:DispositionButton(DemoDispositionChoice()).Center;
    private void BeginMercyInput(Captive captive,bool offer)
    {
        _mercyInput=true;_mercyOfferInput=offer;_mercyCaptive=captive;_mercyMessage="";_mercyTyping=false;
        _mercyDigits=(offer?_setup.Relations.ComputerMercyOffer(_setup,captive):_setup.Relations.ComputerMercyDemand(_setup,captive)).ToString();
    }
    private void ValidateMercyUi()
    {
        if(_mercyResult!=null && !_setup.Relations.MercyResults.Contains(_mercyResult))_mercyResult=null;
        if(_mercyResult==null && _mercyCaptive!=null && !_setup.Relations.Pending.Contains(_mercyCaptive)){_mercyInput=false;_mercyCaptive=null;}
    }
    private void MercyKeyboard(KeyboardState keyboard)
    {
        if(!_mercyInput)return;
        foreach(var key in keyboard.GetPressedKeys().Where(key=>_previousKeyboard.IsKeyUp(key))){
            int digit=key>=Keys.D0 && key<=Keys.D9?(int)key-(int)Keys.D0:key>=Keys.NumPad0 && key<=Keys.NumPad9?(int)key-(int)Keys.NumPad0:-1;
            if(digit>=0){_mercyDigits=(_mercyTyping?_mercyDigits:"")+digit;_mercyTyping=true;}
            else if(key==Keys.Back){_mercyDigits=_mercyDigits.Length>1?_mercyDigits[..^1]:"0";_mercyTyping=true;}
            if(!int.TryParse(_mercyDigits,out int amount))amount=int.MaxValue;
            _mercyDigits=Math.Clamp(amount,0,_setup.Relations.OwnedMercyNodes(_setup,_mercyCaptive)).ToString();
        }
    }
    private void CompleteMercy(int demand)
    {
        _mercyResult=_setup.Relations.ResolveMercy(_setup,_mercyCaptive,demand);_mercyInput=false;
        _operations.Write("mercy_negotiation",_mercyResult);_world.ShowSetup(_setup,true);
    }
    private bool MercyInput(bool click,bool escape)
    {
        ValidateMercyUi();
        if(_mercyResult!=null){
            if(click && MercyConfirm.Contains(_pointer)){
                _mercyResult=null;_mercyCaptive=null;
                if(_setup.Relations.Pending.Count==0){
                    if(TryConquestEnding())return true;
                    _screen=_dispositionReturnList?Screen.InactiveList:Screen.WorldStatus;_dispositionReturnList=false;_lastAutoSaveTurn=-1;AutoSaveDomestic();
                }
            }
            return true;
        }
        if(!_mercyInput){
            if(_setup.Relations.Pending.Count==0)return false;
            var captive=_setup.Relations.Pending[0];
            if(_setup.HumanPlayers.Contains(captive.Ruler) && !HumanMercyVictor(captive) && !_setup.Relations.CanMercy(_setup,captive)){
                var choice=_setup.Relations.Accepts(_setup,captive.Ruler)?DefeatedChoice.Recruit:DefeatedChoice.RemoveBattery;
                _setup.Relations.Choose(_setup,captive,choice);_dispositionScene=captive;_dispositionSceneChoice=choice;_dispositionSceneAge=0;_world.ShowSetup(_setup,true);return true;
            }
            if(!_setup.HumanPlayers.Contains(captive.Ruler) || _setup.Relations.MercyOffers.Any(o=>o.Captive==captive))return false;
            if(!_setup.Relations.CanMercy(_setup,captive))return false;
            BeginMercyInput(captive,true);return true;
        }
        if(!_mercyOfferInput && (escape || click && MercyBack.Contains(_pointer))){_mercyInput=false;return true;}
        if(!click)return true;
        int value=int.Parse(_mercyDigits);
        if(MercyMinus.Contains(_pointer) || MercyPlus.Contains(_pointer)){
            bool plus=MercyPlus.Contains(_pointer);
            value=_mercyOfferInput
                ? (plus?_setup.Relations.MercyAmounts(_setup,_mercyCaptive).FirstOrDefault(n=>n>value,value):_setup.Relations.MercyAmounts(_setup,_mercyCaptive).LastOrDefault(n=>n<value,value))
                :Math.Clamp(value+(plus?1:-1),0,_setup.Relations.OwnedMercyNodes(_setup,_mercyCaptive));
            _mercyDigits=value.ToString();_mercyTyping=false;
        }
        if(!MercyConfirm.Contains(_pointer))return true;
        if(_mercyOfferInput){
            if(!_setup.Relations.SetMercyOffer(_setup,_mercyCaptive,value)){_mercyMessage="機能移転のまとまりを分割できません。＋／－で譲渡できる数を選んでください";return true;}
            if(HumanMercyVictor(_mercyCaptive) && !_autoPlay && !_demoActive)BeginMercyInput(_mercyCaptive,false);
            else CompleteMercy(_setup.Relations.ComputerMercyDemand(_setup,_mercyCaptive));
        }else CompleteMercy(value);
        return true;
    }
    private bool DrawMercyUi()
    {
        ValidateMercyUi();
        if(_mercyResult!=null){
            var result=_mercyResult;
            _ui.Center(result.Success?"見逃し成立":"見逃し不成立",new(200,130,1520,80),1f,Cream);
            _ui.Center($"譲渡の申告 {result.Offer}地点 / 勝者の要求 {result.Demand}地点",new(200,310,1520,65),.7f,Cream);
            _ui.Center(result.Success?$"{_setup.ConquerorNames[result.Captive.Ruler]}から{_setup.ConquerorNames[result.Captive.Victor]}へ {result.Offer}地点を譲渡":
                _setup.ConquerorNames[result.Captive.Ruler]+"の電池を引き抜きました",new(150,460,1620,65),.65f,Cream);
            _ui.Center(result.Success?$"残る直轄地点：{_setup.Relations.OwnedMercyNodes(_setup,result.Captive)} / 独立した征服者として復帰":"所有していた地点は未征服になります",new(200,570,1520,60),.5f,Cream);
            _ui.Button(MercyConfirm,"次へ",Accent,.65f);return true;
        }
        return false;
    }
    private void DrawMercyNumber()
    {
        var captive=_mercyCaptive;int owned=_setup.Relations.OwnedMercyNodes(_setup,captive);
        _ui.Box(new(260,765,1400,270),new Color(27,49,62));
        _ui.Center(_mercyOfferInput?"捕獲されました / 譲ってもいい地点数を申告":"見逃すために要求する最低地点数",new(300,777,1320,50),.6f,Cream);
        _ui.Button(MercyMinus,"－",Muted,.65f);_ui.Button(MercyPlus,"＋",Muted,.65f);
        _ui.Center(_mercyDigits+" 地点",new(750,838,420,78),.95f,Cream);
        string note=_mercyOfferInput?$"直轄 {owned}地点 / 譲渡可能 {_setup.Relations.MercyAmounts(_setup,captive)[^1]}地点 / 申告は秘密 / 数字キーで入力":
            $"敗者の直轄 {owned}地点 / 要求が申告数を超えると不成立・電池を引き抜く";
        _ui.Center(_mercyMessage.Length>0?_mercyMessage:note,new(280,916,1360,30),.35f,Cream);
        if(!_mercyOfferInput)_ui.Button(MercyBack,"処遇の選択へ戻る",Muted,.45f);
        _ui.Button(MercyConfirm,_mercyOfferInput?"この地点数を申告":"要求を提出",Accent,.55f);
        _ui.Center(ConquerorRelations.MercyMinimumExplanation,new(280,1020,1360,30),.32f,Cream);
    }
}
