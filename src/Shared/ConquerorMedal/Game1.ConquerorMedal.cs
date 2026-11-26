namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void DrawSmallOperationMedallion(int ruler,Vector2 center)
    {
        var ring=new Rectangle((int)center.X-49,(int)center.Y-49,98,98);
        PaperOval(ring,_setup.OwnerColor(ruler));ring.Inflate(-3,-3);PaperOval(ring,Cream);
        // The inset square's corners fit inside the round medal.
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new((int)center.X-32,(int)center.Y-32,64,64),_setup.Relations.Powered[ruler],faceOnly:true);
    }
    private void DrawRulerMedallion(int ruler,Vector2 center)=>DrawSouvenirCoin(ruler,center,new(false,1,0));
}
