namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

// Screen-space stage effect, independent of world physics and gameplay randomness.
public sealed class FallingTilePattern
{
    public const int TileSize=30,Columns=64,Rows=36,Count=Columns*Rows;
    public const float ReleaseSeconds=1.65f,DurationSeconds=3.1f;
    public readonly record struct Tile(int X,int Y,float Delay,float Drift,float SpinX,float SpinY,float SpinZ,Color Back);
    public Tile[] Tiles {get;}
    public FallingTilePattern(int seed)
    {
        var random=new Random(seed);Tiles=new Tile[Count];
        Color[] colors={new(235,114,106),new(88,180,181),new(247,195,87),new(148,130,209),new(120,187,139)};
        for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){
            float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
            Tiles[y*Columns+x]=new(x,y,x/(float)(Columns-1)*ReleaseSeconds+R(0,.18f),R(-100,100),R(-5,5),R(-6,6),R(-2,2),colors[random.Next(colors.Length)]);
        }
    }
    public static Vector3 Center(Tile tile,float age)
    {
        float t=Math.Max(0,age-tile.Delay);
        return new(tile.X*TileSize+15+tile.Drift*t,tile.Y*TileSize+15-65*t+1000*t*t,Math.Min(80,t*65));
    }
    public static Matrix Rotation(Tile tile,float age)
    {
        float t=Math.Max(0,age-tile.Delay);
        return Matrix.CreateRotationX(tile.SpinX*t)*Matrix.CreateRotationY(tile.SpinY*t)*Matrix.CreateRotationZ(tile.SpinZ*t);
    }
}