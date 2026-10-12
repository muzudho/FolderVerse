using FolderVerse;
void Check(string[] rows,int expected)
{
    var cells=new List<SurfaceCell>();
    for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[y].Length;x++)
        if(rows[y][x]=='#')cells.Add(new(){Id=y*10+x,Face=0,X=x,Y=y});
    var rectangles=LandSelectionPlan.Rectangles(cells);
    if(rectangles.Length!=expected)throw new Exception($"Expected {expected}, got {rectangles.Length}");
    if(!rectangles.SelectMany(r=>r).Order().SequenceEqual(cells.Select(c=>c.Id).Order()))throw new Exception("Changed selection or overlapping rectangles");
    foreach(var rectangle in rectangles)
    {
        var selected=cells.Where(c=>rectangle.Contains(c.Id)).ToArray();
        if((selected.Max(c=>c.X)-selected.Min(c=>c.X)+1)*(selected.Max(c=>c.Y)-selected.Min(c=>c.Y)+1)!=selected.Length)throw new Exception("Not a rectangle");
    }
}
Check(new[]{"####","####","####"},1);
Check(new[]{"###","#..","#.."},2);
Check(new[]{"#.#",".#.","#.#"},5);
Check(new[]{"###","#.#","###"},4);
Check(new[]{".#.","###",".#."},3);
var faces=new[]{new SurfaceCell{Id=0,Face=0,X=0,Y=0},new SurfaceCell{Id=1,Face=1,X=0,Y=0}};
if(LandSelectionPlan.Rectangles(faces).Length!=2)throw new Exception("Crossed faces");
Console.WriteLine("PASS: minimum rectangles, exact coverage, holes, disconnected cells and separate faces");
