namespace FolderVerse;
using Microsoft.Xna.Framework.Audio;
public partial class Game1
{
    private EndingMusic _endingMusic;
    private bool _endingMusicUnavailable;
    private void StopEndingMusic(){_endingMusic?.Dispose();_endingMusic=null;}
    private void UpdateEndingMusic(bool active)
    {
        if(_screen!=Screen.Ending){StopEndingMusic();return;}
        if(_endingMusicUnavailable)return;
        try{_endingMusic??=new();_endingMusic.Update(active,BgmGain);}
        catch(NoAudioHardwareException error){StopEndingMusic();_endingMusicUnavailable=true;RecordFailure(error,"ending_music");}
    }
}
