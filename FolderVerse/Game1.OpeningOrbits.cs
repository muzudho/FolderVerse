namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private const int VisibleOpeningOrbits=8, ExtraOpeningOrbits=4;
    private readonly Queue<WorldSetup> _openingOrbitCandidates=new();
    private readonly WorldSetup[] _passedOpeningOrbits=new WorldSetup[ExtraOpeningOrbits];
    private Random _openingOrbitRandom;

    private WorldSetup DrawOpeningOrbitCandidate()
    {
        var candidate=new WorldSetup();
        candidate.SetParticipantCounts(_setup.PlayerCount,_setup.ParticipantCount-_setup.PlayerCount);
        _openingPlanets.Draw(candidate,_openingOrbitRandom);
        return candidate;
    }

    private void ResetOpeningOrbitCandidates()
    {
        _openingOrbitCandidates.Clear();
        Array.Clear(_passedOpeningOrbits);
        _openingOrbitRandom=new Random(_random.Next());
        _openingOrbitCandidates.Enqueue(DrawOpeningOrbitCandidate());
        _setup.ApplyLotteryGeography(_openingOrbitCandidates.Peek());
    }

    private void AdvanceOpeningOrbitCandidates()
    {
        _passedOpeningOrbits[_openingOrbitRound%ExtraOpeningOrbits]=_openingOrbitCandidates.Dequeue();
        if(_openingOrbitCandidates.Count==0)_openingOrbitCandidates.Enqueue(DrawOpeningOrbitCandidate());
        _setup.ApplyLotteryGeography(_openingOrbitCandidates.Peek());
    }

    private void PrepareOpeningOrbitCandidate()
    {
        if(_openingOrbitCandidates.Count<VisibleOpeningOrbits+ExtraOpeningOrbits)
            _openingOrbitCandidates.Enqueue(DrawOpeningOrbitCandidate());
    }
    private static readonly (float X,float Y,float Z)[] OpeningOrbitSizes =
    {
        (32,0,28),(36,-2,30),(29,3,34),(43,5,38),
        (48,-5,42),(53,2,47),(58,-3,52),(64,6,57)
    };
    private static readonly Color[] OpeningOrbitColors =
    {
        new(111,180,223),new(196,149,222),new(117,199,156),new(232,156,110),
        new(127,157,232),new(212,193,131),new(224,143,167),new(139,205,211)
    };

    private void DrawOpeningOrbits(float age,bool intro)
    {
        float zoomIn=intro?MathHelper.SmoothStep(0,1,(age-1.4f)/1.35f):
            (_openingOrbitRound>0?MathHelper.SmoothStep(1,0,age/1.25f):0)
            +MathHelper.SmoothStep(0,1,(age-5)/1f);
        float opacity=intro?MathHelper.SmoothStep(0,1,age/.35f)*(1-zoomIn):
            .72f*zoomIn;
        if(opacity<=0)return;

        float approach=intro?zoomIn:1-zoomIn;
        float scale=MathHelper.Lerp(5.1f,7f,approach);
        float elapsed=intro?age:(float)(OrbitOverviewSeconds+_openingOrbitRound*OpeningPlanetInterval+age);
        int focus=_openingOrbitRound%OpeningOrbitSizes.Length;
        var center=new Vector2(960,520);
        Span<(Vector3 Position,Vector3 Size,Color Tint)> bodies=stackalloc (Vector3,Vector3,Color)[17];
        Span<(Vector3 A,Vector3 B,Vector3 C,Vector3 D,Color Tint)> rails=stackalloc (Vector3,Vector3,Vector3,Vector3,Color)[16];
        var candidates=_openingOrbitCandidates.ToArray();
        bodies[0]=(Vector3.Zero,new Vector3(16),new Color(255,182,64));
        for(int i=0;i<OpeningOrbitSizes.Length;i++)
        {
            // Refill the orbit vacated on each pass. Its neighbours continue moving.
            int replacement=(_openingOrbitRound+OpeningOrbitSizes.Length-1-i)/OpeningOrbitSizes.Length;
            var (x,y,z)=OpeningOrbitSizes[i];
            x+=replacement%3*2;
            z-=replacement%3*2;
            float legX=2*x,legZ=2*z;
            float perimeter=2*(legX+legZ);
            float distance=(elapsed*(10+i*2.3f)+i*37+replacement*53)%perimeter;
            var a=new Vector3(-x,y,-z);
            var b=new Vector3(x,y,-z);
            var c=new Vector3(x,y,z);
            var d=new Vector3(-x,y,z);
            var point=distance<legX?Vector3.Lerp(a,b,distance/legX):
                distance<legX+legZ?Vector3.Lerp(b,c,(distance-legX)/legZ):
                distance<2*legX+legZ?Vector3.Lerp(c,d,(distance-legX-legZ)/legX):
                Vector3.Lerp(d,a,(distance-2*legX-legZ)/legZ);

            float replacementFade=!intro && _openingOrbitRound>0 &&
                i==(_openingOrbitRound-1)%OpeningOrbitSizes.Length?
                MathHelper.SmoothStep(0,1,age/.55f):1;
            float ink=opacity*replacementFade;
            var rail=new Color(241,246,255)*(ink*(i==focus?.94f:.62f));
            int offset=(i-focus+VisibleOpeningOrbits)%VisibleOpeningOrbits;
            var candidate=offset<candidates.Length?candidates[offset]:null;
            bodies[i+1]=(point,MiniatureSize(candidate,i),MiniatureColor(candidate,i)*replacementFade);
            if(i==focus)
            {
                Vector3 direction=distance<legX?Vector3.UnitX:distance<legX+legZ?Vector3.UnitZ:
                    distance<2*legX+legZ?-Vector3.UnitX:-Vector3.UnitZ;
                Vector2 lookAhead=Project(point+direction*14)-center;
                center-=Vector2.Clamp(lookAhead,new Vector2(-175,-105),new Vector2(175,105))*approach;
            }
            rails[i]=(a,b,c,d,rail);
        }
        for(int i=0;i<ExtraOpeningOrbits*2;i++)
        {
            bool future=i<ExtraOpeningOrbits;
            int order=i%ExtraOpeningOrbits;
            int candidateIndex=VisibleOpeningOrbits+order;
            int historySlot=(_openingOrbitRound+ExtraOpeningOrbits-1-order)%ExtraOpeningOrbits;
            var candidate=future?(candidateIndex<candidates.Length?candidates[candidateIndex]:null):_passedOpeningOrbits[historySlot];
            float x=62+i*3,z=65+i*2,y=future?-11-order*4:13+order*4;
            float pastOpacity=.28f-order*.07f;
            float ink=opacity*(future?.53f-order*.07f:
                MathHelper.SmoothStep(pastOpacity,pastOpacity-.07f,age/(float)OpeningPlanetInterval));
            var a=new Vector3(-x,y,-z);
            var b=new Vector3(x,y,-z);
            var c=new Vector3(x,y,z);
            var d=new Vector3(-x,y,z);
            float phase=(elapsed*(.035f+i*.0017f)+i*.24f)%1*4;
            var point=phase<1?Vector3.Lerp(a,b,phase):phase<2?Vector3.Lerp(b,c,phase-1):
                phase<3?Vector3.Lerp(c,d,phase-2):Vector3.Lerp(d,a,phase-3);
            rails[VisibleOpeningOrbits+i]=(a,b,c,d,new Color(215,227,247)*ink);
            bodies[VisibleOpeningOrbits+1+i]=(point,MiniatureSize(candidate,i),MiniatureColor(candidate,i)*ink);
        }
        foreach(var rail in rails)
        {
            LandLine(Project(rail.A),Project(rail.B),rail.Tint,2);
            LandLine(Project(rail.B),Project(rail.C),rail.Tint,2);
            LandLine(Project(rail.C),Project(rail.D),rail.Tint,2);
            LandLine(Project(rail.D),Project(rail.A),rail.Tint,2);
        }

        bodies.Sort((a,b)=>Depth(a.Position).CompareTo(Depth(b.Position)));
        foreach(var body in bodies)DrawBody(body.Position,body.Size,body.Tint*opacity);

        Vector2 Project(Vector3 point)=>center+new Vector2(
            point.X+point.Z*.57f, -point.Y+point.Z*.56f-point.X*.26f)*scale;
        static float Depth(Vector3 p)=>p.Z-p.X*.57f+p.Y*.4f;
        static Vector3 MiniatureSize(WorldSetup candidate,int slot)=>candidate==null?
            new Vector3(4+slot%3,4+(slot+1)%3,4+(slot+2)%3):
            new Vector3(candidate.Width,candidate.Height,candidate.Depth)*.8f+new Vector3(3);
        static Color MiniatureColor(WorldSetup candidate,int slot)=>
            OpeningOrbitColors[(candidate?.WorldSeed??slot)%OpeningOrbitColors.Length];

        void DrawBody(Vector3 position,Vector3 size,Color color)
        {
            Vector3 h=size/2;
            var a=Project(position+new Vector3(-h.X,h.Y,-h.Z));
            var b=Project(position+new Vector3(h.X,h.Y,-h.Z));
            var c=Project(position+new Vector3(h.X,h.Y,h.Z));
            var d=Project(position+new Vector3(-h.X,h.Y,h.Z));
            var drop=new Vector2(0,size.Y*scale);
            DrawOpeningOrbitFace(a,d,d+drop,a+drop,color*.78f);
            DrawOpeningOrbitFace(d,c,c+drop,d+drop,color*.59f);
            DrawOpeningOrbitFace(a,b,c,d,color);
        }
    }

    private void DrawOpeningOrbitFace(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
    {
        Span<Vector2> corners=stackalloc Vector2[]{a,b,c,d};
        float minY=MathF.Min(MathF.Min(a.Y,b.Y),MathF.Min(c.Y,d.Y));
        float maxY=MathF.Max(MathF.Max(a.Y,b.Y),MathF.Max(c.Y,d.Y));
        for(int y=(int)MathF.Ceiling(minY);y<maxY;y++)
        {
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            for(int edge=0;edge<corners.Length;edge++)
            {
                var from=corners[edge];
                var to=corners[(edge+1)%corners.Length];
                if(y<MathF.Min(from.Y,to.Y) || y>=MathF.Max(from.Y,to.Y))continue;
                float x=MathHelper.Lerp(from.X,to.X,(y-from.Y)/(to.Y-from.Y));
                left=MathF.Min(left,x);
                right=MathF.Max(right,x);
            }
            if(right>left)
            {
                int x=(int)MathF.Floor(left);
                var line=new Rectangle(x,y,(int)MathF.Ceiling(right)-x,1);
                if(OpeningCanvas.Intersects(line))_ui.Box(Rectangle.Intersect(OpeningCanvas,line),color);
            }
        }
    }
}
