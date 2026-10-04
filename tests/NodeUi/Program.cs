using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using var game=new NodeUiCheck();game.Run();

sealed class NodeUiCheck:Game1
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,updatedFrame=-1,target,enemy;WorldSetup world;CubeNet net;
    Color ownBefore,enemyBefore;Point ownSample,enemySample;
    object Get(string name)=>typeof(Game1).GetField(name,Private)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,Private)!.SetValue(this,value);
    void Click(int x,int y)=>typeof(Game1).GetMethod("MovementClick",Private)!.Invoke(this,new object[]{new Point(x,y),new KeyboardState()});
    void Input(Point pointer,ButtonState button,params Keys[] keys)=>typeof(Game1).GetMethod("HandleInput",Private)!.Invoke(this,new object[]{new MouseState(pointer.X,pointer.Y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(keys),new GameTime(),true});
    void MapClick(Point p){Input(p,ButtonState.Released);Input(p,ButtonState.Pressed);Input(p,ButtonState.Released);}
    void Check(bool result,string message){if(!result)throw new Exception(message);}
    static readonly Rectangle Panel=new(1040,240,790,580);
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        world=(WorldSetup)Get("_setup");net=(CubeNet)Get("_net");Node source=null;
        for(int seed=0;seed<64 && source==null;seed++)
        {
            world.SetWorld(seed);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
            source=world.Nodes.All.FirstOrDefault(p=>world.Routes.Neighbors(p).Length>5);
        }
        Check(source!=null,"No pagination fixture");world.ConquerorLocations[0]=source.Cell;world.ConquerorPoints[0]=source.Center;
        Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Private)!.FieldType,"WorldStatus"));
        ((WorldPreview)Get("_world")).ShowSetup(world,true);net.SetCenter(world,world.Cells[source.Cell].Face);net.ShowRoutes=false;
        Click(430,1044);Check(net.ShowRoutes && (bool)Get("_movementOpen") && net.MovementTargets.Count>5,"Temporary traffic display failed");
        Click(700,936);Check(world.Population.Turn==0 && (bool)Get("_movementOpen"),"Unselected confirm advanced turn");
        var font=(SpriteFont)Get("_font");Check("地盤点滅経番緯番確定".All(font.Characters.Contains),"Font lacks new glyphs");
        target=net.MovementTargets.First(id=>{var p=net.NodePosition(world,world.Nodes.All[id],Panel).ToPoint();return net.HitNode(world,Panel,p,net.MovementTargets)==id;});
    }
    protected override void Update(GameTime time)
    {
        if(updatedFrame==frame)return;updatedFrame=frame;
        switch(frame)
        {
            case 1:
                Click(770,845);Check((int)Get("_movementPage")==1,"Next page failed");break;
            case 2:
                Click(630,845);Check((int)Get("_movementPage")==0,"Previous page failed");
                Click(300,454);Check(world.Population.Turn==0 && (int)Get("_selectedMoveNode")>=0,"List selection moved instead of selecting");break;
            case 3:
                var at=net.NodePosition(world,world.Nodes.All[target],Panel).ToPoint();MapClick(at);
                Check((int)Get("_selectedMoveNode")==target && net.SelectedTarget==target && world.Population.Turn==0,"Map selection failed or moved prematurely");
                Check(net.HitNode(world,Panel,at)==target,"Tooltip hit failed");
                Check((int)Get("_populationCell")==-1,"Map destination opened population panel");break;
            case 4:
                Click(700,936);Check(world.Nodes.Current(0).Id==target && world.Population.Turn==1,"Confirm did not move once");
                Check(!(bool)Get("_movementOpen") && !net.ShowRoutes && net.MovementTargets.Count==0 && net.SelectedTarget==-1,"Confirm did not restore grid");
                while(Get("_screen").ToString()=="Battle")typeof(Game1).GetMethod("UpdateBattle",Private)!.Invoke(this,new object[]{3d,true});
                while(Get("_screen").ToString()=="Disposition"){Set("_pointer",new Point(900,895));typeof(Game1).GetMethod("DispositionInput",Private)!.Invoke(this,new object[]{true,false});}
                MapClick(new Point(180,1044));Check(Get("_screen").ToString()=="WorldStatus","Removed back button still active");break;
            case 5:
                Click(430,1044);Click(829,190);Check(!net.ShowRoutes && !(bool)Get("_movementOpen"),"Cancel X did not restore grid");
                net.ShowRoutes=true;Click(430,1044);Click(430,1044);Check(net.ShowRoutes && !(bool)Get("_movementOpen"),"Cancel toggle lost previous traffic mode");break;
            case 6:
                Click(430,1044);Input(new Point(0,0),ButtonState.Released,Keys.Escape);Input(new Point(0,0),ButtonState.Released);
                Check(net.ShowRoutes && !(bool)Get("_movementOpen") && net.MovementTargets.Count==0,"Escape did not restore mode");break;
            case 7:
                Click(430,1044);MapClick(new Point(1550,104));Check(!net.ShowRoutes && !(bool)Get("_movementOpen") && net.MovementTargets.Count==0,"Layer toggle did not cancel movement and restore grid");
                Click(430,1044);Check(net.ShowRoutes && (bool)Get("_movementOpen"),"Movement did not reopen after layer toggle");
                int turn=world.Population.Turn;MapClick(new Point(700,1044));Check(world.Population.Turn==turn,"Waiting changed candidates during selection");
                Click(430,1044);net.ShowFlags=false;
                enemy=world.Nodes.All.First(n=>Panel.Contains(net.NodePosition(world,n,Panel)) && Vector2.Distance(net.NodePosition(world,n,Panel),net.NodePosition(world,world.Nodes.Current(0),Panel))>60).Id;
                world.ConquerorLocations[1]=world.Nodes.All[enemy].Cell;world.ConquerorPoints[1]=world.Nodes.All[enemy].Center;
                var own=net.NodePosition(world,world.Nodes.Current(0),Panel);var other=net.NodePosition(world,world.Nodes.All[enemy],Panel);
                ownSample=new((int)own.X-12,(int)own.Y-7);enemySample=new((int)other.X-6,(int)other.Y-3);
                Input(new Point(0,0),ButtonState.Released);Set("_animationTime",MathF.PI*3/8);break;
            case 8:
                Set("_animationTime",MathF.PI/8);break;
            case 9:
                net.ZoomAt(Panel,new Point(1435,530),120*4);break;
            case 10:
                Console.WriteLine("PASS: map/list selection waits for confirm; confirm/cancel/escape restore view; candidates, hints, coordinate colors, stable enemy and pulsing player batteries, zoom rendering.");Exit();break;
        }
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);
        if(frame is 0 or 3 or 4 or 7 or 8 or 9)
        {
            var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
            if(frame==7){ownBefore=pixels[ownSample.Y*p.BackBufferWidth+ownSample.X];enemyBefore=pixels[enemySample.Y*p.BackBufferWidth+enemySample.X];}
            if(frame==8)
            {
                Check(pixels[ownSample.Y*p.BackBufferWidth+ownSample.X]!=ownBefore,"Player battery stopped pulsing");
                Check(pixels[enemySample.Y*p.BackBufferWidth+enemySample.X]==enemyBefore,"Enemy battery still pulses");
            }
            if(frame==3)
            {
                // Check the actual location sentence, rather than just the colored legend.
                var sentence=new Rectangle(84,240,760,45);
                bool ContainsColor(Color color)=>Enumerable.Range(sentence.Top,sentence.Height).Any(y=>Enumerable.Range(sentence.Left,sentence.Width).Any(x=>pixels[y*p.BackBufferWidth+x]==color));
                Check(ContainsColor(WorldCoordinates.LongitudeColor) && ContainsColor(WorldCoordinates.LatitudeColor),"Coordinate colors not rendered in location sentence");
            }
            using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"node-ui-{frame}.png"));image.SaveAsPng(output,p.BackBufferWidth,p.BackBufferHeight);
        }
        frame++;
    }
}
