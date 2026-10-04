using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using var game=new GlobeCheck();game.Run();
sealed class GlobeCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,updated=-1;WorldSetup setup;WorldPreview globe;
    object Get(string n)=>typeof(Game1).GetField(n,Flags)!.GetValue(this)!;
    void Set(string n,object v)=>typeof(Game1).GetField(n,Flags)!.SetValue(this,v);
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Input(Point p,ButtonState b)=>typeof(Game1).GetMethod("HandleInput",Flags)!.Invoke(this,new object[]{new MouseState(p.X,p.Y,0,b,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true});
    void Click(Point p){Input(p,ButtonState.Released);Input(p,ButtonState.Pressed);Input(p,ButtonState.Released);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        setup=(WorldSetup)Get("_setup");setup.SetWorld(0);setup.SetCast(123);setup.SetPlacement(456);setup.SelectPlayer(0);
        globe=(WorldPreview)Get("_world");globe.ShowSetup(setup,true);((CubeNet)Get("_net")).Home(setup);
        Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Flags)!.FieldType,"WorldStatus"));
        Set("_yaw",0f);Set("_pitch",0f);
        Check(globe.ProjectVisible(new(0,0,setup.Depth/2f),Vector3.Backward,0,0,new(1040,240,790,580),out var center) && Vector2.Distance(center,new(1435,530))<1,"Front projection wrong");
        Check(!globe.ProjectVisible(new(0,0,-setup.Depth/2f),Vector3.Forward,0,0,new(1040,240,790,580),out _),"Back label visible");
        var panel=new Rectangle(1040,240,790,580);int checkedOffsets=0;
        foreach(var cell in setup.Cells)
        {
            if(!globe.ProjectVisible(cell.Center,cell.Normal,.55f,.3f,panel,out var foot))continue;
            if(!globe.ProjectVisible(cell.Center,cell.Normal,.55f,.3f,panel,out var tip,2f))continue;
            globe.ProjectVisible(cell.Center+cell.Normal*2,cell.Normal,.55f,.3f,panel,out var expected);
            Check(Vector2.Distance(tip,expected)<.001f,"Coordinate is not two cells along its normal");
            if(Vector2.Distance(foot,tip)>10)checkedOffsets++;
        }
        Check(checkedOffsets>0,"Normals did not separate coordinates from the surface");
        Check(!globe.ProjectVisible(new(0,0,-setup.Depth/2f),Vector3.Forward,0,0,panel,out _,2f),"Raised back label visible");
        var faceCells=setup.Cells.Where(c=>c.Face==0).ToArray();
        var gridComponents=WorldCoordinates.GlobeLabelComponents(setup,faceCells.Select(c=>c.Id));
        int minX=faceCells.Min(c=>c.X),maxX=faceCells.Max(c=>c.X),minY=faceCells.Min(c=>c.Y),maxY=faceCells.Max(c=>c.Y);
        var interior=faceCells.Where(c=>c.X>minX && c.X<maxX && c.Y>minY && c.Y<maxY).ToArray();
        Check(interior.Length>0 && interior.All(c=>!gridComponents[c.Id].Longitude && !gridComponents[c.Id].Latitude),"Both interior coordinates must be hidden");
        Check(faceCells.Where(c=>(c.X==minX || c.X==maxX) && (c.Y==minY || c.Y==maxY))
            .All(c=>gridComponents[c.Id].Longitude && gridComponents[c.Id].Latitude),"Corner endpoints must retain both coordinates");
        foreach(bool longitude in new[]{true,false})
        {
            var run=setup.Cells.Where(c=>c.Face==0)
                .GroupBy(c=>longitude?WorldCoordinates.At(setup,c).X:WorldCoordinates.At(setup,c).Y)
                .First(g=>g.Count()>=3).OrderBy(c=>longitude?WorldCoordinates.At(setup,c).Y:WorldCoordinates.At(setup,c).X).ToArray();
            var components=WorldCoordinates.GlobeLabelComponents(setup,run.Select(c=>c.Id));
            for(int i=0;i<run.Length;i++)
            {
                var shown=components[run[i].Id];bool endpoint=i==0 || i==run.Length-1;
                Check((longitude?shown.Longitude:shown.Latitude)==endpoint,"Repeated coordinate endpoints/interior wrong");
                Check(longitude?shown.Latitude:shown.Longitude,"Changing coordinate was suppressed");
            }
            var isolated=WorldCoordinates.GlobeLabelComponents(setup,new[]{run[0].Id,run[^1].Id});
            Check(isolated.Values.All(v=>v.Longitude && v.Latitude),"Gap endpoints were suppressed");
        }
    }
    protected override void Update(GameTime time)
    {
        if(updated==frame)return;updated=frame;
        switch(frame)
        {
            case 0:Click(new(1710,104));Check((bool)Get("_statusGlobe"),"Globe toggle failed");break;
            case 1:
                float yaw=(float)Get("_yaw");Input(new(1400,500),ButtonState.Pressed);Input(new(1460,530),ButtonState.Pressed);Input(new(1460,530),ButtonState.Released);
                Check((float)Get("_yaw")!=yaw,"Globe drag failed");break;
            case 2:Click(new(410,1040));Check(!(bool)Get("_statusGlobe") && (bool)Get("_movementOpen"),"Movement did not open net");break;
            case 3:
                Click(new(830,190));Check((bool)Get("_statusGlobe") && !(bool)Get("_movementOpen"),"Movement did not restore globe");
                Click(new(1730,935));Check((float)Get("_yaw")==0 && (float)Get("_pitch")==0,"Head/belly orientation reset failed");
                Check(WorldPreview.Orientation(0,0)==Matrix.Identity,"Reset orientation is not head up and belly forward");break;
            case 4:Click(new(1810,104));Check(!(bool)Get("_statusGlobe"),"Net toggle failed");break;
            case 5:Click(new(1710,104));var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();break;
            case 6:Console.WriteLine("PASS: globe/net switching, two-cell normal offset and visible normal lines, back-face hiding, drag rotation, movement view restoration, coordinate colors and small window.");Exit();break;
        }
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        if(frame is 0 or 1)
        {
            bool Has(Color c)=>Enumerable.Range(240,580).Any(y=>Enumerable.Range(1040,790).Any(x=>pixels[y*p.BackBufferWidth+x]==c));
            Check(Has(WorldCoordinates.LongitudeColor)&&Has(WorldCoordinates.LatitudeColor),"Projected coordinates missing");
            Check(Has(new Color(172,202,215)),"Normal lines missing");
            var toy=new Rectangle(1601,359,221,265);
            Check(Enumerable.Range(toy.Top,toy.Height).Sum(y=>Enumerable.Range(toy.Left,toy.Width).Count(x=>pixels[y*p.BackBufferWidth+x]!=pixels[300*p.BackBufferWidth+1800]))>1000,"Direction robot missing");
        }
        using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);
        using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"globe-ui-{frame}.png"));texture.SaveAsPng(output,p.BackBufferWidth,p.BackBufferHeight);frame++;
    }
}
