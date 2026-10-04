namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawHierarchyUi()
    {
        _ui.Text("征服者の関係ツリー",new(54,25),.9f,Cream);
        _ui.Text("親が上位の征服者 / 子が直属の手下 / 同じツリーは戦闘せず住民移動が可能",new(54,78),.43f,new(178,210,219));
        var forest=_setup.Relations.Forest(_setup);
        var positions=new Dictionary<int,Rectangle>();
        for(int i=0;i<forest.Length;i++)positions[forest[i].Ruler]=new(90+forest[i].Depth*48,128+i*42,720,36);
        foreach(var entry in forest)
        {
            var box=positions[entry.Ruler];int superior=_setup.Relations.Superiors[entry.Ruler];
            if(superior<0 || !positions.TryGetValue(superior,out var parent))continue;
            int x=box.X-24,y=parent.Bottom;
            var color=_setup.OwnerColor(superior);
            _ui.Box(new(x,y,2,Math.Max(1,box.Center.Y-y)),color);
            _ui.Box(new(x,box.Center.Y,24,2),color);
        }
        foreach(var entry in forest)
        {
            int ruler=entry.Ruler;var box=positions[ruler];bool powered=_setup.Relations.Powered[ruler];
            _ui.Box(box,ruler==_setup.PlayerSlot?Accent:new Color(27,49,62));
            _ui.Box(new(box.X,box.Y,4,box.Height),_setup.OwnerColor(ruler));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(box.X+7,box.Y+2,32,32),powered);
            string status=!powered?"電池なし":_setup.Relations.Released[ruler]?"その場に留まる":ruler==_setup.PlayerSlot?"操作中":_setup.Relations.Allied(ruler,_setup.Relations.Leader)?"操作できる":"敵";
            string label=$"#{ruler+1:00} {_setup.ConquerorNames[ruler]} / "+(entry.Depth==0?"独立":$"手下（{entry.Depth}階層）")+" / "+status;
            _ui.Text(label,new(box.X+48,box.Y+3),Math.Min(.43f,660/_font.MeasureString(label).X),Cream);
        }
        _ui.Button(InactiveBack,"ゲームへ戻る",Muted,.58f);
    }
}
