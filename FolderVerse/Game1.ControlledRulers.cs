namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void DrawControlledRuler(int ruler=-1)
    {
        if(ruler<0)ruler=_setup.PlayerSlot;
        int superior=_setup.Relations.Superiors[ruler];if(superior<0)return;
        var photo=new Rectangle(64,205,144,100);_ui.Box(new(60,201,152,108),_setup.OwnerColor(superior));
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[superior],photo,true,forCreation:true);
        string label=_setup.ConquerorNames[superior]+"の手下です";
        _ui.Center(label,new(220,205,660,40),.58f,Cream);
    }
}
