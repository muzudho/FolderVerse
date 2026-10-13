using FolderVerse;
var battles=Enumerable.Range(0,14).Select(i=>new BattleState(i){Finished=true,Winner=0}).ToArray();
foreach(var b in battles){for(int i=0;i<8;i++)b.NewspaperRanks[i]=i+1;b.Armies.Add(new(){Owner=0});b.Armies.Add(new(){Owner=7});}
battles[13].Winner=7;
var picked=NewspaperSelection.Select(battles,1,-1,9);
if(picked.Length!=9 || picked.Distinct().Count()!=9 || picked[0]!=13)throw new Exception("Upset priority or capacity failed");
if(!picked.SequenceEqual(NewspaperSelection.Select(battles,1,-1,9)))throw new Exception("Unstable selection");
if(NewspaperSelection.Select(battles.Take(2).ToArray(),1,-1).Length!=2)throw new Exception("Small issue omitted a battle");
if(battles[0].Winner!=0 || battles[13].Winner!=7)throw new Exception("Selection changed source");
Console.WriteLine("PASS: upset priority, capacity, all small results, stable order and non-destructive selection");
