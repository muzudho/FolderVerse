namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private bool _wandererPolicyOpen;
    private static Rectangle WandererPolicyOption(int index)=>new(440,330+index*105,1040,80);
    private static readonly Rectangle WandererPolicyButton=new(84,902,770,58);
    private static readonly string[] WandererPolicyLabels={"すぐ撤退する","勝ちに行く","説得しながら危なくなったら撤退する","上記のいずれかをランダムに選ぶ"};
    private void DrawWandererPolicy()
    {
        if(InspectingSupervisor)return;
        _ui.Button(WandererPolicyButton,"在野で戦闘に巻き込まれたときの方針",Muted,.38f);
        _ui.Center(WandererPolicyLabels[(int)_setup.WandererPolicyFor(_setup.PlayerSlot)],new(90,936,755,21),.27f,Cream);
    }
    private bool WandererPolicyClick(Point pointer)
    {
        pointer.X-=NodeInspectorOffset;
        if(!_reignView || InspectingSupervisor || !WandererPolicyButton.Contains(pointer))return false;
        _wandererPolicyOpen=true;return true;
    }
    private bool WandererPolicyInput(bool click,bool escape)
    {
        if(!_wandererPolicyOpen)return false;
        if(escape){_wandererPolicyOpen=false;return true;}
        if(click)for(int i=0;i<4;i++)if(WandererPolicyOption(i).Contains(_pointer))
        {_setup.SetWandererPolicy(_setup.PlayerSlot,(WandererPolicy)i);_wandererPolicyOpen=false;_lastAutoSaveTurn=-1;AutoSaveDomestic();break;}
        return true;
    }
    private void DrawWandererPolicyDialog()
    {
        if(!_wandererPolicyOpen)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.7f);_ui.Box(new(390,200,1140,690),new Color(25,42,56));
        _ui.Center("在野で戦闘に巻き込まれたときの方針",new(420,235,1080,60),.6f,Cream);
        for(int i=0;i<4;i++)_ui.Button(WandererPolicyOption(i),WandererPolicyLabels[i],(int)_setup.WandererPolicyFor(_setup.PlayerSlot)==i?Accent:Muted,.55f);
        _ui.Center("説得中は敵の接近を検知して撤退 / ランダムは戦闘ごとに決定",new(420,790,1080,50),.38f,Cream);
        _ui.Center("Esc キーで閉じる",new(420,842,1080,35),.32f,Cream);
    }
}
