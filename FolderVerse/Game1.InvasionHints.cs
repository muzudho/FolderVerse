namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private string[] InvasionUnavailableReasons(InvasionSource source,Robot robot)
    {
        if(InvasionRobots(source).Any(r=>r.Id==robot.Id))return Array.Empty<string>();
        var reasons=new List<string>();
        if(robot.Owner!=source.Owner)reasons.Add("この出撃元の国が所有するロボットではありません。");
        if(robot.Parts!=RobotParts.Complete)reasons.Add("頭・胴・脚がそろっていません。");
        int native=robot.NativeParts(robot.Owner);
        if(native==0)reasons.Add("損傷していない自国のパーツが１／３以上含まれていません。");
        else if(_setup.Robots.Training[robot.Owner].DeploymentPolicy==0 && native<3)reasons.Add("出撃方針が純国産のみです。損傷していない自国のパーツが３／３必要です。");
        bool locked=source.Queen?_setup.Campaign.RobotBattles.LockedNode(source.Node):_setup.Robots.Reservations?.RobotBattles.LockedNode(source.Node)==true;
        if(locked)reasons.Add("出撃元の地点が戦闘中です。");
        if(_setup.Robots.DirectDelivery.LockedParts(source.Node,robot.Id)!=0)reasons.Add("直送便でパーツを配送する予約があります。");
        if(source.Queen){
            if(robot.Disposal)reasons.Add("廃棄の予約があります。");
            if(robot.Stationed)reasons.Add("守備・迎撃・工場の仕事に配置されています。");
        }else{
            if(_setup.Robots.Workshops[source.Node].DisposalTarget==robot.Id)reasons.Add("廃棄処理が進行中です。");
            if(_setup.Robots.PendingEdits.Any(e=>e.Ids.Contains(robot.Id)))reasons.Add("組立・分解などの変更が予約されています。");
        }
        if(reasons.Count==0)reasons.Add("現在の出撃条件を満たしていません。");
        return reasons.ToArray();
    }
    private bool DrawInvasionUnavailableHint()
    {
        if(!WarCheckMode || _nodeInspectorClosing || _invasionPicker>=0 || _trainingOpen || _supervisorDialog>0 || _inspectorFoldersOpen || _folderTreeOpen || _factionPickerOpen || _hangarDrag.HasValue)return false;
        var point=new Point(_pointer.X-NodeInspectorOffset,_pointer.Y);
        for(int row=0;row<7;row++){
            int index=_invasionPage*7+row;if(index>=_invasionSources.Count)break;
            var source=_invasionSources[index];var robots=InvasionPreviewRobots(source);
            for(int slot=0;slot<robots.Length;slot++){
                if(!InvasionPreviewCard(row,slot).Contains(point))continue;
                var reasons=InvasionUnavailableReasons(source,robots[slot]);if(reasons.Length==0)return false;
                var r=RulebookHintArea;r.Height=Math.Max(r.Height,84+reasons.Length*29);DrawRulebookPaper(r);
                _ui.Text($"ロボット ID {robots[slot].Id}：出撃できない理由",new(r.X+24,r.Y+20),.46f,RulebookInk);
                _ui.Box(new(r.X+24,r.Y+51,r.Width-48,1),RulebookInk*.5f);
                for(int line=0;line<reasons.Length;line++)_ui.Text(reasons[line],new(r.X+24,r.Y+65+line*29),Math.Min(.38f,(r.Width-48)/Math.Max(1,_font.MeasureString(reasons[line]).X)),RulebookInk);
                return true;
            }
        }
        return false;
    }
}
