namespace FolderVerse;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Game1 : Game
{
    private enum Screen { Title, Rolling, Review, CastRolling, CastReview, PlacementRolling, PlacementReview, Ready, PlayerSelect, PlayerReady }
    private readonly GraphicsDeviceManager _graphics;
    private readonly Random _random=new();
    private readonly WorldSetup _setup=new();
    private readonly SeedDialog _seedDialog=new();
    private readonly ScreenshotCapture _screenshots=new();
    private SpriteBatch _spriteBatch;
    private Texture2D _titleScreen,_titleLogo,_pixel;
    private PortraitRenderer _portraitRenderer;
    private SpriteFont _font;
    private UiPainter _ui;
    private ToyCounterRenderer _counter;
    private WorldPreview _world;
    private Screen _screen;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private Point _pointer;
    private double _rollTime;
    private float _yaw=-0.55f,_pitch=0.3f;
    private bool _dragging;
    private CharacterTile[] _selectionTiles=Array.Empty<CharacterTile>();
    private int _hoveredSlot=-1,_previewOwner=-1;
    private float _animationTime;
    private Rectangle SelectionGlobe=>_selectionTiles.Length==0?Rectangle.Empty:_selectionTiles[^1].Bounds;
    private bool IsSelectionScreen=>_screen is Screen.PlayerSelect or Screen.PlayerReady;
    private static readonly Color Accent=new(45,135,142),Muted=new(58,78,100),Cream=new(255,232,184);
    private static readonly Rectangle StartButton=new(740,884,440,106),ActionButton=new(1360,750,400,90),RetryButton=new(1360,860,400,80),BackButton=new(100,960,320,65),PreviewArea=new(130,240,1120,680),WorldSeedButton=new(520,960,730,65);
    private static readonly Rectangle CastPreview=new(312,292,1296,540),CastBack=new(54,1020,200,50),CastRetry=new(266,1020,240,50),CastAction=new(518,1020,480,50);
    private static Rectangle SeedButton(int index)=>new(1010+index*286,1020,274,50);
    private bool IsCastScreen=>_screen>=Screen.CastRolling;
    private bool IsRolling=>_screen is Screen.Rolling or Screen.CastRolling or Screen.PlacementRolling;
    private bool HasTerritories=>_screen is Screen.PlacementRolling or Screen.PlacementReview or Screen.Ready;

