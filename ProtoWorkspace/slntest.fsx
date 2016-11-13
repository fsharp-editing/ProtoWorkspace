System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

#load "scripts/load-references-release.fsx"
#r "bin/release/protoworkspace.dll"
open System.IO
open ProtoWorkspace

let printsq sqs = sqs|>Seq.iter^printfn"%A"

let testSlnPath = "../ProtoWorkspace.sln"

let fswork = new FSharpWorkspace()
;;
string fswork.CurrentSolution.FilePath
;;

let slnFileInfo = (SolutionFileInfo.load testSlnPath)
slnFileInfo.Path
;;
let slnInfo = SolutionFileInfo.toSolutionInfo fswork slnFileInfo

let sln = fswork.AddSolution slnInfo
;;
string fswork.CurrentSolution.FilePath

fswork.CurrentSolution.Projects |> Seq.iter(fun p->
    let docs = p.Documents |> Seq.map (fun p -> "  - " + p.Name) |> String.concat "\n"
    printfn "%s\n%s" p.Name docs)
;;
fswork.CurrentSolution.Projects
