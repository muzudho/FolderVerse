namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

// Contiguous subtree intervals keep unrelated family branches from crossing.
public static class HierarchyLayout
{
    public static string Ordinal(int n)=>n.ToString()+((n%100 is 11 or 12 or 13)?"th":((n%10) switch{1=>"st",2=>"nd",3=>"rd",_=>"th"}));
    public static Dictionary<int,Rectangle> Arrange(int[] parents,int[] ranks,Rectangle area)
    {
        var children=Enumerable.Range(0,parents.Length).ToDictionary(i=>i,i=>Enumerable.Range(0,parents.Length).Where(j=>parents[j]==i).OrderBy(j=>ranks[j]).ToArray());
        var roots=Enumerable.Range(0,parents.Length).Where(i=>parents[i]<0).OrderBy(i=>ranks[i]).ToArray();
        var spans=new Dictionary<int,int>();int deepest=0;
        int Measure(int id,int depth){deepest=Math.Max(deepest,depth);return spans[id]=Math.Max(1,children[id].Sum(c=>Measure(c,depth+1)));}
        foreach(int root in roots)Measure(root,0);
        int leaves=roots.Sum(r=>spans[r]);
        float unit=area.Width/(float)Math.Max(1,leaves),row=area.Height/(float)(deepest+1);
        int side=Math.Max(8,(int)Math.Min(112,row-16));
        side=Math.Min(side,area.Width);
        var result=new Dictionary<int,Rectangle>();
        void Place(int id,float start,int depth){float center=area.X+(start+spans[id]*.5f)*unit;int x=Math.Clamp((int)(center-side/2f),area.Left,area.Right-side);result[id]=new(x,area.Y+(int)(depth*row),side,side);float next=start;foreach(int child in children[id]){Place(child,next,depth+1);next+=spans[child];}}
        float cursor=0;foreach(int root in roots){Place(root,cursor,0);cursor+=spans[root];}
        return result;
    }
}
public partial class Game1
{
    private void DrawHierarchyUi()
    {
        _ui.Text("征服者の関係図 / 暫定順位",new(54,25),.9f,Cream);
        _ui.Text($"自分と配下の合計 / 点数 = 区画数 × {ConquerorRelations.RankingScale(_setup.ActiveCount)} + Entry No. / 電池無し = -100 + Entry No.",new(54,78),.43f,new(178,210,219));
        var forest=_setup.Relations.RankedForest(_setup);
        var ranks=forest.OrderBy(e=>e.Ruler).Select(e=>e.Rank).ToArray();
        var positions=HierarchyLayout.Arrange(_setup.Relations.Superiors.Take(_setup.ActiveCount).ToArray(),ranks,new(54,130,1806,840));
        foreach(var parent in forest)
        {
            var kids=forest.Where(e=>_setup.Relations.Superiors[e.Ruler]==parent.Ruler).ToArray();if(kids.Length==0)continue;
            var box=positions[parent.Ruler];int middle=(box.Bottom+positions[kids[0].Ruler].Top)/2;
            var color=_setup.OwnerColor(parent.Ruler);
            _ui.Box(new(box.Center.X-1,box.Bottom,2,Math.Max(1,middle-box.Bottom)),color);
            int left=kids.Min(k=>positions[k.Ruler].Center.X),right=kids.Max(k=>positions[k.Ruler].Center.X);
            _ui.Box(new(Math.Min(left,box.Center.X),middle,Math.Max(2,Math.Max(right,box.Center.X)-Math.Min(left,box.Center.X)),2),color);
            foreach(var child in kids){var cb=positions[child.Ruler];_ui.Box(new(cb.Center.X-1,middle,2,Math.Max(1,cb.Top-middle)),color);}
        }
        // Human-controlled cards are painted last, including when siblings overlap.
        foreach(var entry in forest.OrderBy(e=>_setup.HumanPlayers.Contains(e.Ruler)||e.Ruler==_setup.PlayerSlot?1:0))
        {
            int ruler=entry.Ruler;var box=positions[ruler];bool own=_setup.HumanPlayers.Contains(ruler)||ruler==_setup.PlayerSlot;
            DrawConquerorSquareChip(ruler,box,entry.Rank,own);
        }
        _ui.Button(InactiveBack,"ゲームへ戻る",Muted,.58f);
    }
}


