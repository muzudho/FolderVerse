using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if(args.Contains("--self-test")){SelfTest();return 0;}
if(args.Length<1){Console.Error.WriteLine("Usage: FontCoverage <game-directory> [--update]");return 2;}
var root=Path.GetFullPath(args[0]);var fontPath=Path.Combine(root,"Content","UiFont.spritefont");
var uses=new SortedDictionary<char,(string File,int Line)>();var counterUses=new SortedDictionary<char,(string File,int Line)>();
foreach(var path in Sources(root)){
    var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path),new CSharpParseOptions(LanguageVersion.Preview),path);
    Collect(tree.GetRoot(),uses,path);
    foreach(var call in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        if(call.Expression is MemberAccessExpressionSyntax member && member.Expression is IdentifierNameSyntax name && name.Identifier.ValueText=="_counter" && member.Name.Identifier.ValueText=="DrawText" && call.ArgumentList.Arguments.Count>1)Collect(call.ArgumentList.Arguments[1],counterUses,path);
}
// Counter font accepts numeric formatted values as well as the source literals at its call sites.
foreach(char c in "0123456789%.")counterUses.TryAdd(c,("numeric counter contract",1));
var doc=XDocument.Load(fontPath,LoadOptions.PreserveWhitespace);var missing=Missing(uses,doc);
if(args.Contains("--update") && missing.Length>0){
    var regions=doc.Descendants("CharacterRegions").Single();
    foreach(char c in missing.Where(c=>!char.IsSurrogate(c)))regions.Add(new XElement("CharacterRegion",new XElement("Start",c),new XElement("End",c)));
    // Preserve the hand-maintained regions, including characters used by external content.
    doc.Save(fontPath,SaveOptions.DisableFormatting);Console.WriteLine($"FontCoverage: added {missing.Count(c=>!char.IsSurrogate(c))} glyphs to {fontPath}");
    missing=Missing(uses,doc);
}
int errors=Report(missing,uses,fontPath);
var counterPath=Path.Combine(root,"Content","ToyCounterFont.spritefont");errors+=Report(Missing(counterUses,XDocument.Load(counterPath,LoadOptions.PreserveWhitespace)),counterUses,counterPath);
if(errors>0){Console.Error.WriteLine("Register glyphs with: dotnet run --project tools/FontCoverage -- FolderVerse --update");return 1;}
Console.WriteLine($"FontCoverage: PASS ({uses.Count} static characters; UI and counter fonts).");return 0;

static IEnumerable<string> Sources(string root){
    foreach(var file in Directory.EnumerateFiles(root,"*.cs"))yield return file;
    foreach(var directory in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal)){
        if(new[]{"bin","obj",".git"}.Contains(Path.GetFileName(directory),StringComparer.OrdinalIgnoreCase))continue;
        foreach(var file in Sources(directory))yield return file;
    }
}
static void Collect(SyntaxNode root,SortedDictionary<char,(string File,int Line)> uses,string file,int lineOffset=0){
    foreach(var token in root.DescendantTokens()){
        string? text=null;
        if(token.Parent is LiteralExpressionSyntax literal && (literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.CharacterLiteralExpression)))text=token.ValueText;
        else if(token.IsKind(SyntaxKind.InterpolatedStringTextToken))text=token.ValueText;
        if(text==null)continue;
        int line=token.GetLocation().GetLineSpan().StartLinePosition.Line+1+lineOffset;
        foreach(char c in text)if(!char.IsControl(c))uses.TryAdd(c,(file,line));
    }
    // Parse inactive preprocessor branches too, so Release-only messages cannot escape Debug checks.
    foreach(var trivia in root.DescendantTrivia().Where(t=>t.IsKind(SyntaxKind.DisabledTextTrivia)))
        Collect(CSharpSyntaxTree.ParseText(trivia.ToFullString(),new CSharpParseOptions(LanguageVersion.Preview)).GetRoot(),uses,file,lineOffset+trivia.GetLocation().GetLineSpan().StartLinePosition.Line);
}
static char[] Missing(SortedDictionary<char,(string File,int Line)> uses,XDocument font){
    var ranges=font.Descendants("CharacterRegion").Select(r=>(Start:(int)r.Element("Start")!.Value.Single(),End:(int)r.Element("End")!.Value.Single())).ToArray();
    // Deliberately ignore DefaultCharacter: fallback must never conceal missing static glyphs.
    return uses.Keys.Where(c=>!ranges.Any(r=>c>=r.Start && c<=r.End)).ToArray();
}
static int Report(char[] missing,SortedDictionary<char,(string File,int Line)> uses,string font){
    foreach(char c in missing){var at=uses[c];
        if(char.IsSurrogate(c))Console.Error.WriteLine($"{at.File}({at.Line}): error FONT002: non-BMP text (U+{(int)c:X4}) needs an image or a Unicode-capable renderer; SpriteFont stores BMP glyphs only.");
        else Console.Error.WriteLine($"{at.File}({at.Line}): error FONT001: {Path.GetFileName(font)} lacks U+{(int)c:X4} '{c}'.");
    }
    return missing.Length;
}
static void SelfTest(){
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    var uses=new SortedDictionary<char,(string File,int Line)>();
    // Construct syntax samples without depending on the shell's Unicode or escape handling.
    string source="class C { string a = "+'"'+"\\u8A66"+'"'+"; string b = $"+'"'+"迎撃{1}船"+'"'+"; char c = '単'; string d = @"+'"'+"市役所"+'"'+"; } // 龍";
    Collect(CSharpSyntaxTree.ParseText(source).GetRoot(),uses,"fixture.cs");
    Check("試単迎撃船市役所".All(uses.ContainsKey) && !uses.ContainsKey('龍'),"Escaped, interpolated, verbatim strings and char literals must be parsed; comments excluded");
    string raw="class C { string a = "+new string('"',3)+"生産"+new string('"',3)+"; string b = $"+new string('"',3)+"工場{1}部位"+new string('"',3)+"; }";
    Collect(CSharpSyntaxTree.ParseText(raw,new CSharpParseOptions(LanguageVersion.Preview)).GetRoot(),uses,"raw.cs");
    Collect(CSharpSyntaxTree.ParseText("#if NEVER\nclass Hidden { string s = "+'"'+"隠"+'"'+"; }\n#endif").GetRoot(),uses,"inactive.cs");
    Check(uses.ContainsKey('隠'),"Inactive preprocessor branches must be checked");
    Check("生産工場部位".All(uses.ContainsKey),"Raw and interpolated raw strings must be checked");
    var font=XDocument.Parse("<Asset><DefaultCharacter>?</DefaultCharacter><CharacterRegions><CharacterRegion><Start> </Start><End>~</End></CharacterRegion></CharacterRegions></Asset>",LoadOptions.PreserveWhitespace);
    Check(Missing(uses,font).Contains('単'),"Missing static glyphs must fail even if a fallback exists");
    foreach(char c in uses.Keys)font.Descendants("CharacterRegions").Single().Add(new XElement("CharacterRegion",new XElement("Start",c),new XElement("End",c)));
    Check(Missing(uses,font).Length==0,"Registered glyphs must pass");
    Console.WriteLine("PASS: regular/escaped/verbatim/raw/interpolated/char syntax, comment exclusion, strict missing-glyph detection and registration.");
}
