namespace FolderVerse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;

// Versioned field graph: preserves private simulation state, PRNGs, shared stores and cycles.
// Only game-domain data, basic values and known collection types may be reconstructed.
public static class AutoSaveStore
{
    public sealed class Entry {
        public string Type {get;set;}="";
        public string Scalar {get;set;}
        public int[] Shape {get;set;}
        public List<int> Items {get;set;}
        public Dictionary<string,int> Fields {get;set;}
    }
    public sealed class Document {
        public int Version {get;set;}=1;
        public int Root {get;set;}
        public List<Entry> Objects {get;set;}=new();
    }
    private static readonly Type[] Collections={typeof(List<>),typeof(Dictionary<,>),typeof(HashSet<>),typeof(Queue<>)};
    private static bool Scalar(Type t)=>t.IsPrimitive || t.IsEnum || t==typeof(string) || t==typeof(decimal);
    private static bool Collection(Type t)=>t.IsGenericType && Collections.Contains(t.GetGenericTypeDefinition());
    private static bool Allowed(Type t)=>Scalar(t) || t.IsArray && Allowed(t.GetElementType()) ||
        Collection(t) && t.GetGenericArguments().All(Allowed) ||
        t.IsGenericType && t.FullName.StartsWith("System.ValueTuple`") && t.GetGenericArguments().All(Allowed) ||
        t.Assembly==typeof(WorldSetup).Assembly && !typeof(Game).IsAssignableFrom(t) && !typeof(Delegate).IsAssignableFrom(t) ||
        new[]{typeof(Vector2),typeof(Vector3),typeof(Quaternion),typeof(Point),typeof(Rectangle),typeof(Color),typeof(Matrix)}.Contains(t);
    private static FieldInfo[] Fields(Type t)=>t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
        .Where(f=>!typeof(Delegate).IsAssignableFrom(f.FieldType)).OrderBy(f=>f.Name,StringComparer.Ordinal).ToArray();
    // Turn-boundary saves need combat state, not thousands of already-played animation frames.
    private static object SavedField(object owner,FieldInfo field){
        bool playback=owner is BattleState && new[]{"<Frames>k__BackingField","<Events>k__BackingField","<Attacks>k__BackingField","<Persuasions>k__BackingField"}.Contains(field.Name) ||
            owner is RobotCampaign && field.Name=="<Scenes>k__BackingField";
        return playback?Activator.CreateInstance(field.FieldType):field.GetValue(owner);
    }
    public static Document Capture(object root){
        var doc=new Document();var ids=new Dictionary<object,int>(ReferenceEqualityComparer.Instance);
        int Visit(object value){
            if(value==null)return -1;
            var type=value.GetType();if(!Allowed(type))throw new InvalidDataException("Unsupported save type: "+type.FullName);
            if(!type.IsValueType && ids.TryGetValue(value,out int known))return known;
            if(doc.Objects.Count>=2000000)throw new InvalidDataException("Autosave object limit exceeded.");
            int id=doc.Objects.Count;var entry=new Entry{Type=type.AssemblyQualifiedName};doc.Objects.Add(entry);
            if(!type.IsValueType)ids[value]=id;
            if(Scalar(type))entry.Scalar=JsonSerializer.Serialize(value,type);
            else if(value is Array array){entry.Shape=Enumerable.Range(0,array.Rank).Select(array.GetLength).ToArray();entry.Items=new();foreach(var item in array)entry.Items.Add(Visit(item));}
            else if(value is IDictionary dictionary){entry.Items=new();foreach(DictionaryEntry pair in dictionary){entry.Items.Add(Visit(pair.Key));entry.Items.Add(Visit(pair.Value));}}
            else if(Collection(type)){entry.Items=new();foreach(var item in (IEnumerable)value)entry.Items.Add(Visit(item));}
            else {entry.Fields=new();foreach(var field in Fields(type))entry.Fields[field.Name]=Visit(SavedField(value,field));}
            return id;
        }
        doc.Root=Visit(root);return doc;
    }
    public static T Restore<T>(Document doc){
        if(doc.Version!=1 || doc.Objects.Count==0 || doc.Objects.Count>2000000)throw new InvalidDataException("Unsupported autosave format.");
        var values=new object[doc.Objects.Count];var types=new Type[values.Length];
        for(int i=0;i<values.Length;i++){
            var entry=doc.Objects[i];var type=Type.GetType(entry.Type,false);if(type==null || !Allowed(type))throw new InvalidDataException("Invalid save type.");types[i]=type;
            if(Scalar(type))values[i]=JsonSerializer.Deserialize(entry.Scalar,type);
            else if(type.IsArray)values[i]=Array.CreateInstance(type.GetElementType(),entry.Shape);
            else if(Collection(type))values[i]=Activator.CreateInstance(type);
            else values[i]=RuntimeHelpers.GetUninitializedObject(type);
        }
        object At(int id)=>id==-1?null:id>=0 && id<values.Length?values[id]:throw new InvalidDataException("Invalid save reference.");
        // Struct keys must be populated before insertion into hash-based collections.
        var populated=new bool[values.Length];
        void Populate(int i){
            if(populated[i])return;populated[i]=true;var entry=doc.Objects[i];var type=types[i];var value=values[i];
            if(Scalar(type))return;
            object Read(int id){if(id>=0 && types[id].IsValueType)Populate(id);return At(id);}
            if(value is Array array){int flat=0;foreach(int id in entry.Items){int n=flat++;var index=new int[array.Rank];for(int d=array.Rank-1;d>=0;d--){index[d]=n%array.GetLength(d);n/=array.GetLength(d);}array.SetValue(Read(id),index);}}
            else if(value is IDictionary dictionary){for(int n=0;n<entry.Items.Count;n+=2)dictionary.Add(Read(entry.Items[n]),Read(entry.Items[n+1]));}
            else if(Collection(type)){var add=type.GetMethod(type.GetGenericTypeDefinition()==typeof(Queue<>)?"Enqueue":"Add");foreach(int id in entry.Items)add.Invoke(value,new[]{Read(id)});}
            else foreach(var field in Fields(type)){
                if(!entry.Fields.TryGetValue(field.Name,out int id)){
                    if(type==typeof(WorldSetup) && field.Name=="<Lessons>k__BackingField"){field.SetValue(value,new BattleLessons());continue;}
                    if(type==typeof(BattleState) && field.Name=="Lessons"){field.SetValue(value,new BattleLesson[WorldSetup.CastCount]);continue;}
                    if(type==typeof(BattleUnit) && field.Name=="WitnessedLessons")continue;
                    if(type==typeof(RobotCampaign) && field.Name=="_homeOwners"){field.SetValue(value,new Dictionary<long,int>());continue;}
                    if(type==typeof(Robot) && (field.Name=="<LastBattlePersuasionTurn>k__BackingField" || field.Name=="<LastDomesticPersuasionTurn>k__BackingField")){field.SetValue(value,-1);continue;}
                    if(type==typeof(RobotWorld) && field.Name=="<DomesticPersuasionTurns>k__BackingField"){field.SetValue(value,new Dictionary<int,int>());continue;}
                    if(type==typeof(BattleState) && field.Name=="GlobalTurn"){field.SetValue(value,0);continue;}
                    if(type==typeof(BattleState) && field.Name=="<PersuadedUnits>k__BackingField"){field.SetValue(value,new HashSet<long>());continue;}
                    if(type==typeof(BattleUnitView) && field.Name=="<Robot>k__BackingField")continue;
                    // Defaults for fields introduced after older turn-boundary saves.
                    if(type==typeof(RobotEncounter) && field.Name=="<ReturnedWanderers>k__BackingField"){field.SetValue(value,new HashSet<long>());continue;}
                    if(type==typeof(WorldSetup) && field.Name=="WandererPolicies"){field.SetValue(value,new WandererPolicy[WorldSetup.CastCount]);continue;}
                    if(type==typeof(WorldSetup) && new[]{"_presetCastCount","TutorialPocketed","TutorialMoved","TutorialPocketNoticeShown","TutorialMoveNoticeShown"}.Contains(field.Name))continue;
                    if(type==typeof(March) && field.Name=="<Peaceful>k__BackingField")continue;
                    if(type==typeof(BattleArmy) && (field.Name=="Wanderer" || field.Name=="WandererPolicy"))continue;
                    if(type==typeof(BattleRules) && field.Name=="<FlagCaptureTurns>k__BackingField"){field.SetValue(value,10);continue;}
                    if(type==typeof(BattleRules) && field.Name=="<FlagDefenseTurns>k__BackingField"){field.SetValue(value,50);continue;}
                    if((type==typeof(BattleState) && (field.Name=="FlagCaptureTurns" || field.Name=="FlagDefenseTurns")) ||
                       (type==typeof(BattleFrame) && (field.Name=="<FlagCaptureTurns>k__BackingField" || field.Name=="<FlagDefenseTurns>k__BackingField")))continue;
                    if(type==typeof(March) && field.Name=="<RobotIds>k__BackingField")continue;
                    if(type==typeof(BattleState) && field.Name=="<Persuasions>k__BackingField"){field.SetValue(value,new List<BattlePersuasion>());continue;}
                    throw new InvalidDataException("Autosave field is missing: "+field.Name);
                }
                field.SetValue(value,Read(id));
            }
        }
        for(int i=0;i<values.Length;i++)Populate(i);
        return At(doc.Root) is T result?result:throw new InvalidDataException("Invalid autosave root.");
    }
    public static void Write(string path,object root){
        var doc=Capture(root);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        string pending=path+".tmp";
        try{
            using(var file=new FileStream(pending,FileMode.Create,FileAccess.Write,FileShare.None)){
                using(var zip=new GZipStream(file,CompressionLevel.Fastest,true))JsonSerializer.Serialize(zip,doc);
                file.Flush(true);
            }
            if(File.Exists(path))File.Replace(pending,path,null);else File.Move(pending,path);
        }finally{if(File.Exists(pending))File.Delete(pending);}
    }
    public static T Read<T>(string path){
        using var file=File.OpenRead(path);using var zip=new GZipStream(file,CompressionMode.Decompress);
        var doc=JsonSerializer.Deserialize<Document>(zip)??throw new InvalidDataException("Empty autosave.");return Restore<T>(doc);
    }
}
