System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
#r "bin/release/protoworkspace.dll"
#r "System.Reflection"
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
open Microsoft.CodeAnalysis.MSBuild
open Microsoft.CodeAnalysis.Host.Mef
open System.IO


let testSlnPath = "../data/TestSln.sln"
let module1 = File.ReadAllText "../data/module_001.fs"
let module2 = File.ReadAllText "../data/module_002.fs"
let script1 = File.ReadAllText "../data/script_001.fsx"


let setup (ctx:CompositionContext) = 
    MefHostServices.Create ctx


let agg = HostServicesAggregator(Seq.empty)

let wks = new FSharpWorkspace()

wks.AddSolution((SolutionFile.load testSlnPath).)

