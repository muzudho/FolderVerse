using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
sealed class CountrySettingsUi:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 void Click(int x,int y){var c=(Rectangle)Call("CanvasBounds");foreach(var s in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(c.X+x*c.Width/1920,c.Y+y*c.Height/1080,0,s,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);}
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetParticipantCounts(1,59);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);}
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==0){Click(1680,43);Click(1550,376);Check((bool)Get("_countrySettingsOpen"),"System menu opens settings through real input");}
  if(frame==1){Click(765,910);Check((int)Get("_countrySettingsRuler")==58,"Bottom row can select country 59");Click(1200,335);Click(1430,457);Click(1535,666);Click(1605,786);Click(1535,786);Check(w.IsHumanPlayer(58) && w.ColorIndices[58]==6 && w.CountrySecondaryColor(58)==7 && w.CountryPattern(58)==1,"Real controls change country 59");Click(1200,786);}
  if(frame==2){Click(1200,457);int secondary=w.CountrySecondaryColor(58);Click(1535,786);Check(w.CountryPattern(58)==0 && w.CountrySecondaryColor(58)==secondary,"Solid pattern hides and disables the second palette without discarding its color");Click(1500,272);Check(!w.ShowComputerOperations(58),"Human toggle is disabled");var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  if(frame==3){Click(1200,520);Check(w.CountryPattern(58)==3 && w.CountrySecondaryColor(58)==1,"Scaled input selects grid and restores the second color");
   Click(1580,335);Click(1500,272);Check(w.ShowComputerOperations(58) && !w.ShowComputerOperations(0),"Scaled toggle affects only the selected CPU");
   Click(1200,335);Click(1500,272);Check(w.ShowComputerOperations(58),"Human mode preserves the saved preference");Click(1580,335);}
  if(frame==4){Click(1780,110);Check(!(bool)Get("_countrySettingsOpen"),"Close dismisses modal");}
  if(frame==5){Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-100d);foreach(var n in w.Nodes.All)n.Owner=58;Array.Fill(w.Owners,58);var net=(CubeNet)Get("_net");net.ShowCountryColors=true;net.ShowRobots=net.ShowBatteries=net.ShowMapSymbols=net.ShowEquator=net.ShowRoutes=false;Set("_statusGlobe",false);var world=(WorldPreview)Get("_world");world.CountryColors=true;world.ShowSetup(w,true);}
  if(frame==6)Set("_statusGlobe",true);
  if(frame==7){Set("_screen",Enum.Parse(Get("_screen").GetType(),"CastReview"));((WorldPreview)Get("_world")).ShowSetup(w,false);}
  if(frame==8){Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("OpenCountrySettings");Click(180,225);Click(1580,335);Click(765,910);Click(1580,335);Click(1780,110);Check(w.ComputerOnly && (bool)Get("_autoPlay"),"Closing after removing the last human starts automatic play");}
  if(frame==9){Click(10,10);Check((bool)Get("_autoPaused"),"Automatic play opens the paused system menu");Click(1550,376);Click(180,225);Click(1200,335);Click(1780,110);Click(1260,43);Check(w.PlayerSlot==0 && !w.ComputerOnly && !(bool)Get("_autoPlay") && !(bool)Get("_autoPaused"),"Resuming after adding a human selects that controller");}
  if(frame==11){
   var battle=new BattleState(9){TargetNode=w.Nodes.Current(0).Id,Defender=0};
   for(int army=0;army<4;army++){
    w.SetCountryAppearance(army,6,1,army+1);battle.Armies.Add(new(){Owner=army,Side=army});
    for(int row=0;row<4;row++){
     long id=100+army*4+row;var robot=new Robot(id,army,RobotParts.Complete){CheapParts=row%2==1?RobotParts.Complete:0,DamagedParts=row>=2?RobotParts.Complete:0};
     battle.Units.Add(new(){Id=id,Owner=army,Role=BattleRole.Soldier,Position=new(1+army*2,1+row*2),Facing=(BattleFacing)army,Robot=robot});
    }
   }
   battle.Capture(BattlePhase.Choose);Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Call("FinishTransport");Set("_battleIntroAge",10d);Set("_lastCutInPhase","戦闘フェーズ");Set("_cutInStarted",-100d);
  }
  if(frame==12){
   w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);
   for(int ruler=1;ruler<w.ActiveCount;ruler+=2)w.ToggleComputerOperations(ruler);
   Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
   ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
   Call("EndDomesticPhase",false);
   var domestic=(Node[])Get("_computerDomesticNodes");
   Check(domestic is {Length:>0} && w.PlayerSlot==domestic[0].Owner && w.Campaign.Phase==CampaignPhase.Domestic,
    "CPU view is selected before domestic actions and delivery waits");
   var node=domestic[0];
   var stock=w.Robots.Nodes[node.Id];
   foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})
    stock.Add(w.Robots.Create(node.Owner,part));
   var beforePlayback=stock.Robots.ToArray();
   Call("UpdateComputerDomestic",.8d,false);
   Check((int)Get("_populationNode")==node.Id,"CPU selects its projected node");
   Call("UpdateComputerDomestic",.8d,false);
   Check((bool)Get("_nodeAssemblyOpen"),"CPU opens the normal hangar");
   Call("UpdateComputerDomestic",.8d,false);
   Check(stock.Robots.SequenceEqual(beforePlayback),"Opening and viewing the hangar leaves physical assembly to delivery");
   base.Draw(time);
   var pp=GraphicsDevice.PresentationParameters;
   var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];
   GraphicsDevice.GetBackBufferData(pixels);
   using(var capture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight))
   using(var output=File.Create(Path.Combine(AppContext.BaseDirectory,"audit-latest-computer-domestic.png")))
   {capture.SetData(pixels);capture.SaveAsPng(output,capture.Width,capture.Height);}
   Call("UpdateComputerDomestic",.8d,false);
   var green=(Dictionary<int,HashSet<int>>)Get("_nodeChecksByPlayer");
   Check(green.TryGetValue(node.Owner,out var marked) && marked.Contains(node.Id),"CPU clicks green after domestic work");
   base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
   Call("UpdateComputerDomestic",1d,false);
   Call("UpdateComputerDomestic",0d,true);
   Check(!green.ContainsKey(node.Owner),"CPU green checks expire before delivery");
   Check(w.PlayerSlot==0 && w.Campaign.Phase==CampaignPhase.Delivery,"Domestic playback restores the human before delivery");
   Call("OpenMilitaryPhase");
   var visible=(March[])Get("_visibleComputerMarches");
   Check(visible is {Length:>0} && visible.All(o=>o.Ruler%2==1),"Only opted-in CPUs show actual campaign orders");
   Check(w.Campaign.Phase==CampaignPhase.Military,"Military orders wait for playback");
   Check(w.PlayerSlot==visible[0].Ruler,"CPU view is selected before military actions");
   for(int step=0;step<3;step++)Call("UpdateComputerMarches",1.1d,false);
   Check((int)Get("_invasionPicker")>=0,"Computer uses the normal sortie picker for a supported attack");
  }
  if(frame==13){
   int reservations=w.Campaign.PlannedOrders.Count;
   var order=((March[])Get("_visibleComputerMarches"))[0];
   var draft=(HashSet<long>)Get("_invasionDraft");
   Check(draft.Count==0,"Computer opens the picker before selecting robots");
   for(int selected=1;selected<=order.Fighters;selected++){
    Call("UpdateComputerMarches",1.1d,false);
    Check((int)Get("_invasionPicker")>=0 && draft.Count==selected && w.Campaign.PlannedOrders.Count==reservations,
     "Computer selects one robot per click without adding a reservation");
    base.Draw(time);
   }
   Check(order.RobotIds==null || draft.SetEquals(order.RobotIds),"Computer selects the planned robots");
   Call("UpdateComputerMarches",1.1d,false);
   Check((int)Get("_invasionPicker")==-1 && w.Campaign.PlannedOrders.Count==reservations,"Normal picker confirmation closes without duplicating the queued order");
   Call("UpdateComputerMarches",1.1d,false);
   var redChecks=(Dictionary<int,HashSet<int>>)Get("_warChecksByPlayer");
   Check(redChecks.TryGetValue(order.Ruler,out var targets) && targets.Contains(order.TargetNode),
    "Computer clicks the red check after confirming");
   Call("UpdateComputerMarches",1.1d,false);
   Call("UpdateComputerMarches",0d,true);
   Check(!redChecks.ContainsKey(order.Ruler),"Computer checks expire when playback finishes");
   Check(w.PlayerSlot==0 && w.Campaign.Phase==CampaignPhase.Domestic,"Skipping restores the human and resolves the military turn");}
  base.Draw(time);
  if(frame==10){
   var batch=(SpriteBatch)Get("_spriteBatch");var c=(Rectangle)Call("CanvasBounds");batch.Begin(transformMatrix:Matrix.CreateScale(c.Width/1920f,c.Height/1080f,1)*Matrix.CreateTranslation(c.X,c.Y,0));
   batch.Draw((Texture2D)Get("_pixel"),new Rectangle(430,160,1100,620),new Color(18,36,48));
   var glyph=typeof(Game1).GetMethods(F).Single(m=>m.Name=="DrawRobotGlyph" && m.GetParameters().Length==5);
   var robot=w.Robots.Create(58,RobotParts.Complete);
   for(int i=0;i<4;i++){var sample=robot with {CheapParts=i%2==1?RobotParts.Complete:0,DamagedParts=i>=2?RobotParts.Complete:0};glyph.Invoke(this,new object[]{new Rectangle(470+i*260,250,180,320),sample,true,1f,null});}
   batch.End();
  }
  if(frame is 1 or 2 or 3 or 5 or 6 or 7 or 10 or 11 or 12){var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"country-settings-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
  if(frame++==13){Console.WriteLine("PASS: settings input, automatic/manual transitions, country patterns and opt-in computer military playback.");Exit();}
 }
}
