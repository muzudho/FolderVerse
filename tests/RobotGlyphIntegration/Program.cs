using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new GlyphProbe(args[0]);game.Run();
sealed class GlyphProbe:Game1
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 readonly string output;SpriteBatch batch=null!;Texture2D pixel=null!;bool drawn;
 void Set(string name,object value)=>typeof(Game1).GetField(name,Flags)!.SetValue(this,value);
 object Get(string name)=>typeof(Game1).GetField(name,Flags)!.GetValue(this)!;
 public GlyphProbe(string output){this.output=output;var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;}
 protected override void LoadContent(){
  // Deliberately omit base.LoadContent: no autosave, achievements, music or game state loading.
  var world=new WorldSetup();world.SetParticipantCounts(1,1);world.SetWorld(0);world.SetPlacement(2);world.SelectTurnRuler(0);world.SetCountryAppearance(0,6,12,1);world.SetCountryEmblem(0,1);Set("_setup",world);
  batch=new(GraphicsDevice);pixel=new(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");var font=Fonts.LoadUi(Content);Set("_font",font);
  var type=typeof(Game1).GetField("_ui",Flags)!.FieldType;Set("_ui",Activator.CreateInstance(type,batch,pixel,font)!);
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(drawn)return;drawn=true;GraphicsDevice.Clear(new Color(25,36,48));batch.Begin();
  var draw=typeof(Game1).GetMethods(Flags).Single(m=>m.Name=="DrawRobotGlyph" && m.GetParameters().Length==5);
  var robots=new[]{new Robot(0,0,RobotParts.Body),new Robot(0,0,RobotParts.Legs),new Robot(0,0,RobotParts.Complete){HeadOrigin=1},new Robot(0,0,RobotParts.Complete){DamagedParts=RobotParts.Head},new Robot(0,0,RobotParts.Complete){CheapParts=RobotParts.Head},new Robot(0,0,RobotParts.Complete,RobotRole.Captain)};
  for(int i=0;i<robots.Length;i++)
  {
   draw.Invoke(this,new object[]{new Rectangle(35+i*150,120,105,180),robots[i],true,i==2?.4f:1f,null});
   if(i==1 && (Get("_robotTorsoTextures")==null || Get("_robotLegTextures")==null))
    throw new Exception("Standalone torso and legs did not load their artwork");
  }
  batch.End();
  if(Get("_robotHeadTextures")==null)throw new Exception("Regular glyph did not load the new head");
  var samplePixels = new Color[960 * 540];
  GraphicsDevice.GetBackBufferData(samplePixels);
  using (var samples = new Texture2D(GraphicsDevice, 960, 540))
  {
   samples.SetData(samplePixels);
   using var file = File.Create(Path.Combine(Path.GetDirectoryName(output)!, "pp-part-samples.png"));
   samples.SaveAsPng(file, 960, 540);
  }
  GraphicsDevice.Clear(new Color(25,36,48));
  var battleParts = typeof(Game1).GetMethod("DrawBattleRobotParts", Flags)!;
  var mixed = new Robot(50, 0, RobotParts.Complete) { HeadOrigin = 1 };
  for (int phase = 0; phase < 2; phase++)
  {
   Set("_battleIntroAge", phase == 0 ? 0d : 2d);
   batch.Begin();
   battleParts.Invoke(this, new object[]{new Rectangle(80 + phase * 160,100,62,85),mixed,0,1f});
   batch.End();
  }
  var introPixels = new Color[960 * 540];
  GraphicsDevice.GetBackBufferData(introPixels);
  int VisiblePixels(Rectangle area) => Enumerable.Range(area.Top,area.Height)
   .Sum(y => Enumerable.Range(area.Left,area.Width).Count(x => introPixels[y*960+x] != new Color(25,36,48)));
  if (VisiblePixels(new(80,100,62,33)) <= VisiblePixels(new(240,100,62,33)) ||
      VisiblePixels(new(240,135,62,50)) == 0)
   throw new Exception("Battle intro did not fade foreign head while keeping native body");
  using (var introImage = new Texture2D(GraphicsDevice,960,540))
  {
   introImage.SetData(introPixels);
   using var file = File.Create(Path.Combine(Path.GetDirectoryName(output)!, "pp-battle-intro.png"));
   introImage.SaveAsPng(file,960,540);
  }
  Console.WriteLine("PASS battle intro uses shared artwork and fades foreign parts independently.");
  var battleWorld = (WorldSetup)Get("_setup");
  battleWorld.SetCountryAppearance(1, 9, 3, 4);
  var uniform = typeof(Game1).GetMethod("DrawBattleUniformPatch", Flags)!;
  var uniformBounds = new Rectangle(30, 30, 62, 85);
  GraphicsDevice.Clear(Color.Black);
  batch.Begin();
  uniform.Invoke(this, new object[]{uniformBounds, uniformBounds, uniformBounds, mixed, RobotParts.Head, 1f});
  batch.End();
  GraphicsDevice.GetBackBufferData(introPixels);
  var inks = Enumerable.Range(uniformBounds.Top, uniformBounds.Height)
   .SelectMany(y => Enumerable.Range(uniformBounds.Left, uniformBounds.Width).Select(x => introPixels[y * 960 + x]))
   .ToHashSet();
  if (!inks.SetEquals(new[]{battleWorld.OwnerColor(1), WorldSetup.Colors[battleWorld.CountrySecondaryColor(1)]}))
   throw new Exception("Battle uniform mixed flag colors from different countries");
  Console.WriteLine("PASS battle uniform uses both colors of the part's country of origin.");
  GraphicsDevice.Clear(Color.Black);
  batch.Begin();
  var plain = new Robot(51, 0, RobotParts.Complete);
  var reserved = plain with { DefenseOnly = true };
  draw.Invoke(this,new object[]{new Rectangle(100,100,100,170),plain,false,1f,null});
  draw.Invoke(this,new object[]{new Rectangle(300,100,100,170),reserved,false,1f,null});
  var shipping = plain with { LongDeliveryNumber = 1 };
  draw.Invoke(this,new object[]{new Rectangle(500,100,100,170),shipping,false,1f,null});
  batch.End();
  GraphicsDevice.GetBackBufferData(introPixels);
  var keyShine = new Color(231,235,237);
  if (introPixels[115*960+173] == keyShine || introPixels[115*960+373] != keyShine ||
      introPixels[115*960+573] != keyShine)
   throw new Exception("Windup key must appear only on the reserved glyph");
  using (var reservationImage = new Texture2D(GraphicsDevice,960,540))
  {
   reservationImage.SetData(introPixels);
   using var file = File.Create(Path.Combine(Path.GetDirectoryName(output)!, "pp-windup-comparison.png"));
   reservationImage.SaveAsPng(file,960,540);
  }
  Console.WriteLine("PASS only the reserved glyph displays the neutral windup key.");
  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin();
  var queen = typeof(Game1).GetMethod("DrawQueenArt", Flags)!;
  var facings = new[]{BattleFacing.South, BattleFacing.East, BattleFacing.North, BattleFacing.West};
  for (int index = 0; index < facings.Length; index++)
  {
   var queenBounds = new Rectangle(100 + 80 * index,100,62,85);
   if (!(bool)queen.Invoke(this,new object[]{queenBounds,facings[index],0,1f})!)
    throw new Exception($"Queen art missing for {facings[index]}");
  }
  queen.Invoke(this,new object[]{new Rectangle(100,200,62,85),BattleFacing.South,1,1f});
  batch.End();
  GraphicsDevice.GetBackBufferData(introPixels);
  for (int index = 0; index < facings.Length; index++)
   if (introPixels[140*960+131+80*index] == new Color(25,36,48) ||
       introPixels[100*960+111+80*index] != new Color(25,36,48))
    throw new Exception($"Queen art must preserve figure and transparent corner for {facings[index]}");
  bool flagChanged = Enumerable.Range(172, 13).Any(y => Enumerable.Range(142, 20)
   .Any(x => introPixels[y * 960 + x] != introPixels[(y + 100) * 960 + x]));
  if (!flagChanged || introPixels[140 * 960 + 131] != introPixels[240 * 960 + 131])
   throw new Exception("Changing the Queen's country must change the flag and preserve the face");
  using (var queenImage = new Texture2D(GraphicsDevice,960,540))
  {
   queenImage.SetData(introPixels);
   using var file = File.Create(Path.Combine(Path.GetDirectoryName(output)!, "queen-battle-flags.png"));
   queenImage.SaveAsPng(file,960,540);
  }
  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin();
  var soldierArt = typeof(Game1).GetMethod("DrawBattleSoldierArt", Flags)!;
  for (int index = 0; index < 3; index++)
  {
   bool captain = index == 1;
   var soldier = new Robot(60 + index, 0, RobotParts.Complete, captain ? RobotRole.Captain : RobotRole.Soldier);
   if (!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(100 + 80 * index,100,62,85),soldier,
        captain ? BattleRole.Captain : BattleRole.Soldier,BattleFacing.South,index != 2,1f})!)
    throw new Exception("Battle soldier art failed to load");
  }
  var wearParts = new[]{(RobotParts)0, RobotParts.Head, RobotParts.Body, RobotParts.Legs};
  for (int index = 0; index < wearParts.Length; index++)
  {
   var soldier = new Robot(70 + index, 0, RobotParts.Complete) { DamagedParts = wearParts[index] };
   if (!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(100 + 80 * index,215,62,85),soldier,
        BattleRole.Soldier,BattleFacing.South,true,1f})!)
    throw new Exception($"Battle art fell back with wear on {wearParts[index]}");
  }
  for (int index = 0; index < 6; index++)
  {
   bool west = index % 2 == 1;
   bool captain = index is 2 or 3 or 5;
   var facing = index >= 4 ? BattleFacing.North : west ? BattleFacing.West : BattleFacing.East;
   var soldier = new Robot(80 + index, 0, RobotParts.Complete, captain ? RobotRole.Captain : RobotRole.Soldier);
   var battle = new BattleState(1);
   var liveUnit = new BattleUnit { Id=soldier.Id, Robot=soldier, Owner=0,
    Role=captain ? BattleRole.Captain : BattleRole.Soldier, Facing=facing, Rifles=1 };
   battle.Units.Add(liveUnit);
   battle.Capture(BattlePhase.Choose);
   liveUnit.Facing=BattleFacing.South;
   liveUnit.Rifles=0;
   battle.Capture(BattlePhase.Consume);
   var frame = battle.Frames[0].Units.Single();
   if (frame.Facing!=facing || frame.Rifles!=1 || battle.Frames[1].Units.Single().Rifles!=0)
    throw new Exception("Historical battle facing or rifle count was overwritten");
   if (!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(100 + 80 * index,330,62,85),frame.Robot,
        frame.Role,frame.Facing,frame.Rifles>0,1f})!)
    throw new Exception($"Soldier art failed to load for {facing}");
  }
  batch.End();
  GraphicsDevice.GetBackBufferData(introPixels);
  var background = new Color(25,36,48);
  for (int index = 0; index < 6; index++)
  {
   int left = 100 + 80 * index;
   foreach (int y in new[]{363, 385, 405})
    if (Enumerable.Range(left + 20, 22).All(x => introPixels[y * 960 + x] == background))
     throw new Exception($"Missing side robot part at y={y} for sample {index}");
  }
  bool rifleChanged = Enumerable.Range(121, 51).Any(y => Enumerable.Range(146, 8)
   .Any(x => introPixels[y * 960 + x] != introPixels[y * 960 + x + 160]));
  if (!rifleChanged)
   throw new Exception("The toy rifle must disappear when the unit has no rifle remaining");
  for (int index = 1; index < wearParts.Length; index++)
  {
   int changed = Enumerable.Range(215, 85).Sum(y => Enumerable.Range(100, 62)
    .Count(x => introPixels[y * 960 + x] != introPixels[y * 960 + x + 80 * index]));
   if (changed < 8)
    throw new Exception($"Wear on {wearParts[index]} is missing at battle size");
  }
  using (var soldierImage = new Texture2D(GraphicsDevice,960,540))
  {
   soldierImage.SetData(introPixels);
   using var file = File.Create(Path.Combine(Path.GetDirectoryName(output)!, "pp-battle-wear.png"));
   soldierImage.SaveAsPng(file,960,540);
  }
  var visible=typeof(Game1).GetMethod("VisibleHangarSlots",BindingFlags.Static|BindingFlags.NonPublic)!;
  var cell=typeof(Game1).GetMethod("HangarCell",BindingFlags.Static|BindingFlags.NonPublic)!;
  foreach(int capacity in new[]{1,6,7,12,59,60}){
   var all=new List<int>();
   for(int page=0;page<(capacity+5)/6;page++){
    var slots=((IEnumerable<int>)visible.Invoke(null,new object[]{capacity,page})!).ToArray();
    if(slots.Length>6)throw new Exception("Too many visible slots");all.AddRange(slots);
    foreach(int slot in slots){var box=(Rectangle)cell.Invoke(null,new object[]{slot})!;
     if(box.Left<54 || box.Right>904 || box.Bottom>850)throw new Exception("Six-slot cell outside inspector");
     if(slots.Any(other=>other!=slot && box.Intersects((Rectangle)cell.Invoke(null,new object[]{other})!)))throw new Exception("Visible cells overlap");
    }
   }
   if(!all.SequenceEqual(Enumerable.Range(0,capacity)))throw new Exception("Paging duplicated or lost physical slot IDs");
  }
  Console.WriteLine("PASS six-slot pages: capacities 1/6/7/12/59/60 cover physical IDs once, inspector containment and no overlap.");
  var w=(WorldSetup)Get("_setup");int node=w.Nodes.Current(w.PlayerSlot)!.Id;
  typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{12});Set("_populationNode",node);
  Set("_hangarPage",0);Set("_hangarDrag",(long?)123);
  var pageClick=typeof(Game1).GetMethod("HangarPageClick",Flags)!;
  pageClick.Invoke(this,new object[]{new Point(830,275)});
  if((int)Get("_hangarPage")!=1 || Get("_hangarDrag")!=null)throw new Exception("Next page must expose slots6-11 and cancel held drag");
  pageClick.Invoke(this,new object[]{new Point(830,275)});
  if((int)Get("_hangarPage")!=1)throw new Exception("Next page exceeded capacity");
  pageClick.Invoke(this,new object[]{new Point(680,275)});
  if((int)Get("_hangarPage")!=0)throw new Exception("Previous page did not return to slots0-5");
  Console.WriteLine("PASS actual page-button input: next/previous clamp and held-drag cancellation.");
  var head=new Robot(100001,0,RobotParts.Head);var body=new Robot(100002,0,RobotParts.Body);
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{new[]{head,body}});
  w.Robots.PartPlanning.Invalidate();w.HumanPlayers.Add(0);Set("_nodeAssemblyOpen",true);
  var mapping=new long?[12];mapping[6]=head.Id;mapping[7]=body.Id;Set("_hangarSlots",mapping);Set("_hangarPage",1);
  var input=typeof(Game1).GetMethod("NodeAssemblyLocalInput",Flags)!;
  var partBox=typeof(Game1).GetMethod("HangarPartBounds",BindingFlags.Static|BindingFlags.NonPublic)!;
  var pressed=new Microsoft.Xna.Framework.Input.MouseState(0,0,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
  Set("_pointer",((Rectangle)partBox.Invoke(null,new object[]{6,0})!).Center);
  input.Invoke(this,new object[]{pressed,true,false});
  if((long?)Get("_hangarDrag")!=head.Id || (int)Get("_hangarSource")!=6)throw new Exception("Page2 head press selected another robot/slot");
  Set("_pointer",((Rectangle)cell.Invoke(null,new object[]{7})!).Center);Set("_previousMouse",pressed);
  input.Invoke(this,new object[]{new Microsoft.Xna.Framework.Input.MouseState(),false,false});
  var reservation=w.Robots.PartPlanning.Orders.Single();
  if(reservation.RobotId!=head.Id || reservation.TargetRobotId!=body.Id || reservation.Parts!=RobotParts.Head)throw new Exception("Page2 drop reserved wrong source or destination");
  if(Get("_hangarDrag")!=null)throw new Exception("Released drag remained active");
  Console.WriteLine("PASS actual page2 part press/release reserves physical slot6 head100001 into slot7 body100002.");
  w.Robots.PartPlanning.Orders.Clear();w.Robots.PartPlanning.Invalidate();
  var legs = new Robot(100003, 0, RobotParts.Legs);
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{new[]{legs,body}});
  w.Robots.PartPlanning.Invalidate();
  mapping[6] = legs.Id;
  Set("_hangarSlots", mapping);
  Set("_pointer", ((Rectangle)partBox.Invoke(null,new object[]{6,2})!).Center);
  input.Invoke(this,new object[]{pressed,true,false});
  if ((long?)Get("_hangarDrag") != legs.Id || (RobotParts)Get("_hangarPart") != RobotParts.Legs)
   throw new Exception("Page2 leg press selected another robot or part");
  Set("_pointer", ((Rectangle)partBox.Invoke(null,new object[]{7,2})!).Center);
  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin(transformMatrix:Matrix.CreateScale(.5f));
  typeof(Game1).GetMethod("DrawNodeAssembly",Flags)!.Invoke(this,null);
  batch.End();
  var dragPixels = new Color[960 * 540];
  GraphicsDevice.GetBackBufferData(dragPixels);
  using (var dragImage = new Texture2D(GraphicsDevice, 960, 540))
  {
   dragImage.SetData(dragPixels);
   using var file = File.Create(Path.Combine(Path.GetDirectoryName(output)!, "inspector-pp-leg-drag.png"));
   dragImage.SaveAsPng(file, 960, 540);
  }
  Set("_previousMouse",pressed);
  input.Invoke(this,new object[]{new Microsoft.Xna.Framework.Input.MouseState(),false,false});
  var legReservation = w.Robots.PartPlanning.Orders.Single();
  if (legReservation.RobotId != legs.Id || legReservation.TargetRobotId != body.Id ||
      legReservation.Parts != RobotParts.Legs || Get("_hangarDrag") != null)
   throw new Exception("Page2 leg drop reserved the wrong assembly");
  Console.WriteLine("PASS actual page2 leg drag renders and reserves the selected part.");
  w.Robots.PartPlanning.Orders.Clear();w.Robots.PartPlanning.Invalidate();
  var complete=Enumerable.Range(0,6).Select(i=>new Robot(200001+i,0,RobotParts.Complete)).ToArray();
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{complete});
  Set("_hangarSlots",complete.Select(r=>(long?)r.Id).Concat(new long?[6]).ToArray());Set("_hangarPage",0);Set("_hangarReservation",HangarReservation.Defense);
  Set("_pointer",((Rectangle)cell.Invoke(null,new object[]{1})!).Center);
  input.Invoke(this,new object[]{pressed,true,false});
  var preview=w.Robots.PartPlanning.Preview(w);
  if(!preview.Nodes[node].Robots.Single(r=>r.Id==200002).DefenseOnly || preview.Nodes[node].Robots.Any(r=>r.Id!=200002 && r.DefenseOnly))throw new Exception("Defense click reserved another displayed robot");
  Console.WriteLine("PASS actual six-slot defense click reserves only second robot200002.");
  Set("_pointer",Point.Zero);GraphicsDevice.Clear(new Color(25,36,48));batch.Begin(transformMatrix:Matrix.CreateScale(.5f));
  typeof(Game1).GetMethod("DrawNodeAssembly",Flags)!.Invoke(this,null);batch.End();
  var data=new Color[960*540];GraphicsDevice.GetBackBufferData(data);using var image=new Texture2D(GraphicsDevice,960,540);image.SetData(data);using(var f=File.Create(output))image.SaveAsPng(f,960,540);
  w.Robots.PartPlanning.Orders.Clear();w.Robots.PartPlanning.Invalidate();
  typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{7});
  var foreign=new Robot(300001,1,RobotParts.Complete);
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{new[]{foreign}});
  var lastPage=new long?[7];lastPage[6]=foreign.Id;Set("_hangarSlots",lastPage);Set("_hangarPage",0);Set("_hangarReturnHomeMode",true);Set("_hangarReservation",HangarReservation.None);
  Set("_pointer",new Point(830,275));input.Invoke(this,new object[]{pressed,true,false});
  if((int)Get("_hangarPage")!=1 || !(bool)Get("_hangarReturnHomeMode"))throw new Exception("Return-home mode swallowed next-page click");
  Set("_pointer",((Rectangle)cell.Invoke(null,new object[]{6})!).Center);input.Invoke(this,new object[]{pressed,true,false});
  if(w.Robots.Nodes[node].Robots.Single().ReturnHomeCommander!=0)throw new Exception("Return-home click missed last-page robot");
  Set("_pointer",((Rectangle)cell.Invoke(null,new object[]{7})!).Center);input.Invoke(this,new object[]{pressed,true,false});
  if(w.Robots.Nodes[node].Robots.Single().ReturnHomeCommander!=0)throw new Exception("Nonexistent cell toggled visible robot");
  Set("_pointer",((Rectangle)cell.Invoke(null,new object[]{6})!).Center);input.Invoke(this,new object[]{pressed,true,false});
  if(w.Robots.Nodes[node].Robots.Single().ReturnHomeCommander.HasValue)throw new Exception("Second return-home click failed to cancel");
  Console.WriteLine("PASS return-home mode pages to single-slot final page; correct ID toggles; nonexistent cells do nothing.");
  var pairNodes=(int[])Get("_directNodes");pairNodes[0]=node;pairNodes[1]=node;
  var pages=(int[])Get("_directPages");pages[0]=pages[1]=0;
  var directClick=typeof(Game1).GetMethod("DirectPageClick",Flags)!;
  directClick.Invoke(this,new object[]{new Point(1840,275)});
  if(pages[0]!=0 || pages[1]!=1)throw new Exception("Right delivery page changed wrong inspector");
  directClick.Invoke(this,new object[]{new Point(1840,275)});
  if(pages[1]!=1)throw new Exception("Right delivery final page exceeded capacity7");
  directClick.Invoke(this,new object[]{new Point(510,275)});
  if(pages[0]!=1 || pages[1]!=1)throw new Exception("Left delivery page changed wrong inspector");
  Console.WriteLine("PASS independent delivery six-slot page buttons and capacity7 final-page clamp.");
  typeof(Game1).GetMethod("SyncLongSlots",Flags)!.Invoke(this,null);pages[0]=pages[1]=0;
  var longPage=typeof(Game1).GetMethod("LongPageClick",Flags)!;
  longPage.Invoke(this,new object[]{new Point(1780,255)});
  if(pages[0]!=0 || pages[1]!=1)throw new Exception("Long-distance final slot is inaccessible at capacity7");
  longPage.Invoke(this,new object[]{new Point(1780,255)});if(pages[1]!=1)throw new Exception("Long-distance page exceeded capacity");
  Console.WriteLine("PASS long-distance dedicated page buttons use six-slot final-page bounds.");
  int destination=w.Routes.Neighbors(w.Nodes.All[node]).Select(r=>w.Nodes.At(r.Target,r.Entry)).First(n=>n.Functional && n.Id!=node).Id;
  w.Nodes.All[destination].Owner=0;
  typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(w.Robots.Nodes[destination],new object[]{7});
  var cargo=new Robot(400001,0,RobotParts.Head);var receiver=new Robot(400002,0,RobotParts.Body);
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{new[]{cargo}});
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[destination],new object[]{new[]{receiver}});
  w.Robots.PartPlanning.Invalidate();pairNodes[0]=node;pairNodes[1]=destination;
  var directSlots=(long?[][])Get("_directSlots");directSlots[0]=new long?[7];directSlots[1]=new long?[7];directSlots[0][6]=cargo.Id;directSlots[1][6]=receiver.Id;
  pages[0]=pages[1]=1;Set("_longChoosingSource",false);Set("_longOrdersOpen",false);
  var longInput=typeof(Game1).GetMethod("LongPairInput",Flags)!;
  var directPart=typeof(Game1).GetMethod("DirectPart",BindingFlags.NonPublic|BindingFlags.Static)!;
  var directCell=typeof(Game1).GetMethod("DirectCell",BindingFlags.NonPublic|BindingFlags.Static)!;
  Set("_pointer",((Rectangle)directPart.Invoke(null,new object[]{0,6,0})!).Center);
  longInput.Invoke(this,new object[]{pressed,true,false});
  if(Get("_directDrag")==null)throw new Exception("Long-distance page2 did not pick up cargo");
  Set("_pointer",((Rectangle)directCell.Invoke(null,new object[]{1,6})!).Center);Set("_previousMouse",pressed);
  longInput.Invoke(this,new object[]{new Microsoft.Xna.Framework.Input.MouseState(),false,false});
  var shipment=w.Robots.LongDelivery.Orders.Single();
  if(shipment.RobotId!=cargo.Id || shipment.Receiver!=receiver.Id || shipment.TargetSlot!=6 || shipment.Source!=node || shipment.Target!=destination)throw new Exception("Long-distance drop reserved incorrect physical ID or slot");
  Console.WriteLine("PASS actual long-distance page2 drag reserves cargo400001 into receiver400002 at physical slot6.");
  for(int side=0;side<2;side++){
   var store=w.Robots.Nodes[pairNodes[side]];
   typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(store,new object[]{12});
   var extras=Enumerable.Range(0,5).Select(i=>new Robot(410000+side*10+i,0,i==0 && side==1?RobotParts.Head:RobotParts.Complete)).ToArray();
   typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(store,new object[]{store.Robots.Concat(extras).ToArray()});
   Array.Resize(ref directSlots[side],12);
   for(int i=0;i<5;i++)directSlots[side][7+i]=extras[i].Id;
  }
  var reverse=w.Robots.LongDelivery.Reserve(w,destination,node,410010,RobotParts.Head,null,0,6);
  Set("_pointer",Point.Zero);GraphicsDevice.Clear(new Color(25,36,48));batch.Begin(transformMatrix:Matrix.CreateScale(.5f));
  var pane=typeof(Game1).GetMethod("DrawLongPane",Flags)!;pane.Invoke(this,new object[]{0});pane.Invoke(this,new object[]{1});batch.End();
  var deliveryPixels=new Color[960*540];GraphicsDevice.GetBackBufferData(deliveryPixels);using(var deliveryImage=new Texture2D(GraphicsDevice,960,540)){deliveryImage.SetData(deliveryPixels);using var file=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"long-panes-tags.png"));deliveryImage.SaveAsPng(file,960,540);}
  Set("_pointer",((Rectangle)directPart.Invoke(null,new object[]{0,6,0})!).Center);
  longInput.Invoke(this,new object[]{pressed,true,false});
  if(w.Robots.LongDelivery.Orders.Single().Number!=reverse.Number)throw new Exception("Cancellation affected the wrong order");
  Console.WriteLine("PASS six loaded cells, stacked incoming/outgoing labels and independent order cancellation.");
  w.Robots.LongDelivery.Cancel(w.Robots,reverse.Number);
  var directInput=typeof(Game1).GetMethod("DirectPairInput",Flags)!;
  Set("_pointer",((Rectangle)directPart.Invoke(null,new object[]{0,6,0})!).Center);
  directInput.Invoke(this,new object[]{pressed,true,false});
  if(Get("_directDrag")==null)throw new Exception("Direct delivery page2 did not pick up cargo");
  Set("_pointer",((Rectangle)directCell.Invoke(null,new object[]{1,6})!).Center);
  Set("_previousMouse",pressed);
  directInput.Invoke(this,new object[]{new Microsoft.Xna.Framework.Input.MouseState(),false,false});
  var directReservation=w.Robots.PartPlanning.Orders.Single();
  if(directReservation.RobotId!=cargo.Id || directReservation.TargetRobotId!=receiver.Id || directReservation.Source!=node || directReservation.Target!=destination || directReservation.Parts!=RobotParts.Head)throw new Exception("Direct delivery page2 selected incorrect robots");
  Console.WriteLine("PASS actual direct-delivery page2 drag reserves head400001 into body400002.");
  Console.WriteLine("PASS Game1 glyph: complete/head-only/mixed origins/opacity/fallbacks render; actual shared glyph entry point verified.");
  using var globe = new WorldPreview(GraphicsDevice);
  var globeWorld = new WorldSetup();
  globeWorld.SetParticipantCounts(1,1);
  globeWorld.SetWorld(0);
  globeWorld.SetPlacement(2);
  globe.ShowSetup(globeWorld,false);
  int flatTriangles=globeWorld.Cells.Length*200;
  int terrainTriangles=(int)typeof(WorldPreview).GetField("_primitiveCount",Flags)!.GetValue(globe)!;
  if (terrainTriangles<=flatTriangles || terrainTriangles>=flatTriangles*5 ||
      WorldTerrain.Relief(.51f)!=0 || WorldTerrain.Relief(.54f)<=0)
   throw new Exception($"Unexpected terrain relief mesh size {terrainTriangles}, flat={flatTriangles}");
  Console.WriteLine($"PASS terrain relief uses {terrainTriangles} triangles, versus {flatTriangles} flat.");
  var globeArea = new Rectangle(65,20,830,500);
  var net = new CubeNet { ShowRobots=false, ShowBatteries=false };
  GraphicsDevice.Clear(new Color(25,36,48));
  globe.Draw(.55f,-.35f,markers:false,globeView:true,globeFrame:globeArea);
  batch.Begin();
  net.DrawGlobeLayers((UiPainter)Get("_ui"),globeWorld,globe,globeArea,.55f,-.35f,0);
  batch.End();
  var globePixels = new Color[960 * 540];
  GraphicsDevice.GetBackBufferData(globePixels);
  foreach (var ink in new Color[]{new(255,216,99),new(108,213,245),new(255,232,153),new(164,226,242)})
   if (!globePixels.Contains(ink)) throw new Exception($"Missing globe ribbon or puck color {ink}");
  using var globeImage = new Texture2D(GraphicsDevice,960,540);
  globeImage.SetData(globePixels);
  using (var globeFile = File.Create(Path.Combine(Path.GetDirectoryName(output)!,"globe-terrain-relief.png")))
   globeImage.SaveAsPng(globeFile,960,540);
  Console.WriteLine("PASS globe land/sea ribbons and pucks rendered.");
  net.SetCenter(globeWorld,4);
  net.FitAll(globeArea);
  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin();
  net.Draw((UiPainter)Get("_ui"),globeWorld,globeArea,-1,0,objects:false);
  net.DrawMapObjects((UiPainter)Get("_ui"),globeWorld,globeArea,0);
  batch.End();
  GraphicsDevice.GetBackBufferData(globePixels);
  if (!globePixels.Contains(new Color(255,232,153)) || !globePixels.Contains(new Color(164,226,242)))
   throw new Exception("The unfolded map did not draw both flat puck colors");
  globeImage.SetData(globePixels);
  using (var netFile = File.Create(Path.Combine(Path.GetDirectoryName(output)!,"net-terrain-shadows.png")))
   globeImage.SaveAsPng(netFile,960,540);
  foreach (var puckNode in globeWorld.Nodes.All.Take(10))
  {
   net.VisibleOperationNodes = new(){puckNode.Id};
   var pointer = net.NodePosition(globeWorld,puckNode,globeArea).ToPoint();
   if (net.HitNode(globeWorld,globeArea,pointer) != puckNode.Id)
    throw new Exception($"The unfolded puck at {puckNode.Id} could not be selected");
  }
  Console.WriteLine("PASS unfolded land/sea pucks draw and accept center clicks.");
  Exit();
 }
 protected override void UnloadContent(){(Get("_robotHeadTextures") as IDisposable)?.Dispose();batch?.Dispose();pixel?.Dispose();}
}
