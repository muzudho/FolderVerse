namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private readonly FramePerformance _framePerformance=new();
    private bool _performanceVisible,_titleSystemMenuOpen;
    private static readonly Rectangle PerformanceMenuItem=new(1320,574,538,52);
    private static readonly Rectangle PerformanceBand=new(270,0,1650,40),PerformanceClose=new(1880,0,40,40);
    private static readonly Rectangle TitleSystemPanel=new(555,340,810,370),TitleSystemAudio=new(610,415,700,58),TitleSystemPerformance=new(610,490,700,58),TitleSystemBack=new(830,610,260,54);
    private bool PerformanceUiInput(MouseState mouse,KeyboardState keys,bool active){
        if(!active)return false;
        bool click=mouse.LeftButton==ButtonState.Pressed&&_previousMouse.LeftButton==ButtonState.Released;
        if(_performanceVisible&&PerformanceBand.Contains(_pointer)&&(mouse.LeftButton==ButtonState.Pressed||mouse.RightButton==ButtonState.Pressed||mouse.MiddleButton==ButtonState.Pressed||mouse.ScrollWheelValue!=_previousMouse.ScrollWheelValue)){
            if(click&&PerformanceClose.Contains(_pointer))_performanceVisible=false;
            return true;
        }
        if(_screen!=Screen.Title){_titleSystemMenuOpen=false;return false;}
        if(!_titleSystemMenuOpen||_bgmMenuOpen)return false;
        if(keys.IsKeyDown(Keys.Escape)&&_previousKeyboard.IsKeyUp(Keys.Escape)){_titleSystemMenuOpen=false;return true;}
        if(click){
            if(TitleSystemAudio.Contains(_pointer))_bgmMenuOpen=true;
            else if(TitleSystemPerformance.Contains(_pointer)){_performanceVisible=true;_titleSystemMenuOpen=false;}
            else if(TitleSystemBack.Contains(_pointer)||!TitleSystemPanel.Contains(_pointer))_titleSystemMenuOpen=false;
        }
        return true;
    }
    private void DrawPerformanceUi(){
        if(_ui==null||(!_performanceVisible&&!(_screen==Screen.Title&&_titleSystemMenuOpen&&!_bgmMenuOpen)))return;
        var canvas=CanvasBounds();if(canvas.Width<=0||canvas.Height<=0)return;
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.Begin(transformMatrix:transform);
        if(_screen==Screen.Title&&_titleSystemMenuOpen&&!_bgmMenuOpen){
            _ui.Box(new(0,0,1920,1080),Color.Black*.5f);_ui.Box(TitleSystemPanel,new Color(24,45,57));
            _ui.Center("システムメニュー",new(610,350,700,50),.65f,Cream);
            _ui.Button(TitleSystemAudio,"BGM・SE音量",Muted,.5f);
            _ui.Button(TitleSystemPerformance,"パフォーマンス表示",Muted,.5f);
            _ui.Button(TitleSystemBack,"戻る",Muted,.5f);
        }
        if(_performanceVisible){
            var r=_framePerformance.Current;
            string text=FormattableString.Invariant($"FPS {r.Fps:F1}  |  フレーム {r.FrameMilliseconds:F1} ms  |  最長 {r.LongestMilliseconds:F1} ms  |  更新 {r.UpdateMilliseconds:F1} ms  |  描画CPU {r.DrawMilliseconds:F1} ms  |  うち地星儀 {r.GlobeMilliseconds:F1} ms");
            _ui.Box(PerformanceBand,Color.Black*.8f);
            _ui.Text(text,new(282,9),Math.Min(.4f,1580/Math.Max(1,_font.MeasureString(text).X)),Color.White);
            _ui.Button(PerformanceClose,"×",new Color(52,62,72,210),.55f);
        }
        _spriteBatch.End();DrawFullscreenCursor();
    }
}