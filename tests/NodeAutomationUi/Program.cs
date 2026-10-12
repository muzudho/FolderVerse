using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CheckGame();game.Run();
sealed class CheckGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 int node;
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);((WorldPreview)Get("_world")).ShowSetup(w,true);node=w.Nodes.All.First(n=>n.Owner==0 && !Enumerable.Range(0,w.ActiveCount).Any(r=>w.Nodes.Current(r)?.Id==n.Id)).Id;
 Set("_populationNode",node);Set("_populationCell",w.Nodes.All[node].Cell);Set("_inspectedNode",node);
 Call("NodeDialogClick",new Point(1100,982),new Microsoft.Xna.Framework.Input.KeyboardState());Call("NodeDialogClick",new Point(1100,938),new Microsoft.Xna.Framework.Input.KeyboardState());Check(w.Robots.Workshops[node].Automation==NodeAutomation.Manual,"Translated manual selector");
 Set("_populationCell",-1);Set("_trainingOpen",true);Call("TrainingInput",new Point(600,580),true,false);Call("TrainingInput",new Point(600,770),true,false);Check(w.Robots.Training[0].DeploymentPolicy==3,"Local training option");Set("_trainingOpen",false);Set("_populationCell",w.Nodes.All[node].Cell);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){GraphicsDevice.Clear(Color.Black);var b=(SpriteBatch)Get("_spriteBatch");b.Begin();Call("DrawStatusUi");b.End();var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"node-ui.png"));texture.SaveAsPng(output,pp.BackBufferWidth,pp.BackBufferHeight);Console.WriteLine("PASS: local dropdown, shifted node mode and supervisor portrait render.");Exit();}
}