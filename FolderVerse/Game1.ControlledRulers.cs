namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private static Rectangle RulerSwitch(int direction)=>new(54+direction*420,96,402,44);
    private bool SwitchControlledRuler(Point pointer)
    {
        if(_populationCell>=0 || _movementOpen || _transportOverview)return false;
        int direction=RulerSwitch(0).Contains(pointer)?-1:RulerSwitch(1).Contains(pointer)?1:0;
        if(direction==0)return false;
        var party=_setup.Relations.Party(_setup).Where(r=>!_setup.Campaign.Submitted.Contains(r)).ToArray();
        if(party.Length<2)return true;
        int index=System.Array.IndexOf(party,_setup.PlayerSlot);_setup.SelectPlayer(party[(index+direction+party.Length)%party.Length]);
        _selectedMoveNode=-1;_populationCell=_populationNode=-1;FocusCurrentNode();_world.ShowSetup(_setup,true);return true;
    }
    private void DrawControlledRuler()
    {
        if(_setup.Relations.Party(_setup).Length>1){_ui.Button(RulerSwitch(0),"← 操作する征服者",Muted,.48f);_ui.Button(RulerSwitch(1),"操作する征服者 →",Muted,.48f);}
        int superior=_setup.Relations.Superiors[_setup.PlayerSlot];if(superior<0)return;
        var photo=new Rectangle(64,155,144,100);_ui.Box(new(60,151,152,108),_setup.OwnerColor(superior));
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[superior],photo,true,forCreation:true);
        string label=_setup.ConquerorNames[superior]+"の手下です";
        _ui.Center(label,new(220,145,660,40),.58f,Cream);
    }
}
