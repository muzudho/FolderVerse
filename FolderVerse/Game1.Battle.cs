namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private BattleRecord[][] _battleWaves=Array.Empty<BattleRecord[]>();
    private CharacterTile[] _battleTiles=Array.Empty<CharacterTile>();
    private int _battleWave;
    private double _battleAge;
    private int _battleFocus;
    private const double BattleSeconds=2.4;
    private static readonly Rectangle BattleContinue=new(1450,1020,410,50);
    private void OpenBattle()
    {
        _battleWaves=BattleSchedule.Create(_setup.Campaign.Battles);
        if(_battleWaves.Length==0)return;
        _battleWave=0;_screen=Screen.Battle;_populationCell=-1;_dragging=false;
        BeginBattleWave();
    }
    private void BeginBattleWave()
    {
        _battleAge=0;_battleFocus=0;
        _battleTiles=CharacterSelectionLayout.CreateBattles(_battleWaves[_battleWave].Select(b=>b.Scale).ToArray());
        _operations.Write("battle_wave",new{wave=_battleWave,battles=_battleWaves[_battleWave],turn=_setup.Population.Turn});
    }
    private void UpdateBattle(double elapsed,bool proceed)
    {
        _battleAge=Math.Min(BattleSeconds,_battleAge+elapsed);
        if(!proceed)return;
        if(_battleAge<BattleSeconds){_battleAge=BattleSeconds;return;}
        if(++_battleWave<_battleWaves.Length)BeginBattleWave();
        else{_screen=Screen.WorldStatus;_battleTiles=Array.Empty<CharacterTile>();}
    }
    private void DrawBattleUi()
    {
        _ui.Text($"戦闘 / ターン {_setup.Population.Turn-1} → {_setup.Population.Turn} / {_battleWave+1}/{_battleWaves.Length}",new(54,25),.8f,Cream);
        // 7 × 5 calendar, each cell divided into 4 × 4 packing units.
        for(int x=0;x<=7;x++)_ui.Box(new(54+x*1806/7,112,1,900),new(58,78,100));
        for(int y=0;y<=5;y++)_ui.Box(new(54,112+y*900/5,1806,1),new(58,78,100));
        float progress=(float)(_battleAge/BattleSeconds);
        foreach(var tile in _battleTiles.Where(t=>t.Slot>=0))
        {
            var b=_battleWaves[_battleWave][tile.Slot];var r=tile.Bounds;
            _ui.Box(new(r.X+3,r.Y+3,r.Width-6,r.Height-6),new(27,49,62));
            string title=b.Kind==BattleKind.Edge?$"辺戦 / Node {b.SourceNode+1} → {b.TargetNode+1}":$"節戦 / Node {b.TargetNode+1}";
            _ui.Center(title,new(r.X+8,r.Y+6,r.Width-16,26),Math.Min(.46f,(r.Width-16)/_font.MeasureString(title).X),Cream);
            int side=Math.Max(24,Math.Min((r.Width-30)/3,(r.Height-90)/2));
            for(int army=0;army<2;army++)
            {
                int ruler=army==0?b.First:b.Second;int x=army==0?r.X+12:r.Right-side-12;
                _ui.Box(new(x-2,r.Y+35,side+4,side+4),_setup.OwnerColor(ruler));
                _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(x,r.Y+37,side,side));
                DrawToySoldier(new(x+side/4,r.Y+side+49,side/2,Math.Max(25,r.Height-side-105)),_setup.OwnerColor(ruler),army==1);
                long before=army==0?b.FirstBefore:b.SecondBefore,after=army==0?b.FirstAfter:b.SecondAfter;
                long remaining=before-(long)((before-after)*(decimal)progress);
                string count=remaining.ToString("N0")+" 人";
                _ui.Center(count,new(army==0?r.X:r.Center.X,r.Bottom-36,r.Width/2,27),Math.Min(.48f,(r.Width/2-8)/_font.MeasureString(count).X),Cream);
            }
            string center=progress<1?"VS":"結果";
            _ui.Center(center,new(r.Center.X-27,r.Center.Y-15,54,30),.4f,new(255,196,112));
        }
        _ui.Text("両軍の戦闘員を相殺 / 同じ組の戦闘は同時進行",new(54,1030),.42f,Cream);
        _ui.Button(BattleContinue,_battleAge<BattleSeconds?"結果まで進む":_battleWave+1<_battleWaves.Length?"続く戦闘へ":"次のターンへ",Accent,.58f);
    }
    private void DrawToySoldier(Rectangle r,Color color,bool mirrored)
    {
        // A reusable standing wind-up soldier sprite built from the game's pixel texture.
        int Unit(int v,int length)=>Math.Max(1,v*length/20);
        void Part(int x,int y,int w,int h,Color c)=>_ui.Box(new(r.X+Unit(mirrored?20-x-w:x,r.Width),r.Y+Unit(y,r.Height),Unit(w,r.Width),Unit(h,r.Height)),c);
        Part(5,0,10,3,color);Part(6,3,8,4,Cream);Part(7,4,1,1,Color.Black);Part(12,4,1,1,Color.Black);
        Part(5,7,10,7,color);Part(2,8,3,6,color);Part(15,8,3,6,color);
        Part(9,8,2,5,new(255,234,124));Part(5,14,4,5,color);Part(11,14,4,5,color);
        Part(3,18,6,2,new(12,27,36));Part(11,18,6,2,new(12,27,36));
        Part(17,9,3,2,new(255,234,124));
    }
    private void DrawBattleGlobe(Rectangle canvas,Matrix transform)
    {
        var tile=_battleTiles.First(t=>t.Slot<0).Bounds;
        var area=new Rectangle(tile.X+8,tile.Y+35,tile.Width-16,tile.Height-65);
        var battle=_battleWaves[_battleWave][_battleFocus];
        var normal=_setup.Cells[_setup.Nodes.All[battle.TargetNode].Cell].Normal;
        float yaw=-MathF.Atan2(normal.X,normal.Z)+(float)Math.Sin(_animationTime*.6f)*.2f,pitch=MathF.Asin(normal.Y);
        var viewport=GraphicsDevice.Viewport;GraphicsDevice.Viewport=OrientationViewport(area,canvas);
        _world.Draw(yaw,pitch,_animationTime,false);GraphicsDevice.Viewport=viewport;
        _spriteBatch.Begin(transformMatrix:transform);
        _ui.Center(_world.GlobeName+" / 戦場",new(tile.X,tile.Y,tile.Width,32),.48f,Cream);
        foreach(var b in _battleWaves[_battleWave])
        foreach(int cellId in new[]{_setup.Nodes.All[b.SourceNode].Cell,_setup.Nodes.All[b.TargetNode].Cell}.Distinct())
        {
            var cell=_setup.Cells[cellId];
            if(!_world.ProjectVisible(cell.Center,cell.Normal,yaw,pitch,area,out var p))continue;
            for(int edge=0;edge<4;edge++)
            {
                _world.ProjectVisible(cell.Corners[edge],cell.Normal,yaw,pitch,area,out var start);
                _world.ProjectVisible(cell.Corners[(edge+1)%4],cell.Normal,yaw,pitch,area,out var end);
                var delta=end-start;
                if(delta.LengthSquared()>.01f)_ui.Tile((start+end)/2,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),new(255,174,70));
            }
            float pulse=(MathF.Sin(_animationTime*10+b.TargetNode)+1)/2;
            _ui.Box(new((int)p.X-8,(int)p.Y-8,16,16),new Color(255,174,70)*(.4f+.5f*pulse));
            for(int i=0;i<4;i++)
            {
                float rise=(_animationTime*.65f+i*.24f)%1;
                int size=8+(int)(rise*14);
                var cloud=new Vector2(p.X+MathF.Sin(i*2+rise)*9,p.Y-rise*42);
                for(int row=-size/2;row<=size/2;row++)
                {
                    int width=(int)MathF.Sqrt(Math.Max(0,size*size/4f-row*row));
                    _ui.Box(new((int)cloud.X-width,(int)cloud.Y+row,width*2+1,1),new Color(177,189,196)*(1-rise));
                }
            }
        }
        _spriteBatch.End();
    }
}
