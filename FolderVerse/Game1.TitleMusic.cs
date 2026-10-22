namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
public partial class Game1
{
    private SoundEffect _titleTheme;
    private SoundEffectInstance _titleThemeInstance;
    private bool _titleMusicUnavailable;
    private void UpdateTitleMusic(bool active){
        if(_screen!=Screen.Title || _demoActive){_titleThemeInstance?.Stop();return;}
        if(_titleMusicUnavailable)return;
        try{
            if(_titleTheme==null){using var input=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Music","folder-verse.wav"));_titleTheme=SoundEffect.FromStream(input);_titleThemeInstance=_titleTheme.CreateInstance();_titleThemeInstance.IsLooped=true;_titleThemeInstance.Volume=.45f;}
            if(_titleThemeInstance.State==SoundState.Paused)_titleThemeInstance.Resume();else if(_titleThemeInstance.State!=SoundState.Playing)_titleThemeInstance.Play();
        }catch(NoAudioHardwareException error){_titleMusicUnavailable=true;RecordFailure(error,"title_music");}
    }
    private void DisposeTitleMusic(){_titleThemeInstance?.Dispose();_titleThemeInstance=null;_titleTheme?.Dispose();_titleTheme=null;}
}
