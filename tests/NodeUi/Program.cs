using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using var game=new NodeUiCheck();game.Run();

sealed class NodeUiCheck:Game1
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,updatedFrame=-1;WorldSetup world;int firstTarget;
    object Get(string name)=>typeof(Game1).GetField(name,Private)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,Private)!.SetValue(this,value);
    void Click(int x,int y)=>typeof(Game1).GetMethod("MovementClick",Private)!.Invoke(this,new object[]{new Point(x,y),new KeyboardState()});
    void Check(bool result,string message){if(!result)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        world=(WorldSetup)Get("_setup");Node source=null;
        for(int seed=0;seed<64 && source==null;seed++)
        {
            world.SetWorld(seed);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
            source=world.Nodes.All.FirstOrDefault(p=>world.Routes.Neighbors(p).Length>5);
        }
        Check(source!=null,"No pagination fixture");world.ConquerorLocations[0]=source.Cell;world.ConquerorPoints[0]=source.Center;
        Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Private)!.FieldType,"WorldStatus"));
        ((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);((CubeNet)Get("_net")).ShowRoutes=true;
        Set("_movementOpen",true);
        var font=(SpriteFont)Get("_font");Check("交通路接続←→１北北西".All(font.Characters.Contains),"Font lacks movement glyphs");
        firstTarget=world.Nodes.At(world.Routes.NodeChoices(0)[0].Target,world.Routes.NodeChoices(0)[0].Entry).Id;
    }
    protected override void Update(GameTime time)
    {
        if(updatedFrame==frame)return;updatedFrame=frame;
        if(frame==1){Click(770,845);Check((int)Get("_movementPage")==1,"Next page failed");}
        if(frame==2){Click(630,845);Check((int)Get("_movementPage")==0,"Previous page failed");int turn=world.Population.Turn;Click(300,454);Check(world.Nodes.Current(0).Id==firstTarget && world.Population.Turn==turn+1,"Node button failed");}
        if(frame==3)
        {
            Set("_movementOpen",false);
            var input=typeof(Game1).GetMethod("HandleInput",Private)!;
            input.Invoke(this,new object[]{new MouseState(180,1044,0,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),time,true});
            input.Invoke(this,new object[]{new MouseState(180,1044,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),time,true});
            Check(Get("_screen").ToString()=="WorldStatus","Removed back button still active");
        }
        if(frame>=4){Console.WriteLine("PASS: rendered movement pages, glyphs, page clicks, Node movement and removed back-button hit area.");Exit();}
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);
        if(frame is 0 or 1 or 3)
        {
            var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
            using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"node-ui-{frame}.png"));image.SaveAsPng(output,p.BackBufferWidth,p.BackBufferHeight);
        }
        frame++;
    }
}
