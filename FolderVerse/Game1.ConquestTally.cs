namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private static Rectangle ConquestRobotWindow=>new(1625,120,180,180);
    private void DrawConquestOrientation(Rectangle canvas,Matrix transform,Viewport viewport){
        var toy=ConquestRobotWindow;int radius=toy.Width/2;
        _spriteBatch.Begin(transformMatrix:transform);
        for(int y=-radius;y<=radius;y++){int half=(int)MathF.Sqrt(radius*radius-y*y);_ui.Box(new(toy.Center.X-half,toy.Center.Y+y,half*2+1,1),new Color(26,51,65,235));}
        _spriteBatch.End();GraphicsDevice.Viewport=OrientationViewport(new(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-30),canvas);
        _orientationToy.Draw(0,0,_conquestGlobe.ViewOrientation(0,0));GraphicsDevice.Viewport=viewport;
    }
    private static string CellCounter(int count)=>$"{count} "+(count==1?"cell":"cells");
    private void DrawConquestTally(){
        int ruler=ConquestCard.Ruler;var color=_setup.OwnerColor(ruler);var tally=_conquestTally;
        bool reminder=ConquestCard.Reminder,identity=reminder || _conquestAge>=tally.CountEnd;
        bool conversion=!reminder && _conquestAge>=tally.ConvertStart,rank=!reminder && _conquestAge>=tally.RankStart;
        _ui.Box(new(55,170,405,535),new Color(25,34,46));_ui.Box(new(55,170,10,535),color);
        if(identity){
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(125,195,260,260),_setup.Relations.Powered[ruler]);
            _ui.Center(_setup.ConquerorNames[ruler],new(70,462,375,45),.55f,Cream);
        }
        int cells=conversion?tally.RemainingCells(_conquestAge):reminder?tally.TotalCells:tally.Counted(_setup,_conquestAge);
        int entry=conversion?tally.RemainingEntry(_conquestAge):tally.Entry;
        if(rank){cells=tally.TotalCells;entry=tally.Entry;}
        string counter=CellCounter(cells)+(identity?$" . Entry {ConquerorRelations.EntryNumber(entry,_setup.ActiveCount)}":"");
        _ui.Center(counter,new(70,515,375,45),.53f,Cream);
        if(!reminder && _conquestAge>=tally.TourSeconds && _conquestAge<tally.CountEnd){
            _ui.Center($"手下 +{Math.Max(0,cells-tally.Cells.Length)} cells",new(70,570,375,35),.42f,new Color(171,214,222));
        }
        if(conversion){
            _ui.Center($"{tally.Score(_conquestAge)} points",new(70,565,375,50),.8f,new Color(255,224,120));
            if(_conquestAge>=tally.EntryConvertEnd && !rank)_ui.Center($"{CellCounter(tally.TotalCells)} . Entry {ConquerorRelations.EntryNumber(tally.Entry,_setup.ActiveCount)}",new(70,615,375,28),.32f,new Color(171,214,222));
        }
        if(rank)_ui.Center($"暫定{ConquestCard.Rank}位",new(70,650,375,48),.68f,Cream);
        else if(reminder)_ui.Center("ボス",new(70,575,375,45),.55f,Cream);
        int player=_setup.HumanPlayers.IndexOf(ruler);
        if(identity && player>=0)_ui.Center($"＜プレイヤー{player+1}＞",new(70,125,375,40),.5f,new Color(255,224,120));
        _ui.Tile(new(460,550),new(35,35),MathHelper.PiOver4,new Color(25,34,46));
        if(rank)DrawConquestSubtree(ruler,_conquestAge-tally.RankStart);
    }
    private void DrawConquestSubtree(int root,double age){
        var entries=new List<(int Ruler,int Depth)>();var queue=new Queue<(int Ruler,int Depth)>();queue.Enqueue((root,0));
        var ranking=_setup.Relations.ProvisionalRanking(_setup).ToDictionary(r=>r.Ruler,r=>r.Rank);
        while(queue.Count>0){var item=queue.Dequeue();entries.Add(item);foreach(int child in Enumerable.Range(0,_setup.ActiveCount).Where(r=>_setup.Relations.Superiors[r]==item.Ruler).OrderBy(r=>ranking[r]))queue.Enqueue((child,item.Depth+1));}
        if(entries.Count<2)return;
        int depth=entries.Max(e=>e.Depth),stride=Math.Min(95,255/Math.Max(1,depth)),size=Math.Clamp(stride-8,32,68);
        var positions=new Dictionary<int,Vector2>{{root,new(257,700)}};
        foreach(var level in entries.Skip(1).GroupBy(e=>e.Depth)){
            var group=level.ToArray();for(int i=0;i<group.Length;i++)positions[group[i].Ruler]=new(70+(i+.5f)*375/group.Length,710+level.Key*stride);
        }
        for(int i=1;i<entries.Count;i++){
            float alpha=Math.Clamp((float)(age-i*.10)/.35f,0,1);if(alpha<=0)continue;
            int ruler=entries[i].Ruler;var at=positions[ruler];var parent=positions[_setup.Relations.Superiors[ruler]];var delta=at-parent;
            _ui.Tile((at+parent)/2,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),_setup.OwnerColor(ruler)*alpha);
            var box=new Rectangle((int)at.X-size/2,(int)at.Y-size/2,size,size);_ui.Box(new(box.X-3,box.Y-3,size+6,size+6),_setup.OwnerColor(ruler)*alpha);
            // The portrait renderer has no opacity parameter: reveal each face after its connector arrives.
            if(alpha>.5f)_portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],box,_setup.Relations.Powered[ruler]);
        }
    }
}
