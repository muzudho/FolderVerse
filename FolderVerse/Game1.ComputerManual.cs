namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private const int ComputerManualPageSize=12;
    private const int ComputerManualPageCount=(ConquerorCatalog.BaseCount+ComputerManualPageSize-1)/ComputerManualPageSize;
    private static readonly Rectangle ComputerManualMenuItem=new(1320,406,538,52);
    private static readonly Rectangle ComputerManualClose=new(1530,54,310,56);
    private static readonly Rectangle ComputerManualPrevious=new(76,780,210,60);
    private static readonly Rectangle ComputerManualNext=new(588,780,210,60);
    private static readonly Rectangle ComputerManualAttack=new(852,352,480,78);
    private static readonly Rectangle ComputerManualGuard=new(1350,352,480,78);
    private static readonly Rectangle ComputerManualReset=new(76,864,354,60);
    private static readonly string[] ComputerAttackNames={"バランス","敵の征服者を優先","守備の少ない地点を優先"};
    private bool _computerManualOpen;
    private int _computerManualPage,_computerManualCharacter;

    private void DrawComputerManualMenuItem() =>
        _ui.Button(ComputerManualMenuItem,"コンピュータープレイヤーの取扱説明書",Muted,.46f);

    private void OpenComputerManual()
    {
        _computerManualOpen=true;
        _statusMenuOpen=false;
    }

    private static Rectangle ComputerManualCard(int index)=>new(76+index%2*368,205+index/2*92,354,80);

    private bool ComputerManualInput(bool click,bool escape)
    {
        if(!_computerManualOpen)return false;
        if(escape || click && ComputerManualClose.Contains(_pointer))
        {
            _computerManualOpen=false;
            _statusMenuOpen=true;
        }
        else if(click)
        {
            var policy=_setup.CharacterComputerPolicy(_computerManualCharacter);
            if(ComputerManualAttack.Contains(_pointer))
            {
                _setup.SetCharacterComputerPolicy(_computerManualCharacter,policy with
                {
                    AttackPriority=(ComputerAttackPriority)(((int)policy.AttackPriority+1)%ComputerAttackNames.Length)
                });
                _lastAutoSaveTurn=-1;
            }
            else if(ComputerManualGuard.Contains(_pointer))
            {
                _setup.SetCharacterComputerPolicy(_computerManualCharacter,policy with { KeepGuard=!policy.KeepGuard });
                _lastAutoSaveTurn=-1;
            }
            else if(ComputerManualReset.Contains(_pointer))
            {
                _setup.SetCharacterComputerPolicy(_computerManualCharacter,null);
                _lastAutoSaveTurn=-1;
            }
            int oldPage=_computerManualPage;
            if(ComputerManualPrevious.Contains(_pointer))_computerManualPage=Math.Max(0,oldPage-1);
            else if(ComputerManualNext.Contains(_pointer))_computerManualPage=Math.Min(ComputerManualPageCount-1,oldPage+1);
            if(_computerManualPage!=oldPage)_computerManualCharacter=_computerManualPage*ComputerManualPageSize;
            for(int index=0;index<ComputerManualPageSize;index++)
            {
                int character=_computerManualPage*ComputerManualPageSize+index;
                if(character<ConquerorCatalog.BaseCount && ComputerManualCard(index).Contains(_pointer))
                    _computerManualCharacter=character;
            }
        }
        return true;
    }

    private void DrawComputerManual()
    {
        GraphicsDevice.Clear(new Color(16,35,46));
        var canvas=CanvasBounds();
        if(canvas.Width<=0 || canvas.Height<=0)return;
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*
            Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.Begin(transformMatrix:transform);
        _ui.Text("コンピュータープレイヤーの取扱説明書",new(76,64),.76f,Cream);
        _ui.Button(ComputerManualClose,"システムメニューへ",Muted,.52f);
        _ui.Text("キャラクター一覧（全60人）",new(76,149),.55f,Cream);
        for(int index=0;index<ComputerManualPageSize;index++)
        {
            int character=_computerManualPage*ComputerManualPageSize+index;
            if(character>=ConquerorCatalog.BaseCount)break;
            _ui.Button(ComputerManualCard(index),$"{character+1:00}  {ConquerorCatalog.Themes[character]}",
                character==_computerManualCharacter?new Color(68,104,111):Muted,.52f);
        }
        if(_computerManualPage>0)_ui.Button(ComputerManualPrevious,"前のページ",Muted,.53f);
        if(_computerManualPage+1<ComputerManualPageCount)_ui.Button(ComputerManualNext,"次のページ",Muted,.53f);
        _ui.Text($"{_computerManualPage+1} / {ComputerManualPageCount}",new(384,793),.55f,Cream);
        _ui.Button(ComputerManualReset,"初期設定に戻す",Muted,.49f);
        _ui.Text("変更はこの世界の同じ No. に適用",new(76,946),.48f,Cream);

        int selected=_computerManualCharacter;
        _ui.Text($"No.{selected+1:00}  {ConquerorCatalog.Themes[selected]}",new(852,149),.68f,Cream);
        _ui.Text("世界征服ランク（A～Z）　未計測",new(852,222),.58f,Cream);
        _ui.Text("同じ条件で比較したキャラクター別の成績が未集計です。",new(852,271),.47f,Cream);
        var policy=_setup.CharacterComputerPolicy(selected);
        _ui.Text("プレイングスタイルカード",new(852,309),.55f,Cream);
        _ui.Button(ComputerManualAttack,$"攻撃の趣向 / {ComputerAttackNames[(int)policy.AttackPriority]}",Muted,.44f);
        _ui.Button(ComputerManualGuard,policy.KeepGuard?"出撃元に守備を残す / ON":"出撃元に守備を残す / OFF",Muted,.44f);
        _ui.Text("捕虜になったときの勧誘への返答",new(852,460),.55f,Cream);
        for(int variant=0;variant<ConquerorCatalog.VariantsPerBase;variant++)
        {
            var look=new ConquerorLook(selected,variant);
            string response=ConquerorRelations.AcceptsRecruitment(look)?"承諾する":"断る";
            _ui.Text($"{variant+1}　{look.PersonalityName}　―　{response}",
                new(852+variant/3*480,515+variant%3*62),.49f,Cream);
        }
        _ui.Text("全キャラクター共通の行動方針",new(852,758),.58f,Cream);
        DrawComputerRuleCard("軍事　守備を見積もり、必要な兵力が揃えば同陣営で共同出撃。",810);
        DrawComputerRuleCard("輸送　後方の余剰機を前線に送り、満杯の格納庫では交換も試みる。",860);
        DrawComputerRuleCard("捕虜　勧誘を優先し、断られたら解放交渉や電池取り外しを選ぶ。",910);
        DrawComputerRuleCard("素材　キャラクター固有の好みは設定されていない。",960);
        _spriteBatch.End();
        DrawFullscreenCursor();
    }

    private void DrawComputerRuleCard(string text,int y)
    {
        _spriteBatch.Draw(_pixel,new Rectangle(852,y,978,44),new Color(32,53,62));
        _ui.Text(text,new(868,y+9),.46f,Cream);
    }
}
