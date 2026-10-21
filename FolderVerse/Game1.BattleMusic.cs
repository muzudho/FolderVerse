namespace FolderVerse;
using Microsoft.Xna.Framework.Audio;
public partial class Game1
{
    private BattleMusic _battleMusic;
    private NewspaperMusic _newspaperMusic;
    private NewspaperMusicMood _newspaperMusicMood;
    private static readonly Microsoft.Xna.Framework.Rectangle NewspaperMusicButton=new(730,1020,690,50);
    private bool _battleMusicUnavailable;
    private BattleMusicMood _battleMusicMood=BattleMusicMood.Balanced;
    private BattleMusicMood CurrentBattleMusicMood {
        get {
            if(_robotScenes.Length==0)return BattleMusicMood.Close;
            var scene=_robotScenes[_battleWave];
            int frame=_robotSummary?scene.Frames.Count-1:RobotFrameIndex(scene);
            return BattleMusicSituation.Classify(scene,frame,_setup.HumanPlayers,_setup.Relations.Allied);
        }
    }
    private void StopBattleMusic(){_battleMusic?.Dispose();_battleMusic=null;_newspaperMusic?.Dispose();_newspaperMusic=null;}
    private void UpdateBattleMusic(bool active)
    {
        if(_screen!=Screen.Battle || _robotScenes.Length==0){StopBattleMusic();return;}
        if(_battleMusicUnavailable)return;
        try{
            if(_robotSummary){_battleMusic?.Dispose();_battleMusic=null;_newspaperMusic??=new(_newspaperMusicMood);_newspaperMusic.Update(_newspaperMusicMood,active);}
            else{_newspaperMusic?.Dispose();_newspaperMusic=null;_battleMusicMood=CurrentBattleMusicMood;_battleMusic??=new(_battleMusicMood);_battleMusic.Update(_battleMusicMood,active);}
        }
        catch(NoAudioHardwareException error){StopBattleMusic();_battleMusicUnavailable=true;RecordFailure(error,"battle_music");}
    }
}
