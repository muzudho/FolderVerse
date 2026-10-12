namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private int _inspectedNode=-1;
    private bool _nodeAutomationDrop;
    private readonly System.Collections.Generic.Dictionary<int,(Texture2D Base,Texture2D Clothes)> _supervisorTextures=new();
    private static readonly Rectangle NodeAutomationField=new(84,922,300,44);
    private static Rectangle NodeAutomationOption(int i)=>new(84,778+i*48,300,48);
    private static readonly string[] NodeAutomationLabels={"オート","緩やかオート","マニュアル"};
    private bool NodeAutomationClick(Point point)
    {
        if(_populationCell<0)return false;
        if(_nodeAutomationDrop){_nodeAutomationDrop=false;if(CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))for(int i=0;i<3;i++)if(NodeAutomationOption(i).Contains(point))_setup.Robots.Workshops[_populationNode].Automation=(NodeAutomation)i;return true;}
        if(NodeAutomationField.Contains(point)){if(CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && !_setup.Campaign.RobotBattles.LockedNode(_populationNode))_nodeAutomationDrop=true;return true;}
        return false;
    }
    private void DrawNodeAutomation()
    {
        if(_populationCell<0)return;
        int mode=(int)_setup.Robots.Workshops[_populationNode].Automation;
        _ui.Button(NodeAutomationField,NodeAutomationLabels[mode]+" ▼",Muted,.53f);
        if(_nodeAutomationDrop)for(int i=0;i<3;i++)_ui.Button(NodeAutomationOption(i),NodeAutomationLabels[i],Accent,.53f);
    }
    private bool SupervisorNodePanel=>InspectingSupervisor && _populationCell>=0 && !_nodeFactoryOpen && !_nodeAssemblyOpen && !_nodeDisposalOpen && !_nodeTransportOpen;
    private bool InspectingSupervisor=>_inspectedNode>=0 && _inspectedNode<_setup.Nodes.All.Length && !Enumerable.Range(0,_setup.ActiveCount).Any(r=>_setup.Nodes.Current(r)?.Id==_inspectedNode);
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