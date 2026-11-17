namespace FolderVerse;

using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private void DrawPrimaplanaWaterfall(UiPainter ui,WorldSetup world,Rectangle panel,float scale)
    {
        foreach(var cell in world.Cells){
            var bounds=CellBounds(cell,panel);
            var rim=new Vector2(bounds.Center.X,bounds.Top);
            ui.Tile(rim+new Vector2(0,scale*.025f),new(bounds.Width+1,scale*.05f),0,new Color(64,145,178));
            for(int band=0;band<20;band++){
                float distance=(band+.5f)/20;
                var center=rim-new Vector2(0,distance*scale);
                var color=new Color(77,168,213)*((1-distance)*(1-distance)*.78f);
                ui.Tile(center,new(bounds.Width+1,scale/20+1),0,color);
            }
        }
    }
}
