namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly Dictionary<int,HashSet<(int A,int B)>> _directChecksByPlayer=new();
    private static readonly Rectangle DirectPairCheckButton=new(996,110,48,48);
    private static (int A,int B) DirectCheckKey(int a,int b)=>(Math.Min(a,b),Math.Max(a,b));
    private bool DirectPairChecked(int a,int b)
    {
        EnsureNodeCheckContext();
        return _directChecksByPlayer.TryGetValue(NodeCheckPlayer,out var checks) && checks.Contains(DirectCheckKey(a,b));
    }
    private void ToggleDirectPairCheck()
    {
        EnsureNodeCheckContext();int player=NodeCheckPlayer;if(player<0)return;
        if(!_directChecksByPlayer.TryGetValue(player,out var checks))_directChecksByPlayer[player]=checks=new();
        var key=DirectCheckKey(_directNodes[0],_directNodes[1]);if(!checks.Remove(key))checks.Add(key);
    }
    private void DrawDirectPairCheckButton()
    {
        bool marked=DirectPairChecked(_directNodes[0],_directNodes[1]);
        var color=marked?new Color(39,143,75):Muted;
        PrimaryRoundBox(DirectPairCheckButton,color,6);
        DrawNodeCheckGlyph(DirectPairCheckButton,marked?Color.White:new Color(172,192,181));
        if(DirectPairCheckButton.Contains(_pointer))_directMessage="直送便にチェック / ターン終了時に消えます";
    }
    private void DrawDirectTruckCheck(int a,int b,Rectangle truck)
    {
        if(!DirectPairChecked(a,b))return;
        var badge=new Rectangle(truck.Center.X-10,truck.Center.Y-10,20,20);
        PrimaryRoundBox(badge,new Color(39,143,75),4);DrawNodeCheckGlyph(badge,Color.White);
    }
}
