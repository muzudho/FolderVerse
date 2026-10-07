namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

// Twelve native pixel drawings: front, back and two profiles for each toy role.
public static class BattlePieceArt
{
    public sealed record Patch(Rectangle Bounds,int Ink);
    public static readonly IReadOnlyList<Patch>[] Pieces=Build();
    public static readonly IReadOnlyList<Patch>[] Flags={
        new Patch[]{new(new(7,6,3,38),2),new(new(10,7,20,13),0),new(new(27,17,3,3),3),new(new(4,44,10,3),3)},
        new Patch[]{new(new(3,41,27,3),2),new(new(12,31,16,10),0),new(new(24,28,4,4),0),new(new(3,39,4,7),3)}
    };
    public static int Index(BattleRole role,BattleFacing facing)=>(int)role*4+(int)facing;
    private static IReadOnlyList<Patch>[] Build()
    {
        var pieces=new IReadOnlyList<Patch>[12];
        foreach(var role in Enum.GetValues<BattleRole>())
        foreach(var facing in Enum.GetValues<BattleFacing>())
        {
            var p=new List<Patch>();
            void Box(int x,int y,int w,int h,int ink)=>p.Add(new(new(x,y,w,h),ink));
            bool profile=facing is BattleFacing.East or BattleFacing.West;
            // Ink: 0 country cloth, 1 face, 2 dark boots/eyes, 3 gold trim, 4 hair, 5 rosy cheeks.
            Box(9,26,14,12,0);Box(6,27,3,10,0);Box(23,27,3,10,0);
            if(profile){Box(11,37,5,8,0);Box(18,37,4,8,0);Box(12,44,9,3,2);Box(20,43,7,3,2);}
            else{Box(10,37,5,8,0);Box(18,37,5,8,0);Box(8,44,7,3,2);Box(18,44,7,3,2);}
            if(role==BattleRole.Queen){Box(8,16,16,15,4);Box(7,22,4,12,4);Box(22,22,4,12,4);}
            Box(profile?12:10,17,profile?11:12,9,facing==BattleFacing.North?0:1);
            if(profile){Box(23,20,3,3,1);Box(21,19,2,2,2);}
            else if(facing==BattleFacing.South){Box(12,20,2,2,2);Box(18,20,2,2,2);Box(14,24,4,1,2);Box(12,28,8,2,3);}
            else{Box(11,18,10,3,2);Box(13,29,6,2,3);}
            if(role==BattleRole.Captain)
            {Box(9,3,14,14,0);Box(8,15,17,3,2);Box(10,12,12,2,3);Box(13,5,6,3,3);}
            else if(role==BattleRole.Queen)
            {
                if(facing==BattleFacing.North)Box(9,17,14,14,4);
                else
                {
                    Box(10,17,12,2,4);Box(9,18,3,9,4);
                    if(profile){Box(12,18,3,6,4);Box(21,23,2,2,5);}
                    else{Box(11,23,2,2,5);Box(19,23,2,2,5);}
                }
                Box(9,12,14,5,3);Box(9,6,4,7,3);Box(14,8,4,5,3);Box(19,6,4,7,3);
                Box(14,13,4,2,0);Box(10,29,12,4,0);Box(8,33,16,5,0);Box(6,38,20,5,0);Box(10,31,12,2,3);Box(7,41,18,2,3);
            }
            else{Box(9,12,14,5,0);Box(profile?10:8,16,profile?16:16,2,2);Box(14,13,4,2,3);}
            if(facing==BattleFacing.West)
                for(int i=0;i<p.Count;i++){var b=p[i].Bounds;p[i]=p[i] with {Bounds=new(32-b.Right,b.Y,b.Width,b.Height)};}
            pieces[Index(role,facing)]=p.AsReadOnly();
        }
        return pieces;
    }
}
