using FolderVerse;
void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
var enabled=Enumerable.Repeat(true,8).ToArray();var costs=new double[8];costs[4]=9;costs[2]=3;costs[7]=999;
var breaker=new DisplayBreaker();for(int i=0;i<3;i++)Check(breaker.Observe(i,100,enabled,costs,true)==null,"wait before sustained overload "+i);
Check(breaker.Observe(3,100,enabled,costs,true)==4,"heaviest measured item trips first, relief is not guessed");enabled[4]=false;
Check(breaker.Observe(4,100,enabled,costs,true)==null&&breaker.Observe(5,100,enabled,costs,true)==null,"one-at-a-time cooldown");Check(breaker.Observe(6,100,enabled,costs,true)==2,"next measured active item trips after settling");Check(breaker.Tripped[4]&&!enabled[4],"no automatic reenable");Check(breaker.LastCost[4]==9,"off item retains last enabled timing");
var spike=new DisplayBreaker();Check(spike.Observe(0,1000,enabled,costs,true)==null,"initial spike waits");for(int i=1;i<10;i++)Check(spike.Observe(i,10,enabled,costs,true)==null,"spike recovery does not trip "+i);
var manual=new DisplayBreaker();enabled=new bool[8];enabled[4]=true;manual.Manual(4,true,0);for(int i=0;i<10;i++)Check(manual.Observe(i,100,enabled,costs,true)==null,"manual on protected "+i);Check(manual.Observe(10,100,enabled,costs,true)==4,"manual protection expires after ten seconds");
var disabled=new DisplayBreaker{Automatic=false};for(int i=0;i<6;i++)Check(disabled.Observe(i,100,enabled,costs,true)==null,"automatic off setting respected "+i);
var hidden=new DisplayBreaker();for(int i=0;i<6;i++)Check(hidden.Observe(i,100,enabled,costs,false)==null,"non-map scene cannot trip "+i);
var unknown=new DisplayBreaker();for(int i=0;i<6;i++)Check(unknown.Observe(i,100,enabled,new double[8],true)==null,"no guessed costs "+i);var gap=new DisplayBreaker();gap.Observe(0,100,enabled,costs,true);Check(gap.Observe(10,10000,enabled,costs,true)==null,"single long stall cannot satisfy three observation windows");
