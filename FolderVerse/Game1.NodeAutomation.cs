namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private int _inspectedNode=-1;
    private readonly System.Collections.Generic.Dictionary<int,(Texture2D Base,Texture2D Clothes)> _supervisorTextures=new();
    private int _supervisorDialog; // 1: leave duration, 2: return policy
    private int _supervisorDialogNode=-1;
    private string _supervisorLeaveValue="1",_supervisorLeaveError="";
    private bool _supervisorRecall,_supervisorReplace;
    private static readonly Rectangle SupervisorLeaveButton=new(84,916,380,48),SupervisorPolicyButton=new(474,916,390,48);
    private static readonly string[] SupervisorKeys={"7","8","9","4","5","6","1","2","3","C","0","<"};
    private static Rectangle SupervisorKey(int i)=>new(240+i%3*150,460+i/3*76,140,66);
    private string SupervisorRole(int node)=>NodeSupervisors.Labels[NodeSupervisors.For(_setup.Nodes.All[node],_setup.Cells[_setup.Nodes.All[node].Cell].Face)];
    private bool NodeAutomationClick(Point point)
    {
        if(_populationCell<0)return false;
        var w=_setup.Robots.Workshops[_populationNode];
        bool allowed=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && !_setup.Campaign.RobotBattles.LockedNode(_populationNode);
        if(SupervisorLeaveButton.Contains(point)){
            if(allowed){_supervisorDialogNode=_populationNode;_supervisorRecall=w.Automation==NodeAutomation.Manual;_supervisorDialog=_supervisorRecall?2:1;_supervisorLeaveValue="1";_supervisorReplace=true;_supervisorLeaveError="";}
            return true;
        }
        if(SupervisorPolicyButton.Contains(point)){
            if(allowed && w.Automation!=NodeAutomation.Manual)w.RecallSupervisor(w.Automation==NodeAutomation.Gentle?NodeAutomation.Auto:NodeAutomation.Gentle);
            return true;
        }
        return false;
    }
    private void SupervisorPress(string key)
    {
        _supervisorLeaveError="";
        if(key=="OK"){
            if(int.TryParse(_supervisorLeaveValue,out int days) && days>=1 && days<=99)_supervisorDialog=2;
            else _supervisorLeaveError="1 ～ 99";
        }
        else if(key=="C"){_supervisorLeaveValue="";_supervisorReplace=false;}
        else if(key=="<"){if(_supervisorLeaveValue.Length>0)_supervisorLeaveValue=_supervisorLeaveValue[..^1];_supervisorReplace=false;}
        else if(key.Length==1 && char.IsDigit(key[0])){if(_supervisorReplace){_supervisorLeaveValue="";_supervisorReplace=false;}if(_supervisorLeaveValue.Length<2)_supervisorLeaveValue+=key;}
    }
    private void SupervisorDialogInput(bool click,bool escape,Microsoft.Xna.Framework.Input.KeyboardState keyboard)
    {
        if(escape){_supervisorDialog=0;return;}
        var p=_pointer;if(SupervisorNodePanel){p.X-=920;p.Y-=40;}
        if(click && new Rectangle(650,800,180,54).Contains(p)){_supervisorDialog=0;return;}
        var w=_setup.Robots.Workshops[_supervisorDialogNode];
        if(!CanManageNodeFactory(_setup.Nodes.All[_supervisorDialogNode].Owner) || _setup.Campaign.RobotBattles.LockedNode(_supervisorDialogNode)){_supervisorDialog=0;return;}
        if(_supervisorDialog==2){
            for(int i=0;i<2;i++)if(click && new Rectangle(134,470+i*110,680,80).Contains(p)){
                var policy=i==0?NodeAutomation.Gentle:NodeAutomation.Auto;
                if(_supervisorRecall)w.RecallSupervisor(policy);else w.GiveSupervisorLeave(int.Parse(_supervisorLeaveValue),policy);
                _supervisorDialog=0;return;
            }
            return;
        }
        if(click){for(int i=0;i<12;i++)if(SupervisorKey(i).Contains(p))SupervisorPress(SupervisorKeys[i]);if(new Rectangle(240,800,380,54).Contains(p))SupervisorPress("OK");}
        foreach(var key in keyboard.GetPressedKeys()){
            if(_previousKeyboard.IsKeyDown(key))continue;
            if(key>=Microsoft.Xna.Framework.Input.Keys.D0 && key<=Microsoft.Xna.Framework.Input.Keys.D9)SupervisorPress(((int)key-(int)Microsoft.Xna.Framework.Input.Keys.D0).ToString());
            else if(key>=Microsoft.Xna.Framework.Input.Keys.NumPad0 && key<=Microsoft.Xna.Framework.Input.Keys.NumPad9)SupervisorPress(((int)key-(int)Microsoft.Xna.Framework.Input.Keys.NumPad0).ToString());
            else if(key==Microsoft.Xna.Framework.Input.Keys.Back)SupervisorPress("<");
            else if(key==Microsoft.Xna.Framework.Input.Keys.Delete)SupervisorPress("C");
            else if(key==Microsoft.Xna.Framework.Input.Keys.Enter)SupervisorPress("OK");
            if(_supervisorDialog!=1)break;
        }
    }
    private void DrawNodeAutomation()
    {
        if(_populationCell<0)return;
        var w=_setup.Robots.Workshops[_populationNode];string role=SupervisorRole(_populationNode);
        string leave=w.Automation==NodeAutomation.Manual?"休暇中の"+role+"をすぐ呼び戻す":role+"に休暇を与える";
        bool allowed=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && !_setup.Campaign.RobotBattles.LockedNode(_populationNode);
        void Button(Rectangle r,string label,Color tint){_ui.Button(r,label,tint,Math.Min(.53f,(r.Width-20)/_ui.Measure(label,1).X));}
        Button(SupervisorLeaveButton,leave,allowed?Accent:Muted);
        if(w.Automation!=NodeAutomation.Manual)Button(SupervisorPolicyButton,w.Automation==NodeAutomation.Gentle?"一任する方針に変更する":"現在の方針を引き継がせる方針に変更する",allowed?Accent:Muted);
        else _ui.Center(w.SupervisorLeaveRemaining>0?$"休暇 残り {w.SupervisorLeaveRemaining} ターン":"休暇中",SupervisorPolicyButton,.53f,Cream);
        if(_supervisorDialog==0)return;
        _ui.Box(new(64,220,820,660),new Color(14,30,43));
        _ui.Center(_supervisorDialog==1?"何ターン休ませますか？":"復帰後を指示してください。",new(84,260,780,64),.75f,Cream);
        if(_supervisorDialog==1){
            _ui.Box(new(240,350,440,80),Muted);_ui.Center(_supervisorLeaveValue.Length==0?"_":_supervisorLeaveValue,new(240,350,440,80),1f,Cream);
            for(int i=0;i<12;i++)_ui.Button(SupervisorKey(i),SupervisorKeys[i],Muted,.85f);
            _ui.Text(_supervisorLeaveError.Length>0?_supervisorLeaveError:"1 ～ 99",new(700,365),.6f,Cream);
            _ui.Button(new(240,800,380,54),"次へ",Accent,.65f);
        }else{
            _ui.Button(new(134,470,680,80),"仕事の方針を引き継がせる",Accent,.65f);
            _ui.Button(new(134,580,680,80),"一任する",Accent,.65f);
        }
        _ui.Button(new(650,800,180,54),"キャンセル",Muted,.55f);
    }
    private bool SupervisorNodePanel=>false;
    private bool InspectingSupervisor=>_populationCell>=0 && _inspectedNode>=0 && _inspectedNode<_setup.Nodes.All.Length;
    private void DrawSupervisor(Rectangle image,int owner)
    {
        int kind=NodeSupervisors.For(_setup.Nodes.All[_inspectedNode],_setup.Cells[_setup.Nodes.All[_inspectedNode].Cell].Face);
        if(!_supervisorTextures.TryGetValue(kind,out var textures)){using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","NodeSupervisors",NodeSupervisors.Kinds[kind]+"-v001.png"));var _supervisorBase=Texture2D.FromStream(GraphicsDevice,stream);
            var pixels=new Color[_supervisorBase.Width*_supervisorBase.Height];_supervisorBase.GetData(pixels);
            for(int y=0;y<_supervisorBase.Height;y++)for(int x=0;x<_supervisorBase.Width;x++){
                int i=y*_supervisorBase.Width+x;var c=pixels[i];float nx=x/(float)_supervisorBase.Width,ny=y/(float)_supervisorBase.Height;
                // White cloth within the jacket silhouette; warm skin, hair, helmet and backdrop remain untouched.
                float cloth=Math.Clamp((65f-(c.R-c.B))/25f,0,1)*Math.Clamp((70f-Math.Abs(c.R-c.G))/25f,0,1);
                float edge=Math.Clamp((ny-.47f)*60,0,1)*Math.Clamp((nx-.10f)*60,0,1)*Math.Clamp((.85f-nx)*60,0,1);
                pixels[i]=c*(cloth*edge);
            }
            var _supervisorClothes=new Texture2D(GraphicsDevice,_supervisorBase.Width,_supervisorBase.Height);_supervisorClothes.SetData(pixels);textures=(_supervisorBase,_supervisorClothes);_supervisorTextures[kind]=textures;
        }
        var crop=PortraitRenderer.FitSource(new(0,0,textures.Base.Width,textures.Base.Height),image);
        _spriteBatch.Draw(textures.Base,image,crop,Color.White);
        _spriteBatch.Draw(textures.Clothes,image,crop,owner<0?Color.White:_setup.OwnerColor(owner));
    }
    private void DrawSupervisorSuperior()
    {
        int owner=_setup.Nodes.All[_inspectedNode].Owner;if(owner<0){_ui.Center("現場監督 / 無征服",new(220,145,660,40),.58f,Cream);return;}
        var photo=new Rectangle(64,155,144,100);_ui.Box(new(60,151,152,108),_setup.OwnerColor(owner));
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[owner],photo,true,forCreation:true);
        _ui.Center(_setup.ConquerorNames[owner]+"の手下です",new(220,145,660,40),.58f,Cream);
    }
}