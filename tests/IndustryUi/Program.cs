using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new CheckGame();game.Run();
sealed class CheckGame:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    WorldSetup w=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);
        w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        w.Industry.Queue(1);Call("FinishTransport");Check(Get("_screen").ToString()=="Industry","No-battle round still enters industry");
        for(int seed=0;seed<1000;seed++){
            var wheel=IndustryCycle.CreateWheel(seed,1);var colors=(Color[])typeof(Game1).GetMethod("IndustryColors",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{wheel})!;
            for(int i=0;i<colors.Length;i++)Check(colors[i]!=colors[(i+1)%colors.Length],"Adjacent roulette colors differ including last/first seam");
        }
        for(int seed=0;seed<100;seed++){
            w.Industry.Reset();w.Industry.Queue(1);w.Industry.Roll(seed);
            for(int turn=0;turn<12;turn++){
                var labels=((string Text,Rectangle Bounds,float Scale,bool Backplate)[])Call("IndustryLabelLayout",new Vector2(1170,625),255f,turn*MathHelper.TwoPi/12);
                for(int i=0;i<labels.Length;i++){
                    var r=labels[i].Bounds;
                    Check(r.Width>0 && !labels[i].Text.Contains('+'),"Wheel uses mixed fractions without an addition sign");
                    foreach(var p in new[]{new Vector2(r.Left,r.Top),new Vector2(r.Right,r.Top),new Vector2(r.Left,r.Bottom),new Vector2(r.Right,r.Bottom)})Check(Vector2.Distance(p,new(1170,625))<255,"Every label corner stays inside the roulette");
                    for(int j=0;j<i;j++)Check(!r.Intersects(labels[j].Bounds),$"Labels overlap: seed={seed},angle={turn},items={i}/{j}");
                }
            }
        }
        var score=new IndustryMusicScore();var pcm=score.Render(IndustryMusicScore.Rate*16);double energy=0;int peak=0;
        for(int i=0;i<pcm.Length;i+=2){int sample=(short)(pcm[i]|pcm[i+1]<<8);energy+=(double)sample*sample;peak=Math.Max(peak,Math.Abs(sample));}
        Check(energy/(pcm.Length/2)>100000 && peak<30000 && score.CompletedLoops==1,"Original industry tune is audible, unclipped and loops");
        var whole=new IndustryMusicScore().Render(4410);var chunked=new IndustryMusicScore();Check(whole.SequenceEqual(chunked.Render().Concat(chunked.Render())),"Industry audio stream is continuous across buffers");
        using(var wav=new BinaryWriter(File.Create(Path.Combine(AppContext.BaseDirectory,"industry-theme.wav")))){wav.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));wav.Write(36+pcm.Length);wav.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));wav.Write(16);wav.Write((short)1);wav.Write((short)1);wav.Write(IndustryMusicScore.Rate);wav.Write(IndustryMusicScore.Rate*2);wav.Write((short)2);wav.Write((short)16);wav.Write(System.Text.Encoding.ASCII.GetBytes("data"));wav.Write(pcm.Length);wav.Write(pcm);}
        Call("UpdateIndustryMusic");if(!(bool)Get("_industryMusicUnavailable")){var music=Get("_industryMusic");var stream=(Microsoft.Xna.Framework.Audio.DynamicSoundEffectInstance)typeof(IndustryMusic).GetField("_stream",F)!.GetValue(music)!;Check(stream.State==Microsoft.Xna.Framework.Audio.SoundState.Playing && stream.PendingBufferCount>0,"Industry BGM streams while roulette is displayed");}
        var font=(SpriteFont)Get("_font");foreach(string text in new[]{"さあ、産業の時間です！　あなたは何台のロボットを製造できるかなー？","数年に一度の当たり年だあ！？　大穴当てたみなさん、今だ、大チャーンス！","ロボット不足の征服者のみなさん、ざんねんでした～！　工場はストップだー！　次は製造できるように、祈ってくださーい！","正規・安いパーツ / 産業ターン累計"}){Check(text.All(c=>font.Characters.Contains(c)),"Missing font glyph: "+text);font.MeasureString(text);}
    }
    protected override void Update(GameTime time){}
    void Save(string name)
    {
        var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,name+".png"));texture.SaveAsPng(output,texture.Width,texture.Height);
    }
    Color[] Pixels(){var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);return pixels;}
    protected override void Draw(GameTime time)
    {
        var batch=(SpriteBatch)Get("_spriteBatch");
        if(Environment.GetCommandLineArgs().Contains("--battle13")){
            CheckBattle13Ui(batch);Exit();return;
        }
        if(Environment.GetCommandLineArgs().Contains("--manufacturing-lanes")){
            CheckManufacturingDelivery(batch);Exit();return;
        }
        if(Environment.GetCommandLineArgs().Contains("--invasion60")){
            CheckInvasion60(batch);Exit();return;
        }
        if(Environment.GetCommandLineArgs().Contains("--premium")){
            CheckPremiumUi(batch);Exit();return;
        }
        foreach(int result in new[]{0,26,120})
        {
            w.Industry.Reset();w.Industry.Queue(1);
            int seed=Enumerable.Range(0,100000).First(s=>IndustryCycle.CreateWheel(s,1)[IndustryCycle.SectorAt(new SeedRandom(s^7919^0x45123).Next(100),IndustryCycle.CreateWheel(s,1))].Turns==result);w.Industry.Roll(seed);
            Call("BeginIndustry");Set("_industryAge",0d);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();Save("industry-spin");
            var initial=Pixels();int clock=w.Robots.IndustryTurn;
            Call("UpdateIndustry",1.49,false);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();
            Check(initial.SequenceEqual(Pixels()) && w.Robots.IndustryTurn==clock,"Initially no numbers, wheel stationary");
            Call("UpdateIndustry",.02,false);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();
            Check(!initial.SequenceEqual(Pixels()),"First number appears after 1.5 seconds");
            for(int i=1;i<=w.Industry.Wheel.Count;i++){
                Set("_industryAge",1.5+(i-1)*.4);Check((int)typeof(Game1).GetProperty("IndustryVisibleLabels",F)!.GetValue(this)! ==i,"Clockwise reveal count");
            }
            double last=1.5+(w.Industry.Wheel.Count-1)*.4;
            double spin=(double)typeof(Game1).GetProperty("IndustryReadSeconds",F)!.GetValue(this)!;
            Check(Math.Abs(spin-last-1.5)<.00001,"Last number remains visible for 1.5 seconds before spinning");
            Set("_industryAge",last);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();var allNumbers=Pixels();
            Call("UpdateIndustry",1.49,false);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();Check(allNumbers.SequenceEqual(Pixels()),"Final reading interval stays still");
            Call("UpdateIndustry",.02,false);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();Check(!allNumbers.SequenceEqual(Pixels()),"Spin starts after final reading interval");
            Call("UpdateIndustry",6d,false);int industry=w.Robots.IndustryTurn;Call("UpdateIndustry",.1,false);Check(w.Robots.IndustryTurn==industry,"Repeated draw/update must not produce twice");
            GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();Save("industry-"+result);
            if(result==0)Check(Get("_industryPrayer")!=null,"Prayer mob on fumble");if(result==120)Check(Get("_industrySurprise")!=null,"Surprised mob on jackpot");
            Set("_industryIdleAge",4.99d);Call("UpdateIndustry",0d,false);Check(Get("_screen").ToString()=="Industry","Result waits until five idle seconds");
            Set("_previousKeyboard",new KeyboardState());Call("HandleInputCore",new MouseState(),new KeyboardState(Keys.Space),new GameTime(TimeSpan.FromSeconds(20),TimeSpan.Zero),true);Check((double)Get("_industryIdleAge")==0 && Get("_screen").ToString()=="Industry","Keyboard activity resets industry timer without skipping result");
            Call("UpdateIndustry",4.99d,false);Check(Get("_screen").ToString()=="Industry","Result remains before timeout");Call("UpdateIndustry",.01d,false);
            Check((bool)Get("_manufacturingActive") && w.Robots.IndustryTurn==clock,"Roulette hands off to manufacturing before production");
            int guard=0;
            while(w.Industry.Pending && guard++<100)Call("UpdateIndustry",.75d,false);
            Check(!w.Industry.Pending && w.Robots.IndustryTurn==clock+result,"Manufacturing applies the exact awarded industry duration once");
            GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawIndustry");batch.End();Save("manufacturing-"+result);
            Call("UpdateIndustry",3d,false);
            Check(Get("_screen").ToString()=="WorldStatus","Manufacturing returns to the next domestic phase");Call("UpdateIndustryMusic");Check(Get("_industryMusic")==null,"Leaving industry disposes its BGM");
        }
        if(Environment.GetCommandLineArgs().Contains("--roulette-only")){
            CheckManufacturingDelivery(batch);
            Console.WriteLine("PASS: 100 seeds x 12 angles, mixed fractions, every label inside the wheel with no overlap, result rendering and industry timing/application.");Exit();return;
        }
        var node=w.Nodes.Current(0);var factory=w.Robots.Workshops[node.Id];factory.LimitLines(3);for(int seed=0;factory.ProductionLines.Length!=3;seed++)factory.GenerateLines(seed);
        Set("_domesticSlide",1f);Set("_populationNode",node.Id);Set("_populationCell",node.Cell);Set("_factoryPlanLine",0);factory.SelectPlan(4);
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawNodeFactory");batch.End();Save("cheap-production-plans");
        var option=(Rectangle)typeof(Game1).GetMethod("FactoryPlanButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{6})!;
        Call("NodeFactoryClick",option.Center);Check(factory.SelectedPlan==6 && factory.CheapProduct,"Cheap option can be selected from actual menu");
        Set("_nodeAssemblyOpen",false);Set("_nodeFactoryOpen",false);Set("_nodeDisposalOpen",false);Set("_nodeTransportOpen",false);
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawNodeDialog");batch.End();Save("unified-node-dialog");
        Check((bool)Get("_nodeAssemblyOpen"),"Initial node view is the assembly view");
        var store=w.Robots.Nodes[node.Id];if(store.Count==0)store.Add(w.Robots.Create(0,RobotParts.Complete));Call("OpenNodeAssembly");
        var cell=(Rectangle)typeof(Game1).GetMethod("HangarCell",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{0})!;
        var robot=store.Robots.First();bool disposal=robot.Disposal;Set("_pointer",new Point(cell.Right-28,cell.Bottom-17));Call("NodeAssemblyInput",new MouseState(),true,false);
        Check(store.Robots.First(r=>r.Id==robot.Id).Disposal!=disposal,"Disposal checkbox works in unified assembly view");
        Call("NodeDialogClick",new Point(300,900),new KeyboardState());Check((bool)Get("_nodeTransportOpen"),"Transport remains accessible from unified view");
        foreach(var item in store.Robots.ToArray())store.Remove(item.Id);
        var left=w.Robots.Create(0,RobotParts.Complete) with {CheapParts=RobotParts.Head,DamagedParts=RobotParts.Head};
        var right=w.Robots.Create(1,RobotParts.Complete);store.Add(left);store.Add(right);Call("OpenNodeAssembly");
        Rectangle Part(int slot)=>(Rectangle)typeof(Game1).GetMethod("HangarPartBounds",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{slot,0})!;
        var pressed=new MouseState(0,0,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
        Set("_pointer",Part(0).Center);Call("NodeAssemblyInput",pressed,true,false);Set("_previousMouse",pressed);Set("_pointer",Part(1).Center);Call("NodeAssemblyInput",new MouseState(),false,false);
        Check(store.Count==2 && store.Robots.Single(r=>r.Id==right.Id).IsCheap(RobotParts.Head) && !store.Robots.Single(r=>r.Id==left.Id).IsCheap(RobotParts.Head),"Actual drag/drop swaps an occupied part");
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawNodeDialog");batch.End();Save("unified-node-dialog");
        foreach(string mode in new[]{"_autoPlay","_demoActive"})
        {
            w.Industry.Reset();w.Industry.Queue(1);Set(mode,true);Call("BeginIndustry");Call("UpdateIndustry",20d,false);
            Check(Get("_screen").ToString()=="WorldStatus","Industry automatically proceeds during "+mode);Set(mode,false);
        }
        Console.WriteLine("PASS: industry entry without battle, font, wheel/result screens, prayer/surprise mobs, exactly-once application, return and cheap factory menu.");Exit();
    }
    private void CheckManufacturingDelivery(SpriteBatch batch)
    {
        w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        foreach(var node in w.Nodes.All)node.Owner=0;
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        foreach(var shop in w.Robots.Workshops){shop.AutoAssemblyDisabled=true;shop.SetReservedSlots(0);foreach(var line in shop.ProductionLines)line.SelectPlan(3);}
        var source=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);
        var route=w.Routes.Neighbors(source)[0];var target=w.Nodes.At(route.Target,route.Entry);
        var factory=w.Robots.Workshops[source.Id];
        for(int seed=0;factory.ProductionLines.Length==0;seed++)factory.GenerateLines(seed);
        var producer=factory.ProductionLines[0];producer.Configure(RobotParts.Complete,1);producer.IntakeTarget=target.Id;
        w.Robots.Nodes[target.Id].Add(w.Robots.Create(0,RobotParts.Head));
        w.Industry.Reset();w.Industry.Queue(1);
        int seed26=Enumerable.Range(0,100000).First(seed=>{
            var wheel=IndustryCycle.CreateWheel(seed,1);
            return wheel[IndustryCycle.SectorAt(new SeedRandom(seed^7919^0x45123).Next(100),wheel)].Turns==26;
        });
        w.Industry.Roll(seed26);
        int clock=w.Robots.IndustryTurn;
        Call("BeginManufacturing");
        Check((int)Get("_manufacturingNode")==target.Id && w.Robots.IndustryTurn==clock && w.Robots.ProductionIntakes.Count==0,
            "Most productive intake destination is selected without producing into the live world");
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawManufacturing");batch.End();var beforeMotion=Pixels();Save("manufacturing-lane-start");
        Call("UpdateManufacturing",.125d,false);
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawManufacturing");batch.End();var duringMotion=Pixels();Save("manufacturing-lane-moving");
        Check(w.Robots.IndustryTurn==clock && w.Robots.ProductionIntakes.Count==0 && !beforeMotion.SequenceEqual(duringMotion),"The shared fluorescent tube moves between ticks without producing early");
        Call("UpdateManufacturing",.625d,false);
        Check(w.Robots.IndustryTurn==clock+3 && w.Robots.ProductionIntakes.Count==3 && w.Industry.Pending,"One displayed industry turn produces actual queued products");
        var flights=((IEnumerable<(ProductionIntake Arrival,double Started)>)Get("_manufacturingFlights")).ToArray();
        Check(flights.Length==3 && flights.Select(f=>f.Started).Distinct().Count()==3,"Fast production retains three independent delivery animations instead of resetting the previous flight");
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawManufacturing");batch.End();Save("manufacturing-delivery");
        for(int i=0;w.Industry.Pending && i<20;i++)Call("UpdateManufacturing",.75d,false);
        Check(w.Robots.ProductionIntakes.Count==11 && (int)Get("_manufacturingReceived")==33 &&
            w.Robots.IndustryTurn==clock+26 && w.Robots.Nodes[target.Id].Count==1,"Capacity and exactly-once production match the intake reservation rule");
        var glyph=typeof(Game1).GetMethods(F).Single(m=>m.Name=="DrawRobotGlyph" && m.GetParameters().Length==5);
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();
        for(int pattern=0;pattern<5;pattern++){
            w.SetCountryAppearance(0,0,5,pattern);
            foreach(bool cheap in new[]{false,true}){
                var box=new Rectangle(130+pattern*340,cheap?550:130,180,280);
                glyph.Invoke(this,new object[]{box,new Robot(100,0,RobotParts.Complete){CheapParts=cheap?RobotParts.Complete:0},true,1f,null});
            }
        }
        batch.End();Save("robot-pattern-quality");
        var shape=new Rectangle(0,0,80,100);
        Check(RobotPartSilhouette.Row(shape,50,false,true).Width<RobotPartSilhouette.Row(shape,20,false,true).Width &&
            RobotPartSilhouette.Row(shape,0,true).Width<shape.Width,"Premium parts have a narrow middle; cheap parts have rounded corners");
        Console.WriteLine("PASS: most productive adjacent intake, one-turn production, visible delivery, capacity and no forecast mutation.");
    }
    private void CheckBattle13Ui(SpriteBatch batch)
    {
        foreach(var terrain in Enum.GetValues<BattleTerrain>()){
            var battle=new BattleState(73,new(){NoWillTurns=100,FlagDefenseTurns=500,PersuasionPercent=0}){Kind=BattleKind.Node,Defender=0,Id=1};
            battle.BuildTerrain(terrain);
            for(int owner=0;owner<13;owner++)battle.AddArmy(owner,0,Enumerable.Range(0,8).Select(_=>w.Robots.Create(owner,RobotParts.Complete)),preferredSide:owner==0?-1:(owner-1)/3);
            battle.ChooseLayout();battle.Deploy();battle.BuildFortifications(12);battle.Capture(BattlePhase.Choose);
            foreach(bool biased in new[]{false,true}){
                var shown=new HashSet<int>();
                foreach(double clock in new[]{0d,4d,8d}){
                    Set("_newspaperClock",clock);
                    var page=((BattleArmy[] Armies,int Page,int Pages))Call("NewspaperArmyPage",battle,0,biased);
                    shown.UnionWith(page.Armies.Select(a=>a.Owner));Check(page.Armies.Length<=5,"Each newspaper page fits within five army columns");
                }
                Check(shown.Count==13,"Newspaper pagination includes every participating army");
            }
            Set("_newspaperClock",0d);
            Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Set("_robotRound",0);Set("_battleWave",0);Set("_battleAge",0d);Set("_battleIntroAge",100d);Set("_robotPlaying",false);Set("_robotSummary",false);
            GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawRobotBattle");batch.End();Save("battle-13-"+terrain.ToString().ToLowerInvariant());
        }
        Console.WriteLine("PASS: thirteen-country battlefield renders for field, fort, mountain and twelve small-boat sea entries.");
    }
    private void CheckInvasion60(SpriteBatch batch)
    {
        var home=w.Nodes.Current(0);var route=w.Routes.Neighbors(home)[0];var target=w.Nodes.At(route.Target,route.Entry);target.Owner=1;home.Owner=0;
        var store=w.Robots.Nodes[home.Id];foreach(var robot in store.Robots.ToArray())store.Remove(robot.Id);
        typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(store,new object[]{60});
        var robots=Enumerable.Range(0,60).Select(_=>w.Robots.Create(0,RobotParts.Complete)).ToArray();foreach(var robot in robots)store.Add(robot);
        Set("_selectedMoveNode",target.Id);Call("BuildInvasionSources",target.Id);
        var sources=(System.Collections.IList)Get("_invasionSources");
        int index=Enumerable.Range(0,sources.Count).Single(i=>!(bool)sources[i]!.GetType().GetField("Queen")!.GetValue(sources[i])! && (int)sources[i]!.GetType().GetField("Node")!.GetValue(sources[i])! ==home.Id);
        Set("_invasionPage",(int)Call("InvasionPageForSource",index));
        var layouts=((IEnumerable<(int Index,int Row,Rectangle Bounds)>)Call("InvasionVisibleRows")).ToArray();
        var bounds=layouts.Single(l=>l.Index==index).Bounds;
        Rectangle Card(int slot)=>(Rectangle)typeof(Game1).GetMethod("InvasionCardAt",F|BindingFlags.Static)!.Invoke(null,new object[]{bounds,slot})!;
        Check(Card(0).X==Card(12).X && Card(59).Bottom<=bounds.Bottom && Card(59).Bottom<850,"60 robots fit in 12 columns and five rows above the controls");
        foreach(int slot in new[]{1,59})Call("InvasionClick",Card(slot).Center);
        Check(w.Campaign.PlannedOrders.Single(o=>o.SourceNode==home.Id && !o.PocketOnly).RobotIds!.ToHashSet().SetEquals(new[]{robots[1].Id,robots[59].Id}),"Second and sixtieth inline cards reserve their exact robots");
        Check((int)Get("_invasionPicker")==-1,"Inline selection needs no separate sortie picker");
        foreach(int slot in Enumerable.Range(0,60).Where(i=>i!=1 && i!=59))Call("InvasionClick",Card(slot).Center);
        Check(w.Campaign.PlannedOrders.Single(o=>o.SourceNode==home.Id && !o.PocketOnly).RobotIds!.Length==60,"An expanded hangar may reserve all 60 robots");
        int queen=Enumerable.Range(0,sources.Count).Single(i=>(bool)sources[i]!.GetType().GetField("Queen")!.GetValue(sources[i])! && (int)sources[i]!.GetType().GetField("Owner")!.GetValue(sources[i])! ==0);
        Set("_invasionPage",(int)Call("InvasionPageForSource",queen));
        var queenBounds=((IEnumerable<(int Index,int Row,Rectangle Bounds)>)Call("InvasionVisibleRows")).Single(l=>l.Index==queen).Bounds;
        var toggle=(Rectangle)typeof(Game1).GetMethod("InvasionQueenAt",F|BindingFlags.Static)!.Invoke(null,new object[]{queenBounds})!;
        Call("InvasionClick",toggle.Center);Check(w.Campaign.PlannedOrders.Any(o=>o.PocketOnly && o.Queen),"Queen participation is selectable directly in the list");
        Call("InvasionClick",toggle.Center);Check(!w.Campaign.PlannedOrders.Any(o=>o.PocketOnly),"Queen participation can be cancelled directly in the list");
        Set("_invasionPage",(int)Call("InvasionPageForSource",index));
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawInvasionPanel");batch.End();Save("invasion-60-inline");
        Console.WriteLine("PASS: 60 inline sortie cards, five rows, exact IDs, no picker and a 60-member reservation.");
    }
    private void CheckPremiumUi(SpriteBatch batch)
    {
        w.SetCountryAppearance(0,0,5,1);w.SetCountryEmblem(0,13);
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("OpenCountrySettings");
        Rectangle Option(string method,int index)=>(Rectangle)typeof(Game1).GetMethod(method,F|BindingFlags.Static)!.Invoke(null,new object[]{index})!;
        Rectangle ColorOption(int row,int index)=>(Rectangle)typeof(Game1).GetMethod("CountryColorOption",F|BindingFlags.Static)!.Invoke(null,new object[]{row,index})!;
        var original=w.CountrySecondaryColor(0);Set("_pointer",ColorOption(1,0).Center);Call("CountrySettingsInput",true,false);
        Check(w.CountrySecondaryColor(0)==original,"Hidden primary color in the secondary palette cannot be selected");
        Set("_pointer",ColorOption(0,5).Center);Call("CountrySettingsInput",true,false);
        Check(w.CountrySecondaryColor(0)!=5,"Selecting the secondary as primary chooses a contrasting secondary");
        for(int emblem=0;emblem<WorldSetup.CountryEmblemNames.Length;emblem++){
            Set("_pointer",Option("CountryEmblemOption",emblem).Center);Call("CountrySettingsInput",true,false);
            Check(w.CountryEmblem(0)==emblem,"Every one-point emblem is selectable");
        }
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawCountrySettings");batch.End();Save("premium-country-settings");
        Set("_countrySettingsOpen",false);w.SetCountryAppearance(0,0,5,1);w.SetCountryEmblem(0,0);
        var glyph=typeof(Game1).GetMethods(F).Single(m=>m.Name=="DrawRobotGlyph" && m.GetParameters().Length==5);
        var rectangles=new List<Rectangle>();
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();
        for(int kind=0;kind<27;kind++){
            var premium=(RobotParts)0;var cheap=(RobotParts)0;
            int choices=kind;
            foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}){
                int quality=choices%3;choices/=3;
                if(quality==1)cheap|=part;if(quality==2)premium|=part;
            }
            var r=new Rectangle(95+kind%9*200,100+kind/9*310,110,210);rectangles.Add(r);
            glyph.Invoke(this,new object[]{r,new Robot(kind+1,0,RobotParts.Complete){CheapParts=cheap,PremiumParts=premium},true,1f,null});
        }
        batch.End();Save("premium-mixed-parts");
        var pixels=Pixels();int width=GraphicsDevice.PresentationParameters.BackBufferWidth;
        foreach(var r in rectangles)foreach(int y in new[]{r.Y+r.Height*7/24,r.Y+r.Height*17/24})
            Check(pixels[y*width+r.Center.X]!=new Color(16,35,46),"Every mixed-quality complete robot has connected neck and waist joints");
        var ui=(UiPainter)Get("_ui");GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();
        for(int emblem=1;emblem<WorldSetup.CountryEmblemNames.Length;emblem++){
            var r=new Rectangle(100+(emblem-1)%7*250,150+(emblem-1)/7*360,150,150);
            CountryEmblemArt.Draw(ui,r,emblem,WorldSetup.Colors[w.CountrySecondaryColor(0)]);
            ui.Center(WorldSetup.CountryEmblemNames[emblem],new(r.X-30,r.Bottom+25,210,50),.5f,Color.White);
        }
        batch.End();Save("premium-emblems");
        var node=w.Nodes.Current(0);var factory=w.Robots.Workshops[node.Id];
        if(factory.ProductionLines.Length==0)for(int seed=0;factory.ProductionLines.Length==0;seed++)factory.GenerateLines(seed);
        var line=factory.ProductionLines[0];
        for(int seed=0;;seed++){line.GeneratePlans(seed);if(line.PlanCount==12)break;}
        Call("OpenNodeInspector",node.Id);Call("OpenNodeFactory");Set("_domesticSlide",1f);Set("_factoryPlanLine",0);
        GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawNodeFactory");batch.End();Save("premium-factory-catalogue");
        Set("_pointer",Option("FactoryPlanButton",11).Center);Call("NodeFactoryClick",(Point)Get("_pointer"));
        Check(line.SelectedPlan==11 && line.CurrentProductionPlan.PremiumParts!=0,"Rare premium recipe can be selected through the factory menu");
        Console.WriteLine("PASS: 27 connected mixed-quality robots, 13 selectable emblems, hidden duplicate palette color and rare premium factory choice.");
    }
}
