namespace FolderVerse;
using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class UiPainter
{
    public SpriteBatch Batch { get; }
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    public Point Pointer { get; set; }
    public UiPainter(SpriteBatch batch, Texture2D pixel, SpriteFont font) { Batch=batch; _pixel=pixel; _font=font; }
    public void Box(Rectangle rect, Color color) => Batch.Draw(_pixel,rect,color);
    public void Tile(Vector2 center,Vector2 size,float rotation,Color color)=>Batch.Draw(_pixel,center,null,color,rotation,new Vector2(.5f),size,SpriteEffects.None,0);
    public Vector2 Measure(string text,float scale)=>_font.MeasureString(text)*scale;
    private static readonly Regex Coordinates=new(@"（(?<longitude>-?\d+),(?<latitude>-?\d+)(?=[,）])",RegexOptions.Compiled);
    public void Text(string text, Vector2 at, float scale, Color color)
    {
        int start=0;
        void Run(string part,Color tint)
        {
            Batch.DrawString(_font,part,at,tint,0,Vector2.Zero,scale,SpriteEffects.None,0);
            at.X+=_font.MeasureString(part).X*scale;
        }
        foreach(Match match in Coordinates.Matches(text))
        {
            var longitude=match.Groups["longitude"];var latitude=match.Groups["latitude"];
            Run(text.Substring(start,longitude.Index-start),color);
            Run(longitude.Value,WorldCoordinates.LongitudeColor);
            Run(",",color);Run(latitude.Value,WorldCoordinates.LatitudeColor);
            start=latitude.Index+latitude.Length;
        }
        Run(text.Substring(start),color);
    }
    public void Center(string text, Rectangle rect, float scale, Color color)
    {
        var size=_font.MeasureString(text)*scale;
        Text(text,new Vector2(rect.Center.X-size.X/2,rect.Center.Y-size.Y/2),scale,color);
    }
    public void Button(Rectangle rect,string label,Color color,float scale=0.7f)
    {
        if(rect.Contains(Pointer)) color=Color.Lerp(color,Color.White,0.14f);
        Box(new Rectangle(rect.X,rect.Y+4,rect.Width,rect.Height),new Color(7,23,35)); Box(rect,color); Center(label,rect,scale,Color.White);
    }
}

// Entirely inside the game's canvas; no native dialogs or Windows Forms.
public sealed class SeedDialog
{
    public enum Target { World, Cast, Placement }
    public bool IsOpen { get; private set; }
    public Target Purpose { get; private set; }
    public string Value { get; private set; } = "";
    public string Error { get; private set; } = "";
    private bool _replace;
    private static readonly Rectangle ApplyButton=new(685,865,320,70), CancelButton=new(1025,865,220,70);
    private static readonly string[] Keys={"7","8","9","4","5","6","1","2","3","C","0","<"};
    public void Open(Target target,int current) { Purpose=target; Value=current.ToString(); Error=""; IsOpen=true; _replace=true; }
    public void Close() { IsOpen=false; }
    private static Rectangle KeyBounds(int index) => new(685+(index%3)*190,475+(index/3)*92,180,82);
    public int? Press(string key)
    {
        Error="";
        if(key=="C") { Value=""; _replace=false; }
        else if(key=="<") { if(Value.Length>0) Value=Value[..^1]; _replace=false; }
        else if(key=="OK")
        {
            if(int.TryParse(Value,out int seed) && seed>=0) { Close(); return seed; }
            Error="0 - 2147483647";
        }
        else if(key.Length==1 && char.IsDigit(key[0]))
        {
            if(_replace) { Value=""; _replace=false; }
            if(Value.Length<10) Value+=key;
        }
        return null;
    }
    public int? Click(Point point)
    {
        if(CancelButton.Contains(point)) { Close(); return null; }
        if(ApplyButton.Contains(point)) return Press("OK");
        for(int i=0;i<Keys.Length;i++) if(KeyBounds(i).Contains(point)) return Press(Keys[i]);
        return null;
    }
    public void Draw(UiPainter ui)
    {
        ui.Box(new Rectangle(0,0,1920,1080),new Color(0,0,0,190));
        ui.Box(new Rectangle(640,170,650,805),new Color(32,59,77));
        string title=Purpose switch { Target.World=>"世界 SEED", Target.Cast=>"登場人物 SEED", _=>"初期配置 SEED" };
        ui.Text(title,new Vector2(685,210),0.9f,new Color(255,232,184));
        ui.Box(new Rectangle(685,300,560,100),new Color(11,29,41));
        ui.Center(Value.Length==0 ? "_" : Value,new Rectangle(700,310,530,80),1.1f,Color.White);
        ui.Text(Error.Length>0 ? Error : "0 - 2147483647 / Enter = OK",new Vector2(685,420),0.55f,new Color(255,204,151));
        for(int i=0;i<Keys.Length;i++) ui.Button(KeyBounds(i),Keys[i],new Color(58,91,112),0.95f);
        ui.Button(ApplyButton,"この SEED を適用",new Color(45,135,142),0.65f);
        ui.Button(CancelButton,"キャンセル",new Color(82,87,105),0.65f);
    }
}
