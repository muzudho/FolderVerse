using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new GlyphProbe(args[0],args.Contains("--gallery"),args.Contains("--shipments"));game.Run();
sealed class GlyphProbe:Game1
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 readonly string output;readonly bool galleryOnly;readonly bool shipmentsOnly;SpriteBatch batch=null!;Texture2D pixel=null!;bool drawn;
 void Set(string name,object value)=>typeof(Game1).GetField(name,Flags)!.SetValue(this,value);
 object Get(string name)=>typeof(Game1).GetField(name,Flags)!.GetValue(this)!;
 public GlyphProbe(string output,bool galleryOnly,bool shipmentsOnly){this.output=output;this.galleryOnly=galleryOnly;this.shipmentsOnly=shipmentsOnly;var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;}
 protected override void LoadContent(){
  // Deliberately omit base.LoadContent: no autosave, achievements, music or game state loading.
  var world=new WorldSetup();world.SetParticipantCounts(1,1);world.SetWorld(0);world.SetPlacement(2);world.SelectTurnRuler(0);world.SetCountryAppearance(0,6,12,1);world.SetCountryEmblem(0,1);Set("_setup",world);
  batch=new(GraphicsDevice);pixel=new(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");var font=Fonts.LoadUi(Content);Set("_font",font);
  var type=typeof(Game1).GetField("_ui",Flags)!.FieldType;Set("_ui",Activator.CreateInstance(type,batch,pixel,font)!);
 }
 protected override void Update(GameTime time){}
 void CheckShipmentArt(){
  var world=(WorldSetup)Get("_setup");
  world.EquipmentMode=RobotEquipmentMode.Materials;
  var delivery=typeof(Game1).GetMethod("DrawHangarRobot",Flags)!;
  var manufacturing=typeof(Game1).GetMethod("DrawManufacturingProduct",Flags)!;
  var background=new Color(25,36,48);
  GraphicsDevice.Clear(background);
  batch.Begin();
  var materials=new[]{RobotMaterial.Cardboard,RobotMaterial.Maple};
  var parts=new[]{RobotParts.Head|RobotParts.Body,RobotParts.Body|RobotParts.Legs,RobotParts.Complete};
  for(int row=0;row<parts.Length;row++)
   for(int i=0;i<materials.Length;i++){
    var material=materials[i];
    var robot=new Robot(0,0,parts[row],row==2?RobotRole.Captain:RobotRole.Soldier){
     HeadMaterial=material,BodyMaterial=material,LegsMaterial=material,
     IntegratedParts=row==2?0:parts[row]};
    var bounds=new Rectangle(45+i*160,15+row*170,100,155);
    delivery.Invoke(this,new object[]{bounds,robot,1f,null});
    manufacturing.Invoke(this,new object[]{new Rectangle(bounds.X+480,bounds.Y,bounds.Width,bounds.Height),robot});
   }
  for(int i=0;i<materials.Length;i++){
   var material=materials[i];
   var compact=new Robot(0,0,RobotParts.Complete){
    HeadMaterial=material,BodyMaterial=material,LegsMaterial=material};
   delivery.Invoke(this,new object[]{new Rectangle(340+i*50,20,28,52),compact,1f,null});
  }
  batch.End();
  var pixels=new Color[960*540];
  GraphicsDevice.GetBackBufferData(pixels);
  int compactDifferences=0;
  for(int y=20;y<72;y++)
   for(int x=340;x<368;x++){
    int index=y*960+x;
    if(pixels[index]!=background && pixels[index+50]!=background && pixels[index]!=pixels[index+50])
     compactDifferences++;
   }
  if(compactDifferences<100)throw new Exception("Compact hangar glyph lost its material artwork.");
  Console.WriteLine($"PASS compact hangar: {compactDifferences} shared-coverage pixels changed by material.");
  if(!Enumerable.Range(365,20).Any(y=>Enumerable.Range(65,60).Any(x=>pixels[y*960+x]!=background)))
   throw new Exception("Captain cap is missing above the robot head.");
  for(int row=0;row<parts.Length;row++){
   int differences=0;
   for(int y=15+row*170;y<170+row*170;y++)
    for(int x=45;x<145;x++){
     int index=y*960+x;
     if(pixels[index]!=pixels[index+480] || pixels[index+160]!=pixels[index+640])
      throw new Exception($"Delivery and manufacturing differ for {parts[row]}");
     if(pixels[index]!=background && pixels[index+160]!=background && pixels[index]!=pixels[index+160])differences++;
    }
   if(differences<100)throw new Exception($"Material artwork did not change for {parts[row]}");
   Console.WriteLine($"PASS {parts[row]}: {differences} shared-coverage pixels changed by material.");
  }
  using var capture=new Texture2D(GraphicsDevice,960,540);
  capture.SetData(pixels);
  using(var file=File.Create(output))capture.SaveAsPng(file,960,540);

  foreach(var node in world.Nodes.All)node.Owner=0;
  world.SelectPlayer(0);
  world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);
  var route=world.Nodes.All.SelectMany(source=>world.Nodes.All
   .Where(target=>target.Id!=source.Id)
   .Select(target=>RobotLongDelivery.Path(world,source.Id,target.Id,0)))
   .First(path=>path.Length>2);
  var stock=world.Robots;
  world.SetCountryAppearance(1,12,6,2);
  world.SetCountryEmblem(1,2);
  var integrated=stock.Create(0,parts[0]) with {
   HeadMaterial=RobotMaterial.Cardboard,BodyMaterial=RobotMaterial.Foam,BodyOrigin=1,IntegratedParts=parts[0]};
  var captain=stock.Create(0,RobotParts.Complete,RobotRole.Captain) with {
   HeadMaterial=RobotMaterial.Maple,BodyMaterial=RobotMaterial.SoftVinyl,LegsMaterial=RobotMaterial.Silicone,
   HeadOrigin=1,LegsOrigin=1};
  stock.Nodes[route[0]].Add(integrated);
  stock.Nodes[route[0]].Add(captain);
  stock.LongDelivery.Reserve(world,route[0],route[^1],integrated.Id,integrated.Parts,null,0,0);
  stock.LongDelivery.Reserve(world,route[0],route[^1],captain.Id,captain.Parts,null,0,1);
  var nodes=(int[])Get("_directNodes");
  nodes[0]=route[0];nodes[1]=route[^1];
  typeof(Game1).GetMethod("SyncLongSlots",Flags)!.Invoke(this,null);
  void DrawArrivals(Color[] destination){
   GraphicsDevice.Clear(background);
   batch.Begin(transformMatrix:Matrix.CreateTranslation(-1320,0,0));
   typeof(Game1).GetMethod("DrawLongPane",Flags)!.Invoke(this,new object[]{1});
   batch.End();
   GraphicsDevice.GetBackBufferData(destination);
  }
  DrawArrivals(pixels);
  capture.SetData(pixels);
  using(var paneFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"long-delivery-arrivals.png")))
   capture.SaveAsPng(paneFile,960,540);
  var alternativePixels=new Color[pixels.Length];
  var sourceStore=stock.Nodes[route[0]];
  var directCell=typeof(Game1).GetMethod("DirectCell",BindingFlags.Static|BindingFlags.NonPublic)!;
  void CheckArrivalVariant(int slot,Robot original,Robot variant){
   var reservedRobot=sourceStore.Remove(original.Id);
   sourceStore.Add(variant with {LongDeliveryNumber=reservedRobot.LongDeliveryNumber});
   DrawArrivals(alternativePixels);
   sourceStore.Remove(variant.Id);
   sourceStore.Add(reservedRobot);
   var cell=(Rectangle)directCell.Invoke(null,new object[]{1,slot})!;
   cell.Offset(-1320,0);
   int differences=0;
   for(int y=0;y<540;y++)
    for(int x=0;x<960;x++){
     int index=y*960+x;
     if(pixels[index]==alternativePixels[index])continue;
     if(!cell.Contains(x,y))throw new Exception($"Changing slot {slot} altered pixel ({x}, {y}) outside {cell}.");
     differences++;
    }
   if(differences==0)throw new Exception($"Incoming glyph ignored the variant in slot {slot}.");
  }
  CheckArrivalVariant(0,integrated,integrated with {IntegratedParts=0});
  CheckArrivalVariant(1,captain,captain with {Role=RobotRole.Soldier});
  Console.WriteLine("PASS long delivery arrival preview distinguishes integration and captain role in their own slots.");
  var advance=typeof(RobotLongDelivery).GetMethod("Advance",Flags)!;
  advance.Invoke(stock.LongDelivery,new object[]{world});
  if(stock.LongDelivery.Orders.Count!=2 || stock.LongDelivery.Orders.Any(order=>!order.Started || order.Node!=route[1]))
   throw new Exception("Both shipments must advance to the intermediate hangar.");
  var relayPixels=new Color[pixels.Length];
  DrawArrivals(relayPixels);
  if(!pixels.SequenceEqual(relayPixels))
   throw new Exception("The destination preview changed after the cargo reached the intermediate hangar.");
  Console.WriteLine("PASS both long delivery material arrivals are pixel-identical after the first hop.");
  for(int hop=2;hop<route.Length;hop++)advance.Invoke(stock.LongDelivery,new object[]{world});
  if(stock.LongDelivery.Orders.Count!=0)
   throw new Exception("Long deliveries were not completed after the final hop.");
  var warehouse=stock.Nodes[route[^1]].Robots;
  var deliveredPair=warehouse.Single(robot=>robot.Id==integrated.Id);
  var deliveredCaptain=warehouse.Single(robot=>robot.Id==captain.Id);
  if(!SameParts(integrated,deliveredPair) || !SameParts(captain,deliveredCaptain))
   throw new Exception("Final arrival lost its materials, integration or role.");
  typeof(Game1).GetMethod("SyncLongSlots",Flags)!.Invoke(this,null);
  DrawArrivals(pixels);
  capture.SetData(pixels);
  using(var paneFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"long-delivery-delivered.png")))
   capture.SaveAsPng(paneFile,960,540);
  Console.WriteLine("PASS final long delivery preserves the integrated pair and captain in the destination hangar.");
  var compactBounds=new Rectangle(40,40,28,52);
  int capHeight=compactBounds.Height/4;
  // The captain reserves the top quarter for the cap and fits the body below it.
  var soldierBounds=new Rectangle(100,compactBounds.Y+capHeight,compactBounds.Width,compactBounds.Height-capHeight);
  GraphicsDevice.Clear(background);
  batch.Begin();
  delivery.Invoke(this,new object[]{compactBounds,deliveredCaptain,1f,null});
  delivery.Invoke(this,new object[]{soldierBounds,deliveredCaptain with {Role=RobotRole.Soldier},1f,null});
  batch.End();
  GraphicsDevice.GetBackBufferData(pixels);
  int capPixels=0;
  for(int y=compactBounds.Top;y<compactBounds.Bottom;y++)
   for(int x=compactBounds.Left;x<compactBounds.Right;x++){
    int index=y*960+x;
    if(pixels[index]==pixels[index+60])continue;
    if(y>=compactBounds.Center.Y)throw new Exception("Captain cap changed pixels below the head.");
    capPixels++;
   }
  if(capPixels<10)throw new Exception("The delivered captain cap is missing at compact hangar size.");
  capture.SetData(pixels);
  using(var compactFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"long-delivery-compact-captain.png")))
   capture.SaveAsPng(compactFile,960,540);
  Console.WriteLine($"PASS delivered compact captain: {capPixels} pixels changed in the upper half.");

  int directTarget=route[1];
  // Home persuasion can change a part's origin and cancel its order before dispatch.
  world.Campaign.RobotBattles.Rules=new BattleRules{PersuasionPercent=0};
  stock.Workshops[directTarget].AutoAssemblyDisabled=true;
  var outgoingPair=integrated with {Id=stock.Create(0,integrated.Parts).Id};
  var outgoingCaptain=captain with {Id=stock.Create(0,captain.Parts).Id};
  stock.Nodes[route[0]].Add(outgoingPair);
  stock.Nodes[route[0]].Add(outgoingCaptain);
  var directFixture=AutoSaveStore.Capture(world);
  nodes[1]=directTarget;
  ((CubeNet)Get("_directPairNet")).Home(world);
  typeof(Game1).GetMethod("FitDirectPairMap",Flags)!.Invoke(this,null);
  int receivingSide=Array.IndexOf(nodes,directTarget);
  var directSlots=(long?[][])Get("_directSlots");
  var drawDirectPair=typeof(Game1).GetMethod("DrawDirectPair",Flags)!;
  var directViewCell=typeof(Game1).GetMethod("DirectViewCell",Flags)!;
  void DrawDirectPanel(Color[] destination){
   GraphicsDevice.Clear(background);
   batch.Begin();drawDirectPair.Invoke(this,null);batch.End();
   GraphicsDevice.GetBackBufferData(destination);
  }
  void DrawPlannedDirectArrivals(Robot pair,Robot leader,Color[] destination){
   var copy=AutoSaveStore.Restore<WorldSetup>(directFixture);
   Set("_setup",copy);
   var source=copy.Robots.Nodes[route[0]];
   source.Remove(pair.Id);source.Add(pair);
   source.Remove(leader.Id);source.Add(leader);
   foreach(var slots in directSlots)Array.Clear(slots);
   Array.Resize(ref directSlots[receivingSide],copy.Robots.Nodes[directTarget].TotalCapacity);
   directSlots[receivingSide][0]=copy.Robots.PartPlanning.Reserve(copy,route[0],directTarget,pair.Id,pair.Parts,null,0);
   directSlots[receivingSide][1]=copy.Robots.PartPlanning.Reserve(copy,route[0],directTarget,leader.Id,leader.Parts,null,0);
   DrawDirectPanel(destination);
  }
  DrawPlannedDirectArrivals(outgoingPair,outgoingCaptain,pixels);
  var plannedWorld=(WorldSetup)Get("_setup");
  var plannedSlots=directSlots[receivingSide].Take(2).ToArray();
  var restoredPlan=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(plannedWorld));
  Set("_setup",restoredPlan);
  foreach(var slots in directSlots)Array.Clear(slots);
  DrawDirectPanel(alternativePixels);
  if(!plannedSlots.SequenceEqual(directSlots[receivingSide].Take(2)) || !pixels.SequenceEqual(alternativePixels))
   throw new Exception("Restoring the direct delivery changed the reconstructed arrival slots or rendered preview.");
  Console.WriteLine("PASS restored direct delivery reconstructs the same arrival slots and preview pixels.");
  capture.SetData(pixels);
  using(var paneFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"direct-delivery-arrivals.png")))
   capture.SaveAsPng(paneFile,960,540);
  void CheckPlannedArrivalVariant(int slot,Robot pair,Robot leader){
   DrawPlannedDirectArrivals(pair,leader,alternativePixels);
   var cell=(Rectangle)directViewCell.Invoke(this,new object[]{receivingSide,slot})!;
   var other=(Rectangle)directViewCell.Invoke(this,new object[]{receivingSide,1-slot})!;
   bool Changed(Rectangle area)=>Enumerable.Range(area.Top/2,area.Height/2).Any(y=>
    Enumerable.Range(area.Left/2,area.Width/2).Any(x=>pixels[y*960+x]!=alternativePixels[y*960+x]));
   if(!Changed(cell) || Changed(other))throw new Exception($"Direct arrival variant changed the wrong glyph in slot {slot}.");
  }
  CheckPlannedArrivalVariant(0,outgoingPair with {IntegratedParts=0},outgoingCaptain);
  CheckPlannedArrivalVariant(1,outgoingPair,outgoingCaptain with {Role=RobotRole.Soldier});
  Set("_setup",world);
  Console.WriteLine("PASS drag-and-drop direct arrival preview distinguishes integration and captain role.");
  stock.DirectDelivery.ReserveParts(world,route[0],directTarget,outgoingPair.Id,outgoingPair.Parts,null,0);
  stock.DirectDelivery.ReserveParts(world,route[0],directTarget,outgoingCaptain.Id,outgoingCaptain.Parts,null,1);
  var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));
  if(restored.EquipmentMode!=RobotEquipmentMode.Materials ||
    !restored.Robots.DirectDelivery.Orders.SequenceEqual(stock.DirectDelivery.Orders))
   throw new Exception("The saved world lost its material mode or direct orders.");
  var persuasion=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));
  var persuasionStock=persuasion.Robots;
  var sourceNode=persuasion.Nodes.All[route[0]];
  persuasion.ConquerorLocations[0]=sourceNode.Cell;
  persuasion.ConquerorPoints[0]=sourceNode.Center;
  for(int ruler=1;ruler<persuasion.ActiveCount;ruler++)
   persuasionStock.DomesticPersuasionTurns[ruler]=persuasion.Population.Turn;
  var bodyLegs=persuasionStock.Create(0,parts[1]) with {
   BodyMaterial=RobotMaterial.Polystyrene,LegsMaterial=RobotMaterial.Maple,LegsOrigin=1,IntegratedParts=parts[1]};
  persuasionStock.Nodes[route[0]].Add(bodyLegs);
  persuasionStock.DirectDelivery.ReserveParts(persuasion,route[0],directTarget,bodyLegs.Id,bodyLegs.Parts,null,2);
  persuasion.Campaign.RobotBattles.Rules=new BattleRules{PersuasionPercent=100};
  var persuasionEvents=persuasionStock.PersuadeAtHome(persuasion);
  var changedPair=persuasionEvents.Single(e=>e.Before.Id==outgoingPair.Id && e.Success);
  if(changedPair.Before.Origin(RobotParts.Head)!=changedPair.After.Origin(RobotParts.Head) ||
     changedPair.Before.Origin(RobotParts.Body)==changedPair.After.Origin(RobotParts.Body) ||
     persuasionStock.DirectDelivery.Orders.Any(o=>o.RobotId==outgoingPair.Id))
   throw new Exception("Changing only the body's origin must cancel the whole integrated shipment.");
  var changedBodyLegs=persuasionEvents.Single(e=>e.Before.Id==bodyLegs.Id && e.Success);
  if(changedBodyLegs.Before.Origin(RobotParts.Body)!=changedBodyLegs.After.Origin(RobotParts.Body) ||
     changedBodyLegs.Before.Origin(RobotParts.Legs)==changedBodyLegs.After.Origin(RobotParts.Legs) ||
     persuasionStock.DirectDelivery.Orders.Any(o=>o.RobotId==bodyLegs.Id))
   throw new Exception("Changing only the legs' origin must cancel the whole integrated shipment.");
  persuasionStock.DirectDelivery.ReserveParts(persuasion,route[0],directTarget,bodyLegs.Id,RobotParts.Legs,null,0);
  var rebooked=persuasionStock.DirectDelivery.Orders.Single(o=>o.RobotId==bodyLegs.Id);
  if(rebooked.Part!=bodyLegs.Parts || rebooked.TargetSlot!=0)
   throw new Exception("The freed slot must accept a new order for the entire integrated pair.");
  var changed=persuasionEvents.Single(e=>e.Before.Id==outgoingCaptain.Id && e.Success);
  var changedPart=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}
   .Single(part=>changed.Before.Origin(part)!=changed.After.Origin(part));
  var remainingOrders=persuasionStock.DirectDelivery.Orders.Where(o=>o.RobotId==outgoingCaptain.Id).ToArray();
  if(remainingOrders.Length!=2 || remainingOrders.Any(o=>o.Part==changedPart))
   throw new Exception("Persuasion did not cancel exactly the changed captain part.");
  var forecastCaptain=persuasionStock.DirectDelivery.Preview(persuasionStock,directTarget).Single(r=>r.Role==RobotRole.Captain);
  if(forecastCaptain.Parts!=(RobotParts.Complete&~changedPart) ||
    !persuasionStock.DirectDelivery.ArrivalOrders(persuasionStock,directTarget,forecastCaptain.Id)
     .Order().SequenceEqual(remainingOrders.Select(o=>o.Number).Order()))
   throw new Exception("The arrival forecast includes the cancelled part.");
  persuasion=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(persuasion));
  persuasionStock=persuasion.Robots;
  var restoredBooking=persuasionStock.DirectDelivery.Orders.Single(o=>o.RobotId==bodyLegs.Id);
  var forecastBodyLegs=persuasionStock.DirectDelivery.Preview(persuasionStock,directTarget)
   .Single(r=>r.IntegratedParts==bodyLegs.Parts);
  if(restoredBooking!=rebooked || persuasionStock.DirectDelivery.Orders.Any(o=>o.RobotId==outgoingPair.Id) ||
     !SameParts(changedBodyLegs.After,forecastBodyLegs) ||
     !persuasionStock.DirectDelivery.ArrivalOrders(persuasionStock,directTarget,forecastBodyLegs.Id)
      .SequenceEqual(new[]{rebooked.Number}))
   throw new Exception("Restoring the rebooking must preserve the new order number and the reused arrival slot.");
  persuasion.Campaign.Advance(persuasion,-1,0,enemies:false);
  var keptPart=persuasionStock.Nodes[route[0]].Robots.Single(r=>r.Id==outgoingCaptain.Id);
  var actualCaptain=persuasionStock.Nodes[directTarget].Robots.Single(r=>r.Role==RobotRole.Captain);
  if(!SameParts(changed.After.Slice(changedPart),keptPart) || !SameParts(forecastCaptain,actualCaptain))
   throw new Exception("Direct arrival after persuasion did not match the forecast and retained part.");
  var retainedPair=persuasionStock.Nodes[route[0]].Robots.Single(r=>r.Id==outgoingPair.Id);
  var arrivedBodyLegs=persuasionStock.Nodes[directTarget].Robots.Single(r=>r.Id==bodyLegs.Id);
  if(!SameParts(changedPair.After,retainedPair) || persuasionStock.Nodes[directTarget].Robots.Count!=2 ||
     !SameParts(changedBodyLegs.After,arrivedBodyLegs))
   throw new Exception("The cancelled integrated pair must stay intact at its source.");
  Console.WriteLine("PASS persuasion cancels only the changed captain part and the arrival forecast matches delivery.");
  var pairBounds=new Rectangle(96,40,28,52);
  var expectedPixels=new Color[pixels.Length];
  RenderCompact(deliveredPair,deliveredCaptain,expectedPixels);
  foreach(var pending in new[]{world,restored})
  {
   var pendingStock=pending.Robots;
   int turn=pending.Population.Turn;
   pending.Campaign.Advance(pending,-1,0,enemies:false);
   if(pending.Population.Turn!=turn+1 || pendingStock.DirectDelivery.Orders.Count!=0 || pendingStock.Nodes[route[0]].Count!=0)
    throw new Exception("The two direct shipments did not arrive on the next global turn.");
   var directPair=pendingStock.Nodes[directTarget].Robots.Single(robot=>robot.Parts==integrated.Parts);
   var directCaptain=pendingStock.Nodes[directTarget].Robots.Single(robot=>robot.Parts==captain.Parts);
   if(!SameParts(integrated,directPair) || !SameParts(captain,directCaptain))
    throw new Exception("Direct arrival lost materials, integration or rank.");
   RenderCompact(directPair,directCaptain,pixels);
   if(!pixels.SequenceEqual(expectedPixels))
    throw new Exception("Direct and long delivery changed the same materials' hangar artwork.");
  }
  capture.SetData(pixels);
  using(var directFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"direct-delivery-compact-arrivals.png")))
   capture.SaveAsPng(directFile,960,540);
  Console.WriteLine("PASS direct delivery preserves mixed materials and origins before and after restore, with identical compact art.");

  var planStock=restoredPlan.Robots;
  var forecastStore=planStock.PartPlanning.Preview(restoredPlan).Nodes[directTarget];
  var plannedPair=forecastStore.Robots.Single(robot=>robot.Id==plannedSlots[0]);
  var plannedCaptain=forecastStore.Robots.Single(robot=>robot.Id==plannedSlots[1]);
  if(!restoredPlan.Campaign.FinishDomestic(restoredPlan) || planStock.PartPlanning.Orders.Count!=0 ||
     planStock.PartPlanning.LastResults.Count(result=>result.Status=="実施完了")!=2 ||
     planStock.Nodes[route[0]].Count!=0 || planStock.Nodes[directTarget].Count!=2)
   throw new Exception("Restored drag-and-drop plans did not deliver both robots exactly once.");
  var arrivedPair=planStock.Nodes[directTarget].Robots.Single(robot=>robot.Parts==plannedPair.Parts);
  var arrivedCaptain=planStock.Nodes[directTarget].Robots.Single(robot=>robot.Parts==plannedCaptain.Parts);
  if(!SameParts(plannedPair,arrivedPair) || !SameParts(plannedCaptain,arrivedCaptain))
   throw new Exception("Confirmed drag-and-drop arrivals differ from the restored forecast.");
  RenderCompact(plannedPair,plannedCaptain,expectedPixels);
  RenderCompact(arrivedPair,arrivedCaptain,pixels);
  if(!pixels.SequenceEqual(expectedPixels))
   throw new Exception("Confirmed drag-and-drop arrival artwork differs from the restored forecast.");
  Console.WriteLine("PASS restored drag-and-drop plans deliver the same parts, roles and compact artwork as forecast.");

  TestIntegratedDirectMerge(outgoingPair,RobotParts.Legs,0);
  TestIntegratedDirectMerge(outgoingPair with {
   Parts=parts[1],IntegratedParts=parts[1],LegsMaterial=RobotMaterial.Maple,LegsOrigin=1},RobotParts.Head,2);
  foreach(bool assembleFirst in new[]{false,true})
  {
   var shipmentWorld=AutoSaveStore.Restore<WorldSetup>(directFixture);
   var shipmentStock=shipmentWorld.Robots;
   int source=route[0], shippingSide=1-receivingSide;
   var robot=outgoingCaptain with {IntegratedParts=0};
   int earlierOrders=0;
   if(assembleFirst){
    shipmentStock.Workshops[source].AutoAssemblyDisabled=true;
    var assemblyStore=shipmentStock.Nodes[source];
    assemblyStore.Remove(outgoingPair.Id);
    assemblyStore.Remove(robot.Id);
    assemblyStore.Add(robot.Slice(RobotParts.Head|RobotParts.Body));
    var legs=robot.Slice(RobotParts.Legs) with {Id=shipmentStock.Create(0,RobotParts.Legs).Id};
    assemblyStore.Add(legs);
    shipmentStock.PartPlanning.Reserve(shipmentWorld,source,source,legs.Id,RobotParts.Legs,robot.Id,0);
    earlierOrders=1;
   }
   Set("_setup",shipmentWorld);
   foreach(var slots in directSlots)Array.Clear(slots);
   Set("_pointer",Point.Zero);
   DrawDirectPanel(alternativePixels);
   int sourceSlot=Array.IndexOf(directSlots[shippingSide],(long?)robot.Id);
   int targetSlot=Array.IndexOf(directSlots[receivingSide],null);
   if(sourceSlot<0 || targetSlot<0)throw new Exception("Whole-unit drag fixture lacks a visible source or target.");
   var sourceCell=(Rectangle)directViewCell.Invoke(this,new object[]{shippingSide,sourceSlot})!;
   var targetCell=(Rectangle)directViewCell.Invoke(this,new object[]{receivingSide,targetSlot})!;
   var input=typeof(Game1).GetMethod("DirectPairInput",Flags)!;
   var pressed=new Microsoft.Xna.Framework.Input.MouseState(0,0,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,
    Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,
    Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
   Set("_pointer",new Point(sourceCell.Center.X,sourceCell.Bottom-8));
   input.Invoke(this,new object[]{pressed,true,false});
   if(Get("_directDrag") is not ValueTuple<int,long,RobotParts> drag || drag!=(shippingSide,robot.Id,(RobotParts)0))
    throw new Exception($"Whole-unit drag did not start (assembly pending: {assembleFirst}).");
   Set("_pointer",targetCell.Center);
   Set("_previousMouse",pressed);
   var released=new Microsoft.Xna.Framework.Input.MouseState();
   input.Invoke(this,new object[]{released,false,false});
   Set("_previousMouse",released);
   var orders=shipmentStock.PartPlanning.Orders;
   if(orders.Count!=earlierOrders+1 || orders.Last().Parts!=RobotParts.Complete || orders.Last().Target!=directTarget ||
      orders.Last().RobotId!=robot.Id || orders.Last().TargetRobotId.HasValue)
    throw new Exception("The whole-unit drop did not reserve all three parts together.");
   if(!shipmentWorld.Campaign.FinishDomestic(shipmentWorld) ||
      shipmentStock.Nodes[source].Robots.Any(unit=>unit.Id==robot.Id) ||
      shipmentStock.Nodes[directTarget].Robots.Single(unit=>unit.Id==robot.Id).Parts!=RobotParts.Complete)
    throw new Exception("The full unit did not reach the receiving bay.");
  }
  Set("_pointer",Point.Zero);
  Console.WriteLine("PASS direct shipping sends all three parts together from the bay, including a pending assembly.");
  var sharedBayWorld=AutoSaveStore.Restore<WorldSetup>(directFixture);
  var sharedBayStock=sharedBayWorld.Robots;
  var sharedBayStore=sharedBayStock.Nodes[directTarget];
  var ppRemainder=new MaplePackage(980001,8){Material=RawMaterial.PP};
  typeof(RobotStore).GetMethod("SetMaplePackages",Flags)!.Invoke(sharedBayStore,new object[]{new[]{ppRemainder}});
  var ppHead=sharedBayStock.Create(0,RobotParts.Head) with {HeadMaterial=RobotMaterial.Polypropylene};
  sharedBayStock.ProductionIntakes.Add(new(directTarget,null,ppHead,new RobotWorkshop()));
  Set("_setup",sharedBayWorld);
  foreach(var slots in directSlots)Array.Clear(slots);
  DrawDirectPanel(expectedPixels);
  int sharedBaySlot=Array.IndexOf(directSlots[receivingSide],(long?)ppHead.Id);
  var storedMaterials=typeof(Game1).GetMethod("DirectStoredMaterials",Flags)!;
  var displayedRaw=(Dictionary<int,MaplePackage>)storedMaterials.Invoke(this,new object[]{receivingSide})!;
  if(sharedBaySlot<0 || displayedRaw.Single().Key/3!=sharedBaySlot || displayedRaw.Single().Key%3==0 ||
     displayedRaw.Single().Value!=ppRemainder || sharedBayStore.Robots.Any(robot=>robot.Id==ppHead.Id))
   throw new Exception("The PP remainder and forecast head do not occupy separate sections of the same bay.");
  sharedBayWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(sharedBayWorld));
  sharedBayStock=sharedBayWorld.Robots;
  sharedBayStore=sharedBayStock.Nodes[directTarget];
  Set("_setup",sharedBayWorld);
  foreach(var slots in directSlots)Array.Clear(slots);
  DrawDirectPanel(alternativePixels);
  var restoredRaw=(Dictionary<int,MaplePackage>)storedMaterials.Invoke(this,new object[]{receivingSide})!;
  if(directSlots[receivingSide][sharedBaySlot]!=ppHead.Id || !restoredRaw.Single().Equals(displayedRaw.Single()) ||
     sharedBayStock.ProductionIntakes.Single().Product.Id!=ppHead.Id ||
     !expectedPixels.SequenceEqual(alternativePixels))
   throw new Exception("Restoring the PP intake changed the material section or the forecast head artwork.");
  var sharedPart=typeof(Game1).GetMethod("DirectViewPart",Flags)!;
  var sharedInput=typeof(Game1).GetMethod("DirectPairInput",Flags)!;
  var sharedPressed=new Microsoft.Xna.Framework.Input.MouseState(0,0,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,
   Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,
   Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
  int rawKey=restoredRaw.Single().Key;
  Set("_pointer",((Rectangle)sharedPart.Invoke(this,new object[]{receivingSide,sharedBaySlot,rawKey%3})!).Center);
  sharedInput.Invoke(this,new object[]{sharedPressed,true,false});
  if(Get("_directMaterialDrag") is not ValueTuple<int,int,long> rawDrag ||
     rawDrag!=(receivingSide,rawKey,ppRemainder.Id) || Get("_directDrag")!=null)
   throw new Exception("Clicking the PP remainder did not select only the raw cargo.");
  typeof(Game1).GetField("_directMaterialDrag",Flags)!.SetValue(this,null);
  Set("_pointer",((Rectangle)sharedPart.Invoke(this,new object[]{receivingSide,sharedBaySlot,0})!).Center);
  sharedInput.Invoke(this,new object[]{sharedPressed,true,false});
  if(Get("_directDrag") is not ValueTuple<int,long,RobotParts> headDrag ||
     headDrag!=(receivingSide,ppHead.Id,RobotParts.Head) || Get("_directMaterialDrag")!=null)
   throw new Exception("Clicking the PP head did not select only the robot part.");
  typeof(Game1).GetField("_directDrag",Flags)!.SetValue(this,null);
  Set("_pointer",Point.Zero);
  var sharedBaySave=AutoSaveStore.Capture(sharedBayWorld);
  if(!sharedBayWorld.Campaign.FinishDomestic(sharedBayWorld) || sharedBayStock.ProductionIntakes.Count!=0)
   throw new Exception("The shared bay intake did not complete.");
  DrawDirectPanel(alternativePixels);
  var sharedCell=(Rectangle)directViewCell.Invoke(this,new object[]{receivingSide,sharedBaySlot})!;
  for(int y=sharedCell.Top/2;y<sharedCell.Bottom/2;y++)
   for(int x=sharedCell.Left/2;x<sharedCell.Right/2;x++)
    if(expectedPixels[y*960+x]!=alternativePixels[y*960+x])
     throw new Exception("The shared PP bay changed after the production intake completed.");
  var rawDispatchWorld=AutoSaveStore.Restore<WorldSetup>(sharedBaySave);
  var rawDispatchStock=rawDispatchWorld.Robots;
  if(!rawDispatchStock.MapleSupplyEnabled)
   typeof(RobotWorld).GetMethod("InitializeMapleSupply",Flags)!.Invoke(rawDispatchStock,new object[]{rawDispatchWorld});
  Set("_setup",rawDispatchWorld);
  foreach(var slots in directSlots)Array.Clear(slots);
  DrawDirectPanel(alternativePixels);
  int rawDestinationSide=1-receivingSide;
  int rawDestinationSlot=Array.IndexOf(directSlots[rawDestinationSide],null);
  if(rawDestinationSlot<0)throw new Exception("No empty bay for the raw-only shipment.");
  Set("_pointer",((Rectangle)sharedPart.Invoke(this,new object[]{receivingSide,sharedBaySlot,rawKey%3})!).Center);
  sharedInput.Invoke(this,new object[]{sharedPressed,true,false});
  Set("_pointer",((Rectangle)sharedPart.Invoke(this,new object[]{rawDestinationSide,rawDestinationSlot,0})!).Center);
  Set("_previousMouse",sharedPressed);
  var sharedReleased=new Microsoft.Xna.Framework.Input.MouseState();
  sharedInput.Invoke(this,new object[]{sharedReleased,false,false});
  Set("_previousMouse",sharedReleased);
  Set("_pointer",Point.Zero);
  if(rawDispatchStock.MapleShipments.Count!=1)
   throw new Exception($"Raw-only shipment was not reserved: {Get("_directMessage")}");
  var rawOrder=rawDispatchStock.MapleShipments.Single();
  if(rawOrder.PackageId!=ppRemainder.Id || rawOrder.Source!=directTarget || rawOrder.Target!=route[0] ||
     rawDispatchStock.ProductionIntakes.Single().Product!=ppHead || rawDispatchStock.PartPlanning.Orders.Count!=0)
   throw new Exception("Dispatching the raw remainder changed the pending head intake.");
  var blockedWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(rawDispatchWorld));
  var blockedStock=blockedWorld.Robots;
  var blockedTarget=blockedStock.Nodes[rawOrder.Target];
  typeof(RobotStore).GetMethod("SetMaplePackages",Flags)!.Invoke(blockedTarget,new object[]{Array.Empty<MaplePackage>()});
  foreach(var robot in blockedTarget.Robots.Where(r=>r.Parts!=RobotParts.Complete).ToArray())
   blockedTarget.Remove(robot.Id);
  long lastFiller=-1;
  while(blockedTarget.HasSpace){var filler=blockedStock.Create(rawOrder.Owner,RobotParts.Complete);blockedTarget.Add(filler);lastFiller=filler.Id;}
  if(lastFiller<0)throw new Exception("Fixture needs a removable robot in the destination hangar.");
  var resolveRaw=typeof(RobotWorld).GetMethod("ResolveMapleShipments",Flags)!;
  void CheckBlocked(RobotWorld stock)
  {
   if(stock.MapleShipments.Single()!=rawOrder || stock.Nodes[rawOrder.Source].MaplePackages.Single(p=>p.Id==ppRemainder.Id)!=ppRemainder ||
      stock.Nodes[rawOrder.Target].MaplePackages.Count!=0)
    throw new Exception("A full destination must keep the PP order and cargo at the source.");
  }
  resolveRaw.Invoke(blockedStock,new object[]{blockedWorld});
  CheckBlocked(blockedStock);
  blockedWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(blockedWorld));
  blockedStock=blockedWorld.Robots;
  resolveRaw.Invoke(blockedStock,new object[]{blockedWorld});
  CheckBlocked(blockedStock);
  blockedStock.Nodes[rawOrder.Target].Remove(lastFiller);
  resolveRaw.Invoke(blockedStock,new object[]{blockedWorld});
  resolveRaw.Invoke(blockedStock,new object[]{blockedWorld});
  if(blockedStock.MapleShipments.Count!=0 || blockedStock.Nodes[rawOrder.Source].MaplePackages.Any(p=>p.Id==ppRemainder.Id) ||
     blockedStock.Nodes[rawOrder.Target].MaplePackages.Single()!=ppRemainder)
   throw new Exception("The restored PP shipment must arrive exactly once after a bay opens.");
  Console.WriteLine("PASS the full destination keeps its PP shipment on save/restore and receives it once a bay opens.");
  var keeperWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(rawDispatchWorld));
  var keeperStock=keeperWorld.Robots;
  var keeperTarget=keeperStock.Nodes[rawOrder.Target];
  var keeper=keeperStock.Create(rawOrder.Owner,RobotParts.Complete);
  typeof(RobotStore).GetMethod("SetMaplePackages",Flags)!.Invoke(keeperTarget,new object[]{Array.Empty<MaplePackage>()});
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(keeperTarget,new object[]{new[]{keeper}});
  typeof(RobotStore).GetMethod("SetWarehouseLimit",Flags)!.Invoke(keeperTarget,new object[]{5});
  if(keeperTarget.StorageLimit!=1 || keeperTarget.HasSpace)throw new Exception("The destination needs exactly one occupied bay before appointment.");
  resolveRaw.Invoke(keeperStock,new object[]{keeperWorld});
  CheckBlocked(keeperStock);
  var ppFactory=keeperStock.Workshops[rawOrder.Target];
  for(int seed=0;ppFactory.ProductionLines.Length==0;seed++)
   ppFactory.GenerateLines(seed,RobotEquipmentMode.Materials);
  foreach(var shop in keeperStock.Workshops)
   foreach(var existing in shop.ProductionLines)existing.Paused=true;
  var ppLine=ppFactory.ProductionLines[0];
  ppLine.Configure(RobotParts.Head,2);
  ppLine.SetDoNotRepeat(true);
  ppLine.Paused=false;
  keeperStock.AdvanceIndustry(n=>keeperWorld.Nodes.All[n].Owner);
  if(!ppLine.WaitingForMaple(keeperStock,rawOrder.Target) || ppLine.ProductionAge!=0)
   throw new Exception("The PP line must wait without progressing while the shipment is blocked.");
  if(!keeperStock.PartPlanning.ReserveRole(keeperWorld,rawOrder.Target,keeper.Id,HangarReservation.Warehouse,rawOrder.Owner) ||
     keeperTarget.StorageLimit!=1)throw new Exception("Warehouse assignment should only expand capacity when the reservation is executed.");
  keeperWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(keeperWorld));
  keeperStock=keeperWorld.Robots;
  keeperStock.ConfigureFactories(keeperWorld);
  CheckBlocked(keeperStock);
  var shortWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(keeperWorld));
  if(!keeperWorld.Campaign.FinishDomestic(keeperWorld) || keeperStock.Nodes[rawOrder.Target].StorageLimit!=5 ||
     !keeperStock.Nodes[rawOrder.Target].Robots.Single(r=>r.Id==keeper.Id).WarehouseKeeper || keeperStock.MapleShipments.Count!=0 ||
     keeperStock.Nodes[rawOrder.Source].MaplePackages.Any(p=>p.Id==ppRemainder.Id) ||
     keeperStock.Nodes[rawOrder.Target].MaplePackages.Single()!=ppRemainder)
   throw new Exception("The restored appointment must open four bays before the waiting PP is delivered in the same phase.");
  Console.WriteLine("PASS restored warehouse appointment expands one bay to five and completes the waiting PP delivery in the same phase.");
  ppLine=keeperStock.Workshops[rawOrder.Target].ProductionLines[0];
  if(ppLine.WaitingForMaple(keeperStock,rawOrder.Target) || ppLine.ProductionAge!=0)
   throw new Exception("The delivered PP must clear the material wait without advancing industry during delivery.");
  keeperStock.AdvanceIndustry(n=>keeperWorld.Nodes.All[n].Owner);
  if(ppLine.ProductionAge!=1)throw new Exception("The PP line did not resume on the first industry unit.");
  keeperStock.AdvanceIndustry(n=>keeperWorld.Nodes.All[n].Owner);
  var ppProduct=keeperStock.ProductionIntakes.Single(intake=>intake.Source==rawOrder.Target).Product;
  if(ppProduct.Parts!=RobotParts.Head || ppProduct.Material(RobotParts.Head)!=RobotMaterial.Polypropylene ||
     keeperStock.Nodes[rawOrder.Target].MaplePackages.Any(p=>p.Material==RawMaterial.PP))
   throw new Exception("The restored line must use exactly eight delivered PP units to manufacture one PP head.");
  Console.WriteLine("PASS delivered PP resumes its restored line and eight units produce exactly one PP head.");
  var shortStock=shortWorld.Robots;
  shortStock.ConfigureFactories(shortWorld);
  var setPackages=typeof(RobotStore).GetMethod("SetMaplePackages",Flags)!;
  var shortSource=shortStock.Nodes[rawOrder.Source];
  var shortTarget=shortStock.Nodes[rawOrder.Target];
  setPackages.Invoke(shortSource,new object[]{shortSource.MaplePackages.Select(p=>p.Id==ppRemainder.Id
   ?p with {Units=7}:p).ToArray()});
  if(!shortWorld.Campaign.FinishDomestic(shortWorld) || shortStock.MapleShipments.Count!=0 ||
     shortTarget.MaplePackages.Single()!=ppRemainder with {Units=7})
   throw new Exception("The seven-unit delivery must reach the restored destination.");
  var shortLine=shortStock.Workshops[rawOrder.Target].ProductionLines[0];
  shortStock.AdvanceIndustry(n=>shortWorld.Nodes.All[n].Owner);
  if(!shortLine.WaitingForMaple(shortStock,rawOrder.Target) || shortLine.ProductionAge!=0 ||
     shortTarget.MaplePackages.Single().Units!=7 || shortStock.ProductionIntakes.Any(i=>i.Source==rawOrder.Target))
   throw new Exception("A one-unit shortfall must preserve the PP and keep the line at zero progress.");
  shortWorld.Campaign.BeginMilitary();
  shortWorld.Campaign.Resolve(shortWorld,Array.Empty<March>());
  var supplement=new MaplePackage(980002,1){Material=RawMaterial.PP};
  setPackages.Invoke(shortSource,new object[]{shortSource.MaplePackages.Append(supplement).ToArray()});
  if(!shortStock.ReserveMapleShipment(shortWorld,rawOrder.Source,rawOrder.Target,supplement.Id) ||
     !shortWorld.Campaign.FinishDomestic(shortWorld) || shortStock.MapleShipments.Count!=0 ||
     shortTarget.MaplePackages.Single()!=ppRemainder || shortLine.WaitingForMaple(shortStock,rawOrder.Target))
   throw new Exception("The next shipment must merge the missing unit and clear the material wait.");
  for(int unit=0;unit<2;unit++)shortStock.AdvanceIndustry(n=>shortWorld.Nodes.All[n].Owner);
  var supplementedProduct=shortStock.ProductionIntakes.Single(i=>i.Source==rawOrder.Target).Product;
  if(supplementedProduct.Parts!=RobotParts.Head || supplementedProduct.Material(RobotParts.Head)!=RobotMaterial.Polypropylene ||
     shortTarget.MaplePackages.Any(p=>p.Material==RawMaterial.PP))
   throw new Exception("The combined shipments must produce one PP head and use all eight units.");
  Console.WriteLine("PASS seven PP units remain untouched until a later shipment supplies the missing unit.");
  var fullWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(blockedWorld));
  var fullStock=fullWorld.Robots;
  fullStock.ConfigureFactories(fullWorld);
  var fullTarget=fullStock.Nodes[rawOrder.Target];
  var fullFactory=fullStock.Workshops[rawOrder.Target];
  for(int seed=0;fullFactory.ProductionLines.Length==0;seed++)
   fullFactory.GenerateLines(seed,RobotEquipmentMode.Materials);
  foreach(var shop in fullStock.Workshops)
   foreach(var existing in shop.ProductionLines)existing.Paused=true;
  var fullLine=fullFactory.ProductionLines[0];
  fullLine.Configure(RobotParts.Head,1);
  fullLine.SetDoNotRepeat(true);
  fullLine.Paused=false;
  var nineUnits=ppRemainder with {Units=9};
  setPackages.Invoke(fullTarget,new object[]{new[]{nineUnits}});
  if(fullTarget.HasSpace || fullLine.WaitingForMaple(fullStock,rawOrder.Target))
   throw new Exception("The production destination must be full while holding sufficient PP.");
  var exactWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(fullWorld));
  var exactStock=exactWorld.Robots;
  exactStock.ConfigureFactories(exactWorld);
  var exactTarget=exactStock.Nodes[rawOrder.Target];
  var exactLine=exactStock.Workshops[rawOrder.Target].ProductionLines[0];
  int robotsBefore=exactTarget.Robots.Count;
  setPackages.Invoke(exactTarget,new object[]{new[]{ppRemainder}});
  if(exactTarget.HasSpace || exactLine.WaitingForMaple(exactStock,rawOrder.Target))
   throw new Exception("The full bay must contain exactly enough PP.");
  var remoteWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(exactWorld));
  exactStock.AdvanceIndustry(n=>exactWorld.Nodes.All[n].Owner);
  var exactIntake=exactStock.ProductionIntakes.Single(i=>i.Source==rawOrder.Target);
  if(exactTarget.MaplePackages.Count!=0 || exactTarget.Robots.Count!=robotsBefore ||
     exactIntake.Product.Parts!=RobotParts.Head ||
     exactIntake.Product.Material(RobotParts.Head)!=RobotMaterial.Polypropylene || exactLine.WaitingProductionTurns!=0)
   throw new Exception("Using all eight units must free the bay and queue one PP head immediately.");
  typeof(RobotWorld).GetMethod("ResolveProductionIntakes",Flags)!.Invoke(exactStock,new object[]{exactWorld});
  if(exactStock.ProductionIntakes.Count!=0 || exactTarget.HasSpace ||
     exactTarget.Robots.Count!=robotsBefore+1 || !exactTarget.Robots.Contains(exactIntake.Product))
   throw new Exception("The product must occupy the bay vacated by the consumed PP.");
  Console.WriteLine("PASS consuming the exact PP cost frees a bay for the head in a full hangar.");
  var remoteStock=remoteWorld.Robots;
  remoteStock.ConfigureFactories(remoteWorld);
  int plant=rawOrder.Target, intakeNode=rawOrder.Source;
  var remoteLine=remoteStock.Workshops[plant].ProductionLines[0];
  remoteStock.ProductionIntakes.Clear();
  remoteStock.SetIntakeTarget(remoteLine,intakeNode);
  remoteStock.Workshops[intakeNode].AutoAssemblyDisabled=true;
  var remoteBay=remoteStock.Nodes[intakeNode];
  long spaceMaker=-1;
  while(remoteBay.HasSpace){
   var filler=remoteStock.Create(rawOrder.Owner,RobotParts.Complete);
   remoteBay.Add(filler);
   spaceMaker=filler.Id;
  }
  if(spaceMaker<0)throw new Exception("Adjacent intake fixture needs a removable filler.");
  remoteStock.AdvanceIndustry(n=>remoteWorld.Nodes.All[n].Owner);
  if(!remoteLine.IntakeFailure.Contains("格納枠") || remoteLine.WaitingProductionTurns!=1 ||
     remoteStock.ProductionIntakes.Count!=0 || remoteStock.Nodes[plant].MaplePackages.Single()!=ppRemainder)
   throw new Exception("A full adjacent intake must preserve PP at the factory.");
  remoteWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(remoteWorld));
  remoteStock=remoteWorld.Robots;
  remoteStock.ConfigureFactories(remoteWorld);
  remoteLine=remoteStock.Workshops[plant].ProductionLines[0];
  remoteBay=remoteStock.Nodes[intakeNode];
  if(remoteBay.HasSpace || !remoteLine.IntakeFailure.Contains("格納枠") || remoteLine.WaitingProductionTurns!=1 ||
     remoteStock.ProductionIntakes.Count!=0 || remoteStock.Nodes[plant].MaplePackages.Single()!=ppRemainder)
   throw new Exception("Restoring must preserve the adjacent bay wait and unconsumed PP.");
  remoteBay.Remove(spaceMaker);
  var sharedIntakeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(remoteWorld));
  var twoFactoryWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(remoteWorld));
  var twoFactoryStock=twoFactoryWorld.Robots;
  int commonBayNode=route[1];
  int[] plants=new[]{route[0],route[2]}.Order().ToArray();
  foreach(int factoryNode in plants)
   twoFactoryWorld.Nodes.All[factoryNode].Terrain=TravelTerrain.Land;
  twoFactoryStock.ConfigureFactories(twoFactoryWorld);
  var commonBay=twoFactoryStock.Nodes[commonBayNode];
  foreach(var robot in commonBay.Robots.ToArray())commonBay.Remove(robot.Id);
  setPackages.Invoke(commonBay,new object[]{Array.Empty<MaplePackage>()});
  typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(commonBay,new object[]{1});
  twoFactoryStock.Workshops[commonBayNode].AutoAssemblyDisabled=true;
  foreach(var shop in twoFactoryStock.Workshops)
   foreach(var lane in shop.ProductionLines)lane.Paused=true;
  var nextPackageId=twoFactoryStock.Nodes.SelectMany(store=>store.MaplePackages).Select(p=>p.Id).DefaultIfEmpty().Max()+1;
  foreach(int factoryNode in plants){
   var shop=twoFactoryStock.Workshops[factoryNode];
   for(int seed=0;shop.ProductionLines.Length==0 && seed<100;seed++)
    shop.GenerateLines(seed,RobotEquipmentMode.Materials);
   var lane=shop.ProductionLines.First();
   lane.Configure(RobotParts.Head,1);
   lane.SetDoNotRepeat(true);
   lane.Paused=false;
   twoFactoryStock.SetIntakeTarget(lane,commonBayNode);
   setPackages.Invoke(twoFactoryStock.Nodes[factoryNode],new object[]{new[]{ppRemainder with {Id=nextPackageId++}}});
  }
  twoFactoryStock.AdvanceIndustry(n=>twoFactoryWorld.Nodes.All[n].Owner);
  var firstFactoryIntake=twoFactoryStock.ProductionIntakes.Single();
  if(firstFactoryIntake.Source!=plants[0] || firstFactoryIntake.Target!=commonBayNode ||
     twoFactoryStock.Nodes[plants[0]].MaplePackages.Count!=0 ||
     twoFactoryStock.Nodes[plants[1]].MaplePackages.Single().Units!=8 ||
     !twoFactoryStock.Workshops[plants[1]].ProductionLines[0].IntakeFailure.Contains("格納枠"))
   throw new Exception("Separate factories must reserve the last shared bay only once and preserve the waiting factory's PP.");
  Console.WriteLine("PASS separate factories claim a shared adjacent bay only once.");
  var firstFactoryProduct=firstFactoryIntake.Product;
  twoFactoryWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(twoFactoryWorld));
  twoFactoryStock=twoFactoryWorld.Robots;
  twoFactoryStock.ConfigureFactories(twoFactoryWorld);
  commonBay=twoFactoryStock.Nodes[commonBayNode];
  var waitingFactoryLine=twoFactoryStock.Workshops[plants[1]].ProductionLines[0];
  if(twoFactoryStock.ProductionIntakes.Single().Product!=firstFactoryProduct ||
     waitingFactoryLine.WaitingProductionTurns!=1 ||
     twoFactoryStock.Nodes[plants[1]].MaplePackages.Single().Units!=8)
   throw new Exception("Restoring must preserve the first product and the second factory's wait and PP.");
  var resolveIntakes=typeof(RobotWorld).GetMethod("ResolveProductionIntakes",Flags)!;
  var expandedIntakeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(twoFactoryWorld));
  var expandedIntakeStock=expandedIntakeWorld.Robots;
  expandedIntakeStock.ConfigureFactories(expandedIntakeWorld);
  var expandedIntakeBay=expandedIntakeStock.Nodes[commonBayNode];
  var intakeKeeper=expandedIntakeStock.Create(rawOrder.Owner,RobotParts.Complete);
  typeof(RobotStore).GetMethod("SetWarehouseLimit",Flags)!.Invoke(expandedIntakeBay,new object[]{5});
  expandedIntakeBay.Add(intakeKeeper);
  if(expandedIntakeBay.HasSpace ||
     !expandedIntakeStock.PartPlanning.ReserveRole(expandedIntakeWorld,commonBayNode,intakeKeeper.Id,HangarReservation.Warehouse,rawOrder.Owner))
   throw new Exception("The shared bay must be full with an appointment pending.");
  expandedIntakeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(expandedIntakeWorld));
  expandedIntakeStock=expandedIntakeWorld.Robots;
  expandedIntakeStock.ConfigureFactories(expandedIntakeWorld);
  if(!expandedIntakeWorld.Campaign.FinishDomestic(expandedIntakeWorld))
   throw new Exception("Delivery must execute the restored warehouse appointment.");
  expandedIntakeBay=expandedIntakeStock.Nodes[commonBayNode];
  if(expandedIntakeBay.StorageLimit!=5 || !expandedIntakeBay.Robots.Single(r=>r.Id==intakeKeeper.Id).WarehouseKeeper ||
     expandedIntakeStock.ProductionIntakes.Single().Product!=firstFactoryProduct)
   throw new Exception("The appointment must open bays while holding the first product that waited before it.");
  expandedIntakeStock.AdvanceIndustry(n=>expandedIntakeWorld.Nodes.All[n].Owner);
  var expandedSecond=expandedIntakeStock.ProductionIntakes.Single(i=>i.Source==plants[1]);
  resolveIntakes.Invoke(expandedIntakeStock,new object[]{expandedIntakeWorld});
  if(expandedIntakeStock.ProductionIntakes.Count!=0 || expandedSecond.Product.Id==firstFactoryProduct.Id ||
     expandedIntakeStock.Nodes[plants[1]].MaplePackages.Count!=0 ||
     !expandedIntakeBay.Robots.Contains(firstFactoryProduct) || !expandedIntakeBay.Robots.Contains(expandedSecond.Product))
   throw new Exception("The expanded bays must admit both factories' products exactly once.");
  Console.WriteLine("PASS a restored keeper appointment opens shared bays for both factories.");
  resolveIntakes.Invoke(twoFactoryStock,new object[]{twoFactoryWorld});
  if(commonBay.HasSpace || !commonBay.Robots.Contains(firstFactoryProduct))
   throw new Exception("The first factory's product must take the only bay after restoring.");
  commonBay.Remove(firstFactoryProduct.Id);
  twoFactoryStock.AdvanceIndustry(n=>twoFactoryWorld.Nodes.All[n].Owner);
  var secondFactoryIntake=twoFactoryStock.ProductionIntakes.Single();
  if(secondFactoryIntake.Source!=plants[1] || secondFactoryIntake.Target!=commonBayNode ||
     secondFactoryIntake.Product.Id==firstFactoryProduct.Id ||
     secondFactoryIntake.Product.HeadMaterial!=RobotMaterial.Polypropylene ||
     twoFactoryStock.Nodes[plants[1]].MaplePackages.Count!=0)
   throw new Exception("The waiting factory must use its PP and schedule one new head in the freed bay.");
  resolveIntakes.Invoke(twoFactoryStock,new object[]{twoFactoryWorld});
  if(twoFactoryStock.ProductionIntakes.Count!=0 || commonBay.Robots.Single()!=secondFactoryIntake.Product)
   throw new Exception("Only the second product should occupy the freed bay.");
  Console.WriteLine("PASS the second factory resumes after restoration once the shared bay opens.");
  remoteStock.AdvanceIndustry(n=>remoteWorld.Nodes.All[n].Owner);
  var remoteIntake=remoteStock.ProductionIntakes.Single();
  if(remoteIntake.Source!=plant || remoteIntake.Target!=intakeNode ||
     remoteIntake.Product.Parts!=RobotParts.Head || remoteIntake.Product.HeadMaterial!=RobotMaterial.Polypropylene ||
     remoteStock.Nodes[plant].MaplePackages.Count!=0)
   throw new Exception("Opening the adjacent bay must consume eight PP units and reserve a PP head there.");
  typeof(RobotWorld).GetMethod("ResolveProductionIntakes",Flags)!.Invoke(remoteStock,new object[]{remoteWorld});
  if(remoteStock.ProductionIntakes.Count!=0 || !remoteBay.Robots.Contains(remoteIntake.Product))
   throw new Exception("The head must arrive only in the selected adjacent hangar.");
  Console.WriteLine("PASS a restored full adjacent intake preserves PP until space opens and then receives the exact product.");
  var sharedStock=sharedIntakeWorld.Robots;
  sharedIntakeWorld.Nodes.All[plant].Terrain=TravelTerrain.Land;
  sharedStock.ConfigureFactories(sharedIntakeWorld);
  var sharedFactory=sharedStock.Workshops[plant];
  sharedFactory.LimitLines(2);
  for(int seed=0;sharedFactory.ProductionLines.Length!=2;seed++)
   sharedFactory.GenerateLines(seed,RobotEquipmentMode.Materials);
  var firstLine=sharedFactory.ProductionLines[0];
  var secondLine=sharedFactory.ProductionLines[1];
  foreach(var lane in sharedFactory.ProductionLines){
   lane.Configure(RobotParts.Head,1);
   lane.SetDoNotRepeat(true);
   lane.Paused=false;
   sharedStock.SetIntakeTarget(lane,intakeNode);
  }
  var sharedStockSource=sharedStock.Nodes[plant];
  var sharedStockTarget=sharedStock.Nodes[intakeNode];
  setPackages.Invoke(sharedStockSource,new object[]{new[]{ppRemainder with {Units=16}}});
  if(sharedStockTarget.StorageLimit-sharedStockTarget.Count!=1)
   throw new Exception("The two lines must compete for exactly one free bay.");
  sharedStock.AdvanceIndustry(n=>sharedIntakeWorld.Nodes.All[n].Owner);
  var firstIntake=sharedStock.ProductionIntakes.Single();
  if(firstIntake.Line!=firstLine || firstIntake.Target!=intakeNode || secondLine.WaitingProductionTurns!=1 ||
     !secondLine.IntakeFailure.Contains("格納枠") || sharedStockSource.MaplePackages.Single()!=ppRemainder)
   throw new Exception("The first line must claim the bay and the second must preserve its eight PP units.");
  var firstProduct=firstIntake.Product;
  sharedIntakeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(sharedIntakeWorld));
  sharedStock=sharedIntakeWorld.Robots;
  sharedStock.ConfigureFactories(sharedIntakeWorld);
  sharedStockSource=sharedStock.Nodes[plant];
  sharedStockTarget=sharedStock.Nodes[intakeNode];
  firstLine=sharedStock.Workshops[plant].ProductionLines[0];
  secondLine=sharedStock.Workshops[plant].ProductionLines[1];
  firstIntake=sharedStock.ProductionIntakes.Single();
  if(firstIntake.Line!=firstLine || firstIntake.Product!=firstProduct || firstIntake.Target!=intakeNode ||
     !firstLine.CycleFinished || secondLine.WaitingProductionTurns!=1 ||
     !secondLine.IntakeFailure.Contains("格納枠") || sharedStockSource.MaplePackages.Single()!=ppRemainder)
   throw new Exception("Saving must preserve the first reservation, the second waiting line and the remaining PP.");
  resolveIntakes.Invoke(sharedStock,new object[]{sharedIntakeWorld});
  if(!sharedStockTarget.Robots.Contains(firstIntake.Product) || sharedStockTarget.HasSpace)
   throw new Exception("The first intake must occupy the last bay.");
  sharedStockTarget.Remove(firstIntake.Product.Id);
  sharedStock.AdvanceIndustry(n=>sharedIntakeWorld.Nodes.All[n].Owner);
  var secondIntake=sharedStock.ProductionIntakes.Single();
  if(secondIntake.Line!=secondLine || secondIntake.Target!=intakeNode || secondIntake.Product.Id==firstIntake.Product.Id ||
     sharedStockSource.MaplePackages.Count!=0)
   throw new Exception("The second line must use its remaining eight units when the bay opens again.");
  resolveIntakes.Invoke(sharedStock,new object[]{sharedIntakeWorld});
  if(sharedStock.ProductionIntakes.Count!=0 || !sharedStockTarget.Robots.Contains(secondIntake.Product))
   throw new Exception("The second product must reach the same designated bay.");
  Console.WriteLine("PASS two lines resume in lane order after restoring a reserved adjacent bay and a waiting line.");
  for(int unit=0;unit<2;unit++)fullStock.AdvanceIndustry(n=>fullWorld.Nodes.All[n].Owner);
  if(fullTarget.MaplePackages.Single()!=nineUnits || fullLine.WaitingProductionTurns!=2 ||
     !fullLine.IntakeFailure.Contains("格納枠") || fullStock.ProductionIntakes.Any(i=>i.Source==rawOrder.Target))
   throw new Exception("Each full-bay retry must preserve all nine units and avoid queuing a product.");
  fullTarget.Remove(fullTarget.Robots.First(r=>!r.Stationed).Id);
  fullStock.AdvanceIndustry(n=>fullWorld.Nodes.All[n].Owner);
  var recoveredProduct=fullStock.ProductionIntakes.Single(i=>i.Source==rawOrder.Target).Product;
  if(recoveredProduct.Parts!=RobotParts.Head || recoveredProduct.Material(RobotParts.Head)!=RobotMaterial.Polypropylene ||
     fullTarget.MaplePackages.Single()!=nineUnits with {Units=1} || fullLine.WaitingProductionTurns!=0)
   throw new Exception("The freed bay must admit a single PP head and charge exactly eight units.");
  Console.WriteLine("PASS repeated full intake waits preserve PP until a bay opens and exactly eight units are charged.");
  if(!rawDispatchWorld.Campaign.FinishDomestic(rawDispatchWorld) ||
     rawDispatchStock.Nodes[directTarget].Robots.Single(robot=>robot.Id==ppHead.Id)!=ppHead ||
     rawDispatchStock.Nodes[directTarget].MaplePackages.Any(raw=>raw.Id==ppRemainder.Id) ||
     rawDispatchStock.Nodes[route[0]].MaplePackages.Count(raw=>raw==ppRemainder)!=1)
   throw new Exception("The head and raw-only shipment did not arrive at their respective destinations.");
  Set("_setup",world);
  Console.WriteLine("PASS the PP head and raw remainder share a bay and reach separate destinations after dispatching the raw cargo.");
  void TestIntegratedDirectMerge(Robot cargoPair,RobotParts receivePart,int pickupRow)
  {
   var mergeWorld=AutoSaveStore.Restore<WorldSetup>(directFixture);
   var mergeStock=mergeWorld.Robots;
   mergeStock.Nodes[route[0]].Remove(outgoingPair.Id);
   mergeStock.Nodes[route[0]].Add(cargoPair);
   var otherPart=mergeStock.Create(0,receivePart) with {HeadMaterial=RobotMaterial.Maple,LegsMaterial=RobotMaterial.Maple};
   var receiver=mergeStock.Create(0,receivePart) with {HeadMaterial=RobotMaterial.Silicone,HeadOrigin=1,LegsMaterial=RobotMaterial.Silicone,LegsOrigin=1};
   mergeStock.Nodes[directTarget].Add(otherPart);
   mergeStock.Nodes[directTarget].Add(receiver);
   Set("_setup",mergeWorld);
   foreach(var slots in directSlots)Array.Clear(slots);
   DrawDirectPanel(pixels);
   int mergeSlot=Array.IndexOf(directSlots[receivingSide],(long?)receiver.Id);
   if(mergeSlot!=1)throw new Exception("The existing part must occupy the second receiving slot.");
   int shippingSide=1-receivingSide;
   int pickupSlot=Array.IndexOf(directSlots[shippingSide],(long?)cargoPair.Id);
   if(pickupSlot<0)throw new Exception("The integrated pair has no shipping slot.");
   var viewPart=typeof(Game1).GetMethod("DirectViewPart",Flags)!;
   var input=typeof(Game1).GetMethod("DirectPairInput",Flags)!;
   var release=new Microsoft.Xna.Framework.Input.MouseState();
   var pressed=new Microsoft.Xna.Framework.Input.MouseState(0,0,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,
    Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,
    Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
   Set("_pointer",((Rectangle)viewPart.Invoke(this,new object[]{shippingSide,pickupSlot,pickupRow})!).Center);
   input.Invoke(this,new object[]{pressed,true,false});
   Set("_pointer",((Rectangle)viewPart.Invoke(this,new object[]{receivingSide,mergeSlot,0})!).Center);
   Set("_previousMouse",pressed);
   input.Invoke(this,new object[]{release,false,false});
   Set("_previousMouse",release);
   Set("_pointer",Point.Zero);
   var mergeOrder=mergeStock.PartPlanning.Orders.Single();
   if(mergeOrder.RobotId!=cargoPair.Id || mergeOrder.TargetRobotId!=receiver.Id || mergeOrder.Parts!=cargoPair.Parts)
    throw new Exception("Dropping on the second receiving slot selected the wrong assembly.");
   DrawDirectPanel(expectedPixels);
   var receivingCell=(Rectangle)directViewCell.Invoke(this,new object[]{receivingSide,mergeSlot})!;
   bool ReceivingCellChanged(Color[] reference,Color[] comparison)=>
    Enumerable.Range(receivingCell.Top/2,receivingCell.Height/2).Any(y=>
     Enumerable.Range(receivingCell.Left/2,receivingCell.Width/2).Any(x=>reference[y*960+x]!=comparison[y*960+x]));
   if(!ReceivingCellChanged(pixels,expectedPixels))
    throw new Exception("The assembly forecast did not change the receiving slot on the direct panel.");
   var forecastMerge=mergeStock.PartPlanning.Preview(mergeWorld).Nodes[directTarget].Robots.Single(robot=>robot.Id==receiver.Id);
   if(!SameParts(receiver.Merge(cargoPair,0),forecastMerge))
    throw new Exception("Drag-and-drop forecast lost the integrated pair or the existing part.");
   var shippingLayout=directSlots[shippingSide].ToArray();
   mergeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(mergeWorld));
   mergeStock=mergeWorld.Robots;
   Set("_setup",mergeWorld);
   foreach(var slots in directSlots)Array.Clear(slots);
   DrawDirectPanel(alternativePixels);
   if(directSlots[receivingSide][mergeSlot]!=receiver.Id || ReceivingCellChanged(expectedPixels,alternativePixels))
    throw new Exception("Restoring the assembly forecast changed the receiving slot or its artwork.");
   shippingLayout.CopyTo(directSlots[shippingSide],0);
   DrawDirectPanel(alternativePixels);
   if(!expectedPixels.SequenceEqual(alternativePixels))
    throw new Exception("Restoring the same shipping slot layout did not reproduce the direct panel.");
   var undoWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(mergeWorld));
   Set("_setup",undoWorld);
   Set("_pointer",new Point(960,84));
   input.Invoke(this,new object[]{pressed,true,false});
   Set("_pointer",Point.Zero);
   DrawDirectPanel(alternativePixels);
   var undoForecast=undoWorld.Robots.PartPlanning.Preview(undoWorld);
   if(undoWorld.Robots.PartPlanning.Orders.Count!=0 ||
      !SameParts(cargoPair,undoForecast.Nodes[route[0]].Robots.Single(robot=>robot.Id==cargoPair.Id)) ||
      !SameParts(receiver,undoForecast.Nodes[directTarget].Robots.Single(robot=>robot.Id==receiver.Id)) ||
      directSlots[receivingSide][mergeSlot]!=receiver.Id || ReceivingCellChanged(pixels,alternativePixels))
    throw new Exception("Undoing the restored direct shipment did not restore the source pair and receiving cell.");
   int rebookSource=Array.IndexOf(directSlots[shippingSide],(long?)cargoPair.Id);
   int rebookTarget=Array.IndexOf(directSlots[receivingSide],(long?)otherPart.Id);
   if(rebookSource<0 || rebookTarget!=0)throw new Exception("The cancelled shipment cannot be rebooked into the adjacent slot.");
   Set("_pointer",((Rectangle)viewPart.Invoke(this,new object[]{shippingSide,rebookSource,pickupRow})!).Center);
   input.Invoke(this,new object[]{pressed,true,false});
   Set("_pointer",((Rectangle)viewPart.Invoke(this,new object[]{receivingSide,rebookTarget,0})!).Center);
   Set("_previousMouse",pressed);
   input.Invoke(this,new object[]{release,false,false});
   Set("_previousMouse",release);
   Set("_pointer",Point.Zero);
   var rebookedPlan=undoWorld.Robots.PartPlanning.Orders.Single();
   DrawDirectPanel(expectedPixels);
   var rebookedCell=(Rectangle)directViewCell.Invoke(this,new object[]{receivingSide,rebookTarget})!;
   bool RebookedCellChanged(Color[] reference,Color[] comparison)=>
    Enumerable.Range(rebookedCell.Top/2,rebookedCell.Height/2).Any(y=>
     Enumerable.Range(rebookedCell.Left/2,rebookedCell.Width/2).Any(x=>reference[y*960+x]!=comparison[y*960+x]));
   if(!RebookedCellChanged(pixels,expectedPixels) || ReceivingCellChanged(pixels,expectedPixels))
    throw new Exception("Rebooking changed the wrong receiving slot.");
   undoWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(undoWorld));
   Set("_setup",undoWorld);
   foreach(var slots in directSlots)Array.Clear(slots);
   DrawDirectPanel(alternativePixels);
   if(undoWorld.Robots.PartPlanning.Orders.Single()!=rebookedPlan)
    throw new Exception("Restoring the rebooked delivery changed the order.");
   void CheckRestoredCell(long robotId,Rectangle beforeCell){
    int slot=Array.IndexOf(directSlots[receivingSide],(long?)robotId);
    if(slot<0)throw new Exception($"Restoring the rebooked delivery lost robot {robotId} from the receiving pane.");
    var afterCell=(Rectangle)directViewCell.Invoke(this,new object[]{receivingSide,slot})!;
    for(int y=0;y<beforeCell.Height/2;y++)
     for(int x=0;x<beforeCell.Width/2;x++)
      if(expectedPixels[(beforeCell.Top/2+y)*960+beforeCell.Left/2+x]!=
         alternativePixels[(afterCell.Top/2+y)*960+afterCell.Left/2+x])
       throw new Exception($"Restoring the rebooked delivery changed the receiving artwork for robot {robotId}.");
   }
   CheckRestoredCell(otherPart.Id,rebookedCell);
   CheckRestoredCell(receiver.Id,receivingCell);
   int restoredReceiverSlot=Array.IndexOf(directSlots[receivingSide],(long?)receiver.Id);
   int receiverRow=receivePart==RobotParts.Head?0:2;
   Set("_pointer",((Rectangle)viewPart.Invoke(this,new object[]{receivingSide,restoredReceiverSlot,receiverRow})!).Center);
   input.Invoke(this,new object[]{pressed,true,false});
   if(Get("_directDrag") is not ValueTuple<int,long,RobotParts> picked ||
      picked!=(receivingSide,receiver.Id,receivePart))
    throw new Exception("Clicking the restored receiver picked a different robot or part.");
   Set("_previousMouse",pressed);
   input.Invoke(this,new object[]{release,false,false});
   Set("_previousMouse",release);
   Set("_pointer",Point.Zero);
   if(undoWorld.Robots.PartPlanning.Orders.Single()!=rebookedPlan)
    throw new Exception("Releasing the restored receiver on itself changed the rebooked order.");
   if(rebookedPlan.Number==mergeOrder.Number || rebookedPlan.TargetRobotId!=otherPart.Id ||
      rebookedPlan.RobotId!=cargoPair.Id || rebookedPlan.Parts!=cargoPair.Parts ||
      !undoWorld.Campaign.FinishDomestic(undoWorld) || undoWorld.Robots.PartPlanning.Orders.Count!=0 ||
      !SameParts(otherPart.Merge(cargoPair,0),undoWorld.Robots.Nodes[directTarget].Robots.Single(robot=>robot.Id==otherPart.Id)) ||
      !SameParts(receiver,undoWorld.Robots.Nodes[directTarget].Robots.Single(robot=>robot.Id==receiver.Id)))
    throw new Exception("Rebooking after undo did not assemble solely in the adjacent receiving slot.");
   Set("_setup",world);
   var restoredMerge=mergeStock.PartPlanning.Preview(mergeWorld).Nodes[directTarget].Robots.Single(robot=>robot.Id==receiver.Id);
   if(mergeStock.PartPlanning.Orders.Single()!=mergeOrder || restoredMerge.Id!=receiver.Id ||
      !SameParts(forecastMerge,restoredMerge))
    throw new Exception("Restoring the direct shipment changed its receiver or forecast assembly.");
   RenderCompact(forecastMerge,plannedCaptain,expectedPixels);
   RenderCompact(restoredMerge,plannedCaptain,pixels);
   if(!pixels.SequenceEqual(expectedPixels))
    throw new Exception("Restoring the direct shipment changed the assembly forecast artwork.");
   if(!mergeWorld.Campaign.FinishDomestic(mergeWorld) || mergeStock.PartPlanning.Orders.Count!=0 ||
      mergeStock.Nodes[route[0]].Robots.Any(robot=>robot.Id==cargoPair.Id))
    throw new Exception("Drag-and-drop did not confirm the integrated shipment.");
   var confirmedMerge=mergeStock.Nodes[directTarget].Robots.Single(robot=>robot.Id==receiver.Id);
   if(!SameParts(forecastMerge,confirmedMerge) ||
      !SameParts(otherPart,mergeStock.Nodes[directTarget].Robots.Single(robot=>robot.Id==otherPart.Id)))
    throw new Exception("Confirmed assembly changed the receiver, integration, materials or origins.");
   RenderCompact(confirmedMerge,plannedCaptain,pixels);
   if(!pixels.SequenceEqual(expectedPixels))
    throw new Exception("Assembly after direct delivery changed the forecast artwork.");
   Console.WriteLine("PASS direct arrival into an existing part preserves the integrated pair and mixed compact artwork.");
  }

  static bool SameParts(Robot expected,Robot actual)=>
   expected.Parts==actual.Parts && expected.IntegratedParts==actual.IntegratedParts && expected.Role==actual.Role &&
   new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(part=>(expected.Parts&part)!=0)
    .All(part=>expected.Material(part)==actual.Material(part) && expected.Origin(part)==actual.Origin(part));

  void RenderCompact(Robot pair,Robot chief,Color[] target)
  {
   GraphicsDevice.Clear(background);
   batch.Begin();
   delivery.Invoke(this,new object[]{pairBounds,pair,1f,null});
   delivery.Invoke(this,new object[]{compactBounds,chief,1f,null});
   batch.End();
   GraphicsDevice.GetBackBufferData(target);
  }
 }
 protected override void Draw(GameTime time){
  if(drawn)return;drawn=true;if(shipmentsOnly){CheckShipmentArt();Exit();return;}if(galleryOnly){DrawMaterialGallery();Exit();return;}GraphicsDevice.Clear(new Color(25,36,48));batch.Begin();
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
  pageClick.Invoke(this,new object[]{new Point(470,844)});
  if((int)Get("_hangarPage")!=1 || Get("_hangarDrag")!=null)throw new Exception("Down arrow must scroll one row and cancel stale drag");
  pageClick.Invoke(this,new object[]{new Point(470,844)});
  if((int)Get("_hangarPage")!=1)throw new Exception("Second scroll did not show the last row");
  pageClick.Invoke(this,new object[]{new Point(470,844)});
  if((int)Get("_hangarPage")!=1)throw new Exception("Scroll exceeded capacity");
  pageClick.Invoke(this,new object[]{new Point(85,288)});
  pageClick.Invoke(this,new object[]{new Point(863,288)});
  if((int)Get("_hangarPage")!=0)throw new Exception("Previous page did not return to slots0-5");
  Console.WriteLine("PASS actual page-button input: next/previous clamp and held-drag cancellation.");
  var head=new Robot(100001,0,RobotParts.Head);var body=new Robot(100002,0,RobotParts.Body);
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{new[]{head,body}});
  w.Robots.PartPlanning.Invalidate();w.HumanPlayers.Add(0);Set("_nodeAssemblyOpen",true);
  var mapping=new long?[12];mapping[0]=head.Id;mapping[8]=body.Id;Set("_hangarSlots",mapping);Set("_hangarPage",0);
  var input=typeof(Game1).GetMethod("NodeAssemblyLocalInput",Flags)!;
  var partBox=typeof(Game1).GetMethod("AssemblyPartBounds",Flags)!;
  var pressed=new Microsoft.Xna.Framework.Input.MouseState(0,0,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
  Set("_pointer",((Rectangle)partBox.Invoke(this,new object[]{0,0})!).Center);
  input.Invoke(this,new object[]{pressed,true,false});
  if((long?)Get("_hangarDrag")!=head.Id || (int)Get("_hangarSource")!=0)throw new Exception("Head press selected another robot/slot");
  Set("_pointer",new Point(470,844));Set("_previousMouse",pressed);Set("_animationTime",10f);
  input.Invoke(this,new object[]{pressed,false,false});
  Set("_animationTime",10.4f);input.Invoke(this,new object[]{pressed,false,false});
  if((int)Get("_hangarPage")!=0)throw new Exception("Drag hover skipped its initial delay");
  Set("_animationTime",10.6f);input.Invoke(this,new object[]{pressed,false,false});
  if((int)Get("_hangarPage")!=1 || (long?)Get("_hangarDrag")!=head.Id)throw new Exception("Drag hover must expose the second row while retaining the source");
  Set("_animationTime",11f);input.Invoke(this,new object[]{pressed,false,false});
  if((int)Get("_hangarPage")!=1)throw new Exception("Drag hover repeated too quickly");
  Set("_animationTime",11.4f);input.Invoke(this,new object[]{pressed,false,false});
  if((int)Get("_hangarPage")!=1)throw new Exception("Holding the arrow must repeat after the interval");
  Set("_pointer",new Point(85,288));input.Invoke(this,new object[]{pressed,false,false});
  Set("_animationTime",12f);input.Invoke(this,new object[]{pressed,false,false});
  if((int)Get("_hangarPage")!=0 || (long?)Get("_hangarDrag")!=head.Id)
   throw new Exception("Hovering the up arrow must scroll back with the same dragged part");
  Set("_hangarPage",1);Set("_pointer",((Rectangle)cell.Invoke(null,new object[]{3})!).Center);
  input.Invoke(this,new object[]{new Microsoft.Xna.Framework.Input.MouseState(),false,false});
  var reservation=w.Robots.PartPlanning.Orders.Single();
  if(reservation.RobotId!=head.Id || reservation.TargetRobotId!=body.Id || reservation.Parts!=RobotParts.Head)throw new Exception("Page2 drop reserved wrong source or destination");
  if(Get("_hangarDrag")!=null)throw new Exception("Released drag remained active");
  Console.WriteLine("PASS held drag waits, scrolls one row, and drops physical slot 0 into physical slot 8.");
  w.Robots.PartPlanning.Orders.Clear();w.Robots.PartPlanning.Invalidate();
  var legs = new Robot(100003, 0, RobotParts.Legs);
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{new[]{legs,body}});
  w.Robots.PartPlanning.Invalidate();
  mapping=new long?[12];mapping[8]=legs.Id;mapping[9]=body.Id;Set("_hangarPage",1);
  Set("_hangarSlots", mapping);
  Set("_pointer", ((Rectangle)partBox.Invoke(this,new object[]{8,2})!).Center);
  input.Invoke(this,new object[]{pressed,true,false});
  if ((long?)Get("_hangarDrag") != legs.Id || (RobotParts)Get("_hangarPart") != RobotParts.Legs)
   throw new Exception("Page2 leg press selected another robot or part");
  Set("_pointer", ((Rectangle)partBox.Invoke(this,new object[]{9,2})!).Center);
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
  long?[] PlaceSix(Robot[] robots){var ids=new long?[12];for(int i=0;i<6;i++)ids[i+i/3]=robots[i].Id;return ids;}
  var complete=Enumerable.Range(0,6).Select(i=>new Robot(200001+i,0,RobotParts.Complete)).ToArray();
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{complete});
  Set("_hangarSlots",PlaceSix(complete));Set("_hangarPage",0);Set("_hangarReservation",HangarReservation.Defense);
  Set("_pointer",((Rectangle)cell.Invoke(null,new object[]{1})!).Center);
  input.Invoke(this,new object[]{pressed,true,false});
  var preview=w.Robots.PartPlanning.Preview(w);
  if(!preview.Nodes[node].Robots.Single(r=>r.Id==200002).DefenseOnly || preview.Nodes[node].Robots.Any(r=>r.Id!=200002 && r.DefenseOnly))throw new Exception("Defense click reserved another displayed robot");
  Console.WriteLine("PASS actual six-slot defense click reserves only second robot200002.");
  Set("_pointer",Point.Zero);GraphicsDevice.Clear(new Color(25,36,48));batch.Begin(transformMatrix:Matrix.CreateScale(.5f));
  typeof(Game1).GetMethod("DrawNodeAssembly",Flags)!.Invoke(this,null);batch.End();
  var data=new Color[960*540];GraphicsDevice.GetBackBufferData(data);using var image=new Texture2D(GraphicsDevice,960,540);image.SetData(data);using(var f=File.Create(output))image.SaveAsPng(f,960,540);
  w.Robots.PartPlanning.Orders.Clear();w.Robots.PartPlanning.Invalidate();
  var originalMode=w.EquipmentMode;
  int originalEmblem=w.CountryEmblem(0);
  w.EquipmentMode=RobotEquipmentMode.Materials;
  var paperHangar=complete.Select((robot,index)=>robot with {
   HeadMaterial=RobotMaterial.Paper,BodyMaterial=RobotMaterial.Paper,LegsMaterial=RobotMaterial.Paper,
   DamagedParts=index%2==0?(RobotParts)0:RobotParts.Complete
  }).ToArray();
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{paperHangar});
  Set("_hangarReservation",HangarReservation.None);
  var assemblyDraw=typeof(Game1).GetMethod("DrawNodeAssembly",Flags)!;
  Set("_hangarSlots",new long?[60]);
  var zoomVisible=typeof(Game1).GetMethod("VisibleAssemblySlots",Flags)!;
  var assemblyCell=typeof(Game1).GetMethod("AssemblyCell",Flags)!;
  var limits=new[]{2,6,12,24,40,60};
  for(int level=0;level<limits.Length;level++){
   Set("_hangarZoom",level);
   var visibleSlots=((IEnumerable<int>)zoomVisible.Invoke(this,null)!).ToArray();
   if(visibleSlots.Length!=limits[level])throw new Exception("Wrong zoom capacity");
   foreach(int slot in visibleSlots){
    var bounds=(Rectangle)assemblyCell.Invoke(this,new object[]{slot})!;
    if(bounds.X<84 || bounds.Y<300 || bounds.Right>864 || bounds.Bottom>830)
     throw new Exception("Zoom bay outside viewport");
   }
  }
  Set("_hangarZoom",1);
  Set("_pointer",new Point(100,320));
  Set("_previousMouse",new Microsoft.Xna.Framework.Input.MouseState());
  var wheel=new Microsoft.Xna.Framework.Input.MouseState(0,0,-120,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
  input.Invoke(this,new object[]{wheel,false,false});
  if((int)Get("_hangarZoom")!=2)throw new Exception("Wheel must select the next smaller zoom");
  input.Invoke(this,new object[]{wheel,false,false});
  if((int)Get("_hangarZoom")!=2)throw new Exception("Wheel changed zoom during cooldown");
  Set("_hangarZoom",1);
  pageClick.Invoke(this,new object[]{new Point(879,560)});
  if((int)Get("_hangarColumn")!=1 || ((IEnumerable<int>)zoomVisible.Invoke(this,null)!).First()!=1)
   throw new Exception("Right arrow moved the wrong physical slots");
  pageClick.Invoke(this,new object[]{new Point(66,560)});
  if((int)Get("_hangarColumn")!=0)throw new Exception("Left arrow did not return");
  Console.WriteLine("PASS all six zoom capacities and horizontal slot navigation.");
  var rightPressed=new Microsoft.Xna.Framework.Input.MouseState(0,0,0,
   Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,
   Microsoft.Xna.Framework.Input.ButtonState.Pressed,Microsoft.Xna.Framework.Input.ButtonState.Released,
   Microsoft.Xna.Framework.Input.ButtonState.Released);
  Set("_previousMouse",new Microsoft.Xna.Framework.Input.MouseState());
  Set("_pointer",new Point(500,650));input.Invoke(this,new object[]{rightPressed,false,false});
  Set("_previousMouse",rightPressed);
  Set("_pointer",new Point(220,360));input.Invoke(this,new object[]{rightPressed,false,false});
  if(!(bool)Get("_hangarPanning") || (int)Get("_hangarColumn")!=1 || (int)Get("_hangarPage")!=1)
   throw new Exception("Right drag must pan the hangar in both axes");
  Set("_pointer",new Point(-3000,-3000));input.Invoke(this,new object[]{rightPressed,false,false});
  if((int)Get("_hangarColumn")!=7 || (int)Get("_hangarPage")!=4)
   throw new Exception("Right drag must clamp at the last column and row");
  input.Invoke(this,new object[]{new Microsoft.Xna.Framework.Input.MouseState(),false,false});
  if((bool)Get("_hangarPanning"))throw new Exception("Releasing the right button must stop panning");
  Set("_hangarColumn",0);Set("_hangarPage",0);
  Console.WriteLine("PASS right drag pans diagonally, clamps and stops on release.");
  typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{60});
  Set("_hangarIntroStart",20f);
  foreach(var (frameTime,name) in new[]{(20f,"overview"),(20.1f,"transition"),(20.21f,"basic")}){
   Set("_animationTime",frameTime);
   GraphicsDevice.Clear(Color.Black);batch.Begin(transformMatrix:Matrix.CreateScale(.5f));
   assemblyDraw.Invoke(this,null);batch.End();
   GraphicsDevice.GetBackBufferData(data);
   image.SetData(data);
   using var frame=File.Create(Path.Combine(Path.GetDirectoryName(output)!,$"hangar-{name}.png"));
   image.SaveAsPng(frame,960,540);
  }
  typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{12});
  Set("_hangarSlots",PlaceSix(paperHangar));
  Set("_hangarPage",0);
  var withEmblem=new Color[data.Length];
  foreach(int emblem in new[]{1,0}){
   w.SetCountryEmblem(0,emblem);
   GraphicsDevice.Clear(new Color(25,36,48));batch.Begin(transformMatrix:Matrix.CreateScale(.5f));
   assemblyDraw.Invoke(this,null);batch.End();
   GraphicsDevice.GetBackBufferData(emblem==1?withEmblem:data);
  }
  for(int slot=0;slot<6;slot++){
   var bounds=(Rectangle)cell.Invoke(null,new object[]{slot})!;
   int changed=Enumerable.Range(bounds.Top/2,bounds.Height/2).Sum(y=>
    Enumerable.Range(bounds.Left/2,bounds.Width/2).Count(x=>withEmblem[y*960+x]!=data[y*960+x]));
   if(changed==0)throw new Exception($"Paper emblem not visible in hangar slot {slot}");
  }
  image.SetData(withEmblem);
  using(var f=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"paper-hangar.png")))image.SaveAsPng(f,960,540);
  w.SetCountryEmblem(0,originalEmblem);
  w.EquipmentMode=originalMode;
  Console.WriteLine("PASS paper emblems affect pixels in all six actual hangar cells at 960x540.");
  typeof(RobotStore).GetMethod("SetCapacity",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{7});
  var foreign=new Robot(300001,1,RobotParts.Complete);
  typeof(RobotStore).GetMethod("Replace",Flags)!.Invoke(w.Robots.Nodes[node],new object[]{new[]{foreign}});
  var lastPage=new long?[7];lastPage[3]=foreign.Id;Set("_hangarSlots",lastPage);Set("_hangarPage",0);Set("_hangarReturnHomeMode",true);Set("_hangarReservation",HangarReservation.None);
  Set("_pointer",new Point(879,560));input.Invoke(this,new object[]{pressed,true,false});
  if((int)Get("_hangarColumn")!=1 || !(bool)Get("_hangarReturnHomeMode"))throw new Exception("Return-home mode swallowed right-arrow click");
  Set("_pointer",((Rectangle)assemblyCell.Invoke(this,new object[]{3})!).Center);input.Invoke(this,new object[]{pressed,true,false});
  if(w.Robots.Nodes[node].Robots.Single().ReturnHomeCommander!=0)throw new Exception("Return-home click missed the robot");
  Set("_pointer",((Rectangle)assemblyCell.Invoke(this,new object[]{7})!).Center);input.Invoke(this,new object[]{pressed,true,false});
  if(w.Robots.Nodes[node].Robots.Single().ReturnHomeCommander!=0)throw new Exception("Nonexistent cell toggled visible robot");
  Set("_pointer",((Rectangle)assemblyCell.Invoke(this,new object[]{3})!).Center);input.Invoke(this,new object[]{pressed,true,false});
  if(w.Robots.Nodes[node].Robots.Single().ReturnHomeCommander.HasValue)throw new Exception("Second return-home click failed to cancel");
  Set("_hangarColumn",0);
  Console.WriteLine("PASS return-home mode scrolls right; correct ID toggles; nonexistent cells do nothing.");
  var pairNodes=(int[])Get("_directNodes");pairNodes[0]=node;pairNodes[1]=node;
  var directSlots=(long?[][])Get("_directSlots");directSlots[0]=new long?[7];directSlots[1]=new long?[7];
  var pages=(int[])Get("_directPages");pages[0]=pages[1]=0;
  var columns=(int[])Get("_directColumn");
  var directClick=typeof(Game1).GetMethod("DirectPageClick",Flags)!;
  directClick.Invoke(this,new object[]{new Point(1840,900)});
  if(columns[0]!=0 || columns[1]!=1)throw new Exception("Right delivery scroll changed wrong inspector");
  directClick.Invoke(this,new object[]{new Point(1840,900)});
  if(columns[1]!=1)throw new Exception("Right delivery final column exceeded capacity7");
  directClick.Invoke(this,new object[]{new Point(510,900)});
  if(columns[0]!=1 || columns[1]!=1)throw new Exception("Left delivery scroll changed wrong inspector");
  Console.WriteLine("PASS independent delivery horizontal scrolling and capacity7 final-column clamp.");
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
  directSlots[0][6]=cargo.Id;directSlots[1][6]=receiver.Id;
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
  var directViewPart=typeof(Game1).GetMethod("DirectViewPart",Flags)!;
  Set("_pointer",((Rectangle)directViewPart.Invoke(this,new object[]{0,6,0})!).Center);
  directInput.Invoke(this,new object[]{pressed,true,false});
  if(Get("_directDrag")==null)throw new Exception("Direct delivery page2 did not pick up cargo");
  Set("_pointer",((Rectangle)directViewPart.Invoke(this,new object[]{1,6,0})!).Center);
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
  var scenicType=typeof(CubeNet).Assembly.GetType("FolderVerse.ScenicRoutes")!;
  var throughRoutes=scenicType.GetMethod("ThroughRoutes",BindingFlags.Static|BindingFlags.NonPublic)!;
  var edges=new (int,int)[]{(0,1),(1,2),(1,3),(3,4),(4,1),(2,5),(6,7)};
  var functional=new[]{true,false,true,false,false,false,false,false};
  bool[] Through()=> (bool[])throughRoutes.Invoke(null,new object[]{functional,edges})!;
  if(!Through().SequenceEqual(new[]{true,true,false,false,false,false,false}))
   throw new Exception("An empty through stop must stay solid while dead ends and attached empty loops become dotted");
  functional[4]=true;
  if(!Through().SequenceEqual(new[]{true,true,true,true,true,false,false}))
   throw new Exception("Reactivating a stop must restore both ways through its loop");
  net.VisibleOperationNodes=null;
  foreach(var scenicNode in globeWorld.Nodes.All) scenicNode.FunctionUnits=0;
  Color[] ScenicFrame(bool onGlobe,float time,string name)
  {
   GraphicsDevice.Clear(new Color(25,36,48));
   if(onGlobe)globe.Draw(.55f,-.35f,markers:false,globeView:true,globeFrame:globeArea);
   batch.Begin();
   if(onGlobe)net.DrawGlobeLayers((UiPainter)Get("_ui"),globeWorld,globe,globeArea,.55f,-.35f,time);
   else net.Draw((UiPainter)Get("_ui"),globeWorld,globeArea,-1,time,objects:false);
   batch.End();
   var frame=new Color[960*540];
   GraphicsDevice.GetBackBufferData(frame);
   if(name!=null){
    globeImage.SetData(frame);
    using var file=File.Create(Path.Combine(Path.GetDirectoryName(output)!,name));
    globeImage.SaveAsPng(file,960,540);
   }
   return frame;
  }
  foreach(bool onGlobe in new[]{true,false}){
   var first=ScenicFrame(onGlobe,0,onGlobe?"globe-scenic-routes.png":"net-scenic-routes.png");
   var later=ScenicFrame(onGlobe,2.5f,null);
   foreach(var ink in new Color[]{new(255,230,140),new(111,228,255),new(181,133,63),new(190,77,56)})
    if(!first.Contains(ink) && !later.Contains(ink))throw new Exception($"Missing scenic route or vehicle ink {ink}, globe={onGlobe}");
   if(first.SequenceEqual(later))throw new Exception("Scenic vehicles must move between frames");
  }
  foreach(var scenicNode in globeWorld.Nodes.All) scenicNode.FunctionUnits=1;
  var restoredRoutes=ScenicFrame(true,0,null);
  if(!restoredRoutes.Contains(new Color(255,216,99)) || !restoredRoutes.Contains(new Color(108,213,245)) ||
     restoredRoutes.Contains(new Color(181,133,63)))
   throw new Exception("Changing node functions must invalidate the scenic display");
  Console.WriteLine("PASS scenic routes retain through paths, animate land and sea vehicles in both views and refresh with node functions.");
  var materialWorld=(WorldSetup)Get("_setup");
  materialWorld.EquipmentMode=RobotEquipmentMode.Materials;
  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin();
  var mapleHead=new Robot(501,0,RobotParts.Head){HeadMaterial=RobotMaterial.Maple};
  for(int variant=0;variant<3;variant++)
  {
   var part=variant switch {1=>mapleHead with {HeadOrigin=1},2=>mapleHead with {DamagedParts=RobotParts.Head},_=>mapleHead};
   draw.Invoke(this,new object[]{new Rectangle(45+200*variant,110,140,210),part,false,1f,null});
   var full=part with {Parts=RobotParts.Complete,BodyMaterial=RobotMaterial.Maple,LegsMaterial=RobotMaterial.Maple};
   if(variant==1)full=full with {BodyOrigin=1,LegsOrigin=1};
   if(variant==2)full=full with {DamagedParts=RobotParts.Complete};
   draw.Invoke(this,new object[]{new Rectangle(45+200*variant,250,140,210),full,false,1f,null});
  }
  batch.End();
  if(Get("_mapleHeadTextures")==null)throw new Exception("Material glyph did not load maple head");
  if(Get("_mapleTorsoTextures")==null || Get("_mapleLegTextures")==null)
   throw new Exception("Material glyph did not load maple torso and legs");
  GraphicsDevice.GetBackBufferData(globePixels);
  for(int variant=1;variant<3;variant++)
  {
   int changes=Enumerable.Range(100,86).Sum(y=>Enumerable.Range(65,100)
    .Count(x=>globePixels[y*960+x]!=globePixels[y*960+x+200*variant]));
   if(changes<20)throw new Exception("Maple flag or wear did not change the drawn head");
  }
  globeImage.SetData(globePixels);
  using (var mapleFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"maple-game-glyph.png")))
   globeImage.SaveAsPng(mapleFile,960,540);
  Console.WriteLine("PASS actual maple glyph shows country and wear variants.");
  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin();
  var mapleSoldier = mapleHead with { Parts = RobotParts.Complete };
  var directions = new[] { BattleFacing.East, BattleFacing.West, BattleFacing.North };
  for (int index = 0; index < directions.Length; index++)
  for (int owner = 0; owner < 2; owner++)
  {
   var soldier = mapleSoldier with { HeadOrigin = owner, BodyMaterial = RobotMaterial.Maple,
     LegsMaterial = RobotMaterial.Maple, BodyOrigin = owner, LegsOrigin = owner };
   if (!(bool)soldierArt.Invoke(this, new object[] { new Rectangle(100 + 225 * index, 100 + 175 * owner, 96, 145),
     soldier, BattleRole.Soldier, directions[index], false, 1f })!)
    throw new Exception($"Maple head did not draw facing {directions[index]}");
  }
  batch.End();
  var sideHead = (RobotHeadTexture)Get("_mapleSideHead");
  int size = RobotHeadTexture.Resolution;
  var firstFlag = new Color[size * size];
  var secondFlag = new Color[size * size];
  sideHead.Get(materialWorld,0).GetData(firstFlag);
  sideHead.Get(materialWorld,1).GetData(secondFlag);
  int protectedPixels = 0, paintedChanges = 0;
  for (int index = 0; index < firstFlag.Length; index++)
  {
   if (firstFlag[index].A != secondFlag[index].A)
    throw new Exception("Maple profile alpha changed");
   if (index % size >= 170 && firstFlag[index].A != 0)
   {
    protectedPixels++;
    if (firstFlag[index] != secondFlag[index])
     throw new Exception("Maple profile face or bare wood changed");
   }
   if (firstFlag[index] != secondFlag[index]) paintedChanges++;
  }
  if (protectedPixels < 100 || paintedChanges < 100)
   throw new Exception("Maple profile mask or paint did not cover the sprite");
  var rearLegs = (RobotHeadTexture)Get("_mapleRearLegs");
  var sideLegs = (RobotHeadTexture)Get("_mapleSideLegs");
  if (Get("_mapleSideTorso") == null || sideLegs == null)
   throw new Exception("Maple side body assets were not used");
  sideLegs.Get(materialWorld,0).GetData(firstFlag);
  sideLegs.Get(materialWorld,1).GetData(secondFlag);
  if (!firstFlag.Select(pixel => pixel.A).SequenceEqual(secondFlag.Select(pixel => pixel.A)) ||
      firstFlag[190 * size + 100].A == 0 ||
      firstFlag[190 * size + 100] != secondFlag[190 * size + 100] ||
      firstFlag.SequenceEqual(secondFlag))
   throw new Exception("Maple side legs lost their alpha, bare boot or flag paint");
  if (Get("_mapleRearTorso") == null || rearLegs == null)
   throw new Exception("Maple rear body assets were not used");
  rearLegs.Get(materialWorld,0).GetData(firstFlag);
  rearLegs.Get(materialWorld,1).GetData(secondFlag);
  if (!firstFlag.Select(pixel => pixel.A).SequenceEqual(secondFlag.Select(pixel => pixel.A)) ||
      firstFlag[190 * size + size / 2].A != 0 ||
      firstFlag[219 * size + 80].A == 0 ||
      firstFlag[219 * size + 80] != secondFlag[219 * size + 80] ||
      firstFlag.SequenceEqual(secondFlag))
   throw new Exception("Maple rear legs lost their transparent gap, bare heel or flag paint");
  GraphicsDevice.GetBackBufferData(globePixels);
  globeImage.SetData(globePixels);
  using (var orientedFile = File.Create(Path.Combine(Path.GetDirectoryName(output)!, "maple-oriented.png")))
   globeImage.SaveAsPng(orientedFile,960,540);
  Console.WriteLine("PASS full maple soldiers face east, west and north with protected faces, wooden boots and unchanged alpha.");
  var scene = new BattleState(1) { BoardSize = 10 };
  materialWorld.SetCast(2);
  scene.AddArmy(0, 0, Array.Empty<Robot>());
  scene.AddArmy(1, 1, Array.Empty<Robot>());
  var replayUnit = new BattleUnit { Id = mapleSoldier.Id, Robot = mapleSoldier with
   { BodyMaterial = RobotMaterial.Maple, LegsMaterial = RobotMaterial.Maple },
   Owner = 0, Role = BattleRole.Soldier, Position = new Point(4, 4), Facing = BattleFacing.East, Rifles = 1 };
  scene.Units.Add(replayUnit);
  scene.Capture(BattlePhase.Choose);
  replayUnit.Rifles = 0;
  scene.Capture(BattlePhase.Consume);
  replayUnit.Facing = BattleFacing.North;
  scene.Capture(BattlePhase.Choose);
  var scenes = new[] { scene };
  var playback = new RobotBattlePlayback(scenes);
  Set("_robotScenes", scenes);
  Set("_robotPlayback", playback);
  Set("_robotRound", 0);
  Set("_battleWave", 0);
  Set("_robotPlaying", true);
  Set("_battleIntroAge", 100d);
  double seconds = RobotBattlePlayback.SecondsPerFrame(scene, playback.Rounds[0][0]);
  var drawBattle = typeof(Game1).GetMethod("DrawRobotBattle", Flags)!;
  var battleFramePixels = new Color[3][];
  for (int index = 0; index < battleFramePixels.Length; index++)
  {
   Set("_battleAge", seconds * (index + .1));
   GraphicsDevice.Clear(background);
   batch.Begin(transformMatrix: Matrix.CreateScale(.5f));
   drawBattle.Invoke(this, null);
   batch.End();
   var pixels = battleFramePixels[index] = new Color[960 * 540];
   GraphicsDevice.GetBackBufferData(pixels);
   globeImage.SetData(pixels);
   using var battleFile = File.Create(Path.Combine(Path.GetDirectoryName(output)!, $"maple-battle-frame-{index}.png"));
   globeImage.SaveAsPng(battleFile,960,540);
  }
  int UnitChanges(Color[] before, Color[] after) => Enumerable.Range(224, 39).Sum(y =>
   Enumerable.Range(227, 36).Count(x => before[y * 960 + x] != after[y * 960 + x]));
  if (UnitChanges(battleFramePixels[0], battleFramePixels[1]) < 4 ||
      UnitChanges(battleFramePixels[1], battleFramePixels[2]) < 20)
   throw new Exception("Full battle rendering did not show rifle removal and the turn to face north");
  Console.WriteLine("PASS full battle screen selects historical maple facing and rifle supply at 960x540.");
  var resolved = new BattleState(42) { Kind = BattleKind.Node, Defender = 1 };
  var mapleRobot = replayUnit.Robot;
  resolved.AddArmy(0, 0, new[] { mapleRobot, mapleRobot with { Id = 502 } }, true);
  resolved.AddArmy(1, 1, new[] { new Robot(601,1,RobotParts.Complete) }, true);
  resolved.BuildTerrain(BattleTerrain.Field);
  resolved.Deploy();
  for (int turn = 0; turn < 200 && !resolved.Finished; turn++)
   BattleTurnResolver.Advance(resolved);
  if (!resolved.Finished || !resolved.Frames.Any(frame => frame.Units.Any(unit =>
      unit.Robot?.HeadMaterial == RobotMaterial.Maple)))
   throw new Exception("Seeded replay must finish with a maple soldier in its history");
  var resolvedScenes = new[] { resolved };
  var resolvedPlayback = new RobotBattlePlayback(resolvedScenes);
  Set("_robotScenes", resolvedScenes);
  Set("_robotPlayback", resolvedPlayback);
  var selectFrame = typeof(Game1).GetMethod("RobotFrameIndex", Flags)!;
  int rendered = 0;
  for (int round = 0; round < resolvedPlayback.Rounds.Length; round++)
  {
   Set("_robotRound", round);
   var slice = resolvedPlayback.Rounds[round][0];
   double frameSeconds = RobotBattlePlayback.SecondsPerFrame(resolved, slice);
   for (int frameIndex = slice.Start; frameIndex <= slice.End; frameIndex++)
   {
    Set("_battleAge", frameSeconds * (frameIndex - slice.Start + .1));
    Set("_animationTime", frameIndex * .1f);
    if ((int)selectFrame.Invoke(this, new object[] { resolved })! != frameIndex)
     throw new Exception("Battle playback skipped a captured frame");
    GraphicsDevice.Clear(background);
    batch.Begin(transformMatrix: Matrix.CreateScale(.5f));
    drawBattle.Invoke(this, null);
    batch.End();
    rendered++;
    if (frameIndex == 0 || frameIndex == resolved.Frames.Count / 2 || frameIndex == resolved.Frames.Count - 1)
    {
     GraphicsDevice.GetBackBufferData(globePixels);
     globeImage.SetData(globePixels);
     using var resolvedFile = File.Create(Path.Combine(Path.GetDirectoryName(output)!, $"maple-resolved-{frameIndex}.png"));
     globeImage.SaveAsPng(resolvedFile,960,540);
    }
   }
  }
  if (rendered != resolved.Frames.Count)
   throw new Exception("The complete battle history was not rendered");
  Console.WriteLine($"PASS seeded battle rendered all {rendered} frames through turn {resolved.Turn}, winner {resolved.Winner}.");
  GraphicsDevice.Clear(background);
  batch.Begin();
  var paper = new Robot(700,0,RobotParts.Head){HeadMaterial=RobotMaterial.Paper};
  draw.Invoke(this,new object[]{new Rectangle(100,110,84,150),paper,false,1f,null});
  draw.Invoke(this,new object[]{new Rectangle(300,110,84,150),paper with {HeadOrigin=1},false,1f,null});
  var paperSoldier=paper with {Parts=RobotParts.Complete, BodyMaterial=RobotMaterial.Paper, LegsMaterial=RobotMaterial.Paper};
  draw.Invoke(this,new object[]{new Rectangle(100,270,84,150),paperSoldier,false,1f,null});
  draw.Invoke(this,new object[]{new Rectangle(300,270,84,150),paperSoldier with {DamagedParts=RobotParts.Complete},false,1f,null});
  if (!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(510,110,62,85),paperSoldier,
       BattleRole.Soldier,BattleFacing.South,true,1f})!)
   throw new Exception("Front soldier failed to use paper head artwork");
  for(int index=0;index<directions.Length;index++){
   var orientedPaper=paperSoldier with {BodyOrigin=index%2,LegsOrigin=index%2, DamagedParts=index==1?RobotParts.Complete:0};
   if (!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(480+120*index,270,62,85),
        orientedPaper with {HeadOrigin=index%2},BattleRole.Soldier,directions[index],true,1f})!)
    throw new Exception($"Oriented soldier failed to use paper artwork facing {directions[index]}");
  }
  batch.End();
  if(Get("_paperSideHead")==null || Get("_paperRearHead")==null)
   throw new Exception("Oriented paper head textures were not loaded");
  var paperArt=(RobotHeadTexture)Get("_paperHeadTextures");
  paperArt.Get(materialWorld,0).GetData(firstFlag);
  paperArt.Get(materialWorld,1).GetData(secondFlag);
  if (firstFlag.SequenceEqual(secondFlag) ||
      !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)) ||
      firstFlag[142*size+110].A==0 || firstFlag[142*size+110]!=secondFlag[142*size+110] ||
      firstFlag[140*size+184].A==0 || firstFlag[140*size+184]!=secondFlag[140*size+184])
   throw new Exception("Paper head flags must preserve face, fold and alpha");
  foreach(string name in new[]{"_paperTorsoTextures","_paperLegTextures","_paperSideTorso","_paperSideLegs","_paperRearTorso","_paperRearLegs"}){
   var partArt=(RobotHeadTexture)Get(name);
   partArt.Get(materialWorld,0).GetData(firstFlag);
   partArt.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)))
    throw new Exception("Paper body wear must preserve alpha and change painted pixels");
  }
  GraphicsDevice.GetBackBufferData(globePixels);
  globeImage.SetData(globePixels);
  using(var paperFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"paper-glyph.png")))
   globeImage.SaveAsPng(paperFile,960,540);
  Console.WriteLine("PASS paper head in shared glyph and front soldier preserves face, fold and alpha.");
  GraphicsDevice.Clear(background);
  batch.Begin();
  var cardboard = new Robot(701,0,RobotParts.Complete){
   HeadMaterial=RobotMaterial.Cardboard,BodyMaterial=RobotMaterial.Cardboard,LegsMaterial=RobotMaterial.Cardboard};
  draw.Invoke(this,new object[]{new Rectangle(100,110,84,150),cardboard,false,1f,null});
  draw.Invoke(this,new object[]{new Rectangle(300,110,84,150),
   cardboard with {HeadOrigin=1,BodyOrigin=1,LegsOrigin=1,DamagedParts=RobotParts.Complete},false,1f,null});
  if (!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(510,110,62,85),cardboard,
       BattleRole.Soldier,BattleFacing.South,true,1f})!)
   throw new Exception("Front soldier failed to draw cardboard parts");
  for(int index=0;index<directions.Length;index++){
   var oriented=cardboard with {HeadOrigin=index%2,BodyOrigin=index%2,LegsOrigin=index%2,
       DamagedParts=index==1?RobotParts.Complete:0};
   if (!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(480+120*index,270,62,85),
        oriented,BattleRole.Soldier,directions[index],true,1f})!)
    throw new Exception($"Soldier failed to draw cardboard head facing {directions[index]}");
  }
  batch.End();
  foreach(string name in new[]{"_cardboardHeadTextures","_cardboardTorsoTextures","_cardboardLegTextures",
      "_cardboardSideHead","_cardboardRearHead","_cardboardSideTorso","_cardboardSideLegs",
      "_cardboardRearTorso","_cardboardRearLegs"}){
   var cardboardArt=(RobotHeadTexture)Get(name);
   cardboardArt.Get(materialWorld,0).GetData(firstFlag);
   cardboardArt.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)))
    throw new Exception($"Cardboard wear must change paint and preserve alpha: {name}");
  }
  GraphicsDevice.GetBackBufferData(globePixels);
  globeImage.SetData(globePixels);
  using(var cardboardFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"cardboard-glyph.png")))
   globeImage.SaveAsPng(cardboardFile,960,540);
  Console.WriteLine("PASS full cardboard shared glyph and front soldier with alpha-preserving wear.");
  GraphicsDevice.Clear(background);
  batch.Begin();
  var foam=new Robot(702,0,RobotParts.Complete){
   HeadMaterial=RobotMaterial.Foam,BodyMaterial=RobotMaterial.Foam,LegsMaterial=RobotMaterial.Foam};
  draw.Invoke(this,new object[]{new Rectangle(100,110,84,150),foam,false,1f,null});
  draw.Invoke(this,new object[]{new Rectangle(300,110,84,150),
   foam with {HeadOrigin=1,BodyOrigin=1,LegsOrigin=1,DamagedParts=RobotParts.Complete},false,1f,null});
  if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(510,110,62,85),foam,
      BattleRole.Soldier,BattleFacing.South,true,1f})!)
   throw new Exception("Front soldier failed to draw foam parts");
  for(int i=0;i<directions.Length;i++){
   var oriented=foam with {
       HeadOrigin=i%2,BodyOrigin=i%2,LegsOrigin=i%2,
       DamagedParts=i==0?0:directions[i]==BattleFacing.North?RobotParts.Head:RobotParts.Complete};
   if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(120+180*i,290,88,125),oriented,
       BattleRole.Soldier,directions[i],true,1f})!)
    throw new Exception($"Foam soldier failed to draw facing {directions[i]}");
  }
  var polystyrene = foam with { HeadMaterial = RobotMaterial.Polystyrene, BodyMaterial = RobotMaterial.Polystyrene,
    LegsMaterial = RobotMaterial.Polystyrene, DamagedParts = RobotParts.Complete };
  draw.Invoke(this,new object[]{new Rectangle(625,110,84,150),polystyrene,false,1f,null});
  if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(715,110,62,85),polystyrene,
    BattleRole.Soldier,BattleFacing.South,true,1f})!)
   throw new Exception("Front soldier failed to draw polystyrene head");
  for(int i=0;i<directions.Length;i++){
   if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(570+100*i,290,88,125),polystyrene,
      BattleRole.Soldier,directions[i],true,1f})!)
    throw new Exception($"Polystyrene head failed to draw facing {directions[i]}");
  }
  var silicone = foam with { HeadMaterial = RobotMaterial.Silicone, BodyMaterial = RobotMaterial.Silicone,
    LegsMaterial = RobotMaterial.Silicone, DamagedParts = RobotParts.Complete };
  draw.Invoke(this,new object[]{new Rectangle(12,290,84,150),silicone,false,1f,null});
  if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(12,445,62,85),silicone,
    BattleRole.Soldier,BattleFacing.South,true,1f})!)
   throw new Exception("Silicone front soldier failed to draw");
  var tinplate = silicone with { HeadMaterial = RobotMaterial.Tin, BodyMaterial = RobotMaterial.Tin,
    LegsMaterial = RobotMaterial.Tin };
  draw.Invoke(this,new object[]{new Rectangle(350,280,84,150),tinplate,false,1f,null});
  if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(350,445,62,85),tinplate,
    BattleRole.Soldier,BattleFacing.South,true,1f})!)
   throw new Exception("Tinplate front soldier failed to draw");
  for(int i=0;i<directions.Length;i++)
   if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(104+80*i,104,62,85),
     tinplate,
     BattleRole.Soldier,directions[i],true,1f})!)
    throw new Exception($"Tinplate head failed to draw facing {directions[i]}");
  for(int i=0;i<directions.Length;i++){
   if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(110+90*i,420,62,85),silicone,
      BattleRole.Soldier,directions[i],true,1f})!)
    throw new Exception($"Silicone soldier failed to draw facing {directions[i]}");
  }
  var softVinyl = foam with { HeadMaterial = RobotMaterial.SoftVinyl, BodyMaterial = RobotMaterial.SoftVinyl,
    LegsMaterial = RobotMaterial.SoftVinyl, DamagedParts = RobotParts.Complete };
  draw.Invoke(this,new object[]{new Rectangle(785,110,84,150),softVinyl,false,1f,null});
  if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(880,110,62,85),softVinyl,
    BattleRole.Soldier,BattleFacing.South,true,1f})!)
   throw new Exception("Soft vinyl front art must draw");
  for(int i=0;i<directions.Length;i++){
   if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(450+100*i,420,62,85),softVinyl,
      BattleRole.Soldier,directions[i],true,1f})!)
    throw new Exception($"Soft vinyl head failed to draw facing {directions[i]}");
  }
  batch.End();
  var tinplateArt=(RobotHeadTexture)Get("_tinplateHeadTextures");
  tinplateArt.Get(materialWorld,0).GetData(firstFlag);
  tinplateArt.Get(materialWorld,0,true).GetData(secondFlag);
  if(firstFlag.SequenceEqual(secondFlag) || firstFlag[137*size+98]!=secondFlag[137*size+98] ||
     firstFlag[89*size+72]!=secondFlag[89*size+72] ||
     !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)))
   throw new Exception("Tinplate wear must change the shell and preserve face, fastener and alpha");
  foreach(var (name, x, y) in new[]{("_tinplateTorsoTextures",87,91),("_tinplateLegTextures",85,68),
    ("_tinplateSideHead",198,132),("_tinplateRearHead",71,94),
    ("_tinplateSideTorso",134,104),("_tinplateSideLegs",157,66),
    ("_tinplateRearTorso",87,91),("_tinplateRearLegs",128,138)}){
   var art=(RobotHeadTexture)Get(name);
   art.Get(materialWorld,0).GetData(firstFlag);
   art.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || firstFlag[y*size+x]!=secondFlag[y*size+x] ||
      !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)))
    throw new Exception($"Tinplate wear must preserve fastener and alpha: {name}");
  }
  var siliconeArt=(RobotHeadTexture)Get("_siliconeHeadTextures");
  siliconeArt.Get(materialWorld,0).GetData(firstFlag);
  siliconeArt.Get(materialWorld,0,true).GetData(secondFlag);
  if(firstFlag.SequenceEqual(secondFlag) || firstFlag[137*size+100]!=secondFlag[137*size+100] ||
     !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)))
   throw new Exception("Silicone wear must change the shell and preserve face and alpha");
  foreach(string name in new[]{"_siliconeTorsoTextures","_siliconeLegTextures","_siliconeSideHead","_siliconeRearHead",
     "_siliconeSideTorso","_siliconeSideLegs","_siliconeRearTorso","_siliconeRearLegs"}){
   var art=(RobotHeadTexture)Get(name);
   art.Get(materialWorld,0).GetData(firstFlag);
   art.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(c=>c.A).SequenceEqual(secondFlag.Select(c=>c.A)) ||
      name=="_siliconeTorsoTextures" && firstFlag[95*size+58]!=secondFlag[95*size+58] ||
      name=="_siliconeSideTorso" && firstFlag[93*size+155]!=secondFlag[93*size+155] ||
      name=="_siliconeRearTorso" && firstFlag[95*size+58]!=secondFlag[95*size+58] ||
      name=="_siliconeSideHead" && firstFlag[132*size+195]!=secondFlag[132*size+195])
    throw new Exception($"Silicone wear must change paint and preserve face, shoulders and alpha: {name}");
  }
  var softVinylArt=(RobotHeadTexture)Get("_softVinylHeadTextures");
  softVinylArt.Get(materialWorld,0).GetData(firstFlag);
  softVinylArt.Get(materialWorld,0,true).GetData(secondFlag);
  if(firstFlag.SequenceEqual(secondFlag) || firstFlag[137*size+100]!=secondFlag[137*size+100] ||
     !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)))
   throw new Exception("Soft vinyl wear must change the shell and preserve face and alpha");
  foreach(string name in new[]{"_softVinylTorsoTextures","_softVinylLegTextures","_softVinylSideHead","_softVinylRearHead",
    "_softVinylSideTorso","_softVinylSideLegs","_softVinylRearTorso","_softVinylRearLegs"}){
   var art=(RobotHeadTexture)Get(name);
   art.Get(materialWorld,0).GetData(firstFlag);
   art.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(c=>c.A).SequenceEqual(secondFlag.Select(c=>c.A)) ||
      name=="_softVinylTorsoTextures" && firstFlag[55*size+128]!=secondFlag[55*size+128] ||
      name=="_softVinylSideHead" && firstFlag[137*size+195]!=secondFlag[137*size+195])
    throw new Exception($"Soft vinyl wear must change paint and preserve trim and alpha: {name}");
  }
  var foamArt=(RobotHeadTexture)Get("_foamHeadTextures");
  foamArt.Get(materialWorld,0).GetData(firstFlag);
  foamArt.Get(materialWorld,0,true).GetData(secondFlag);
  if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(p=>p.A).SequenceEqual(secondFlag.Select(p=>p.A)) ||
     firstFlag[139*size+82]!=secondFlag[139*size+82])
   throw new Exception("Foam wear must change the shell and preserve face and alpha");
  foreach(string name in new[]{"_foamTorsoTextures","_foamLegTextures"}){
   var partArt=(RobotHeadTexture)Get(name);
   partArt.Get(materialWorld,0).GetData(firstFlag);
   partArt.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(c=>c.A).SequenceEqual(secondFlag.Select(c=>c.A)))
    throw new Exception($"Foam wear must change paint and preserve alpha: {name}");
  }
  foreach(string name in new[]{"_foamSideHead","_foamRearHead","_foamSideTorso","_foamSideLegs","_foamRearTorso","_foamRearLegs"}){
   var art=(RobotHeadTexture)Get(name);
   art.Get(materialWorld,0).GetData(firstFlag);
   art.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(c=>c.A).SequenceEqual(secondFlag.Select(c=>c.A)) ||
      name=="_foamSideHead" && firstFlag[130*size+188]!=secondFlag[130*size+188])
    throw new Exception($"Oriented foam wear must preserve the face and alpha: {name}");
  }
  GraphicsDevice.GetBackBufferData(globePixels);
  globeImage.SetData(globePixels);
  var polystyreneArt=(RobotHeadTexture)Get("_polystyreneHeadTextures");
  polystyreneArt.Get(materialWorld,0).GetData(firstFlag);
  polystyreneArt.Get(materialWorld,0,true).GetData(secondFlag);
  if(firstFlag.SequenceEqual(secondFlag) || firstFlag[142*size+126]!=secondFlag[142*size+126] ||
     firstFlag.Where((pixel,i)=>pixel.A!=secondFlag[i].A).Any())
   throw new Exception("Polystyrene wear must change the shell and preserve face and alpha");
  foreach(string name in new[]{"_polystyreneTorsoTextures","_polystyreneLegTextures","_polystyreneSideHead","_polystyreneRearHead","_polystyreneSideTorso","_polystyreneSideLegs","_polystyreneRearTorso","_polystyreneRearLegs"}){
   var art=(RobotHeadTexture)Get(name);
   art.Get(materialWorld,0).GetData(firstFlag);
   art.Get(materialWorld,0,true).GetData(secondFlag);
   if(firstFlag.SequenceEqual(secondFlag) || !firstFlag.Select(c=>c.A).SequenceEqual(secondFlag.Select(c=>c.A)) ||
      name=="_polystyreneSideHead" && firstFlag[132*size+200]!=secondFlag[132*size+200])
    throw new Exception($"Polystyrene wear must change paint and preserve alpha: {name}");
  }
  using(var foamFile=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"foam-glyph.png")))
   globeImage.SaveAsPng(foamFile,960,540);
  Console.WriteLine("PASS full foam shared glyph and front soldier with protected face and alpha.");
  DrawMaterialGallery();
  Exit();
 }
 void DrawMaterialGallery(){
  ((WorldSetup)Get("_setup")).EquipmentMode=RobotEquipmentMode.Materials;
  var glyph=typeof(Game1).GetMethods(Flags).Single(m=>m.Name=="DrawRobotGlyph" && m.GetParameters().Length==5);
  var soldierArt=typeof(Game1).GetMethod("DrawBattleSoldierArt",Flags)!;
  var queen=typeof(Game1).GetMethod("DrawQueenArt",Flags)!;
  var facings=new[]{BattleFacing.South,BattleFacing.East,BattleFacing.North,BattleFacing.West};
  var globePixels=new Color[960*540];
  using var globeImage=new Texture2D(GraphicsDevice,960,540);
  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin();
  var materials = new[]{RobotMaterial.Paper,RobotMaterial.Foam,RobotMaterial.Cardboard,
   RobotMaterial.Polystyrene,RobotMaterial.SoftVinyl,RobotMaterial.Polypropylene,
   RobotMaterial.Silicone,RobotMaterial.Maple,RobotMaterial.Tin};
  var galleryRobot = new Robot(0,0,RobotParts.Complete);
  var galleryFont = (SpriteFont)Get("_font");
  void Label(string caption,int center,int y)
  {
   const float scale=.28f;
   batch.DrawString(galleryFont,caption,new Vector2(center-galleryFont.MeasureString(caption).X*scale/2,y),
    Color.White,0,Vector2.Zero,scale,SpriteEffects.None,0);
  }
  for(int column=0;column<materials.Length;column++)
  {
   var material=materials[column];
   var sample=galleryRobot with {HeadMaterial=material,BodyMaterial=material,LegsMaterial=material};
   int x=18+column*104;
   glyph.Invoke(this,new object[]{new Rectangle(x,130,96,180),sample,true,1f,null});
   Label(RobotMaterials.Label(material),x+48,320);
  }
  batch.End();
  GraphicsDevice.GetBackBufferData(globePixels);
  globeImage.SetData(globePixels);
  using(var hangar=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"robot-hangar-nine.png")))
   globeImage.SaveAsPng(hangar,960,540);

  GraphicsDevice.Clear(new Color(25,36,48));
  batch.Begin();
  for(int column=0;column<materials.Length;column++){
   var material=materials[column];
   var sample=galleryRobot with { HeadMaterial=material, BodyMaterial=material, LegsMaterial=material };
   int x=18+column*104;
   Label(RobotMaterials.Label(material),x+48,8);
   for(int row=0;row<facings.Length;row++){
    var facing=facings[row];
    foreach(var role in new[]{BattleRole.Soldier,BattleRole.Captain}){
     const int portraitWidth=48;
     int offset=role==BattleRole.Soldier?0:portraitWidth;
     if(!(bool)soldierArt.Invoke(this,new object[]{new Rectangle(x+offset,36+row*106,portraitWidth,82),
       sample,role,facing,role==BattleRole.Soldier,1f})!)
      throw new Exception($"Missing artwork for {material}, {role}, {facing}");
    }
   }
  }
  for(int i=0;i<facings.Length;i++)
   if(!(bool)queen.Invoke(this,new object[]{new Rectangle(18+104*i,453,62,85),facings[i],0,1f})!)
    throw new Exception($"Missing Queen artwork for {facings[i]}");
  batch.End();
  GraphicsDevice.GetBackBufferData(globePixels);
  globeImage.SetData(globePixels);
  using(var matrix=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"robot-art-matrix.png")))
   globeImage.SaveAsPng(matrix,960,540);
  Console.WriteLine("PASS 9 materials × 4 facings × 2 soldier roles, plus 4 Queen facings at 960x540.");
 }
 protected override void UnloadContent(){(Get("_robotHeadTextures") as IDisposable)?.Dispose();batch?.Dispose();pixel?.Dispose();}
}