    public Game1()
    {
        _graphics=new GraphicsDeviceManager(this) { PreferredBackBufferWidth=1920,PreferredBackBufferHeight=1080,PreferredDepthStencilFormat=DepthFormat.Depth24 };
        InactiveSleepTime=TimeSpan.Zero;
        Content.RootDirectory="Content"; Window.Title="Folder Verse"; Window.AllowUserResizing=true; IsMouseVisible=true;
    }
    protected override void LoadContent()
    {
        _spriteBatch=new SpriteBatch(GraphicsDevice); _titleScreen=Content.Load<Texture2D>("Images/title-screen");
        _titleLogo=Content.Load<Texture2D>("Images/title-logo");
        _portraitRenderer=new PortraitRenderer(Content); _font=Content.Load<SpriteFont>("UiFont");
        _pixel=new Texture2D(GraphicsDevice,1,1); _pixel.SetData(new[]{Color.White});
        _counter=new ToyCounterRenderer(Content.Load<SpriteFont>("ToyCounterFont"),_pixel);
        _ui=new UiPainter(_spriteBatch,_pixel,_font); _world=new WorldPreview(GraphicsDevice);
    }
    private Rectangle CanvasBounds()
    {
        var v=GraphicsDevice.Viewport; float s=Math.Min(v.Width/1920f,v.Height/1080f);
        int w=(int)(1920*s),h=(int)(1080*s); return new((v.Width-w)/2,(v.Height-h)/2,w,h);
    }
    private Point CanvasPoint(MouseState mouse)
    {
        var b=CanvasBounds(); if(b.Width==0 || b.Height==0)return new(-1,-1);
        return new((int)((mouse.X-b.X)*1920f/b.Width),(int)((mouse.Y-b.Y)*1080f/b.Height));
    }
    private void SetWorldSeed(int seed) { _setup.SetWorld(seed); _world.ShowSetup(_setup,false); }
    private void BeginRolling() { _screen=Screen.Rolling; _rollTime=0; _dragging=false; SetWorldSeed(_random.Next()); }
    private void BeginCast() { _screen=Screen.CastRolling; _rollTime=0; _dragging=false; _setup.SetCast(_random.Next()); _world.ShowSetup(_setup,false); }
    private void BeginPlacement() { _screen=Screen.PlacementRolling; _rollTime=0; _dragging=false; SetPlacementSeed(_random.Next()); }
    private void SetPlacementSeed(int seed) { _setup.SetPlacement(seed); _world.ShowSetup(_setup,true); }
    private void ApplySeed(int seed)
    {
        switch(_seedDialog.Purpose)
        {
            case SeedDialog.Target.World: SetWorldSeed(seed); _screen=Screen.Review; break;
            case SeedDialog.Target.Cast: _setup.SetCast(seed); _world.ShowSetup(_setup,false); _screen=Screen.CastReview; break;
            case SeedDialog.Target.Placement: SetPlacementSeed(seed); _screen=Screen.PlacementReview; break;
        }
        _rollTime=0; _dragging=false;
    }
    protected override void Update(GameTime gameTime)
    { HandleInput(Mouse.GetState(),Keyboard.GetState(),gameTime,IsActive); base.Update(gameTime); }
    private void HandleInput(MouseState mouse,KeyboardState keyboard,GameTime gameTime,bool active)
    {
        _pointer=CanvasPoint(mouse); _ui.Pointer=_pointer;
        bool click=active && mouse.LeftButton==ButtonState.Pressed && _previousMouse.LeftButton==ButtonState.Released;
        bool enter=active && keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter);
        bool escape=active && keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape);
        double elapsed=gameTime.ElapsedGameTime.TotalSeconds;
        _animationTime+=(float)elapsed;
        _screenshots.Update(gameTime.ElapsedGameTime.TotalSeconds);
        bool captureChord=keyboard.IsKeyDown(Keys.P) && (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl));
        bool previousChord=_previousKeyboard.IsKeyDown(Keys.P) && (_previousKeyboard.IsKeyDown(Keys.LeftControl) || _previousKeyboard.IsKeyDown(Keys.RightControl));
        if(active && captureChord && !previousChord)_screenshots.Request();
        if(_seedDialog.IsOpen)
        {
            int? result=null;
            if(escape)_seedDialog.Close();
            else
            {
                if(click)result=_seedDialog.Click(_pointer);
                if(active && _seedDialog.IsOpen)
                {
                    foreach(var key in keyboard.GetPressedKeys())
                    {
                        if(_previousKeyboard.IsKeyDown(key))continue;
                        if(key>=Keys.D0 && key<=Keys.D9) result=_seedDialog.Press(((int)key-(int)Keys.D0).ToString());
                        else if(key>=Keys.NumPad0 && key<=Keys.NumPad9) result=_seedDialog.Press(((int)key-(int)Keys.NumPad0).ToString());
                        else if(key==Keys.Back)result=_seedDialog.Press("<");
                        else if(key==Keys.Delete)result=_seedDialog.Press("C");
                        else if(key==Keys.Enter)result=_seedDialog.Press("OK");
                        if(!_seedDialog.IsOpen)break;
                    }
                }
                if(result.HasValue)ApplySeed(result.Value);
            }
        }
        else if(escape)
        {
            if(_screen==Screen.Title)Exit(); else { _screen=Screen.Title; _dragging=false; }
        }
        else if(_screen==Screen.Title)
        { if((click && StartButton.Contains(_pointer)) || enter)BeginRolling(); }
        else if(IsSelectionScreen)
        {
            _hoveredSlot=active && _screen==Screen.PlayerSelect?HitSelection(_pointer):-1;
            if(click && CastBack.Contains(_pointer)){_screen=Screen.Ready;_hoveredSlot=-1;_world.HighlightOwner(_setup,-1);}
            else if(click && _screen==Screen.PlayerReady && CastRetry.Contains(_pointer)){_screen=Screen.PlayerSelect;}
            else if(click && _hoveredSlot>=0){_setup.SelectPlayer(_hoveredSlot);_screen=Screen.PlayerReady;_hoveredSlot=-1;}
            if(IsSelectionScreen)
            {
                Rotate(mouse,click,active,SelectionGlobe);
                if(!_dragging)_yaw+=(float)elapsed*0.3f;
                if(_hoveredSlot>=0)_previewOwner=_hoveredSlot;
                _world.HighlightOwner(_setup,_screen==Screen.PlayerReady?_setup.PlayerSlot:_previewOwner);
            }
        }
        else if(!IsCastScreen)
        {
            if(click && BackButton.Contains(_pointer))_screen=Screen.Title;
            else if(click && WorldSeedButton.Contains(_pointer))_seedDialog.Open(SeedDialog.Target.World,_setup.WorldSeed);
            else if((click && ActionButton.Contains(_pointer)) || enter)
            { if(_screen==Screen.Rolling)_screen=Screen.Review; else BeginCast(); }
            else if(_screen==Screen.Review && click && RetryButton.Contains(_pointer))BeginRolling();
            else if(_screen==Screen.Review)Rotate(mouse,click,active,PreviewArea);
            TickSlots(elapsed);
        }
        else
        {
            if(click && CastBack.Contains(_pointer)) { _screen=Screen.Review; _setup.ClearPlacement(); _world.ShowSetup(_setup,false); }
            else if(click && SeedButton(0).Contains(_pointer))_seedDialog.Open(SeedDialog.Target.World,_setup.WorldSeed);
            else if(click && SeedButton(1).Contains(_pointer))_seedDialog.Open(SeedDialog.Target.Cast,_setup.CastSeed);
            else if(HasTerritories && click && SeedButton(2).Contains(_pointer))_seedDialog.Open(SeedDialog.Target.Placement,_setup.PlacementSeed);
            else if((click && CastAction.Contains(_pointer)) || enter)
            {
                switch(_screen)
                {
                    case Screen.CastRolling:_screen=Screen.CastReview;break;
                    case Screen.CastReview:BeginPlacement();break;
                    case Screen.PlacementRolling:_screen=Screen.PlacementReview;break;
                    case Screen.PlacementReview:BeginSelection();break;
                    case Screen.Ready:BeginSelection();break;
                }
            }
            else if(click && CastRetry.Contains(_pointer))
            { if(_screen is Screen.CastRolling or Screen.CastReview)BeginCast(); else BeginPlacement(); }
            else if(!IsRolling)Rotate(mouse,click,active,CastPreview);
            TickSlots(elapsed);
        }
        _previousMouse=mouse; _previousKeyboard=keyboard;
    }
    private void TickSlots(double elapsed)
    {
        if(_seedDialog.IsOpen)return;
        if(IsRolling)
        {
            _rollTime+=elapsed;
            if(_rollTime>=1.15)
            {
                _rollTime%=1.15;
                if(_screen==Screen.Rolling)SetWorldSeed(_random.Next());
                else if(_screen==Screen.CastRolling)_setup.SetCast(_random.Next());
                else SetPlacementSeed(_random.Next());
            }
        }
        if(IsRolling || (IsCastScreen && !_dragging))_yaw+=(float)elapsed*0.3f;
    }
    private void Rotate(MouseState mouse,bool click,bool active,Rectangle area)
    {
        if(click && area.Contains(_pointer))_dragging=true;
        if(!active || mouse.LeftButton==ButtonState.Released)_dragging=false;
        if(_dragging && !click)
        { var old=CanvasPoint(_previousMouse); _yaw+=(_pointer.X-old.X)*0.008f; _pitch=MathHelper.Clamp(_pitch+(_pointer.Y-old.Y)*0.008f,-MathHelper.PiOver2,MathHelper.PiOver2); }
    }
    private void Parameter(string label,int value,int y)
    {
        _ui.Box(new(1360,y,400,115),new(31,62,79)); _ui.Text(label,new(1390,y+35),0.7f,new(179,207,215));
        _ui.Text(value.ToString(),new(1660,y+9),1.6f,new(255,221,145));
    }
    private void DrawWorldUi()
    {
        _ui.Box(PreviewArea,new(23,49,64));
        _ui.Text("FOLDER VERSE / 世界作成",new(100,60),1.05f,Cream);
        _ui.Text(_screen==Screen.Rolling?"抽選中 / STOP で確認":"確認 / 左ドラッグで回転",new(100,140),0.7f,new(170,206,216));
        Parameter("Width",_world.Width,270); Parameter("Height",_world.Height,405); Parameter("Depth",_world.Depth,540);
        _ui.Text(_world.Kind,new(1360,685),0.85f,new(255,221,145));
        _ui.Button(ActionButton,_screen==Screen.Rolling?"STOP":"この世界を確定",Accent,0.75f);
        if(_screen==Screen.Review)_ui.Button(RetryButton,"再抽選",Muted);
        _ui.Button(BackButton,"タイトルへ戻る",Muted);
        _ui.Button(WorldSeedButton,"世界 SEED "+_setup.WorldSeed+" / 入力",Muted,0.6f);
    }
    private static Rectangle PortraitCell(int slot)
    {
        int x,y;
        if(slot<7){x=slot;y=0;} else if(slot<10){x=6;y=slot-6;} else if(slot<17){x=16-slot;y=4;} else{x=0;y=20-slot;}
        return new(54+x*258,112+y*180,258,180);
    }
    private void DrawCastUi()
    {
        _ui.Text("世界征服者作成",new(54,22),1.1f,Cream);
        _ui.Box(CastPreview,new(23,49,64));
        for(int slot=0;slot<20;slot++)
        {
            var rect=PortraitCell(slot);int portrait=_setup.Portraits[slot];Color color=_setup.OwnerColor(slot);
            bool active=slot<_setup.ActiveCount;
            _ui.Box(new(rect.X+2,rect.Y+2,rect.Width-4,rect.Height-4),active?color:new Color(57,69,77));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[slot],new Rectangle(rect.X+7,rect.Y+7,rect.Width-14,rect.Height-14),active);
            _ui.Box(new(rect.X+8,rect.Y+8,70,28),new Color(10,25,35,215));
            _ui.Text("#"+(slot+1).ToString("00"),new(rect.X+16,rect.Y+10),0.46f,active?color:Color.Gray);
            if(HasTerritories)
                _counter.Draw(_spriteBatch,active?_setup.TerritoryCounts[slot]:0,
                    new Rectangle(rect.X+7,rect.Y+7,rect.Width-14,rect.Height-14),color);
        }
        _ui.Button(CastBack,"世界へ戻る",Muted,0.48f);
        _ui.Button(CastRetry,_screen is Screen.CastRolling or Screen.CastReview?"人物を再抽選":"配置を再抽選",Muted,0.48f);
        string action=IsRolling?"STOP":_screen==Screen.CastReview?"登場人物を確定 / 初期配置へ":_screen==Screen.PlacementReview?"初期配置を確定":"初期配置を確定済み";
        _ui.Button(CastAction,_screen==Screen.Ready?"世界征服者を選択":action,Accent,0.57f);
        _ui.Button(SeedButton(0),"世界 SEED "+_setup.WorldSeed,Muted,0.36f);
        _ui.Button(SeedButton(1),"人物 SEED "+_setup.CastSeed,Muted,0.36f);
        if(HasTerritories)_ui.Button(SeedButton(2),"配置 SEED "+_setup.PlacementSeed,Muted,0.36f);
        else _ui.Center("配置 SEED / 人物確定後",SeedButton(2),0.36f,Color.Gray);
    }
    private void BeginSelection()
    {
        _selectionTiles=CharacterSelectionLayout.Create(_setup.TerritoryCounts,_setup.ActiveCount);
        _screen=Screen.PlayerSelect;_dragging=false;_hoveredSlot=-1;_previewOwner=-1;
        _world.HighlightOwner(_setup,-1);
    }
    private int HitSelection(Point point)
    {
        // Keep both the resting and raised bounds hot to prevent hover jitter.
        if(_hoveredSlot>=0)
        {
            var rect=_selectionTiles[_hoveredSlot].Bounds;
            var raised=rect;raised.Offset(-10,-10);
            if(rect.Contains(point) || raised.Contains(point))return _hoveredSlot;
        }
        foreach(var tile in _selectionTiles)if(tile.Slot>=0 && tile.Bounds.Contains(point))return tile.Slot;
        return -1;
    }
    private void DrawSelectionTile(CharacterTile tile,bool floating)
    {
        var rect=tile.Bounds;Color color=_setup.OwnerColor(tile.Slot);
        if(floating)
        {
            // Logical canvas pixels; scales with the 16:9 game view.
            for(int pad=6;pad>=0;pad-=2)
                _ui.Box(new(rect.X-pad,rect.Y-pad,rect.Width+pad*2,rect.Height+pad*2),new Color(0,0,0,35));
            rect.Offset(-10,-10);
        }
        _ui.Box(rect,color);
        var image=new Rectangle(rect.X+4,rect.Y+4,rect.Width-8,rect.Height-8);
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[tile.Slot],image);
        _counter.Draw(_spriteBatch,_setup.TerritoryCounts[tile.Slot],image,color);
        _ui.Box(new(rect.X+5,rect.Y+5,Math.Min(65,rect.Width-10),27),new Color(10,25,35,210));
        _ui.Text("#"+(tile.Slot+1).ToString("00"),new(rect.X+10,rect.Y+7),Math.Min(0.42f,(rect.Width-20)/_font.MeasureString("#00").X),color);
        if(_screen==Screen.PlayerReady && tile.Slot==_setup.PlayerSlot)
        {
            _ui.Box(new(rect.X+4,rect.Y+34,Math.Min(rect.Width-8,140),33),new Color(10,25,35,220));
            _ui.Text("あなた",new(rect.X+10,rect.Y+34),Math.Min(0.55f,(rect.Width-20)/_font.MeasureString("あなた").X),Cream);
        }
    }
    private void DrawSelectionUi()
    {
        _ui.Text("世界征服者選択",new(54,22),1.1f,Cream);
        _ui.Text("領地の大きさで顔も変わる / クリックして自分を決めよう",new(730,40),0.58f,new Color(170,206,216));
        for(int y=0;y<CharacterSelectionLayout.Rows;y++)for(int x=0;x<CharacterSelectionLayout.Columns;x++)
        {
            var cell=new CharacterTile(0,x,y,1).Bounds;
            _ui.Box(cell,(x/4+y/4)%2==0?new Color(27,51,64):new Color(24,45,58));
            _ui.Box(new(cell.X,cell.Y,cell.Width,y%4==0?2:1),y%4==0?new Color(62,85,96):new Color(41,65,76));
            _ui.Box(new(cell.X,cell.Y,x%4==0?2:1,cell.Height),x%4==0?new Color(62,85,96):new Color(41,65,76));
        }
        foreach(var tile in _selectionTiles)if(tile.Slot>=0 && tile.Slot!=_hoveredSlot)DrawSelectionTile(tile,false);
        var globe=SelectionGlobe;
        _ui.Box(globe,new Color(74,111,128));
        _ui.Box(new(globe.X+4,globe.Y+4,globe.Width-8,globe.Height-8),new Color(23,49,64));
        _ui.Center("地球儀 / 左ドラッグで回転",new(globe.X,globe.Bottom-35,globe.Width,30),0.46f,Cream);
        if(_hoveredSlot>=0)DrawSelectionTile(_selectionTiles[_hoveredSlot],true);
        _ui.Button(CastBack,"配置を確認",Muted,0.48f);
        if(_screen==Screen.PlayerReady)_ui.Button(CastRetry,"選び直す",Accent,0.48f);
        int focus=_screen==Screen.PlayerReady?_setup.PlayerSlot:_hoveredSlot;
        string detail=focus<0?"顔にカーソルを合わせて選択":
            (_screen==Screen.PlayerReady?"あなたは ":"")+"#"+(focus+1).ToString("00")+" "+ConquerorCatalog.Themes[_setup.Looks[focus].BaseId];
        _ui.Center(detail,new(520,1020,1340,50),0.64f,focus<0?Cream:_setup.OwnerColor(focus));
    }
    private void DrawCastPreviewInfo()
    {
        _ui.Box(new(CastPreview.X+18,CastPreview.Y+12,CastPreview.Width-36,48),new Color(12,32,44,220));
        _ui.Center($"{_setup.Width} x {_setup.Height} x {_setup.Depth} / {_setup.Cells.Length} セル / {_setup.ActiveCount} 人",new(CastPreview.X,CastPreview.Y+12,CastPreview.Width,48),0.72f,Cream);
        string stage=_screen switch { Screen.CastRolling=>"登場人物を抽選中",Screen.CastReview=>"登場人物を確認",Screen.PlacementRolling=>"初期配置を抽選中",Screen.PlacementReview=>"初期配置を確認",_=>"初期配置を確定済み" };
        string detail=stage+" / 左ドラッグで回転";
        for(int slot=0;slot<20;slot++) if(PortraitCell(slot).Contains(_pointer))
        {
            var look=_setup.Looks[slot];int sisters=0;
            foreach(var other in _setup.Looks)if(other.BaseId==look.BaseId)sisters++;
            detail="B"+(look.BaseId+1).ToString("00")+"-"+(look.VariantId+1)+" "+ConquerorCatalog.Themes[look.BaseId]+" / "+look.PersonalityName+" / "+look.SituationName;
            if(sisters>1)detail+=" / 姉妹 "+sisters+" 人";
            break;
        }
        _ui.Center(detail,new(CastPreview.X,CastPreview.Bottom-48,CastPreview.Width,40),0.48f,new(170,206,216));
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(16,35,46)); var viewport=GraphicsDevice.Viewport; var canvas=CanvasBounds();
        if(canvas.Width<=0 || canvas.Height<=0){base.Draw(gameTime);return;}
        float cover=Math.Max(viewport.Width/(float)_titleScreen.Width,viewport.Height/(float)_titleScreen.Height);
        int bw=(int)Math.Ceiling(_titleScreen.Width*cover),bh=(int)Math.Ceiling(_titleScreen.Height*cover);
        _spriteBatch.Begin(samplerState:SamplerState.LinearClamp);
        _spriteBatch.Draw(_titleScreen,new Rectangle((viewport.Width-bw)/2,(viewport.Height-bh)/2,bw,bh),new Color(65,75,85));
        if(_screen==Screen.Title)_spriteBatch.Draw(_titleScreen,canvas,Color.White); _spriteBatch.End();
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        if(_screen!=Screen.Title)
        {
            _spriteBatch.Begin(transformMatrix:transform);
            _ui.Box(new(0,0,1920,1080),new(16,35,46));
            _spriteBatch.Draw(_titleLogo,new Rectangle(0,0,1920,1080),Color.White*0.14f);
            if(IsSelectionScreen)DrawSelectionUi();else if(IsCastScreen)DrawCastUi();else DrawWorldUi(); _spriteBatch.End();
            {
            var globe=SelectionGlobe;
            var area=IsSelectionScreen?new Rectangle(globe.X+8,globe.Y+8,globe.Width-16,globe.Height-48):IsCastScreen?CastPreview:PreviewArea;
            GraphicsDevice.Viewport=new Viewport(canvas.X+(int)(area.X*canvas.Width/1920f),canvas.Y+(int)(area.Y*canvas.Height/1080f),Math.Max(1,(int)(area.Width*canvas.Width/1920f)),Math.Max(1,(int)(area.Height*canvas.Height/1080f)));
            _world.Draw(_yaw,_pitch,_animationTime); GraphicsDevice.Viewport=viewport;
            if(IsCastScreen && !IsSelectionScreen) { _spriteBatch.Begin(transformMatrix:transform); DrawCastPreviewInfo(); _spriteBatch.End(); }
            if(IsSelectionScreen && _hoveredSlot>=0){_spriteBatch.Begin(transformMatrix:transform);DrawSelectionTile(_selectionTiles[_hoveredSlot],true);_spriteBatch.End();}
            }
        }
        if(_seedDialog.IsOpen) { _spriteBatch.Begin(transformMatrix:transform); _seedDialog.Draw(_ui); _spriteBatch.End(); }
        // Read the finished game frame before the save notification is drawn.
        _screenshots.CaptureAfterDraw(GraphicsDevice);
        if(_screenshots.MessageSeconds>0)
        {
            _spriteBatch.Begin(transformMatrix:transform);
            _ui.Box(new(430,18,1440,55),new Color(8,24,34,230));
            _ui.Center(_screenshots.Message,new(440,18,1420,55),0.4f,Color.White);
            _spriteBatch.End();
        }
        base.Draw(gameTime);
    }
    protected override void UnloadContent(){_world?.Dispose();_pixel?.Dispose();_spriteBatch?.Dispose();base.UnloadContent();}
}