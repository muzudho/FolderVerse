namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _phasePlaqueFrame,_phasePlaqueIcons;
    // Logical 1920x1080 coordinates. Below this 80px header, map controls start at y=82.
    private static readonly Rectangle PhasePlaqueBounds=new(696,0,420,80);
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
    private void DrawPhasePlaque(){if(CurrentPhasePlaque is {} label)DrawPhasePlaqueLabel(label.Main,label.Sub,label.Icon);}
    private void DrawPhasePlaqueLabel(string main,string sub,int icon){
        Texture2D Load(string name){
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Ui",name));
            var texture=Texture2D.FromStream(GraphicsDevice,stream);
            // PNGs store straight alpha; SpriteBatch's AlphaBlend expects premultiplied RGB.
            var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color((byte)(c.R*c.A/255),(byte)(c.G*c.A/255),(byte)(c.B*c.A/255),c.A);}
            texture.SetData(pixels);return texture;
        }
        _phasePlaqueFrame??=Load("phase-plaque-frame-v001.png");_phasePlaqueIcons??=Load("phase-plaque-icons-v001.png");
        var plate=new Rectangle(752,2,358,78);
        var accent=DomesticTheme?_setup.OwnerColor(_setup.PlayerSlot):DefaultAccent;
        // A national-color backing rim; the title itself stays ivory and readable.
        _spriteBatch.Draw(_phasePlaqueFrame,new Rectangle(plate.X-2,plate.Y-1,plate.Width+4,plate.Height+2),accent);
        _spriteBatch.Draw(_phasePlaqueFrame,plate,Color.White);
        int cw=_phasePlaqueIcons.Width/3,ch=_phasePlaqueIcons.Height/3;
        _spriteBatch.Draw(_phasePlaqueIcons,new Rectangle(696,-2,96,82),new Rectangle(icon%3*cw,icon/3*ch,cw,ch),Color.White);
        var ink=new Color(57,43,30);
        var title=new Rectangle(797,9,268,36);var subtitle=new Rectangle(839,54,186,22);
        float Fit(string text,Rectangle area,float maximum)=>Math.Min(maximum,(area.Width-8)/Math.Max(1,_font.MeasureString(text).X));
        PortraitHudLayout.CenterLines(_ui,_font,new[]{main},title,Fit(main,title,.68f),ink);
        PortraitHudLayout.CenterLines(_ui,_font,new[]{sub},subtitle,Fit(sub,subtitle,.38f),ink);
    }
    private void DrawPhaseBackdropLogo(){
        // Translate the watermark without changing its aspect ratio; clip the part below the canvas.
        const int top=112,height=1080-top;
        var source=new Rectangle(0,0,_titleLogo.Width,(int)MathF.Round(_titleLogo.Height*height/1080f));
        _spriteBatch.Draw(_titleLogo,new Rectangle(0,top,1920,height),source,Color.White*.14f);
    }
    private void DisposePhasePlaques(){_phasePlaqueFrame?.Dispose();_phasePlaqueIcons?.Dispose();_phasePlaqueFrame=_phasePlaqueIcons=null;}
}
