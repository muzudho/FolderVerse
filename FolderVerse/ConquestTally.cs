namespace FolderVerse;
using System;
using System.Linq;

// Pure timeline shared by the cell paint, counters, score and reveal. No simulation writes.
public sealed class ConquestTally
{
    public readonly int[] Cells,Faces;
    public readonly int TotalCells,Entry,Scale;
    public readonly double[] FaceStarts,FaceDurations;
    public double TourSeconds {get;}
    public double CountEnd=>TourSeconds+Math.Max(0,TotalCells-Cells.Length)*.18;
    public double CellsStart=>CountEnd;
    public double EntryStart=>CellsStart+1;
    public double ScoreStart=>EntryStart+1;
    public double ConvertStart=>ScoreStart;
    public double RankStart=>ScoreStart+1;
    public double FaceStart=>RankStart+.7;
    public double Duration=>FaceStart+4;
    public ConquestTally(WorldSetup w,ConquestPresentation.Card card){
        Faces=card.Faces;Entry=card.Ruler+1;Scale=ConquerorRelations.RankingScale(w.ActiveCount);
        TotalCells=w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==card.Ruler).Cells;
        Cells=ConquestPresentation.OwnCells(w,card.Ruler).OrderBy(id=>Array.IndexOf(Faces,w.Cells[id].Face)).ThenBy(id=>id).ToArray();
        FaceStarts=new double[Faces.Length];FaceDurations=new double[Faces.Length];double age=0;
        for(int i=0;i<Faces.Length;i++){FaceStarts[i]=age;FaceDurations[i]=1+Math.Max(1.2,Cells.Count(id=>w.Cells[id].Face==Faces[i])*.18);age+=FaceDurations[i];}
        TourSeconds=Math.Max(1.2,age);
    }
    public int Painted(WorldSetup w,double age){
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
