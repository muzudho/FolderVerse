namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private double _summaryReadAge;
    private const double SummaryReadSeconds=6;
    private int _summaryReadPapers;
    private static Rectangle SummaryPaperCard(int i)=>new(54+i*226,916,216,78);
    private bool SelectSummaryPaper()
    {
        for(int i=0;i<NewspaperProfiles.All.Length;i++)if(SummaryPaperCard(i).Contains(_pointer)){
            _newspaperEdition=i;_newspaperClock=0;return true;
        }
        return false;
    }
    private void UpdateAutomaticSummary(double elapsed,bool demo)
    {
        // The reading clock belongs to the result screen, independently of battle playback.
        _summaryReadAge+=elapsed;_newspaperSlideAge+=elapsed;
        if(_summaryReadAge+1e-8<SummaryReadSeconds)return;
        _summaryReadAge=0;
        
        
        if(++_summaryReadPapers<(demo?3:1)){SwitchNewspaper();return;}
        RobotBattleInput(0,false,true);
    }
    private void DrawRobotBattleSummary()
    {
        var profile=NewspaperProfiles.All[_newspaperEdition];
        var paper=_newspaperEdition==5?new Color(245,235,220):new Color(238,225,199);
        var ink=new Color(58,49,42);
        _ui.Box(new(40,20,1830,986),paper);
        DrawNewspaperMasthead(_newspaperEdition,new(470,28,980,80));
        _ui.Box(new(54,119,1798,3),ink);
        _ui.Center("今号の戦況 / 注目の戦闘",new(54,124,1000,42),.7f,ink);
        var selected=NewspaperSelection.Select(_robotScenes,_newspaperEdition,_setup.PlayerSlot,9);
        _ui.Text(profile.Label+$" / 全 {_robotScenes.Length} 戦場 / 掲載 {selected.Length} 件",new(54,175),.5f,ink);
        
        _newspaperPortraits??=new NewspaperPortraitRenderer(Content);
        for(int at=0;at<Math.Min(3,selected.Length);at++){
            int i=selected[at];var b=_robotScenes[i];
            int x=62+(at%3)*598,y=226+(at/3)*332;
            _ui.Box(new(x,y,576,312),ink*.06f);
            string name=b.Winner>=0?_setup.ConquerorNames[b.Winner]+"国":"勝者なし";
            string outcome=!b.Finished?"継戦":name+" / 勝利！";
            _ui.Center(outcome,new(x+8,y+6,560,42),Math.Min(.64f,550/Math.Max(1,_font.MeasureString(outcome).X)),ink);
            _ui.Text($"戦場 {i+1} / 地点 {b.TargetNode+1}",new(x+12,y+55),.46f,ink);
            var photo=new Rectangle(x+12,y+95,138,184);
            if(b.Winner>=0){
                if(!_newspaperPortraits.Draw(_spriteBatch,_setup.Looks[b.Winner],photo,profile.PhotoKind,profile.AllowFallback))DrawNewspaperToy(b.Winner,new(photo.Center.X,photo.Center.Y),100);
                if(profile.Wanted)DrawWantedStamp(photo);
            }
            int row=0;
            foreach(var army in b.Armies.OrderByDescending(a=>profile.FavorsArmy && a.Owner==b.Winner).Take(5)){
                int owner=army.Owner;
                var units=b.Units.Concat(army.Waiting).Where(u=>u.Owner==owner && u.Role!=BattleRole.Queen).DistinctBy(u=>u.Id).ToArray();
                string country=_setup.ConquerorNames[owner]+"国";
                _ui.Text(country,new(x+166,y+99+row*38),Math.Min(.43f,390/Math.Max(1,_font.MeasureString(country).X)),ink);
                _ui.Text($"転倒 {units.Count(u=>u.Fallen)} / 撤退 {units.Count(u=>u.Retreated)}",new(x+166,y+119+row*38),.36f,ink);
                row++;
            }
        }
        for(int at=3;at<selected.Length;at++){var b=_robotScenes[selected[at]];string winner=b.Winner>=0?_setup.ConquerorNames[b.Winner]+"国":"勝者なし";
            _ui.Text($"戦況短信 / 戦場 {selected[at]+1}：{(!b.Finished?"継戦":winner+" 勝利")}",new(72,570+(at-3)*42),.46f,ink);
        }
        int omitted=_robotScenes.Length-selected.Length;
        _ui.Text($"戦況短信：その他 {omitted} 戦場 / 終結 {_robotScenes.Count(b=>b.Finished)} / 継戦 {_robotScenes.Count(b=>!b.Finished)}",new(62,895),.38f,ink);
        // A horizontal shelf of miniature papers is also the publisher selector.
        for(int i=0;i<NewspaperProfiles.All.Length;i++){
            var r=SummaryPaperCard(i);_ui.Box(r,i==_newspaperEdition?Accent:ink*.13f);
            var sheet=new Rectangle(r.X+5,r.Y+5,r.Width-10,r.Height-10);_ui.Box(sheet,paper);
            DrawNewspaperMasthead(i,new(sheet.X+4,sheet.Y+3,sheet.Width-8,36));
            _ui.Center(NewspaperProfiles.All[i].Label,new(sheet.X,sheet.Y+42,sheet.Width,23),.28f,ink);
            _ui.Box(new(sheet.X+8,sheet.Bottom-5,sheet.Width-16,1),ink*.4f);
        }
        _ui.Button(SummaryNewspaperSwitch,"新聞を切り替える",Muted,.5f);
        _ui.Button(NewspaperMusicButton,"BGM："+NewspaperMusicScore.Labels[(int)_newspaperMusicMood],Muted,.5f);
        _ui.Button(BattleContinue,"世界へ戻る",Accent,.55f);
        DrawPhaseTimer(new(54,1000,1758,10),_summaryReadAge,SummaryReadSeconds);
    }
}
