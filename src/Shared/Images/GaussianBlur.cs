namespace FolderVerse;
using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

// Separable Gaussian convolution on premultiplied RGBA. Sigma is in input-image pixels.
public static class GaussianBlur
{
    public static Color[] Apply(Color[] pixels,int width,int height,float sigmaPixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if(width<=0 || height<=0 || pixels.Length!=checked(width*height))throw new ArgumentException("Image dimensions do not match pixels.");
        if(!float.IsFinite(sigmaPixels) || sigmaPixels<0 || sigmaPixels>int.MaxValue/6f)throw new ArgumentOutOfRangeException(nameof(sigmaPixels));
        if(sigmaPixels==0)return (Color[])pixels.Clone();
        int radius=(int)Math.Ceiling(3*sigmaPixels);var weights=new float[checked(radius*2+1)];double total=0;
        for(int x=-radius;x<=radius;x++){double weight=Math.Exp(-.5*(x/(double)sigmaPixels)*(x/(double)sigmaPixels));weights[x+radius]=(float)weight;total+=weight;}
        for(int i=0;i<weights.Length;i++)weights[i]/=(float)total;
        var horizontal=new Vector4[pixels.Length];var output=new Color[pixels.Length];
        var options=new ParallelOptions{MaxDegreeOfParallelism=Math.Min(2,Environment.ProcessorCount)};
        Parallel.For(0,height,options,y=>{
            for(int x=0;x<width;x++){
                var sum=Vector4.Zero;
                for(int k=-radius;k<=radius;k++){var c=pixels[y*width+Math.Clamp(x+k,0,width-1)];float w=weights[k+radius];sum+=new Vector4(c.R,c.G,c.B,c.A)*w;}
                horizontal[y*width+x]=sum;
            }
        });
        Parallel.For(0,height,options,y=>{
            for(int x=0;x<width;x++){
                var sum=Vector4.Zero;
                for(int k=-radius;k<=radius;k++)sum+=horizontal[Math.Clamp(y+k,0,height-1)*width+x]*weights[k+radius];
                output[y*width+x]=new Color(Byte(sum.X),Byte(sum.Y),Byte(sum.Z),Byte(sum.W));
            }
        });
        return output;
    }
    private static int Byte(float value)=>Math.Clamp((int)MathF.Round(value),0,255);
}