namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private (string Main,string Sub,int Icon)? CurrentPhasePlaque=>_screen switch {
        Screen.Title or Screen.Ending=>null,
        Screen.OpeningBook=>("世界構築","物語を開く",0),
        Screen.PlayerCount=>("世界構築","遊ぶ人数",0),
        Screen.ComputerPlayerCount=>("世界構築","コンピューターの人数",0),
        Screen.OpeningSpace=>("世界構築","宇宙へ",0),
        Screen.Rolling or Screen.Review or Screen.OpeningLottery or Screen.OpeningReview=>("世界構築","星抽選",0),
        Screen.CastRolling or Screen.CastReview=>("世界構築","征服者抽選",0),
        Screen.PlacementRolling or Screen.PlacementReview or Screen.Ready=>("世界構築","区画権利の抽選",0),
        Screen.PlayerSelect or Screen.PlayerReady=>("世界構築","征服者選択",0),
        Screen.LandConquest=>("世界構築","領土選択",0),
        Screen.Fortune=>("世界構築","運否天賦・初期配置",0),
        Screen.Industry=>("産業",_manufacturingActive?"製造レーン稼働":"産業ルーレット",2),
        Screen.Newspaper=>("新聞","朝刊",3),
        Screen.Diplomacy=>("外交","取引の返事",8),
        Screen.Transport=>("伝播",DeliveryPlayback?"配送":"出兵",7),
        Screen.Battle when _setup.Campaign.UseRobotCombat && _robotSummary=>("新聞","戦況まとめ",3),
        Screen.Battle=>("戦闘","ロボット戦",5),
        Screen.BattleResearch=>("戦闘研究","戦訓カード",6),
        Screen.LessonCatalog=>("戦闘研究","戦訓図鑑",6),
        Screen.WorldStatus when _recoveryOpen || _salvageCatalogOpen || _directPairOpen || _tradePlannerOpen || _countrySettingsOpen=>null,
        Screen.WorldStatus=>("内政",GuideVisible?"ガイドの指南":"作戦と準備",1),
        Screen.Pocket=>("内政","ポケット",1),
        Screen.Hierarchy=>("内政","所属関係図",1),
        Screen.InactiveList=>("内政","電池を戻す",1),
        Screen.Disposition=>("内政","捕虜の処遇",1),
        _=>null
    };
    private void DrawPhasePlaque(){if(CurrentPhasePlaque is {} label)DrawPhasePlaqueLabel(label.Main,label.Sub,label.Icon);DrawPopupHints();}
    private void DrawPhaseBackdropLogo(){
        // Translate the watermark without changing its aspect ratio; clip the part below the canvas.
        const int top=176,height=1080-top;
        var source=new Rectangle(0,0,_titleLogo.Width,(int)MathF.Round(_titleLogo.Height*height/1080f));
        _spriteBatch.Draw(_titleLogo,new Rectangle(0,top,1920,height),source,Color.White*.14f);
    }
}
