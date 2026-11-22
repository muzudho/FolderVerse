namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private void ResetPaperRunner(){_paperRunnerImage?.Dispose();_paperRunnerImage=null;_paperRunnerSound?.Dispose();_paperRunnerSound=null;_paperRunnerEntered=false;}
    private void BeginPaperRunner()
    {
        ResetPaperRunner();
        var stock=_setup.Robots.Nodes.Concat(_setup.Robots.Retinues).SelectMany(s=>s.Robots);
        _paperRunner=TransitionRunner.Select(stock,_paperReadCountry,Random.Shared.Next());
    }
    private void PreparePaperRunner()
    {
        float age=(float)(_paperAge-PaperTilesStart)+TransitionRunner.LeadSeconds;
        if(age<0 || age>TransitionRunner.DurationSeconds)return;
        if(_paperRunnerImage==null){
            _spriteBatch.End();var targets=GraphicsDevice.GetRenderTargets();var viewport=GraphicsDevice.Viewport;var canvas=CanvasBounds();
            try{
                _paperRunnerImage=new RenderTarget2D(GraphicsDevice,160,240,false,SurfaceFormat.Color,DepthFormat.None);
                GraphicsDevice.SetRenderTarget(_paperRunnerImage);GraphicsDevice.Clear(Color.Transparent);_spriteBatch.Begin();
                try{
                    if(_paperRunner!=null)DrawHangarRobot(new(0,20,160,220),_paperRunner);
                    else DrawQueenArt(new(0,0,160,240),BattleFacing.East,_paperReadCountry,1);
                }finally{_spriteBatch.End();}
            }finally{
                GraphicsDevice.SetRenderTargets(targets);GraphicsDevice.Viewport=viewport;
                _spriteBatch.Begin(transformMatrix:Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0));
            }
        }
    }
    private void DrawPaperRunner()
    {
        float age=(float)(_paperAge-PaperTilesStart)+TransitionRunner.LeadSeconds;
        if(age<0 || age>TransitionRunner.DurationSeconds || _paperRunnerImage==null)return;
        var foot=TransitionRunner.Position(age);float stride=MathF.Sin(TransitionRunner.MotionTime(age)*24);
        _ui.Tile(new(foot.X,766),new(100,12),0,new Color(0,0,0)*.22f);
        // Keep a body/legs integrated mould rigid. Other legs alternate like a toy's crank.
        if(_paperRunner!=null && _paperRunner.IntegratedParts!=(RobotParts.Body|RobotParts.Legs)){
            _spriteBatch.Draw(_paperRunnerImage,new Vector2(foot.X,foot.Y-80),new Rectangle(0,0,160,160),Color.White,.12f,new Vector2(80,160),1,SpriteEffects.None,0);
            for(int side=0;side<2;side++)_spriteBatch.Draw(_paperRunnerImage,new Vector2(foot.X+(side==0?-40:40),foot.Y-80),new Rectangle(side*80,160,80,80),Color.White,.12f+stride*(side==0?.36f:-.36f),new Vector2(40,0),1,SpriteEffects.None,0);
        }else _spriteBatch.Draw(_paperRunnerImage,foot,null,Color.White,.12f,new Vector2(80,240),1,SpriteEffects.None,0);
    }
    private void UpdatePaperRunnerSound()
    {
        double age=_paperAge-PaperTilesStart+TransitionRunner.LeadSeconds;
        if(_paperRunnerEntered || age<0)return;
        _paperRunnerEntered=true;
        // Do not replay a late footfall after a click/large time step skipped the entrance.
        if(age>.15 || SeGain<=0 || _paperTileAudioUnavailable)return;
        try{
            _paperRunnerSound??=new SoundEffect(TransitionRunner.EntryPcm(_paperRunner?.LegsMaterial??RobotMaterial.Tin),22050,AudioChannels.Mono);
            _paperRunnerSound.Play(.28f*SeGain,0,-.8f);
        }catch(NoAudioHardwareException error){_paperTileAudioUnavailable=true;RecordFailure(error,"paper_runner_audio");}
    }
}