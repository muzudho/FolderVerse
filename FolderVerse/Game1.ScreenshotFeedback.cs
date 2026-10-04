namespace FolderVerse;

using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StationeryUI.MonoGame.Audio;
using StationeryUI.MonoGame.Effects;

public partial class Game1
{
    private const double ScreenshotEffectDuration=.42;
    private readonly ScreenshotEffect _screenshotEffect=new();
    private double _screenshotEffectStarted=double.NegativeInfinity;
    private SoundEffect _shutterSound;
    private SoundEffectInstance _shutterInstance;

    private void LoadScreenshotFeedback()
    {
        try
        {
            _shutterSound=ScreenshotShutterSound.Create();
            _shutterInstance=_shutterSound.CreateInstance();
            _shutterInstance.Volume=.72f;
        }
        catch(NoAudioHardwareException error){RecordFailure(error,"screenshot_audio_init");}
    }
    private void StartScreenshotFeedback(double now)
    {
        _screenshotEffectStarted=now;
        if(_shutterInstance==null)return;
        try
        {
            _shutterInstance.Stop();
            _shutterInstance.Play();
        }
        catch(NoAudioHardwareException error){RecordFailure(error,"screenshot_audio_play");}
    }
    private void DrawScreenshotFeedback(double now)
    {
        double age=now-_screenshotEffectStarted;
        if(age<0 || age>=ScreenshotEffectDuration)return;
        _spriteBatch.Begin(samplerState:SamplerState.PointClamp);
        _screenshotEffect.Draw((float)(age/ScreenshotEffectDuration),
            new ScreenshotEffectDrawingCallbacks(GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight,
                (rectangle,color)=>_spriteBatch.Draw(_pixel,rectangle,color)));
        _spriteBatch.End();
    }
    private void DisposeScreenshotFeedback()
    {
        _shutterInstance?.Dispose();
        _shutterSound?.Dispose();
    }
}
