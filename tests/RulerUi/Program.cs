using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using var game=new RulerCheck();game.Run();

sealed class RulerCheck:Game1
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly Rectangle Panel=new(1040,240,790,580);
    int frame,updated=-1;WorldSetup world;CubeNet net;SeamLabel selected;
    static readonly Color RulerBackground=new(32,57,70),Highlight=new(255,230,112);
    object Get(string name)=>typeof(Game1).GetField(name,Private)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,Private)!.SetValue(this,value);
    static void Check(bool result,string message){if(!result)throw new Exception(message);}
    void Input(Point p,ButtonState button)=>typeof(Game1).GetMethod("HandleInput",Private)!.Invoke(this,new object[]{new MouseState(p.X,p.Y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true});
    void Click(Point p){Input(p,ButtonState.Released);Input(p,ButtonState.Pressed);Input(p,ButtonState.Released);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        world=(WorldSetup)Get("_setup");net=(CubeNet)Get("_net");world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
        ((WorldPreview)Get("_world")).ShowSetup(world,true);net.SetCenter(world,0);net.ShowRoutes=true;
        Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Private)!.FieldType,"WorldStatus"));
        Check(net.ShowRulers,"Rulers should start visible");
        var labels=net.SeamLabels(Panel);Check(labels.GroupBy(l=>l.Name).All(g=>g.Count()==2),"Seam pairs incomplete");
        Check(labels.All(l=>Vector2.Distance(new(l.Bounds.Center.X,l.Bounds.Center.Y),(l.A+l.B)/2)<18),"Letters too far from edges");
        selected=labels.First(l=>labels.Where(p=>p.Name==l.Name).All(p=>CubeNet.MapViewport(Panel).Contains(p.Bounds.Center) && !net.OnRuler(Panel,p.Bounds.Center)) &&
            typeof(CubeNet).GetMethod("AttachPlan",Private)!.Invoke(net,new object[]{l.Face,l.Side,null,0f})!=null);
        Check(((SpriteFont)Get("_font")).Characters.Contains('規'),"Missing ruler glyph");
    }
    protected override void Update(GameTime time)
    {
        if(updated==frame)return;updated=frame;
        switch(frame)
        {
            case 1:
                float zoom=net.Zoom;var pan=net.Pan;Click(new(1760,104));
                Check(!net.ShowRulers && net.Zoom==zoom && net.Pan==pan,"Toggle changed map");break;
            case 2:
                Click(new(1760,104));Input(selected.Bounds.Center,ButtonState.Released);
                Check(net.ShowRulers && net.HoveredSeam==selected.Name,"Letter hover failed");break;
            case 3:
                Input(((selected.A+selected.B)/2).ToPoint(),ButtonState.Released);
                Check(net.HoveredSeam==selected.Name,"Edge hover failed");break;
            case 4:
                Click(selected.Bounds.Center);Check(net.IsAnimating,"Letter click did not join seam");break;
            case 5:
                net.UpdateAnimation(1);net.ZoomAt(Panel,new(1435,530),480);net.Drag(new(18,-23));Input(new(0,0),ButtonState.Released);break;
            case 6:
                net.SetCenter(world,2);net.Turn(world,1);
                var cell=world.Cells.First(c=>c.Face==2 && Panel.Contains(net.CellBounds(c,Panel).Center));Input(net.CellBounds(cell,Panel).Center,ButtonState.Released);
                Check((int)Get("_statusCell")==cell.Id,"Polar reference hover failed");break;
            case 7:
                var manager=(GraphicsDeviceManager)Get("_graphics");manager.PreferredBackBufferWidth=960;manager.PreferredBackBufferHeight=540;manager.ApplyChanges();break;
            case 8:
                Console.WriteLine("PASS: ruler on/off rendering; red longitude and green latitude; fixed bars during zoom/pan; polar rotation; paired letters/edges highlight; letter click joins seam; half-size window.");Exit();break;
        }
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        Color At(Point point)=>pixels[point.Y*p.BackBufferWidth+point.X];
        if(frame<7)
        {
            var top=CubeNet.TopRuler(Panel);var left=CubeNet.LeftRuler(Panel);
            Check((At(new(top.Left+1,top.Top+1))==RulerBackground)==net.ShowRulers,"Ruler background toggle mismatch");
            Check((At(new(left.Left+1,left.Bottom-2))==RulerBackground)==net.ShowRulers,"Left ruler moved or did not toggle");
            if(frame==0)
            {
                bool Contains(Rectangle r,Color color)=>Enumerable.Range(r.Top,r.Height).Any(y=>Enumerable.Range(r.Left,r.Width).Any(x=>pixels[y*p.BackBufferWidth+x]==color));
                Check(Contains(top,WorldCoordinates.LongitudeColor) && Contains(left,WorldCoordinates.LatitudeColor),"Ruler colors missing");
            }
            if(frame is 2 or 3)foreach(var label in net.SeamLabels(Panel).Where(l=>l.Name==selected.Name))
            {
                Check(At(new(label.Bounds.Left+1,label.Bounds.Top+1))==Highlight,"Paired letter not highlighted");
                Check(At(((label.A+label.B)/2).ToPoint())==Highlight,"Paired edge not highlighted");
            }
        }
        using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);
        using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"ruler-ui-{frame}.png"));image.SaveAsPng(output,p.BackBufferWidth,p.BackBufferHeight);
        frame++;
    }
}
