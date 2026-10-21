namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private static Rectangle ConquestRobotWindow=>new(478,145,76,76);
    private void DrawConquestOrientation(Rectangle canvas,Matrix transform,Viewport viewport){
        var toy=ConquestRobotWindow;int radius=toy.Width/2;
        _spriteBatch.Begin(transformMatrix:transform);
        for(int y=-radius;y<=radius;y++){int half=(int)MathF.Sqrt(radius*radius-y*y);_ui.Box(new(toy.Center.X-half,toy.Center.Y+y,half*2+1,1),new Color(26,51,65,235));}
        _spriteBatch.End();GraphicsDevice.Viewport=OrientationViewport(new(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-30),canvas);
        _orientationToy.Draw(0,0,_conquestGlobe.ViewOrientation(0,0));GraphicsDevice.Viewport=viewport;
    }

    private void DrawConquestNumber(Rectangle r,int value,bool reveal){
        _ui.Box(r,new Color(111,140,157));_ui.Box(new(r.X+2,r.Y+2,r.Width-4,r.Height-4),new Color(40,56,71));
        if(!reveal){for(int i=0;i<4;i++)_ui.Box(new(r.X+10+i*(r.Width-20)/4,r.Center.Y-10,Math.Max(4,(r.Width-28)/4),20),new Color(8,16,24));return;}
        string text=value.ToString();var size=_font.MeasureString(text);float scale=Math.Min(.85f,Math.Min((r.Width-18)/size.X,(r.Height-12)/size.Y));
        _ui.Text(text,new(r.Right-10-size.X*scale,r.Center.Y-size.Y*scale/2),scale,Cream);
    }
    private void DrawConquestTally(){
        var tally=_conquestTally;var panel=ConquestFormulaSpace;int x=panel.X,y=panel.Y,ruler=ConquestCard.Ruler;
        double age=_conquestStage==3?0:_conquestStage==5?tally.Duration:_conquestAge;
        bool main=!ConquestCard.Reminder,cells=main && age>=tally.CellsStart,entry=main && age>=tally.EntryStart;
        bool score=main && age>=tally.ScoreStart,rank=main && age>=tally.RankStart;
        _ui.Box(new(x,y,4,panel.Height),_setup.OwnerColor(ruler));
        var cellsBox=new Rectangle(x+30,y+65,130,62);var entryBox=new Rectangle(x+350,y+65,140,62);
        var pointsBox=new Rectangle(x+45,y+225,190,62);var rankBox=new Rectangle(x+308,y+225,175,62);
        _ui.Center("Entry No.",new(entryBox.X,y+30,entryBox.Width,28),.38f,Cream);
        DrawConquestNumber(entryBox,tally.Entry,entry);
        if(tally.Powered){
            DrawConquestNumber(cellsBox,tally.TotalCells,cells);
            _ui.Center($"× {tally.Scale} +",new(x+170,y+65,170,62),.63f,Cream);
            _ui.Center("cells",new(cellsBox.X,y+135,cellsBox.Width,28),.38f,Cream);
        }else if(cells){
            _ui.Center("電池無し -100 pts",new(x+15,y+65,290,62),.43f,Cream);
            _ui.Center("+",new(x+305,y+65,35,62),.63f,Cream);
        }
        if(cells && tally.SubordinateCells>0)
            _ui.Center($"（内、手下 {tally.SubordinateCells} cells）",new(x+15,y+165,290,25),.30f,Cream);
        _ui.Center("暫定",new(rankBox.X,y+188,rankBox.Width,30),.45f,Cream);
        _ui.Center("=",new(x+5,y+225,35,62),.75f,Cream);
        DrawConquestNumber(pointsBox,tally.Score(age),score);DrawConquestNumber(rankBox,ConquestCard.Rank,rank);
        _ui.Center("points",new(pointsBox.X,y+298,pointsBox.Width,30),.38f,Cream);
        _ui.Text("位",new(rankBox.Right-35,y+298),.4f,Cream);
        if(entry || ConquestCard.Reminder){
            string name=(ConquestCard.Reminder?"ボス：":"")+_setup.ConquerorNames[ruler];var map=ConquestMapSpace;
            _ui.Text(name,new(map.X+10,map.Y+4),Math.Min(.38f,(map.Width-95)/_font.MeasureString(name).X),_setup.OwnerColor(ruler));
            int player=_setup.HumanPlayers.IndexOf(ruler);
            if(player>=0)_ui.Center($"＜プレイヤー{player+1}＞",new(map.X+8,map.Bottom-27,map.Width-16,25),.38f,new Color(255,224,120));
        }
        if(main && age>=tally.FaceStart)DrawConquestSubtree(ruler,age-tally.FaceStart);
    }
    private void DrawConquestSubtree(int root,double age){
        var area=ConquestTreeSpace;var entries=new List<(int Ruler,int Depth)>();var queue=new Queue<(int Ruler,int Depth)>();queue.Enqueue((root,0));
        var ranking=_setup.Relations.ProvisionalRanking(_setup).ToDictionary(r=>r.Ruler,r=>r.Rank);
        while(queue.TryDequeue(out var item)){entries.Add(item);foreach(int child in Enumerable.Range(0,_setup.ActiveCount).Where(r=>_setup.Relations.Superiors[r]==item.Ruler).OrderBy(r=>ranking[r]))queue.Enqueue((child,item.Depth+1));}
        if(entries.Count<2)return;
        int depth=entries.Max(e=>e.Depth),stride=Math.Min(65,130/Math.Max(1,depth)),size=Math.Clamp(stride-6,10,44);
        var positions=new Dictionary<int,Vector2>{{root,new(area.Center.X,area.Y+22)}};
        foreach(var level in entries.Skip(1).GroupBy(e=>e.Depth)){
            var group=level.ToArray();for(int i=0;i<group.Length;i++)positions[group[i].Ruler]=new(area.X+(i+.5f)*area.Width/group.Length,area.Y+22+level.Key*stride);
        }
        for(int i=1;i<entries.Count;i++){
            float alpha=Math.Clamp((float)(age-i*.10)/.35f,0,1);if(alpha<=0)continue;
            int ruler=entries[i].Ruler;var at=positions[ruler];var parent=positions[_setup.Relations.Superiors[ruler]];var delta=at-parent;
            _ui.Tile((at+parent)/2,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),_setup.OwnerColor(ruler)*alpha);
            var box=new Rectangle((int)at.X-size/2,(int)at.Y-size/2,size,size);_ui.Box(new(box.X-2,box.Y-2,size+4,size+4),_setup.OwnerColor(ruler)*alpha);
            if(alpha>.5f)_portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],box,_setup.Relations.Powered[ruler]);
        }
    }
}
