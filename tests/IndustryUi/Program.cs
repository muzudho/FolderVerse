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
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");
        w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        w.Industry.Queue(1);Call("FinishTransport");Check(Get("_screen").ToString()=="Industry","No-battle round still enters industry");
        for(int seed=0;seed<1000;seed++){
            var wheel=IndustryCycle.CreateWheel(seed,1);var colors=(Color[])typeof(Game1).GetMethod("IndustryColors",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{wheel})!;
            for(int i=0;i<colors.Length;i++)Check(colors[i]!=colors[(i+1)%colors.Length],"Adjacent roulette colors differ including last/first seam");
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
        foreach(int result in new[]{0,7,40})
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
            if(result==0)Check(Get("_industryPrayer")!=null,"Prayer mob on fumble");if(result==40)Check(Get("_industrySurprise")!=null,"Surprised mob on jackpot");
            Set("_industryIdleAge",4.99d);Call("UpdateIndustry",0d,false);Check(Get("_screen").ToString()=="Industry","Result waits until five idle seconds");
            Set("_previousKeyboard",new KeyboardState());Call("HandleInputCore",new MouseState(),new KeyboardState(Keys.Space),new GameTime(TimeSpan.FromSeconds(20),TimeSpan.Zero),true);Check((double)Get("_industryIdleAge")==0 && Get("_screen").ToString()=="Industry","Keyboard activity resets industry timer without skipping result");
            Call("UpdateIndustry",4.99d,false);Check(Get("_screen").ToString()=="Industry","Result remains before timeout");Call("UpdateIndustry",.01d,false);Check(Get("_screen").ToString()=="WorldStatus","Idle result automatically proceeds at five seconds");Call("UpdateIndustryMusic");Check(Get("_industryMusic")==null,"Leaving industry disposes its BGM");
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
}
