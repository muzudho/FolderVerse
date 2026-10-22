namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private LessonAward[] _lessonAwards=Array.Empty<LessonAward>();
    private int _lessonPage;
    private double _lessonAge;
    private static readonly Rectangle LessonCatalogButton=new(770,230,120,64);
    private static readonly Rectangle LessonContinue=new(1370,974,440,64),LessonBack=new(90,974,440,64);
    private void BeginBattleResearch()
    {
        StopBattleMusic();_lessonAwards=_setup.Lessons.Research();_lessonPage=0;_lessonAge=0;_screen=Screen.BattleResearch;
    }
    private void UpdateBattleResearch(double elapsed,bool proceed)
    {
        _lessonAge+=elapsed;
        if(_lessonAge<1.8 || !proceed && _lessonAge<7)return;
        if(++_lessonPage>=Math.Max(1,_lessonAwards.Length)){BeginIndustry();return;}
        _lessonAge=0;
    }
    private void LessonText(string text,Rectangle box,float scale,Color color)=>_ui.Center(text,box,Math.Min(scale,(box.Width-28)/_font.MeasureString(text).X),color);
    private void DrawLessonCard(BattleLesson lesson,Rectangle box,bool owned,float reveal=1)
    {
        var ink=new Color(48,61,68);var accent=lesson==BattleLesson.FlagCourtesy?new Color(51,139,143):lesson==BattleLesson.RetreatBackwards?new Color(184,110,69):new Color(102,109,172);
        _ui.Box(new(box.X+8,box.Y+10,box.Width,box.Height),new Color(5,16,25)*.45f);
        _ui.Box(box,owned?new Color(250,243,219):new Color(175,186,187));
        _ui.Box(new(box.X+12,box.Y+12,box.Width-24,9),accent);
        LessonText(BattleLessons.Title(lesson),new(box.X+18,box.Y+35,box.Width-36,65),.72f,ink);
        int cx=box.Center.X,cy=box.Y+235;
        _ui.Box(new(box.X+30,box.Y+120,box.Width-60,235),new Color(224,224,202));
        void Robot(int x,int y,BattleFacing facing,Color color)
        {
            foreach(var patch in BattlePieceArt.Pieces[BattlePieceArt.Index(BattleRole.Soldier,facing)])
                _ui.Box(new(x+patch.Bounds.X*2,y+patch.Bounds.Y*2,patch.Bounds.Width*2,patch.Bounds.Height*2),color);
        }
        if(lesson==BattleLesson.FlagCourtesy){
            _ui.Box(new(cx-3,cy-38,6,77),ink);_ui.Box(new(cx+3,cy-38,52,30),accent);
            Robot(cx-32,cy-105,BattleFacing.South,accent);Robot(cx+85,cy-32,BattleFacing.West,new Color(170,106,79));Robot(cx-145,cy-32,BattleFacing.East,new Color(170,106,79));
            LessonText("北 → 東 → 南 → 西",new(box.X+30,box.Y+360,box.Width-60,42),.52f,ink);
        }else if(lesson==BattleLesson.RetreatBackwards){
            Robot(cx-70,cy-40,BattleFacing.East,accent);Robot(cx+100,cy-40,BattleFacing.West,new Color(102,111,122));
            _ui.Box(new(cx+20,cy+5,65,4),ink);LessonText("←",new(cx-170,cy-15,80,60),1.1f,accent);
            LessonText("敵を向いたまま、後ろへ",new(box.X+30,box.Y+360,box.Width-60,42),.52f,ink);
        }else{
            Robot(cx-145,cy-40,BattleFacing.East,accent);Robot(cx-5,cy-40,BattleFacing.South,new Color(170,106,79));Robot(cx+115,cy-40,BattleFacing.West,new Color(102,111,122));
            _ui.Box(new(cx-70,cy+5,180,4),ink);LessonText("STOP",new(cx-150,cy+65,300,45),.7f,accent);
            LessonText("射線と、その両隣を確認",new(box.X+30,box.Y+360,box.Width-60,42),.52f,ink);
        }
        string[] lines=lesson switch {
            BattleLesson.FlagCourtesy=>new[]{"旗への同時侵入で、巻き戻された。","仲間と譲り合い、北・東・南・西の順で","１体だけが旗セルに入る。"},
            BattleLesson.RetreatBackwards=>new[]{"撤退中、背中を撃たれて転倒した。","撤退方向と逆を向いて移動する。","正面の盾で身を守る機会を残す。"},
            _=>new[]{"仲間に攻撃が命中した。","射線に仲間がいれば小銃を撃たない。","両隣から射線を向く仲間にも注意。"}};
        for(int i=0;i<lines.Length;i++)LessonText(lines[i],new(box.X+24,box.Y+423+i*43,box.Width-48,40),.48f,ink);
        LessonText(owned?"取得済み / 自国の兵隊ロボットに適用":"未取得 / 生き残った目撃者が持ち帰る",new(box.X+24,box.Bottom-73,box.Width-48,45),.46f,owned?accent:ink);
        if(reveal<1)_ui.Box(box,new Color(27,43,55)*(1-reveal));
    }
    private void DrawBattleResearch()
    {
        _ui.Text("戦闘研究フェーズ",new(80,45),1.1f,Cream);
        if(_lessonAwards.Length==0){
            _ui.Center("今回、新しく取得した戦訓カードはありません",new(180,430,1560,100),.85f,Cream);
            _ui.Center("失敗を目撃したロボットが、転倒せず帰還すると研究できます",new(180,555,1560,70),.55f,Cream);
        }else{
            var award=_lessonAwards[_lessonPage];
            float t=MathHelper.SmoothStep(0,1,(float)Math.Clamp((_lessonAge-.35)/.8,0,1));
            DrawLessonCard(award.Lesson,new(110,180+(int)((1-t)*70),760,710),true,t);
            _ui.Center("新しい戦訓カードを取得！",new(950,285,850,90),.95f,new(255,220,128));
            LessonText(_setup.ConquerorNames[award.Owner],new(950,420,850,100),1.1f,_setup.OwnerColor(award.Owner));
            _ui.Center("生き残ったロボットの経験を、国の戦訓へ",new(950,555,850,70),.56f,Cream);
            _ui.Center($"{_lessonPage+1} / {_lessonAwards.Length}",new(950,710,850,60),.65f,Cream);
        }
        _ui.Button(LessonContinue,"次へ / Enter",Accent,.65f);
    }
    private void DrawLessonCatalog()
    {
        _ui.Text("戦訓カード",new(80,45),1.1f,Cream);
        LessonText(_setup.ConquerorNames[_setup.PlayerSlot]+" の戦訓",new(570,45,1250,70),.75f,_setup.OwnerColor(_setup.PlayerSlot));
        for(int i=0;i<BattleLessons.All.Length;i++){
            var lesson=BattleLessons.All[i];DrawLessonCard(lesson,new(80+i*590,180,560,710),(_setup.Lessons.Owned[_setup.PlayerSlot]&lesson)!=0);
        }
        _ui.Button(LessonBack,"世界征服計画へ戻る / Esc",Muted,.55f);
    }
}
