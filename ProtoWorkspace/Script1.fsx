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
open System.Composition
open System.Composition.Hosting
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.Editing
open Microsoft.CodeAnalysis.MSBuild
open Microsoft.CodeAnalysis.Host.Mef
open System.IO



let module1 = File.ReadAllText "../data/module_001.fs"
let module2 = File.ReadAllText "../data/module_002.fs"
let script1 = File.ReadAllText "../data/script_001.fsx"

let srctxt = SourceText.From module1


let readSln filePath =
    use stream = File.OpenRead filePath
    use reader = StreamReader stream
    Solution
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
srctxt.Lines |> Seq.iter (printfn "%A")







