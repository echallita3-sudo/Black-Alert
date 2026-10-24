using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var files=Directory.GetFiles("Assets","*.cs",SearchOption.AllDirectories);
if(files.Length<9)throw new InvalidOperationException("Unity source files unexpectedly missing");
var total=0;
foreach(var defines in new[]{new string[0],new[]{"UNITY_EDITOR","UNITY_ANDROID"}})
{
    var options=new CSharpParseOptions(LanguageVersion.Latest,DocumentationMode.Diagnose,
        SourceCodeKind.Regular,defines);
    foreach(var file in files)
    {
        var text=File.ReadAllText(file);
        var tree=CSharpSyntaxTree.ParseText(text,options,path:file);
        var errors=tree.GetDiagnostics().Where(d=>d.Severity==DiagnosticSeverity.Error).ToArray();
        total+=errors.Length;
        foreach(var error in errors)Console.WriteLine("SYNTAX ERROR "+error);
    }
}
if(total>0)throw new InvalidOperationException(total+" C# syntax errors detected.");
Console.WriteLine("PASS: Roslyn C# syntax parsed "+
    files.Length+" Unity scripts in Android and Editor modes (no Unity license/minutes).");
Console.WriteLine("NOTE: parsing cannot verify Unity API signatures, shaders, assets or runtime behavior.");
