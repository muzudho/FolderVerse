using FolderVerse;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
var world=new WorldSetup();
for(int seed=0;seed<64;seed++)
{
    world.SetWorld(seed);int perimeter=2*(world.Width+world.Depth);
    var coordinates=world.Cells.Select(c=>WorldCoordinates.At(world,c)).ToArray();
    Check(coordinates.Distinct().Count()==world.Cells.Length,$"Duplicate coordinate, seed {seed}");
    Check(coordinates.All(c=>c.X>=-perimeter/2 && c.X<perimeter/2),"Signed range changed");
    foreach(var cell in world.Cells)
    {
        int x=(int)MathF.Floor(cell.Center.X+world.Width/2f),z=(int)MathF.Floor(cell.Center.Z+world.Depth/2f);
        var coordinate=WorldCoordinates.At(world,cell);
        if(cell.Face==4)Check(coordinate.X==x,"Belly does not start at chopsticks side");
        if(cell.Face==1 && z==world.Depth-1)Check(coordinate.X==-1,"Chopsticks boundary should be -1");
        if(cell.Face is 2 or 3)
        {
            int ring=Math.Min(Math.Min(x,world.Width-1-x),Math.Min(z,world.Depth-1-z));
            if(x==ring && z==world.Depth-1-ring)Check(coordinate.X==0,"Polar ring meridian incorrect");
            Check(coordinate.Y==(cell.Face==2?world.Height-world.Height/2+ring:-world.Height/2-1-ring),"Polar latitude changed");
        }
        else Check(coordinate.Y==(int)MathF.Floor(cell.Center.Y+world.Height/2f)-world.Height/2,"Latitude changed");
    }
}
Console.WriteLine("PASS: 64 worlds; chopsticks/belly meridian, belly columns, polar ring origins, signed ranges, unique coordinates and unchanged latitudes.");
