using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
using var game=new CurtainUi(args[0]);game.Run();
sealed class CurtainUi(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;SpriteBatch batch=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!; protected override void LoadContent(){Window.Position=new(-3000,-3000);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));w=new(){EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.SelectPlayer(0);w.Relations.Leader=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);w.Salvage.Reset();w.Salvage.EnableExpeditions(w);foreach(var n in w.Nodes.All)n.Owner=0;int h=w.Nodes.All.First(n=>n.Functional).Id;w.ConquerorLocations[0]=w.Nodes.All[h].Cell;w.ConquerorPoints[0]=w.Nodes.All[h].Center;
 foreach(var material in Enum.GetValues<RobotMaterial>()){var robot=w.Robots.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=material,BodyMaterial=(RobotMaterial)(((int)material+1)%9),LegsMaterial=material};w.Robots.Nodes[h].Add(robot);var good=w.Robots.Create(1,RobotParts.Complete) with{HeadMaterial=material,BodyMaterial=material,LegsMaterial=material,IntegratedParts=(int)material%2==0?RobotParts.Head|RobotParts.Body:0};w.Salvage.Items.Add(new(1000+(int)material,h,good));}
 Set("_setup",w);batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");Set("_portraitRenderer",new PortraitRenderer(Content));var fonts=new Microsoft.Xna.Framework.Content.ContentManager(Services,Path.Combine(Environment.GetEnvironmentVariable("PROBE_FONT")!,"Content"));var font=fonts.Load<SpriteFont>("UiFont");Set("_font",font);Set("_ui",Activator.CreateInstance(typeof(Game1).GetField("_ui",F)!.FieldType,batch,pixel,font)!);Set("_conquestAudioUnavailable",true);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("BeginConquestOverview");} protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
 var cases=new[]{(0,0d,false),(0,.325d,false),(1,.5d,true),(2,.325d,false),(3,0d,false),(6,.325d,false),(7,.5d,true),(8,.325d,false),(8,.65d,false)};
 var item=cases[frame];Set("_conquestStage",item.Item1);Set("_conquestAge",item.Item2);
 using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);base.Draw(t);
 var pixels=new Color[1920*1080];image.GetData(pixels);int red=pixels.Count(c=>c.R>c.G*3&&c.R>c.B*2);
 if(item.Item3 && red!=pixels.Length)throw new Exception("Closed curtain must cover all broadcast HUD pixels");
 if(item.Item1 is 0 or 2 or 6 or 8 && item.Item2==.325 && red<pixels.Length*.45)throw new Exception("Moving curtain missing");
 if(item.Item1==3 || item.Item1==0&&item.Item2==0 || item.Item1==8&&item.Item2==.65){if(red>pixels.Length*.4)throw new Exception("Curtain must be open");}
 GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,$"curtain-{frame}.png")))image.SaveAsPng(file,1920,1080);
 Console.WriteLine($"PASS stage={item.Item1} age={item.Item2} curtain pixels={red}");
 if(++frame==cases.Length){var node=w.Nodes.All[((ConquestBroadcast)Get("_conquestBroadcast")).Location];string key=(string)Call("ConquestLocationSceneryKey",node);if(key!=(string)Get("_conquestSceneryKey"))throw new Exception("Location image mismatch");Console.WriteLine("PASS actual location image loaded: "+key);CheckFolderTree();Exit();}
 }
 void CheckFolderTree(){
 Set("_world",new WorldPreview(GraphicsDevice));Set("_conquestOverview",false);Call("OpenFolderTree");Set("_domesticSlide",1f);Set("_folderScroll",0);
 var expanded=(System.Collections.Generic.HashSet<int>)Get("_expandedFolderCountries");
 if(!expanded.Contains(0))throw new Exception("Country must start expanded");
 Set("_pointer",new Point(306+177,324));Call("FolderTreeInput",new Microsoft.Xna.Framework.Input.MouseState(),true,false,true);
 if(expanded.Contains(0))throw new Exception("Folder icon must collapse country");
 Call("FolderTreeInput",new Microsoft.Xna.Framework.Input.MouseState(),true,false,true);
 if(!expanded.Contains(0))throw new Exception("Folder icon must expand country");
 Console.WriteLine("PASS folder icon accepts clicks beyond former plus-only hitbox");
 var rows=(System.Collections.Generic.List<(int Kind,int Id,int Depth,int Owner,string Label)>)Get("_inspectorFolderRows");
 int index=rows.FindIndex(r=>r.Kind==2&&w.Nodes.All[r.Id].Functional);if(index<0)throw new Exception("No node row");int target=rows[index].Id;
 Set("_folderScroll",index);Set("_pointer",new Point(306+600,324));Call("FolderTreeInput",new Microsoft.Xna.Framework.Input.MouseState(),true,false,true);
 if((int)Get("_populationNode")!=-1||!(bool)Get("_folderTreeOpen")||(int)Get("_folderTreeKind")!=2||(int)Get("_folderTreeId")!=target)throw new Exception("Node list must focus exact node without opening inspector");
 Console.WriteLine("PASS node label focuses exact node on map and keeps tree open");
 }}