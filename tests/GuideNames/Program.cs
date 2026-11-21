using FolderVerse;

int checks=0;
void Check(bool condition){checks++;if(!condition)throw new Exception($"Check {checks}");}
foreach(int seed in new[]{0,1,-1,int.MinValue,int.MaxValue,42,104729}){
    var names=GuideNames.Generate(60,seed);
    Check(names.Length==60 && names.Distinct().Count()==60);
    Check(names.SequenceEqual(GuideNames.Generate(60,seed)));
    Check(names.Take(10).SequenceEqual(GuideNames.Generate(10,seed)));
    Check(names.All(n=>n.Split('・').Length==2 && n.Length<=15));
}
Check(!GuideNames.Generate(60,1).SequenceEqual(GuideNames.Generate(60,2)));
Check(GuideNames.Generate(0,1).Length==0);
foreach(int invalid in new[]{-1,61}){
    bool rejected=false;try{GuideNames.Generate(invalid,0);}catch(ArgumentOutOfRangeException){rejected=true;}
    Check(rejected);
}
Console.WriteLine($"PASS {checks} guide name checks");
foreach(string name in GuideNames.Generate(6,42))Console.WriteLine("ガイド　"+name);
