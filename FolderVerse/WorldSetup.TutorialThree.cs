namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class WorldSetup
{
    public int PolarAxis=1;
    public int TutorialMapStage,TutorialMapRotations,TutorialMapNotices;
    public bool TutorialMapEdgeChanged;
    public Vector3 North=>PolarAxis==0?Vector3.Right:Vector3.Up;
    public Vector3 Geographic(Vector3 p)=>PolarAxis==0?new(-p.Y,p.X,p.Z):p;
    public int GeographicFace(int face)=>Array.IndexOf(CubeNet.Normals,Geographic(CubeNet.Normals[face]));
    public bool IsPolarFace(int face)=>Math.Abs(Vector3.Dot(CubeNet.Normals[face],North))>.5f;
    public string FaceName(int face)=>CubeNet.FaceNames[GeographicFace(face)];
    public void PrepareTutorialStepThree(int worldSeed,int castSeed,int placementSeed)
    {
        SetPlayerCount(1);
        while(true){SetWorld(worldSeed);if(Width==3 && Height==1 && Depth==2)break;worldSeed=worldSeed==int.MaxValue?0:worldSeed+1;}
        PolarAxis=0;_presetCastCount=7;SetCast(castSeed);
        for(int i=0;i<7;i++){Portraits[i]=i;Looks[i]=new(i,0);}
        ConquerorFullNames=WorldNames.Conquerors(Looks,castSeed);ConquerorNames=WorldNames.ShortNames(ConquerorFullNames);
        // Use the normal new-game rights, random land claiming, fortune and robot setup.
        SetTerritoryRights(placementSeed);TutorialStep=3;
        var random=new SeedRandom(placementSeed);var remaining=Enumerable.Range(0,Cells.Length).ToArray();random.Shuffle(remaining);int start=0;
        foreach(int ruler in LandOrder()){var claim=remaining.Skip(start).Take(TerritoryRights[ruler]).ToArray();ClaimLand(ruler,claim);start+=claim.Length;}
        CompleteLand((int)(random.Next()&int.MaxValue));
    }
}
