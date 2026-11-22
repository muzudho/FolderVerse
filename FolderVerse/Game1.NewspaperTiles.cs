namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private const double PaperTilesStart=4.5;
    private FallingTilePattern _paperTilePattern;
    private FallingTileRenderer _paperTileRenderer;
    private RenderTarget2D _paperOldScene,_paperNextScene;
    private SoundEffect _paperTileSound;
    private int _paperLastTileBeat=-1;
    private bool _paperTileAudioUnavailable;
    private void ResetPaperTileSnapshots(){_paperOldScene?.Dispose();_paperNextScene?.Dispose();_paperOldScene=_paperNextScene=null;}
    private void DrawPaperScenes()
    {
        float age=(float)(_paperAge-PaperTilesStart);
        if(age<0){DrawPaperScenery(false);return;}
        if(age>=FallingTilePattern.DurationSeconds){DrawPaperScenery(true);return;}
        // Both full scenes are ready behind the screen-panel effect. Keep the HUD live.
        _spriteBatch.End();var viewport=GraphicsDevice.Viewport;var targets=GraphicsDevice.GetRenderTargets();var canvas=CanvasBounds();
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        try{
            void Capture(ref RenderTarget2D image,bool next){
                if(image!=null)return;
                image=new RenderTarget2D(GraphicsDevice,1920,1080,false,SurfaceFormat.Color,DepthFormat.None);
                GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Black);_spriteBatch.Begin();
                try{DrawPaperScenery(next);}finally{_spriteBatch.End();}
            }
            Capture(ref _paperOldScene,false);Capture(ref _paperNextScene,true);
            GraphicsDevice.SetRenderTargets(targets);GraphicsDevice.Viewport=viewport;
            _spriteBatch.Begin(transformMatrix:transform);_spriteBatch.Draw(_paperNextScene,new Rectangle(0,0,1920,1080),Color.White);_spriteBatch.End();
            GraphicsDevice.Viewport=new Viewport(canvas);
            _paperTileRenderer??=new FallingTileRenderer(GraphicsDevice);
            _paperTilePattern??=new FallingTilePattern(73);
            _paperTileRenderer.Draw(_paperOldScene,_paperTilePattern,age);
        }finally{
            GraphicsDevice.SetRenderTargets(targets);GraphicsDevice.Viewport=viewport;
            _spriteBatch.Begin(transformMatrix:transform);
        }
    }
    private void UpdatePaperTileSound()
    {
        double age=_paperAge-PaperTilesStart;
        if(_paperTileAudioUnavailable || SeGain<=0 || age<0 || age>=FallingTilePattern.DurationSeconds)return;
        int beat=(int)(age/.12);if(beat==_paperLastTileBeat)return;_paperLastTileBeat=beat;
        try{
            if(_paperTileSound==null){
                const int rate=22050,count=rate/6;var pcm=new byte[count*2];var random=new Random(3048);
                for(int i=0;i<count;i++){
                    double t=i/(double)rate;
                    double tap=Math.Sin(t*2*Math.PI*820)*Math.Exp(-t*72)+.32*(random.NextDouble()*2-1)*Math.Exp(-t*130);
                    if(t>.038){double u=t-.038;tap+=.65*Math.Sin(u*2*Math.PI*1150)*Math.Exp(-u*90);}
                    short value=(short)(Math.Clamp(tap,-1,1)*10000);pcm[i*2]=(byte)value;pcm[i*2+1]=(byte)(value>>8);
                }
                _paperTileSound=new SoundEffect(pcm,rate,AudioChannels.Mono);
            }
            _paperTileSound.Play(.16f*SeGain*(1-(float)(age/FallingTilePattern.DurationSeconds)*.5f),(float)Math.Sin(beat*1.7)*.22f,MathHelper.Lerp(-.7f,.7f,(float)(age/FallingTilePattern.DurationSeconds)));
        }catch(NoAudioHardwareException error){_paperTileAudioUnavailable=true;RecordFailure(error,"paper_tiles_audio");}
    }
}