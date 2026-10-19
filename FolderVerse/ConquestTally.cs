namespace FolderVerse;
using System;
using System.Linq;

// Pure timeline shared by the cell paint, counters, score and reveal. No simulation writes.
public sealed class ConquestTally
{
    public readonly int[] Cells,Faces;
    public readonly int TotalCells,Entry,Scale;
    public readonly double[] FaceStarts,FaceDurations;
    public bool Compact {get;}
    public double TourSeconds {get;}
    public double CountEnd=>Compact?0:TourSeconds+Math.Max(0,TotalCells-Cells.Length)*.18;
    public double CellsStart=>CountEnd;
    public double EntryStart=>Compact?0:CellsStart+1;
    public double ScoreStart=>Compact?0:EntryStart+1;
    public double ConvertStart=>ScoreStart;
    public double RankStart=>Compact?0:ScoreStart+1;
    public double FaceStart=>Compact?0:RankStart+.7;
    private readonly double _compactSeconds;
    public static double CompactSeconds(int count)=>count>0?5d/count:0;
    public double Duration=>Compact?_compactSeconds:FaceStart+4;
    public ConquestTally(WorldSetup w,ConquestPresentation.Card card,bool compact=false){
        Compact=compact;
        _compactSeconds=CompactSeconds(w.Relations.ProvisionalRanking(w).Count(r=>!ConquestPresentation.Detailed(w,r.Ruler,r.Rank)));
        Faces=card.Faces;Entry=card.Ruler+1;Scale=ConquerorRelations.RankingScale(w.ActiveCount);
        TotalCells=w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==card.Ruler).Cells;
        Cells=ConquestPresentation.OwnCells(w,card.Ruler).OrderBy(id=>Array.IndexOf(Faces,w.Cells[id].Face)).ThenBy(id=>id).ToArray();
        FaceStarts=new double[Faces.Length];FaceDurations=new double[Faces.Length];double age=0;
        for(int i=0;i<Faces.Length;i++){FaceStarts[i]=age;FaceDurations[i]=1+Math.Max(1.2,Cells.Count(id=>w.Cells[id].Face==Faces[i])*.18);age+=FaceDurations[i];}
        TourSeconds=Compact?0:Math.Max(1.2,age);
    }
    public int Painted(WorldSetup w,double age){
        if(Compact)return Cells.Length;
        int count=0;
        for(int i=0;i<Faces.Length;i++){
            int cells=Cells.Count(id=>w.Cells[id].Face==Faces[i]);
            count+=Math.Clamp((int)Math.Floor((age-FaceStarts[i]-1)/.18+1e-7),0,cells);
        }
        return count;
    }
    public int Counted(WorldSetup w,double age)=>Math.Min(TotalCells,Painted(w,age)+(int)Math.Floor(Math.Max(0,age-TourSeconds)/.18+1e-7));
    public int Score(double age)=>checked(TotalCells*Scale+Entry);
}
