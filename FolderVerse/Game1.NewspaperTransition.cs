namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

public partial class Game1
{
    private int _newspaperPrevious;
    private double _newspaperSlideAge=1;
    private const double NewspaperSlideSeconds=.75;
    private SoundEffect _newspaperRustle;
    private void SwitchNewspaper()
    {
        if(_newspaperSlideAge<NewspaperSlideSeconds)return;
        _newspaperPrevious=_newspaperEdition;
        _newspaperEdition=(_newspaperEdition+1)%NewspaperProfiles.All.Length;
        _newspaperClock=0;_newspaperSlideAge=0;
        try
        {
            if(_newspaperRustle==null)
            {
                const int rate=22050,count=rate/3;var bytes=new byte[count*2];var random=new Random(2718);double previous=0;
                for(int i=0;i<count;i++)
                {
                    double noise=random.NextDouble()*2-1,t=i/(double)count;
                    short sample=(short)((noise-previous)*4500*Math.Sin(Math.PI*t)*Math.Exp(-t));previous=noise;
                    bytes[2*i]=(byte)sample;bytes[2*i+1]=(byte)(sample>>8);
                }
                _newspaperRustle=new SoundEffect(bytes,rate,AudioChannels.Mono);
            }
            _newspaperRustle.Play(.35f,0,0);
        }
        catch(NoAudioHardwareException error){RecordFailure(error,"newspaper_audio_init");}
    }
    private void DrawBattleNewspaper(BattleState battle,BattleFrame frame)
    {
        if(_newspaperSlideAge>=NewspaperSlideSeconds){DrawBattleNewspaperSheet(battle,frame);return;}
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        var oldScissor=GraphicsDevice.ScissorRectangle;
        _spriteBatch.End();
        GraphicsDevice.ScissorRectangle=new(canvas.X+(int)(1094*canvas.Width/1920f),canvas.Y+(int)(203*canvas.Height/1080f),(int)(776*canvas.Width/1920f),(int)(587*canvas.Height/1080f));
        using var clip=new RasterizerState{ScissorTestEnable=true};
        float t=(float)Math.Clamp(_newspaperSlideAge/NewspaperSlideSeconds,0,1);t=t*t*(3-2*t);
        int current=_newspaperEdition;
        for(int sheet=0;sheet<2;sheet++)
        {
            _newspaperEdition=sheet==0?_newspaperPrevious:current;
            float offset=(sheet==0?t:t-1)*590;
            _spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(0,offset,0)*transform,rasterizerState:clip);
            DrawBattleNewspaperSheet(battle,frame);_spriteBatch.End();
        }
        _newspaperEdition=current;GraphicsDevice.ScissorRectangle=oldScissor;
        _spriteBatch.Begin(transformMatrix:transform);
    }
}