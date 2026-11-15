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
    private static readonly Vector2 OpeningOrbitCenter=new(960,520);
    private static Vector2 OpeningOrbitProjection(Vector3 point)=>
        new(point.X+point.Z*.57f,-point.Y+point.Z*.56f-point.X*.26f);

    private static readonly Vector2 OpeningFocusCenter=new(960,492.5f);
    private readonly record struct OpeningGlobeHandoff(Vector2 Center,float Size,float Opacity,
        float ModelOpacity,Color Tint,Quaternion Pose);

    private static readonly float[] OpeningSpinPeriods={1.35f,.88f,1.84f,1.12f,2.1f,1.57f,1.02f,1.73f};
    private static float OpeningOrbitSpin(float elapsed,int slot)
    {
        const float turnSeconds=.60f;
        float period=OpeningSpinPeriods[slot%OpeningSpinPeriods.Length];
        float time=elapsed+slot*.19f;
        float quarter=MathF.Floor(time/period);
        float sinceStop=time-quarter*period;
        float turn=(sinceStop-(period-turnSeconds))/turnSeconds;
        return (quarter%4+MathHelper.SmoothStep(0,1,turn))*MathHelper.PiOver2;
    }
    private static bool HasLongOpeningOrbit(int candidateIndex)=>candidateIndex>=0 && candidateIndex%20==19;
    private static Matrix OpeningPolarAlignment(int polarAxis)=>polarAxis switch
    {
        0=>Matrix.CreateRotationZ(MathHelper.PiOver2),
        2=>Matrix.CreateRotationX(-MathHelper.PiOver2),
        _=>Matrix.Identity
    };

    private static (float Pan,float Zoom) OpeningOrbitCamera(float age)
    {
        float progress=Math.Clamp(Math.Min(age/(float)OpeningZoomInEnd,
            (float)((OpeningOverviewStart-age)/(OpeningOverviewStart-OpeningZoomOutStart))),0,1);
        const float panShare=.45f;
        return (MathHelper.SmoothStep(0,1,progress/panShare),
            MathHelper.SmoothStep(0,1,(progress-panShare)/(1-panShare)));
    }

    private OpeningGlobeHandoff? DrawOpeningOrbits(float age,bool intro,float modelBlend=1)
    {
        float cameraAge=age+(intro?0:(float)OpeningExitOffset);
        var (pan,approach)=intro?(0f,0f):OpeningOrbitCamera(cameraAge);
        float globeBlend=intro?0:Math.Min(
            MathHelper.SmoothStep(0,1,(float)((age-OpeningZoomInEnd)/(OpeningGlobeReady-OpeningZoomInEnd))),
            MathHelper.SmoothStep(1,0,(float)((cameraAge-OpeningGlobeEnd)/(OpeningZoomOutStart-OpeningGlobeEnd))));
        float opacity=intro?modelBlend:1;
        float longest=Math.Max(_setup.Width,Math.Max(_setup.Height,_setup.Depth));
        float scale=MathHelper.Lerp(5.1f,345/(longest*.65f),approach);
        float elapsed=intro?age:(float)(OrbitOverviewSeconds+_openingOrbitRound*OpeningPlanetInterval+_openingOrbitTimeOffset+age);
        int focus=_openingOrbitRound%OpeningOrbitSizes.Length;
        var center=OpeningOrbitCenter;
        OpeningGlobeHandoff? handoff=null;
        Span<(Vector3 Position,Vector3 Size,Color Tint,float Spin,int PolarAxis)> bodies=
            stackalloc (Vector3,Vector3,Color,float,int)[17];
        Span<(Vector3 A,Vector3 B,Vector3 C,Vector3 D,Color Tint)> rails=stackalloc (Vector3,Vector3,Vector3,Vector3,Color)[16];
        var candidates=_openingOrbitCandidates.ToArray();
        float sunVisibility=1-pan;
        bodies[0]=(Vector3.Zero,new Vector3(16),new Color(255,182,64)*sunVisibility,0,1);
        for(int i=0;i<OpeningOrbitSizes.Length;i++)
        {
            // Refill the orbit vacated on each pass. Its neighbours continue moving.
            int replacement=(_openingOrbitRound+OpeningOrbitSizes.Length-1-i)/OpeningOrbitSizes.Length;
            var (x,y,z)=OpeningOrbitSizes[i];
            int offset=(i-focus+VisibleOpeningOrbits)%VisibleOpeningOrbits;
            x+=replacement%3*2;
            z-=replacement%3*2;
            if(HasLongOpeningOrbit(_openingOrbitRound+offset)){x=108;z=21;}
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
            var candidate=offset<candidates.Length?candidates[offset]:null;
            var tint=MiniatureColor(candidate,i);
            var rail=Color.Lerp(tint,Color.White,.26f)*(ink*(i==focus?.94f:.62f)*(1-.7f*globeBlend));
            float spin=OpeningOrbitSpin(elapsed,i);
            if(i==focus && !intro)
            {
                float focusedSpin=OpeningOrbitSpin(elapsed-age+(float)OpeningZoomInEnd,i);
                spin+=MathHelper.WrapAngle(focusedSpin-spin)*approach;
            }
            float useCloseModel=i==focus?MathHelper.SmoothStep(0,1,(approach-.72f)/.28f):0;
            int polarAxis=candidate?.PolarAxis??1;
            bodies[i+1]=(point,MiniatureSize(candidate,i),tint*(replacementFade*(1-useCloseModel)),spin,polarAxis);
            if(i==focus)
            {
                center+=(OpeningFocusCenter-Project(point))*pan;
                if(useCloseModel>0)
                {
                    var pose=Quaternion.CreateFromRotationMatrix(OpeningPolarAlignment(polarAxis)
                        *Matrix.CreateRotationY(spin)*Matrix.CreateFromYawPitchRoll(.48f,.42f,0));
                    double tourAge=_openingRejectedAt??age;
                    var face=WorldPreview.OpeningFaceTour((tourAge-OpeningGlobeReady)/(OpeningGlobeEnd-OpeningGlobeReady));
                    pose=Quaternion.Slerp(pose,face,globeBlend);
                    handoff=new OpeningGlobeHandoff(Project(point),MathHelper.Lerp(.72f,1,useCloseModel),globeBlend,
                        useCloseModel*(1-globeBlend),tint,pose);
                }
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
            if(HasLongOpeningOrbit(_openingOrbitRound+(future?candidateIndex:-1-order))){x=108;z=21;}
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
            Color tint=MiniatureColor(candidate,i);
            rails[VisibleOpeningOrbits+i]=(a,b,c,d,Color.Lerp(tint,Color.White,.26f)*(ink*(1-.7f*globeBlend)));
            bodies[VisibleOpeningOrbits+1+i]=(point,MiniatureSize(candidate,i),tint*ink,
                OpeningOrbitSpin(elapsed,VisibleOpeningOrbits+i),candidate?.PolarAxis??1);
        }
        if(sunVisibility>0)DrawOpeningSunGlow(Project(Vector3.Zero),scale,opacity*sunVisibility);
        foreach(var rail in rails)
        {
            DrawOpeningOrbitRail(Project(rail.A),Project(rail.B),rail.Tint);
            DrawOpeningOrbitRail(Project(rail.B),Project(rail.C),rail.Tint);
            DrawOpeningOrbitRail(Project(rail.C),Project(rail.D),rail.Tint);
            DrawOpeningOrbitRail(Project(rail.D),Project(rail.A),rail.Tint);
        }

        bodies.Sort((a,b)=>Depth(a.Position).CompareTo(Depth(b.Position)));
        foreach(var body in bodies)DrawBody(body.Position,body.Size,body.Tint*opacity,body.Spin,body.PolarAxis);
        return handoff;

        Vector2 Project(Vector3 point)=>center+OpeningOrbitProjection(point)*scale;
        static float Depth(Vector3 p)=>p.Z-p.X*.57f+p.Y*.4f;
        static Vector3 MiniatureSize(WorldSetup candidate,int slot)=>candidate==null?
            new Vector3(4+slot%3,4+(slot+1)%3,4+(slot+2)%3)*.5f:
            new Vector3(candidate.Width,candidate.Height,candidate.Depth)*.65f;
        static Color MiniatureColor(WorldSetup candidate,int slot)=>candidate==null?
            OpeningOrbitColors[slot%OpeningOrbitColors.Length]:WorldTerrain.PlanetModelTint(candidate.WorldSeed);

        void DrawBody(Vector3 position,Vector3 size,Color color,float spin,int polarAxis)
        {
            if(color.A==0)return;
            Vector3 h=size/2;
            var rotation=OpeningPolarAlignment(polarAxis)*Matrix.CreateRotationY(spin);
            foreach(var normal in CubeNet.Normals)
            {
                var facing=Vector3.TransformNormal(normal,rotation);
                if(Depth(facing)<=.001f)continue;
                var axis=Vector3.Cross(Math.Abs(normal.Y)>.5f?Vector3.UnitZ:Vector3.UnitY,normal);
                var u=axis*h;
                var v=Vector3.Cross(normal,axis)*h;
                var mid=normal*h;
                var shade=facing.Y>.5f?1:.57f+.22f*Math.Abs(facing.X);
                DrawOpeningOrbitFace(Corner(mid-u-v),Corner(mid+u-v),Corner(mid+u+v),Corner(mid-u+v),color*shade);
            }
            Vector2 Corner(Vector3 point)=>Project(position+Vector3.Transform(point,rotation));
        }
    }

    private void DrawOpeningSunGlow(Vector2 center,float scale,float opacity)
    {
        float radius=17*scale;
        var glowBounds=new Rectangle((int)(center.X-radius),(int)(center.Y-radius),(int)(radius*2),(int)(radius*2));
        if(!OpeningCanvas.Intersects(glowBounds))return;
        for(int layer=8;layer>0;layer--)
        {
            float r=radius*(.43f+layer*.071f);
            int top=Math.Max(0,(int)(center.Y-r));
            int bottom=Math.Min(OpeningCanvas.Bottom,(int)(center.Y+r));
            for(int y=top;y<bottom;y+=3)
            {
                float dy=(y-center.Y)/r;
                float half=r*MathF.Sqrt(Math.Max(0,1-dy*dy));
                var row=new Rectangle((int)(center.X-half),y,(int)(half*2),3);
                if(OpeningCanvas.Intersects(row))
                    _ui.Box(Rectangle.Intersect(OpeningCanvas,row),new Color(255,171,56)*(.045f*opacity));
            }
        }
    }

    private void DrawOpeningOrbitRail(Vector2 from,Vector2 to,Color tint)
    {
        var delta=to-from;
        float first=0,last=1;
        if(Clip(from.X,delta.X,OpeningCanvas.Width-2)
            && Clip(from.Y,delta.Y,OpeningCanvas.Height-2))
            LandLine(from+delta*first,from+delta*last,tint,2);

        bool Clip(float start,float travel,float limit)
        {
            if(travel==0)return start>=2 && start<=limit;
            float a=(2-start)/travel,b=(limit-start)/travel;
            first=Math.Max(first,Math.Min(a,b));
            last=Math.Min(last,Math.Max(a,b));
            return first<last;
        }
    }

    private void DrawOpeningOrbitFace(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
    {
        Span<Vector2> corners=stackalloc Vector2[]{a,b,c,d};
        float minY=MathF.Min(MathF.Min(a.Y,b.Y),MathF.Min(c.Y,d.Y));
        float maxY=MathF.Max(MathF.Max(a.Y,b.Y),MathF.Max(c.Y,d.Y));
        for(int y=Math.Max(0,(int)MathF.Ceiling(minY));y<Math.Min(maxY,OpeningCanvas.Bottom);y++)
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
