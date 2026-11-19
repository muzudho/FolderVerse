using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
try{using var game=new MapProbe(args[0]);game.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
sealed class MapProbe(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;SpriteBatch batch=null!;WorldSetup w=null!;RobotShipment shipment=null!;Color[] baseline=null!;int frame;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 protected override void LoadContent(){
 w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.Relations.Reset();w.SelectPlayer(0);w.Relations.Leader=0;foreach(var n in w.Nodes.All)n.Owner=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);var r=w.Robots;
 int source=w.Nodes.All.First(n=>n.Functional && FunctionalDeliveryRoutes.Targets(w,n.Id).Length>0).Id;int target=FunctionalDeliveryRoutes.Targets(w,source)[0];
 var robot=(r.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=RobotMaterial.Maple,BodyMaterial=RobotMaterial.Cardboard,LegsMaterial=RobotMaterial.SoftVinyl,IntegratedParts=RobotParts.Head|RobotParts.Body,DamagedParts=RobotParts.Body}).Materialize();r.ConfigureFactories(w);var line=new RobotWorkshop{IntakeTarget=target};if(!(bool)typeof(RobotWorld).GetMethod("QueueProduction",F)!.Invoke(r,new object[]{line,source,0,robot,0})!)throw new Exception("Queue failed");w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;r.ConfigureFactories(w);typeof(RobotWorld).GetMethod("ResolveProductionIntakes",F)!.Invoke(r,new object[]{w});var playback=(IEnumerable<(RobotShipment,int)>)typeof(RobotWorld).GetProperty("IntakePlayback",F)!.GetValue(r)!;typeof(RobotTransport).GetMethod("AppendPlannedTransfers",F)!.Invoke(r.Transport,new object[]{playback});shipment=r.Transport.LastTransfers.Single();if(r.ProductionIntakes.Count!=0 || shipment.Visual!=r.Nodes[target].Robots.Single() || shipment.Visual!.Role!=RobotRole.Captain || shipment.Visual.IntegratedParts!=(RobotParts.Head|RobotParts.Body))throw new Exception("Saved remote product snapshot differs from arrival");Console.WriteLine("PASS queued remote product save/resolve keeps mixed captain integration and exact arrival snapshot");
 typeof(ConquestCampaign).GetField("<Phase>k__BackingField",F)!.SetValue(w.Campaign,CampaignPhase.Delivery);w.SetCountryAppearance(0,6,12,1);w.SetCountryEmblem(0,1);Set("_setup",w);Set("_robotScenes",Array.Empty<BattleState>());
 var travel=(Dictionary<(int,int),float>)typeof(Game1).GetField("_propagationTravel",F)!.GetValue(this)!;travel[(source,target)]=.5f;
 var net=(CubeNet)typeof(Game1).GetField("_net",F)!.GetValue(this)!;net.SetCenter(w,w.Cells[w.Nodes.All[source].Cell].Face);net.ShowRobots=false;
 batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");var font=Fonts.LoadUi(Content);Set("_font",font);var type=typeof(Game1).GetField("_ui",F)!.FieldType;Set("_ui",Activator.CreateInstance(type,batch,pixel,font)!);
 }
 protected override void Update(GameTime t){AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_ART_CONTENT_BASE")??AppContext.BaseDirectory);}
 protected override void Draw(GameTime t){
 using var target=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(16,35,46));
 var arrival=w.Robots.Nodes[shipment.Target].Robots.Single();var material=(RobotMaterial)(frame%9);var current=arrival with{HeadMaterial=material,BodyMaterial=material,LegsMaterial=material};
 var area=new Rectangle(600,250,48,72);batch.Begin();typeof(Game1).GetMethod("DrawHangarBay",F)!.Invoke(this,new object[]{new Rectangle(570,225,420,600),false,false});var drawn=typeof(Game1).GetMethod("DrawBattleSoldierArt",F)!.Invoke(this,new object[]{area,current,BattleRole.Captain,(BattleFacing)(frame/9),true,1f});if(!(bool)drawn!)throw new Exception("battle material art unavailable");batch.End();GraphicsDevice.SetRenderTarget(null);
 var pixels=new Color[1920*1080];target.GetData(pixels);if(frame%9==0)baseline=pixels;else{int diff=0,outside=0;for(int y=0;y<1080;y++)for(int x=0;x<1920;x++)if(pixels[y*1920+x]!=baseline[y*1920+x]){diff++;if(!area.Contains(x,y))outside++;}if(diff<20||outside!=0)throw new Exception($"arrival variant {frame} diff={diff},outside={outside}");Console.WriteLine($"PASS integrated battle material variant{frame}: {diff} differences inside robot only");}
 using(var file=File.Create(Path.Combine(output,$"arrival-{frame}.png")))target.SaveAsPng(file,1920,1080);if(++frame==36)Exit();
 }
}










