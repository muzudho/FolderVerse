using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using var game=new ToyCheck();game.Run();

sealed class ToyCheck:Game1
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,updated=-1;WorldSetup setup;WorldPreview world;Color[] front;
    object Get(string name)=>typeof(Game1).GetField(name,Private)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,Private)!.SetValue(this,value);
    void Screen(string name)=>Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Private)!.FieldType,name));
    static void Check(bool result,string message){if(!result)throw new Exception(message);}
    void Input(int x,int y,ButtonState button,double elapsed=0)=>typeof(Game1).GetMethod("HandleInput",Private)!.Invoke(this,new object[]{new MouseState(x,y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.Zero,TimeSpan.FromSeconds(elapsed)),true});
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        setup=(WorldSetup)Get("_setup");world=(WorldPreview)Get("_world");
        world.Generate(1,1,1,0);Check(world.GlobeName=="地棒儀","Unit world has 1x1 faces");
        world.Generate(1,1,4,0);Check(world.GlobeName=="地棒儀","Rod name");
        world.Generate(1,3,4,0);Check(world.GlobeName=="地盤儀","Slab name");
        world.Generate(2,3,4,0);Check(world.GlobeName=="地箱儀","Box name");
        setup.SetWorld(0);setup.SetCast(123);setup.SetPlacement(456);world.ShowSetup(setup,false);
        Screen("Review");Set("_yaw",0f);Set("_pitch",0f);
        var font=(SpriteFont)Get("_font");Check("地棒儀地盤儀地箱儀碗箸頭足腹背向きの人形".All(font.Characters.Contains),"Missing orientation glyphs");
    }
    protected override void Update(GameTime time)
    {
        if(updated==frame)return;updated=frame;
        switch(frame)
        {
            case 1:
                Input(400,400,ButtonState.Released);Input(400,400,ButtonState.Pressed);Input(470,440,ButtonState.Pressed);Input(470,440,ButtonState.Released);
                Check(Math.Abs((float)Get("_yaw")-.56f)<.001f && Math.Abs((float)Get("_pitch")-.32f)<.001f,"Globe drag failed");break;
            case 2:Set("_yaw",MathF.PI);Set("_pitch",0f);break;
            case 3:
                world.ShowSetup(setup,true);Screen("PlacementReview");Set("_yaw",-.6f);Set("_pitch",.25f);
                float yaw=(float)Get("_yaw");Input(0,0,ButtonState.Released,.5);Check((float)Get("_yaw")>yaw,"Auto rotation failed");break;
            case 4:
                typeof(Game1).GetMethod("BeginSelection",Private)!.Invoke(this,null);Input(0,0,ButtonState.Released,.2);break;
            case 5:
                var manager=(GraphicsDeviceManager)Get("_graphics");manager.PreferredBackBufferWidth=960;manager.PreferredBackBufferHeight=540;manager.ApplyChanges();break;
            case 6:
                Console.WriteLine("PASS: globe names; front/back toy rendering; shared world rotation during drag and automatic rotation; placement/selection layouts; half-size window.");Exit();break;
        }
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);
        var toy=(ToyOrientationRenderer)Get("_orientationToy");
        var globeEffect=(BasicEffect)typeof(WorldPreview).GetField("_effect",Private)!.GetValue(world)!;
        var toyEffect=(BasicEffect)typeof(ToyOrientationRenderer).GetField("_effect",Private)!.GetValue(toy)!;
        Check(globeEffect.World==toyEffect.World,"Toy and globe orientation diverged");
        var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        if(frame is 0 or 2)
        {
            var areas=((Rectangle Globe,Rectangle Toy,Rectangle Legend))typeof(Game1).GetMethod("OrientationAreas",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{new Rectangle(130,240,1120,680),false})!;
            var toyPixels=Enumerable.Range(areas.Toy.Top,areas.Toy.Height).SelectMany(y=>Enumerable.Range(areas.Toy.Left,areas.Toy.Width).Select(x=>pixels[y*p.BackBufferWidth+x])).ToArray();
            if(frame==0)front=toyPixels;else Check(!toyPixels.SequenceEqual(front),"Toy back view unchanged");
        }
        using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);
        using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"toy-ui-{frame}.png"));image.SaveAsPng(output,p.BackBufferWidth,p.BackBufferHeight);
        frame++;
    }
}
