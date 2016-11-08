System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
#r "../packages/Microsoft.Build/lib/net46/Microsoft.Build.dll"
#r "../packages/Microsoft.Build.Framework/lib/net46/Microsoft.Build.Framework.dll"
#r "../packages/Microsoft.Build.Tasks.Core/lib/net46/Microsoft.Build.Tasks.Core.dll"
#r "../packages/Microsoft.Build.Utilities.Core/lib/net46/Microsoft.Build.Utilities.Core.dll"
#r "bin/release/protoworkspace.dll"

open System.IO
open Microsoft.Build
open Microsoft.Build.Evaluation
open Microsoft.Build.Execution


let manager = BuildManager.DefaultBuildManager

let buildParam = BuildParameters(DetailedSummary=true)

let fsprojFile = Path.Combine(__SOURCE_DIRECTORY__, "ProtoWorkspace.fsproj")

File.ReadAllLines fsprojFile
;;
let project = ProjectInstance fsprojFile
let requestReferences =
        BuildRequestData(project,
            [|
                "ResolveAssemblyReferences"
                "ResolveProjectReferences"
            |])

let fromBuildRes targetName (result:BuildResult) =
        result.ResultsByTarget.[targetName].Items
        |> Seq.iter(fun r -> printfn "%s" r.ItemSpec)

let exec() =
    let result = manager.Build(buildParam,requestReferences)
    fromBuildRes "ResolveProjectReferences" result
    fromBuildRes "ResolveAssemblyReferences" result
;;
exec()
;;
project.Properties|>Seq.iter(fun p -> printfn "%s -%s" p.Name p.EvaluatedValue)


//project.Items|>Seq.iter(fun p -> p.ItemType p.EvaluatedInclude)




