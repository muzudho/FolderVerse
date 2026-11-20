namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _workpieceLane=-1,_workpiecePage;
    private Rectangle WorkpieceOpen(int row)=>new(91,FactoryRowY(row)+19,72,28);
    private static Rectangle WorkpieceCard(int i)=>new(110+i%6*286,220+i/6*325,268,300);
    private static readonly Rectangle WorkpiecePrevious=new(110,902,180,40),WorkpieceNext=new(1620,902,180,40);
    private Robot[] WorkpieceCandidates()=>_setup.Robots.Nodes[_populationNode].Robots.Where(r=>r.IsWorkpiece).OrderBy(r=>r.Id).ToArray();
    private void DrawFactoryWorkpieceOverlay(){if(_populationNode>=0 && _populationNode<_setup.Robots.Workshops.Length)DrawFactoryWorkpieces(FactoryScreenLines(PlanningWorkshop(_populationNode),out _));}
    private bool FactoryWorkpieceClick(Point pointer,RobotWorkshop[] lines)
    {
        if(_workpieceLane<0)return false;
        if(_workpieceLane>=lines.Length || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || _setup.Campaign.RobotBattles.LockedNode(_populationNode)){_workpieceLane=-1;return true;}
        var candidates=WorkpieceCandidates();int pages=Math.Max(1,(candidates.Length+11)/12);
        if(WorkpiecePrevious.Contains(pointer)){_workpiecePage=Math.Max(0,_workpiecePage-1);return true;}
        if(WorkpieceNext.Contains(pointer)){_workpiecePage=Math.Min(pages-1,_workpiecePage+1);return true;}
        for(int i=0;i<12;i++)if(WorkpieceCard(i).Contains(pointer)){
            int index=_workpiecePage*12+i;
            if(index<candidates.Length && _setup.Robots.PartPlanning.ReserveWorkpieceResume(_setup,_populationNode,lines[_workpieceLane],candidates[index].Id,_setup.Nodes.All[_populationNode].Owner)){_workpieceLane=-1;_factoryMessage="仕掛品の再開を予約しました / ターン終了時に実施";}
            else _factoryMessage="製造候補・所有国・予約を確認してください";
            return true;
        }
        _workpieceLane=-1;return true;
    }
    private void DrawFactoryWorkpieces(RobotWorkshop[] lines)
    {
        if(_workpieceLane<0 || _workpieceLane>=lines.Length)return;
        var candidates=WorkpieceCandidates();int pages=Math.Max(1,(candidates.Length+11)/12);_workpiecePage=Math.Min(_workpiecePage,pages-1);
        _ui.Box(new(0,0,1920,1080),Color.Black*.7f);DrawRulebookPaper(new(63,137,1794,830));
        _ui.Text("仕掛品から製造再開 / このレーンで製造できる品を選択",new(110,165),.5f,RulebookInk);
        for(int i=0;i<12 && i+_workpiecePage*12<candidates.Length;i++){
            var robot=candidates[i+_workpiecePage*12];var card=WorkpieceCard(i);
            bool enabled=_setup.Robots.CanResumeWorkpiece(_setup,_populationNode,lines[_workpieceLane],robot);
            _ui.Box(card,enabled?new Color(43,77,87):new Color(38,44,50));
            DrawRobotGlyphCore(new(card.X+62,card.Y+12,144,195),robot,true,enabled?1:.4f,null,true);
            _ui.Center("仕掛品 ID "+robot.Id,new(card.X+4,card.Y+212,card.Width-8,26),.35f,Cream);
            int row=0;foreach(var part in RobotMaterials.Parts.Where(p=>robot.Work(p)!=null)){
                var work=robot.Work(part);DrawMixedNumber((int)work.Progress.Numerator,(int)work.Progress.Denominator,new(card.X+12,card.Y+240+row++*18,card.Width-24,18),Cream,.27f,prefix:PartsLabel(part)+" ");
            }
            if(!enabled && card.Contains(_pointer))DrawRulebookHint("製造再開できません","同じ部位・素材・結合の製造候補と、予約のない自国の仕掛品が必要です");
        }
        _ui.Button(WorkpiecePrevious,"前",Muted,.4f);_ui.Button(WorkpieceNext,"次",Muted,.4f);
        _ui.Center($"{_workpiecePage+1}/{pages} / 枠の外をクリックして閉じる",new(400,903,1120,35),.4f,RulebookInk);
        if(candidates.Length==0)_ui.Text("この地点に仕掛品はありません / 廃品回収で集められます",new(220,400),.6f,RulebookInk);
    }
}
