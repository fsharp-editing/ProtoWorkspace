System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
#r "bin/release/protoworkspace.dll"
#r "System.IO"
#r "System.Reflection"
#r "System.Text.Encoding"
#r "../packages/System.Reflection.Metadata/lib/netstandard1.1/System.Reflection.Metadata.dll"
#r "../packages/System.Collections.Immutable/lib/netstandard1.0/System.Collections.Immutable.dll"
#r "../packages/Microsoft.Composition/lib/portable-net45+win8+wp8+wpa81/System.Composition.AttributedModel.dll"
#r "../packages/Microsoft.Composition/lib/portable-net45+win8+wp8+wpa81/System.Composition.Convention.dll"
#r "../packages/Microsoft.Composition/lib/portable-net45+win8+wp8+wpa81/System.Composition.Hosting.dll"
#r "../packages/Microsoft.Composition/lib/portable-net45+win8+wp8+wpa81/System.Composition.Runtime.dll"
#r "../packages/Microsoft.Composition/lib/portable-net45+win8+wp8+wpa81/System.Composition.TypedParts.dll"
#r "../packages/Microsoft.CodeAnalysis.Common/lib/net45/Microsoft.CodeAnalysis.dll"
#r "../packages/Microsoft.CodeAnalysis.Workspaces.Common/lib/net45/Microsoft.CodeAnalysis.Workspaces.Desktop.dll"
#r "../packages/Microsoft.CodeAnalysis.Workspaces.Common/lib/net45/Microsoft.CodeAnalysis.Workspaces.dll"

open ProtoWorkspace
open ProtoWorkspace.Workspace
open System
open System.IO
open System.Composition
open System.Composition.Hosting
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.Editing
open Microsoft.CodeAnalysis.MSBuild
open Microsoft.CodeAnalysis.Host.Mef

let testSlnPath = "../data/TestSln.sln"
let module1 = File.ReadAllText "../data/module_001.fs"
let module2 = File.ReadAllText "../data/module_002.fs"
let script1 = File.ReadAllText "../data/script_001.fsx"

let srctxt = SourceText.From module1


let private getNextNonEmptyLine (reader:TextReader) =
    let rec getLine (line:string) =
        if isNull line || line.Trim() = String.Empty then line else
        getLine <| reader.ReadLine()
    getLine <| reader.ReadLine()


let testStr = """

# skip me biatch

    the first line
"""



let reader = new StringReader(testStr)


//let rec getLines() = seq {
//    // finish if not a commentline, empty line, or if it's the end of the file
//    if reader.Peek() = -1 || not (Array.contains (reader.Peek()|>char) [|'#';'\r';'\n'|]) then () else
//    if reader.Peek() = -1 then () else
//    yield reader.ReadLine()
//    yield! getLines()
//}
//;;
//getLines() |> Seq.iter (printfn "%s")

let readSln filePath =
    use stream = File.OpenRead filePath
    use reader = new StreamReader(stream)
    SolutionFile.parse reader
;;

(readSln testSlnPath).ToString()


(*

private static SolutionFile ReadSolutionFile(string filePath)
{
    using (var stream = File.OpenRead(filePath))
    using (var reader = new StreamReader(stream))
    {
        return SolutionFile.Parse(reader);
    }
}



*)


//
//
//srctxt.Lines |> Seq.iter (printfn "%A")







