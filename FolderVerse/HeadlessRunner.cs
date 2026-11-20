namespace FolderVerse;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

// No Game1, window, audio device, or user autosave is created by this entry point.
public static class HeadlessRunner
{
    public static int Run(string[] args,TextWriter output=null,TextWriter error=null)
    {
        output??=Console.Out;error??=Console.Error;StreamWriter file=null;
        try{
            int seed=0,cast=271828,rights=492471852,fortune=1544938375,players=20,maxTurns=400,scatter=25,raw=25;string directory=null;
            for(int i=0;i<args.Length;i++){
                string option=args[i];if(option=="--headless")continue;
                if(option=="--help"){output.WriteLine("--headless [--seed N] [--cast-seed N] [--rights-seed N] [--fortune-seed N] [--players 1..60] [--max-turns 1..400] [--robot-scatter 0..100] [--raw-stock 0..100] [--output DIRECTORY]");return 0;}
                if(i+1==args.Length)throw new ArgumentException("Missing value: "+option);
                string value=args[++i];if(option=="--output"){directory=Path.GetFullPath(value);continue;}
                if(!int.TryParse(value,out int number))throw new ArgumentException("Expected integer: "+option);
                switch(option){
                    case "--seed":seed=number;break;case "--cast-seed":cast=number;break;
                    case "--rights-seed":rights=number;break;case "--fortune-seed":fortune=number;break;
                    case "--players":players=number;break;case "--max-turns":maxTurns=number;break;
                    case "--robot-scatter":scatter=number;break;case "--raw-stock":raw=number;break;
                    default:throw new ArgumentException("Unknown option: "+option);
                }
            }
            if(players<1 || players>GameLimits.MaxConquerors || maxTurns<1 || maxTurns>400 || scatter<0 || scatter>100 || raw<0 || raw>100)
                throw new ArgumentException("players: 1..60; max-turns: 1..400; robot-scatter/raw-stock: 0..100");
            if(directory!=null){
                if(File.Exists(Path.Combine(directory,"world.fvs")))throw new IOException("Output save already exists.");
                Directory.CreateDirectory(directory);
                file=new StreamWriter(new FileStream(Path.Combine(directory,"run.jsonl"),FileMode.CreateNew,FileAccess.Write,FileShare.Read)){AutoFlush=true};
            }
            void Write(object value){string json=JsonSerializer.Serialize(value);output.WriteLine(json);file?.WriteLine(json);}
            var clock=Stopwatch.StartNew();var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};
            w.SetParticipantCounts(0,players);w.SetWorld(seed);w.SetCast(cast);w.SetTerritoryRights(rights);
            var planner=new ComputerLandPlanner(w);foreach(int ruler in w.LandOrder())w.ClaimLand(ruler,planner.Select(ruler,null).Cells);
            w.CompleteLand(fortune,scatter,raw);
            using(var assembly=File.OpenRead(typeof(WorldSetup).Assembly.Location))
                Write(new{kind="start",seed,castSeed=cast,rightsSeed=rights,fortuneSeed=fortune,players,maxTurns,robotScatter=scatter,rawStock=raw,
                    equipmentMode=w.EquipmentMode.ToString(),w.RelayLogisticsEnabled,rawEnabled=w.Robots.MapleSupplyEnabled,
                    version=typeof(WorldSetup).Assembly.GetName().Version?.ToString(),dllSha256=Convert.ToHexString(SHA256.HashData(assembly)),
                    characters=w.Looks.Take(w.ActiveCount).Select(l=>l.BaseId),
                    initial=w.Robots.Nodes.Select((s,n)=>new{node=n,s.Count,s.TotalCapacity,s.WarehouseLimit,keepers=s.Robots.Count(r=>r.WarehouseKeeper),rawUnits=s.MaplePackages.Sum(p=>p.Units)})});
            bool Won()=>w.Relations.ProvisionalRanking(w).Any(r=>r.Cells==w.Cells.Length && w.Relations.CanWinConquest(r.Ruler));
            bool won=Won();
            for(int step=0;step<maxTurns && !won;step++){
                w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
                if(!w.Campaign.FinishDomestic(w,automaticAll:true))throw new InvalidOperationException("Domestic phase did not finish.");
                w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true);won=Won();
                if(!won){w.Industry.Roll(w.PlacementSeed^w.FortuneSeed);w.Industry.Apply(w);}
                if(w.Population.Turn%20==0 || won)Write(new{kind="checkpoint",turn=w.Population.Turn,won,ranking=w.Relations.ProvisionalRanking(w).Select(r=>new{r.Ruler,r.Cells,r.Rank})});
                w.Campaign.Trade.Replies.Clear();
            }
            if(directory!=null)AutoSaveStore.Write(Path.Combine(directory,"world.fvs"),w);
            Write(new{kind="finish",turn=w.Population.Turn,won,stopReason=won?"conquest":"turn_limit_convergence_undetermined",elapsedSeconds=clock.Elapsed.TotalSeconds,ranking=w.Relations.ProvisionalRanking(w).Select(r=>new{r.Ruler,r.Cells,r.Rank})});
            return 0;
        }catch(Exception exception){error.WriteLine(exception.ToString());return 2;}
        finally{file?.Dispose();}
    }
}
