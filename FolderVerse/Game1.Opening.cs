namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _openingActive,_openingTutorial,_openingSkipNarration,_openingWorldSelected;
    private Action _openingFixedStart;
    private double _openingAge;
    private const double OpeningPlanetInterval = 6;
    private const double OpeningPlanetFlight = 8;
    private WorldPreview _departingOpeningPlanet;
    private static readonly Rectangle OpeningNext=new(710,955,500,70),OpeningBack=new(70,965,280,60),OpeningSkip=new(1500,30,350,50);
    private static readonly Rectangle CastDuplicateButton=new(1450,22,405,48);
    private bool IsOpeningScreen=>_screen is Screen.OpeningBook or Screen.OpeningSpace or Screen.OpeningLottery or Screen.OpeningReview ||
        _openingActive && _screen is Screen.PlayerCount or Screen.ComputerPlayerCount;
    private bool OpeningFixed=>_openingTutorial || _openingFixedStart!=null;
    private float OpeningStarX=>_screen==Screen.OpeningReview?960:OpeningPlanetX(_openingAge);
    private static float OpeningPlanetX(double age)=>age<2?MathHelper.Lerp(2250,960,(float)(age/2)):
        age<5?MathHelper.Lerp(960,820,(float)((age-2)/3)):MathHelper.Lerp(820,-650,(float)((age-5)/3));
    private bool OpeningStarSelectable=>OpeningStarX>=400 && OpeningStarX<=1520;
    private void BeginOpening(bool tutorial,Action fixedStart=null)
    {
        DisposeDepartingOpeningPlanet();
        BeginPlayerCount();_openingActive=true;_openingTutorial=tutorial;_openingFixedStart=fixedStart;
        if(!tutorial && fixedStart==null)_setup.EquipmentMode=RobotEquipmentMode.Materials;
        _openingSkipNarration=false;_openingAge=0;_titleMenuOpen=_tutorialMenuOpen=_autoPlay=_demoActive=_autoPaused=false;
        _tutorialChromeSlide=0;_tutorialGuideClosing=-1;
        _screen=Screen.OpeningBook;
    }
    private void EnterOpeningSpace(){_screen=Screen.OpeningSpace;_openingAge=0;}
    private void BeginOpeningLottery()
    {
        _openingActive=true;_openingWorldSelected=false;_openingAge=2;_screen=Screen.OpeningLottery;
        _world.GlobeAnchor=null;_world.GlobePan=Vector2.Zero;_world.GlobeZoom=1;
        SetWorldSeed(_random.Next());
    }
    private void FinishFixedOpening()
    {
        var start=_openingFixedStart;_openingActive=false;_openingFixedStart=null;
        if(start!=null)start();else BeginPetitstellaTutorial();
    }
    private void OpeningInput(bool click,bool enter,bool escape,double elapsed,KeyboardState keys)
    {
        bool next=enter || click && OpeningNext.Contains(_pointer),back=escape || click && OpeningBack.Contains(_pointer);
        if(_screen is Screen.OpeningBook or Screen.OpeningSpace && click && OpeningSkip.Contains(_pointer)){
            _openingSkipNarration=true;
            if(OpeningFixed){FinishFixedOpening();return;}
            if(_screen==Screen.OpeningBook)_screen=Screen.PlayerCount;
            else if(_screen==Screen.OpeningSpace)BeginOpeningLottery();
            return;
        }
        if(_screen==Screen.PlayerCount){
            if(back || click && BackButton.Contains(_pointer)){_screen=Screen.OpeningBook;_openingAge=0;}
            else PlayerCountInput(click,enter,keys);
            return;
        }
        if(_screen==Screen.ComputerPlayerCount){
            if(back || click && BackButton.Contains(_pointer))_screen=Screen.PlayerCount;
            else ComputerPlayerCountInput(click,enter,keys);
            return;
        }
        if(back){
            if(_screen==Screen.OpeningReview){_screen=Screen.OpeningLottery;_openingAge=2;}
            else if(_screen==Screen.OpeningLottery){DisposeDepartingOpeningPlanet();_screen=Screen.ComputerPlayerCount;}
            else if(_screen==Screen.OpeningSpace){_screen=OpeningFixed?Screen.OpeningBook:Screen.ComputerPlayerCount;_openingAge=0;}
            else ReturnToTitle();
            return;
        }
        if(next){
            if(_screen==Screen.OpeningBook){if(OpeningFixed)EnterOpeningSpace();else _screen=Screen.PlayerCount;return;}
            if(_screen==Screen.OpeningSpace){if(OpeningFixed)FinishFixedOpening();else BeginOpeningLottery();return;}
            // Commit the planet the player actually saw, before advancing this frame's clock.
            if(_screen==Screen.OpeningLottery && OpeningStarSelectable){DisposeDepartingOpeningPlanet();_screen=Screen.OpeningReview;_openingAge=2;return;}
            if(_screen==Screen.OpeningReview){_openingWorldSelected=true;_openingActive=false;BeginCast();return;}
        }
        _openingAge+=Math.Min(.25,elapsed);
        if(_screen==Screen.OpeningLottery && _openingAge>=OpeningPlanetInterval)
        {
            _openingAge-=OpeningPlanetInterval;
            var incoming = _departingOpeningPlanet ?? new WorldPreview(GraphicsDevice);
            _departingOpeningPlanet = _world;
            _world = incoming;
            SetWorldSeed(_random.Next());
        }
    }
    private void DisposeDepartingOpeningPlanet()
    {
        _departingOpeningPlanet?.Dispose();
        _departingOpeningPlanet = null;
    }
    private bool CastDuplicateInput(bool click)
    {
        if(!click || _screen is not (Screen.CastRolling or Screen.CastReview) || !CastDuplicateButton.Contains(_pointer))return false;
        _setup.AllowDuplicateCharacters=!_setup.AllowDuplicateCharacters;
        _setup.SetCast(_setup.CastSeed);_world.ShowSetup(_setup,false);_rollTime=0;
        return true;
    }
    private void OpeningStars()
    {
        for(int i=0;i<135;i++){
            float x=(float)((i*367.0+1920*100-_openingAge*(18+i%5*14))%1920),y=85+i*173%780;
            int size=i%9==0?4:2;_ui.Box(new((int)x,(int)y,size,size),new Color(180+i%60,190+i%50,225)*(.35f+i%4*.15f));
        }
    }
    private void StoryBook(Rectangle box,float open=0)
    {
        // Warm spotlight and layered paper are code-drawn, so the book stays sharp at any window size.
        for(int layer=15;layer>0;layer--){
            int rx=box.Width/2+80+layer*10,ry=box.Height/2+35+layer*6;
            for(int y=-ry;y<ry;y+=10){int width=(int)(rx*MathF.Sqrt(Math.Max(0,1-y*y/(float)(ry*ry))));
                _ui.Box(new(box.Center.X-width,box.Center.Y+y,width*2,10),new Color(181,150,87)*.018f);}
        }
        _ui.Box(new(box.X+18,box.Y+22,box.Width,box.Height),new Color(0,0,0,140));
        _ui.Box(box,new Color(104,65,78));
        _ui.Box(new(box.X+10,box.Y+9,box.Width-20,box.Height-18),Cream);
        for(int i=0;i<4;i++)_ui.Box(new(box.X+16,box.Bottom-16-i*4,box.Width-26,2),new Color(176,153,121));
        _ui.Box(new(box.X+17,box.Y+12,box.Width-34,box.Height-40),new Color(38,76,89));
        _ui.Box(new(box.X+18,box.Y+13,15,box.Height-42),new Color(26,51,64));
        if(open>0){
            int spread=(int)(box.Width*.42f*open);
            _ui.Box(new(box.Center.X-spread,box.Y+22,spread*2,box.Height-55),new Color(248,235,206));
            _ui.Box(new(box.Center.X-2,box.Y+22,4,box.Height-55),new Color(174,146,115));
        }else{
            _ui.Center("FOLDER VERSE",new(box.X+30,box.Y+55,box.Width-60,55),.68f,Cream);
            if(_marginStar!=null)_spriteBatch.Draw(_marginStar,new Rectangle(box.Center.X-75,box.Center.Y-55,150,150),Color.White);
            _ui.Center("星をめぐる絵本",new(box.X+25,box.Bottom-105,box.Width-50,45),.55f,Cream);
        }
    }
    private void DrawOpeningFrame(GameTime time)
    {
        GraphicsDevice.Clear(new Color(3,8,18));var viewport=GraphicsDevice.Viewport;var canvas=CanvasBounds();if(canvas.Width<=0 || canvas.Height<=0)return;
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.Begin(samplerState:SamplerState.LinearClamp,transformMatrix:transform);
        _ui.Box(new(0,0,1920,1080),new Color(3,8,18));
        if(_screen is Screen.PlayerCount or Screen.ComputerPlayerCount){
            StoryBook(new(140,300,400,510));if(_screen==Screen.PlayerCount)DrawPlayerCount();else DrawComputerPlayerCount();
        }else if(_screen==Screen.OpeningBook){
            _ui.Center("これは、ひとつの星の、すべての陣地を手に入れるお話。",new(170,100,1580,85),.82f,Cream);
            StoryBook(new(680,270,560,440));
            _ui.Center(OpeningFixed?"ひとりで読む、練習のお話を開きましょう。":"この絵本を読む＜大きなお子様＞は、何人いますか？",new(250,785,1420,70),.65f,Cream);
            _ui.Button(OpeningNext,OpeningFixed?"絵本を開く":"人数を選ぶ　次へ",DefaultAccent,.65f);
        }else{
            OpeningStars();
            if(_screen==Screen.OpeningSpace){
                if(_openingAge<1.2)StoryBook(new(730,315,460,350),MathHelper.SmoothStep(0,1,(float)(_openingAge/1.2)));
                _ui.Center("絵本を開くと、宇宙が有りました。",new(180,195,1560,90),.95f,Cream);
                _ui.Center("そこには、わたしたちの知っている地球とは違う星が、いくつも有りました。",new(140,690,1640,70),.6f,Cream);
                _ui.Center("どの星にも、見覚えのある顔。でも、征服者の名前や国は星ごとに違います。",new(140,790,1640,55),.48f,new Color(173,199,215));
                _ui.Button(OpeningNext,OpeningFixed?"このお話を始める":"星を見に行く　次へ",DefaultAccent,.65f);
            }else{
                _ui.Center(_screen==Screen.OpeningReview?"この星で、すべての陣地を目指そう。":"星抽選フェーズ / どの星の物語にしますか？",new(180,65,1560,75),.8f,Cream);
                _ui.Center(_world.GlobeName+" / 世界 SEED "+_setup.WorldSeed,new(360,800,1200,60),.6f,Cream);
                _ui.Center(_screen==Screen.OpeningReview?"戻ると、この星から選び直せます。":"右から左へ通る星。中央に来た星を［次へ］で選びます。",new(310,870,1300,50),.5f,new Color(173,199,215));
                _ui.Button(OpeningNext,_screen==Screen.OpeningReview?"この星で進む　次へ":OpeningStarSelectable?"この星を選ぶ　次へ":"星が中央へ来るのを待つ",_screen==Screen.OpeningReview || OpeningStarSelectable?DefaultAccent:Muted,.6f);
            }
        }
        if(_screen is Screen.OpeningBook or Screen.OpeningSpace)_ui.Button(OpeningSkip,"オープニングをスキップ",Muted,.48f);
        if(_screen is not (Screen.PlayerCount or Screen.ComputerPlayerCount))_ui.Button(OpeningBack,"戻る / Esc",Muted,.55f);
        _spriteBatch.End();
        if(_screen is Screen.OpeningLottery or Screen.OpeningReview){
            double departingAge = _openingAge + OpeningPlanetInterval;
            if(_screen==Screen.OpeningLottery && _departingOpeningPlanet!=null && departingAge<OpeningPlanetFlight)
                DrawPlanet(_departingOpeningPlanet, OpeningPlanetX(departingAge), departingAge);
            DrawPlanet(_world, OpeningStarX, _openingAge);
        }
        void DrawPlanet(WorldPreview planet, float x, double age)
        {
            var frame=new Rectangle(canvas.X+(int)((x-480)*canvas.Width/1920f),canvas.Y+(int)(210*canvas.Height/1080f),(int)(960*canvas.Width/1920f),(int)(565*canvas.Height/1080f));
            var clipped=Rectangle.Intersect(frame,canvas);
            if(clipped.Width>0 && clipped.Height>0){GraphicsDevice.Viewport=new Viewport(clipped);planet.Draw(-.55f+(float)age*.09f,.3f,(float)age,false,true,frame);GraphicsDevice.Viewport=viewport;}
        }
        if(_screenshots.CaptureAfterDraw(GraphicsDevice))StartScreenshotFeedback(time.TotalGameTime.TotalSeconds);
        DrawScreenshotFeedback(time.TotalGameTime.TotalSeconds);DrawFullscreenCursor();
    }
}
