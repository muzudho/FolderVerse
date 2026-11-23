namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private SectorChangeArt _sectorChangeArt;
    private static Rectangle ConquestAxisBotWindow=>new(478,145,76,76);
    private void DrawConquestAxisBot(Rectangle canvas,Matrix transform,Viewport viewport){
        var toy=ConquestAxisBotWindow;
        var model=new Rectangle(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-30);
        DrawAxisBotBody(toy,model,canvas,transform,_conquestGlobe.ViewOrientation(0,0));
        GraphicsDevice.Viewport=viewport;
    }

    private void DrawConquestTerritoryFlags(){
        if(!ConquestStudioCut || _conquestGlobe.PresentationCells==null)return;
        foreach(int id in _conquestGlobe.PresentationCells){
            var cell=_setup.Cells[id];
            if(_conquestGlobe.ProjectVisible(cell.Center,cell.Normal,0,0,ConquestGlobeArea,out var at) && ConquestGlobeArea.Contains(at.ToPoint()))
                CountryFlagArt.Marker(_ui,_setup,ConquestCard.Ruler,at,32);
        }
    }
    private static readonly Color ConquestMarkerInk=new(40,57,76);
    private void DrawConquestTally(){
        var tally=_conquestTally;var panel=ConquestFormulaSpace;
        double age=_conquestStage==3?0:_conquestStage==5?tally.Duration:_conquestAge;
        if(ConquestCard.Reminder)return;
        if(age>=tally.CellsStart){
            if(tally.Powered && !tally.Subordinate){
                string detail=tally.SubordinateCells>0?$"（内、手下 {tally.SubordinateCells} {ToyCounterRenderer.SectorUnit(tally.SubordinateCells)}）":"";
                Line(0,(tally.TotalCells.ToString(),.95f),(ToyCounterRenderer.SectorUnit(tally.TotalCells),.48f),(detail,.32f));
            }else Line(0,(tally.Powered?"手下":"脱落",.55f));
        }
        if(age>=tally.MultiplyStart)
            Line(1,tally.Powered && !tally.Subordinate?($"× {tally.Scale}",.78f):($"{(tally.Powered?0:-tally.Scale)} points",.65f));
        if(age>=tally.EntryStart)Line(2,("＋ entry no",.52f),(tally.Entry.ToString(),.95f));
        if(age>=tally.ScoreStart)Line(3,("＝",.72f),(tally.Score(age).ToString(),.95f),("points",.48f));
        if(age>=tally.RankStart)Line(4,("暫定",.52f),(ConquestCard.Rank.ToString(),.95f),("位",.52f));

        // Keep each number and its label together, including the subordinate breakdown.
        void Line(int row,params (string Text,float Scale)[] tokens){
            const float gap=8;
            float width=tokens.Sum(t=>_font.MeasureString(t.Text).X*t.Scale)+gap*Math.Max(0,tokens.Length-1);
            float fit=Math.Min(1,(panel.Width-44)/Math.Max(1,width));
            float at=panel.X+22,centerY=panel.Y+42+row*70;
            foreach(var token in tokens){
                var size=_font.MeasureString(token.Text);float scale=token.Scale*fit;
                _ui.Text(token.Text,new(at,centerY-size.Y*scale/2),scale,ConquestMarkerInk);
                at+=size.X*scale+gap*fit;
            }
        }
    }
    private void DrawSectorChange(Rectangle digits,Rectangle area,int ruler,int currentSectors)
    {
        int change=_setup.Campaign.SectorChange(_setup,ruler,currentSectors);
        if(change==0)return;
        int size=Math.Min(64,Math.Min(area.Width/2,Math.Max(16,digits.Height*5/4)));
        int overlap=Math.Max(2,size/8);
        size=Math.Min(size,digits.Left-area.Left+overlap);
        if(size<12)return;
        var badge=new Rectangle(digits.Left-size+overlap,
            digits.Center.Y-size/2,size,size);
        (_sectorChangeArt??=new SectorChangeArt(GraphicsDevice)).Draw(_spriteBatch,badge,change);
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
