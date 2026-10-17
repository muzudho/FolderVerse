namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1
{
    // Each source drawing is exactly 8 x 8 pixels: shield, dagger, cartridge.
    private static readonly string[][] BattleSupplyPixels={
        new[]{".######.","##....##","##.##.##","##.##.##","##.##.##",".##..##.","..####..","...##..."},
        new[]{".....##.","....###.","...###..","..###...",".###....","#####...","..##....",".##....."},
        new[]{"...##...","..####..","..####..","..####..","..####..","..####..","..####..","..####.."}
    };
    private void DrawBattleSupplies(BattleUnitView unit,int x,int y,float opacity)
    {
        if(unit.Fallen || unit.Retreated)return;
        int[] counts={unit.Shields,unit.Daggers,unit.Rifles};
        Color[] colors={new(125,221,255),new(230,235,240),new(255,210,85)};
        int visible=0;foreach(int count in counts)if(count>0)visible++;
        if(visible==0)return;
        _ui.Box(new(x-3,y-3,visible*25+3,16),Color.Black*(.45f*opacity));
        int slot=0;
        for(int i=0;i<3;i++){
            if(counts[i]<=0)continue;
            int left=x+slot++*25;
            for(int row=0;row<8;row++)for(int col=0;col<8;col++)
                if(BattleSupplyPixels[i][row][col]=='#')_ui.Box(new(left+col,y+row,1,1),colors[i]*opacity);
            _ui.Text(counts[i].ToString(),new(left+10,y-2),.27f,Cream*opacity);
        }
    }
}
