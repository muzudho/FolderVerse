namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private bool _trainingOpen;
    private int _trainingDrop=-1;
    private static readonly Rectangle TrainingButton=new(480,912,190,52),TrainingClose=new(1340,220,58,44);
    private static Rectangle TrainingField(int field)=>new(480,360+field*190,900,64);
    private static Rectangle TrainingOption(int field,int option)=>new(480,424+field*190+option*48,900,48);
    private static readonly string[] AssemblyLabels={"自国のパーツのみ","自国のパーツが２／３以上","自国のパーツが１／３以上","現地に任せる"};
    private static readonly string[] DeploymentLabels={"自国のパーツのみのロボット","自国のパーツが多いロボットから","自国のパーツが少ないロボットから","現地に任せる"};
    private bool TrainingInput(Point point,bool click,bool escape)
    {
        if(_setup.TutorialStep is 1 or 2 || StrategyReadOnly){_trainingOpen=false;_trainingDrop=-1;return false;}
        if(!_trainingOpen){if(click && _reignView && _populationCell<0 && !_movementOpen && !_transportOverview && TrainingButton.Contains(new Point(point.X-NodeInspectorOffset,point.Y))){_trainingOpen=true;_setup.Robots.RevalidateWorkReservations();_trainingDrop=-1;return true;}return false;}
        if(escape || click && TrainingClose.Contains(point)){_trainingOpen=false;_setup.Robots.RevalidateWorkReservations();_trainingDrop=-1;return true;}
        if(!click)return true;
        var policy=_setup.Robots.Training[_setup.PlayerSlot];
        if(_trainingDrop>=0){int field=_trainingDrop;var labels=field==1?DeploymentLabels:AssemblyLabels;for(int i=0;i<labels.Length;i++)if(TrainingOption(field,i).Contains(point)){if(field==0)policy.AssemblyPolicy=i;else if(field==1)policy.DeploymentPolicy=i;else policy.WorkerPolicy=i;break;}_setup.Robots.RevalidateWorkReservations();_trainingDrop=-1;return true;}
        for(int field=0;field<3;field++)if(TrainingField(field).Contains(point))_trainingDrop=field;
        return true;
    }
    private void DrawTraining()
    {
        if(_setup.TutorialStep is 1 or 2 || !_reignView || StrategyReadOnly)return; DrawInspector(()=>_ui.Button(TrainingButton,"調練",Accent,.6f));
        if(!_trainingOpen)return;
        var policy=_setup.Robots.Training[_setup.PlayerSlot];
        _ui.Box(new(420,200,1020,820),new(27,49,62));_ui.Text("調練 / 征服者直属",new(460,240),.8f,Cream);_ui.Button(TrainingClose,"×",Muted,.7f);
        _ui.Text("配備ロボットに求める自国パーツの割合",new(480,310),.58f,Cream);
        _ui.Text("戦闘に出す兵隊ロボット",new(480,500),.58f,Cream);
        _ui.Text("工場作業員ロボットに求める自国パーツの割合",new(480,690),.58f,Cream);
        for(int f=0;f<3;f++){var r=TrainingField(f);_ui.Button(r,(f==0?AssemblyLabels[policy.AssemblyPolicy]:f==1?DeploymentLabels[policy.DeploymentPolicy]:AssemblyLabels[policy.WorkerPolicy]),Muted,.55f);for(int row=0;row<10;row++)_ui.Box(new(r.Right-30+row,r.Center.Y-4+row,20-row*2,1),Cream);}
        _ui.Text("自国パーツ     盾     短剣     小銃",new(480,840),.40f,Cream);
        _ui.Text("１／３             1          1          1",new(480,880),.40f,Cream);
        _ui.Text("２／３             2          2          2",new(480,920),.40f,Cream);
        _ui.Text("３／３             3          3          4",new(480,960),.40f,Cream);
        if(_trainingDrop>=0){var labels=_trainingDrop==1?DeploymentLabels:AssemblyLabels;for(int i=0;i<labels.Length;i++)_ui.Button(TrainingOption(_trainingDrop,i),labels[i],Accent,.52f);}
    }
}
