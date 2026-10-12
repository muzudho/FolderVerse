using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CheckGame();game.Run();
sealed class CheckGame:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
    object Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,args)!;
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);int node=w.Nodes.All.First(n=>n.Owner==0).Id;Set("_populationNode",node);var r=w.Robots.Create(1,RobotParts.Complete);Check((bool)Call("CanMoveHangarPart",r,RobotParts.Head),"Captured foreign parts movable");w.Nodes.All[node].Owner=-1;Check(!(bool)Call("CanMoveHangarPart",r,RobotParts.Head),"Neutral foreign part disabled");Check((bool)Call("CanMoveHangarPart",w.Robots.Create(0,RobotParts.Head),RobotParts.Head),"Neutral own part movable");w.Nodes.All[node].Owner=1;Check(!(bool)Call("CanMoveHangarPart",r,RobotParts.Head),"Foreign node disabled");Set("_trainingOpen",true);Call("TrainingInput",new Point(600,380),true,false);Call("TrainingInput",new Point(600,530),true,false);Check(w.Robots.Training[0].AssemblyPolicy==2,"Assembly dropdown selection");Call("TrainingInput",new Point(600,580),true,false);Call("TrainingInput",new Point(600,735),true,false);Check(w.Robots.Training[0].DeploymentPolicy==2 && w.Robots.Training[1].DeploymentPolicy==0,"Deployment dropdown independent");}
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time){GraphicsDevice.Clear(Color.Black);var batch=(SpriteBatch)Get("_spriteBatch");batch.Begin();Call("DrawTraining");batch.End();var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"training-ui.png"));texture.SaveAsPng(output,pp.BackBufferWidth,pp.BackBufferHeight);Console.WriteLine("PASS: neutral/foreign/captured permissions and training dialog render.");Exit();}
}