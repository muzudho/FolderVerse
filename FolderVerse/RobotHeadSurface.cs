namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

// Maps for the neutral PP sprites. Face regions use measured geometry.
public static class RobotHeadSurface
{
    // Bounds measured on the neutral PP torso and legs. Transparent pixels
    // stay outside the paint mask, including the gaps between the feet.
    public static RobotFlagComposite.Surface[] PolypropyleneBody(
        Color[] neutral, int width, int height, RectangleF bounds, RectangleF? protectedFace = null)
    {
        if (width < 1 || height < 1 || neutral == null || (long)width * height != neutral.Length)
            throw new ArgumentException("Body image dimensions do not match its pixels.");
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new ArgumentException("The paint bounds must have positive dimensions.", nameof(bounds));

        var result = new RobotFlagComposite.Surface[neutral.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            Color pixel = neutral[index];
            if (pixel.A == 0)
                continue;

            float imageX = (x + .5f) / width;
            float imageY = (y + .5f) / height;
            float u = (imageX - bounds.X) / bounds.Width;
            float v = (imageY - bounds.Y) / bounds.Height;
            float coverage = 1;
            if (protectedFace is { } face)
            {
                const float radius = .05f;
                float dx = Math.Max(Math.Abs(imageX - face.X - face.Width / 2) - (face.Width / 2 - radius), 0);
                float dy = Math.Max(Math.Abs(imageY - face.Y - face.Height / 2) - (face.Height / 2 - radius), 0);
                coverage = Math.Clamp((MathF.Sqrt(dx * dx + dy * dy) - radius) / .008f, 0, 1);
            }
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            result[index] = new(
                new(Math.Clamp(u, 0, 1), Math.Clamp(v, 0, 1)),
                Byte(coverage),
                Byte(Math.Clamp(light / .612f, 0, 1)),
                Byte(Math.Clamp((light - .738f) / .262f, 0, 1) * .95f));
        }
        return result;
    }

    public readonly record struct RectangleF(float X, float Y, float Width, float Height);

    public static RobotFlagComposite.Surface[] Maple(Color[] neutral,int width,int height,RobotParts part=RobotParts.Head)
    {
        if(width<1 || height<1 || neutral==null || (long)width*height!=neutral.Length)
            throw new ArgumentException("Head image dimensions do not match its pixels.");
        var result=new RobotFlagComposite.Surface[neutral.Length];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;
            var pixel=neutral[i];
            if(pixel.A==0)continue;
            float u=(x+.5f)/width,v=(y+.5f)/height;
            float coverage;
            if(part==RobotParts.Body)
            {
                // Bare end-grain medallion on the chest.
                float dx=(u-.476f)/.133f,dy=(v-.517f)/.204f;
                coverage=Math.Clamp((MathF.Sqrt(dx*dx+dy*dy)-1)/.055f,0,1);
            }
            else if(part==RobotParts.Legs)
                coverage=Math.Clamp((.63f-v)/.025f,0,1);
            else
            {
                float dx=Math.Max(Math.Abs(u-.4235f)-.1025f,0);
                float dy=Math.Max(Math.Abs(v-.534f)-.069f,0)*2/3;
                float face=Math.Clamp((MathF.Sqrt(dx*dx+dy*dy)-.072f)/.008f,0,1);
                coverage=face*Math.Clamp((.686f-u)/.008f,0,1);
            }
            byte diffuse=(byte)Math.Clamp((pixel.R+pixel.G+pixel.B)/3+30,0,255);
            result[i]=new(new(Math.Clamp((u-.20f)/.49f,0,.999f),Math.Clamp((v-.15f)/.68f,0,.999f)),
                (byte)(220*coverage),diffuse,0);
        }
        return result;
    }

    public static RobotFlagComposite.Surface[] Polypropylene(Color[] neutral,int width,int height)
    {
        if(width<1 || height<1 || neutral==null || (long)width*height!=neutral.Length)
            throw new ArgumentException("Head image dimensions do not match its pixels.");
        var result=new RobotFlagComposite.Surface[neutral.Length];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){
            int i=y*width+x;float u=(x+.5f)/width,v=(y+.5f)/height;
            // Protect the cream inset, its rim, eyes and smile with a rounded
            // rectangle measured on the approved neutral head, plus a soft edge.
            float dx=Math.Max(Math.Abs(u-.390f)-.168f,0),dy=Math.Max(Math.Abs(v-.537f)-.077f,0);
            float distance=MathF.Sqrt(dx*dx+dy*dy)-.098f;
            float coverage=Math.Clamp(distance/.008f,0,1);
            if(neutral[i].A==0)coverage=0;
            float light=(neutral[i].R+neutral[i].G+neutral[i].B)/(3f*255);
            float reflection=Math.Clamp((light-.738f)/.262f,0,1)*.95f;
            float diffuse=Math.Clamp(light/.612f,0,1);
            // Receding right side compresses the design, as in the approved
            // prototype. Full flag and emblem sampling remains shared C# code.
            float surfaceX=(u-.06f)+MathF.Pow(Math.Max(0,u-.68f),2)*2.2f;
            float surfaceY=(v-.15f)+.032f*MathF.Sin((u-.1f)*MathF.PI);
            result[i]=new(new(surfaceX,surfaceY),Byte(coverage),Byte(diffuse),Byte(reflection));
        }
        return result;
    }
    private static byte Byte(float value)=>(byte)Math.Clamp((int)MathF.Round(value*255),0,255);
}
