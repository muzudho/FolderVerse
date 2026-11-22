namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
public partial class Game1
{
    private Texture2D _coinFront,_coinBack;
    private readonly Dictionary<int,(ConquerorLook Look,Color Metal,Texture2D Art)> _coinPortraits=new();
    private readonly Dictionary<int,int> _coinRanks=new();
    private WorldSetup _coinRankWorld;
    private int _coinRankTurn=-1;
    private SoundEffect _coinChime;
    private bool _coinAudioUnavailable;
    private WorldSetup _coinEntryWorld;
    private int _coinEntryTurn=-1,_coinEntryRuler=-1;
    private bool _coinEntryPending,_coinEntrySound;
    private double _coinEntryStarted=-1,_coinClock;
    private void ResetDomesticMedallionEntry(){_coinEntryWorld=null;_coinRanks.Clear();_coinEntryPending=false;_coinEntryStarted=-1;_coinEntrySound=false;}
    private void QueueDomesticMedallion(){
        if(ReferenceEquals(_coinEntryWorld,_setup) && _coinEntryTurn==_setup.Population.Turn && _coinEntryRuler==_setup.TurnPlayer)return;
        _coinRanks.Clear();_coinEntryWorld=_setup;_coinEntryTurn=_setup.Population.Turn;_coinEntryRuler=_setup.TurnPlayer;
        _coinEntryPending=true;_coinEntryStarted=-1;_coinEntrySound=false;
    }
    private void DrawDomesticMedallion(int ruler,Vector2 center){
        if(_coinEntryPending){
            bool covered=_conquestOverview || GuideVisible || _lastCutInPhase!=PlanningPhaseLabel || _coinClock-_cutInStarted<PhaseCutInSeconds;
            if(covered){DrawSouvenirCoin(ruler,center,new(true,1,0));return;}
            _coinEntryPending=false;_coinEntryStarted=_coinClock;
        }
        float age=_coinEntryStarted<0?10:(float)(_coinClock-_coinEntryStarted);
        if(_coinEntryStarted>=0 && !_coinEntrySound && age>=SouvenirMedallion.HoldSeconds){_coinEntrySound=true;PlayMedallionChime();}
        DrawSouvenirCoin(ruler,center,SouvenirMedallion.Pose(age));
    }
    private Texture2D LoadCoinArt(string file){
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Ui",file));var art=Texture2D.FromStream(GraphicsDevice,stream);
        var pixels=new Color[art.Width*art.Height];art.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];int gray=(int)MathF.Round((c.R*.2126f+c.G*.7152f+c.B*.0722f)*c.A/255);pixels[i]=new Color(gray,gray,gray,c.A);}
        art.SetData(pixels);return art;
    }
    private void DrawSouvenirCoin(int ruler,Vector2 center,MedallionPose pose){
        _coinFront??=LoadCoinArt("souvenir-front-neutral-v001.png");_coinBack??=LoadCoinArt("souvenir-back-neutral-v001.png");
        if(!ReferenceEquals(_coinRankWorld,_setup) || _coinRankTurn!=_setup.Population.Turn || _coinRanks.Count!=_setup.ActiveCount){
            _coinRanks.Clear();foreach(var row in _setup.Relations.ProvisionalRanking(_setup))_coinRanks[row.Ruler]=row.Rank;
            _coinRankWorld=_setup;_coinRankTurn=_setup.Population.Turn;
        }
        int rank=_coinRanks.GetValueOrDefault(ruler,4);
        var metal=SouvenirMedallion.Metal(rank);float width=Math.Max(.008f,pose.ScaleX);float diameter=MedallionRadius*2*1.06f;
        // The reeded cylinder edge stays visible when a face is nearly edge-on.
        float edge=6*MathF.Sqrt(Math.Max(0,1-width*width));
        if(edge>.2f)for(int y=-(int)MedallionRadius;y<(int)MedallionRadius;y+=2){
            float chord=MathF.Sqrt(Math.Max(0,MedallionRadius*MedallionRadius-y*y))*width;
            var ink=Color.Lerp(metal,new Color(25,24,23),(y/3&1)==0?.4f:.7f);
            _ui.Box(new((int)(center.X+chord),(int)center.Y+y,Math.Max(1,(int)MathF.Ceiling(edge)),2),ink);
        }
        void Face(Texture2D art,float size,Color tint)=>_spriteBatch.Draw(art,center,null,tint,0,new Vector2(art.Width/2f,art.Height/2f),new Vector2(size/art.Width*width,size/art.Height),SpriteEffects.None,0);
        if(pose.Reverse)Face(_coinBack,diameter,metal);
        else{
            var look=_setup.Looks[ruler];
            if(!_coinPortraits.TryGetValue(ruler,out var cached) || cached.Look!=look || cached.Metal!=metal){
                cached.Art?.Dispose();cached=(look,metal,_portraitRenderer.MedallionRelief(GraphicsDevice,look,240,metal));_coinPortraits[ruler]=cached;
            }
            Face(cached.Art,MedallionPortraitRadius*2,Color.White);Face(_coinFront,diameter,metal);
            if(width>.85f)DrawPortraitMedallionName(center,_setup.ConquerorFullNames[ruler],metal);
        }
        // Nation enamel is separate from rank metal and remains present on both faces.
        for(int i=0;i<64;i++){
            float a=i*MathHelper.TwoPi/64,b=(i+1)*MathHelper.TwoPi/64;var axis=new Vector2(MedallionRadius*.94f*width,MedallionRadius*.94f);
            var from=center+new Vector2(MathF.Cos(a),MathF.Sin(a))*axis;var to=center+new Vector2(MathF.Cos(b),MathF.Sin(b))*axis;var d=to-from;
            var ink=_setup.CountryMapColor(ruler,i,0);_ui.Tile((from+to)/2,new(d.Length()+1,2),MathF.Atan2(d.Y,d.X),ink);
        }
        if(pose.Glint>0){var at=center+new Vector2(MedallionRadius*.6f,-MedallionRadius*.72f);for(int i=0;i<4;i++)_ui.Tile(at,new(25*pose.Glint,2),i*MathHelper.PiOver4,new Color(255,253,215)*pose.Glint);}
    }
    private void PlayMedallionChime(){
        if(_demoActive || _autoPlay || _coinAudioUnavailable)return;
        try{_coinChime??=new SoundEffect(SouvenirMedallion.ChimePcm(),22050,AudioChannels.Mono);_coinChime.Play(.55f*SeGain,0,0);}
        catch(NoAudioHardwareException error){_coinAudioUnavailable=true;RecordFailure(error,"medallion_chime");}
    }
    private void DisposeSouvenirMedallions(){_coinFront?.Dispose();_coinBack?.Dispose();_coinChime?.Dispose();foreach(var portrait in _coinPortraits.Values)portrait.Art.Dispose();_coinPortraits.Clear();}
}
