namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawHierarchyUi()
    {
        _ui.Text("征服者の関係図 / 暫定順位",new(54,25),.9f,Cream);
        _ui.Text("征服セル数 → 征服節点数 → 作成番号 / 自分と配下の合計 / 各段を順位順に表示",new(54,78),.43f,new(178,210,219));
        var forest=_setup.Relations.RankedForest(_setup);
        var positions=new Dictionary<int,Rectangle>();
        int levels=forest.Max(e=>e.Depth)+1;
        int stride=Math.Min(172,850/levels),height=Math.Max(30,stride-24);
        foreach(var level in forest.GroupBy(e=>e.Depth))
        {
            var entries=level.ToArray();int width=1806/entries.Length;
            for(int i=0;i<entries.Length;i++)positions[entries[i].Ruler]=new(54+i*width,130+level.Key*stride,width-8,height);
        }
        foreach(var entry in forest)
        {
            var box=positions[entry.Ruler];int superior=_setup.Relations.Superiors[entry.Ruler];
            if(superior<0 || !positions.TryGetValue(superior,out var parent))continue;
            int middle=(parent.Bottom+box.Top)/2;
            var color=_setup.OwnerColor(superior);
            _ui.Box(new(parent.Center.X,parent.Bottom,2,Math.Max(1,middle-parent.Bottom)),color);
            _ui.Box(new(Math.Min(parent.Center.X,box.Center.X),middle,Math.Max(2,Math.Abs(parent.Center.X-box.Center.X)),2),color);
            _ui.Box(new(box.Center.X,middle,2,Math.Max(1,box.Top-middle)),color);
        }
        foreach(var entry in forest)
        {
            int ruler=entry.Ruler;var box=positions[ruler];bool own=ruler==_setup.PlayerSlot;
            _ui.Box(box,own?new Color(255,226,124):new Color(58,78,100));
            var inset=new Rectangle(box.X+3,box.Y+3,box.Width-6,box.Height-6);_ui.Box(inset,own?Accent:new Color(27,49,62));
            _ui.Box(new(box.X+3,box.Y+3,4,box.Height-6),_setup.OwnerColor(ruler));
            float textScale=Math.Min(.5f,(box.Width-12)/_font.MeasureString($"{entry.Rank}位 #00").X);
            if(height<65)
            {
                _ui.Center($"{entry.Rank}位 #{ruler+1:00}"+(own?" 自分":""),inset,Math.Min(textScale,.34f),Cream);continue;
            }
            _ui.Center($"{entry.Rank}位 #{ruler+1:00}"+(own?" 自分":""),new(box.X+5,box.Y+5,box.Width-10,27),Math.Min(textScale,(box.Width-12)/_font.MeasureString($"{entry.Rank}位 #{ruler+1:00}"+(own?" 自分":"")).X),Cream);
            int size=Math.Min(62,height-62);_portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(box.Center.X-size/2,box.Y+32,size,size),_setup.Relations.Powered[ruler]);
            var name=_setup.ConquerorNames[ruler];
            _ui.Center(name,new(box.X+6,box.Bottom-33,box.Width-12,16),Math.Min(.32f,(box.Width-12)/_font.MeasureString(name).X),Cream);
            _ui.Center($"{entry.Cells} セル / {entry.Nodes} 節点"+(!_setup.Relations.Powered[ruler]?" / 電池なし":""),new(box.X+6,box.Bottom-17,box.Width-12,16),Math.Min(.3f,(box.Width-12)/_font.MeasureString($"{entry.Cells} セル / {entry.Nodes} 節点 / 電池なし").X),Cream);
        }
        _ui.Button(InactiveBack,"ゲームへ戻る",Muted,.58f);
    }
}
