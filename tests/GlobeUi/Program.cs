using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using var game=new GlobeCheck();game.Run();
sealed class GlobeCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,updated=-1,portTarget;WorldSetup setup;WorldPreview globe;
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
        foreach(float yaw in new[]{-.55f,.55f,2.2f})
        {
            var positions=setup.Cells.Where(c=>c.Face is not (2 or 3)).ToDictionary(c=>c.Id,c=>
            {
                globe.ProjectVisible(c.Center,c.Normal,yaw,.3f,panel,out var point,2f);return point;
            });
            var screenComponents=WorldCoordinates.GlobeScreenLabelComponents(setup,positions);
            foreach(var group in setup.Cells.Where(c=>positions.ContainsKey(c.Id)).GroupBy(c=>(c.Face,WorldCoordinates.At(setup,c).X)))
            {
                var kept=group.Where(c=>screenComponents[c.Id].Longitude).ToArray();
                Check(kept.Length==1 && positions[kept[0].Id].Y==group.Min(c=>positions[c.Id].Y),"Longitude is not only at the screen top");
            }
            foreach(var group in setup.Cells.Where(c=>positions.ContainsKey(c.Id)).GroupBy(c=>(c.Face,WorldCoordinates.At(setup,c).Y)))
            {
                var kept=group.Where(c=>screenComponents[c.Id].Latitude).ToArray();
                Check(kept.Length==1 && positions[kept[0].Id].X==group.Min(c=>positions[c.Id].X),"Latitude is not only at the screen left");
            }
        }
        foreach(float height in new[]{.4f,.5f,.53f,.6f,.73f,.8f})
        {
            var head=WorldTerrain.ColorAt(height,Vector3.Up);var foot=WorldTerrain.ColorAt(height,Vector3.Down);
            Check(head==foot,"Polar palettes differ");
            Check(WorldTerrain.ColorAt(height,Vector3.Right)==WorldTerrain.ColorAt(height),"Side palette changed");
            Check(WorldTerrain.Kind(height)==TerrainKind.Sea?head.R>100 && head.B>head.R:head.R>=224 && head.G>=224 && head.B>=224,"Polar terrain is not icy blue or snow white");
        }
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
            case 2:
                var port=setup.Nodes.All.First(p=>p.IsHarbor);portTarget=port.Id;
                var route=setup.Routes.Neighbors(port).First();var source=setup.Nodes.At(route.Target,route.Entry);
                foreach(var post in setup.Nodes.All)post.Population.People[0]=0;
                setup.ConquerorLocations[0]=source.Cell;setup.ConquerorPoints[0]=source.Center;
                source.Owner=0;source.Population.People[0]=1000;port.Owner=1;port.Population.People[0]=10;
                Set("_yaw",-MathF.Atan2(setup.Cells[port.Cell].Normal.X,setup.Cells[port.Cell].Normal.Z));Set("_pitch",MathF.Asin(setup.Cells[port.Cell].Normal.Y));
                Click(new(410,1040));Check((bool)Get("_statusGlobe") && (bool)Get("_movementOpen") && ((CubeNet)Get("_net")).ShowRoutes,"Globe movement switched view or did not open routes");
                Set("_escort",100L);
                float yawBefore=(float)Get("_yaw");Input(new(1540,300),ButtonState.Pressed);Input(new(1550,305),ButtonState.Pressed);Input(new(1550,305),ButtonState.Released);
                Check((float)Get("_yaw")!=yawBefore && (bool)Get("_statusGlobe"),"Globe cannot rotate during movement selection");
                var area=(Rectangle)typeof(Game1).GetProperty("StatusGlobeArea",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
                Check(globe.ProjectVisible(setup.Routes.Position(port.Cell,port.Center),setup.Cells[port.Cell].Normal,(float)Get("_yaw"),(float)Get("_pitch"),area,out var target),"Port target not visible");
                Click(target.ToPoint());Check((int)Get("_selectedMoveNode")==portTarget && setup.Nodes.Current(0).Id==source.Id,"Globe click did not select port or moved before confirm");break;
            case 3:
                Click(new(700,935));Check((bool)Get("_statusGlobe") && !(bool)Get("_movementOpen") && setup.Nodes.Current(0).Id==portTarget && setup.Nodes.All[portTarget].Owner==0,"Globe port confirm or conquest failed");
                Check(!((CubeNet)Get("_net")).ShowRoutes,"Confirm did not restore grid mode");
                Click(new(410,1040));Click(new(830,190));Check((bool)Get("_statusGlobe") && !(bool)Get("_movementOpen") && !((CubeNet)Get("_net")).ShowRoutes,"Cancel changed globe or failed to restore grid");
                Click(new(1730,935));Check((float)Get("_yaw")==0 && (float)Get("_pitch")==0,"Head/belly orientation reset failed");
                Check(WorldPreview.Orientation(0,0)==Matrix.Identity,"Reset orientation is not head up and belly forward");break;
            case 4:Click(new(1810,104));Check(!(bool)Get("_statusGlobe"),"Net toggle failed");break;
            case 5:Click(new(1710,104));var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();break;
            case 6:Console.WriteLine("PASS: globe retained during movement; traffic mode, rotation, port picking and conquest; confirm/cancel restore grid; coordinates, robot and small window.");Exit();break;
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
