namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _invasionPicker=-1;
    private readonly HashSet<long> _invasionDraft=new();
    private bool _invasionQueenDraft;
    private Robot[] InvasionRobots(InvasionSource row)=>_setup.Campaign.RobotBattles.SelectableRobots(_setup,row.Owner,row.Node,row.Queen);
    private static Rectangle InvasionRobotCard(int i)=>new(350+i%6*200,320+i/6*170,185,155);
    private static readonly Rectangle InvasionPickerDone=new(1290,795,250,55),InvasionPickerCancel=new(1010,795,250,55),InvasionQueenToggle=new(380,705,520,55);
    private void OpenInvasionPicker(int index)
    {
        _invasionPicker=index;_invasionDraft.Clear();_invasionDraft.UnionWith(_invasionSources[index].Robots);
        _invasionQueenDraft=_invasionSources[index].Selected;
    }
    private void InvasionPickerInput(bool click,bool escape)
    {
        if(escape || click && InvasionPickerCancel.Contains(_pointer)){_invasionPicker=-1;return;}
        if(!click)return;
        var row=_invasionSources[_invasionPicker];var robots=InvasionRobots(row);
        if(InvasionPickerDone.Contains(_pointer))
        {
            row.Robots.Clear();row.Robots.UnionWith(_invasionDraft.Intersect(robots.Select(r=>r.Id)));
            row.Count=row.Robots.Count;row.Selected=row.Queen && _invasionQueenDraft;_invasionPicker=-1;return;
        }
        if(row.Queen && InvasionQueenToggle.Contains(_pointer)){_invasionQueenDraft=!_invasionQueenDraft;return;}
        for(int i=0;i<robots.Length;i++)if(InvasionRobotCard(i).Contains(_pointer))
        {if(!_invasionDraft.Remove(robots[i].Id))_invasionDraft.Add(robots[i].Id);return;}
    }
    private void DrawInvasionPicker()
    {
        if(_invasionPicker<0)return;
        var row=_invasionSources[_invasionPicker];var robots=InvasionRobots(row);
        _ui.Box(new(0,0,1920,1080),new Color(0,0,0,175));_ui.Box(new(310,185,1300,720),new Color(27,49,62));
        _ui.Text(row.Queen?"ポケットから出撃するロボットを選択":"格納庫から出撃するロボットを選択",new(350,215),.7f,Cream);
        _ui.Text("クリックで選択・解除 / 選ばなかったロボットは残ります",new(350,270),.47f,Cream);
        for(int i=0;i<robots.Length;i++)
        {
            var r=InvasionRobotCard(i);var robot=robots[i];bool selected=_invasionDraft.Contains(robot.Id);
            _ui.Box(r,selected?Accent:Muted);DrawRobotGlyph(new(r.X+65,r.Y+8,55,75),robot,whiteOutline:true);
            _ui.Center($"ID {robot.Id} / {PartsLabel(robot.Parts)}",new(r.X,r.Y+90,r.Width,28),.38f,Cream);
            _ui.Center(selected?"選択中":"待機",new(r.X,r.Y+122,r.Width,26),.4f,Cream);
        }
        if(robots.Length==0)_ui.Text("出撃できるロボットはいません",new(380,375),.6f,Cream);
        if(row.Queen)_ui.Button(InvasionQueenToggle,_invasionQueenDraft?"クイーン：参加（移動も予約）":"クイーン：参加しない",_invasionQueenDraft?Accent:Muted,.5f);
        _ui.Text($"ロボット {_invasionDraft.Count}体"+(row.Queen && _invasionQueenDraft?" ＋ クイーン":""),new(380,800),.55f,Cream);
        _ui.Button(InvasionPickerCancel,"取消",Muted,.55f);_ui.Button(InvasionPickerDone,"選択を確定",Accent,.55f);
        _ui.Text("０体かつクイーン不参加なら、この出撃元は参加しません",new(350,865),.42f,Cream);
    }
}
