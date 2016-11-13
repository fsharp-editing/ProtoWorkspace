System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
#r "System.Threading.Tasks"
#load "scripts/load-references-release.fsx"
#r "bin/release/protoworkspace.dll"

open System
open System.IO
open System.Collections.Generic
open Microsoft.CodeAnalysis
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.Build
open Microsoft.Build.Framework
open Microsoft.Build.Evaluation
open Microsoft.Build.Execution
open Microsoft.Build.Utilities
open System.Xml
open System.Xml.Linq
open ProtoWorkspace
open System.Threading


let printsq sqs = sqs |> Seq.iter (printfn "%A")


//let library1path = "../data/projects/Library1/Library1.fsproj" |> Path.GetFullPath
let library1path = "ProtoWorkspace.fsproj" |> Path.GetFullPath
let library2path = "../data/projects/Library2/Library2.fsproj" |> Path.GetFullPath


let xdoc = (library1path |> File.ReadAllText |> XDocument.Parse).Root

let fswork = new FSharpWorkspace()


let lib1info = (ProjectFileInfo.create library1path) |> ProjectFileInfo.toProjectInfo fswork

let lib1proj = fswork.AddProject lib1info
;;
lib1proj.Documents
|> Seq.iter (fun doc -> printfn "%s - %s" doc.Name doc.FilePath)


lib1proj.Documents |> Seq.find(fun doc -> doc.Name = "Library1")
|> fun doc ->
    printfn "%s" doc.Name
    let text = doc.GetTextAsync().Result

    text.ToString()
;;

let checker = FSharpChecker.Create()

let fsopts = lib1proj.ToFSharpProjectOptions fswork
;;
let checkDoc (doc:Document) = async {
    let! version = doc.GetTextVersionAsync() |> Async.AwaitTask
    let! text = doc.GetTextAsync() |> Async.AwaitTask
    let! parseResults, checkAnswer = checker.ParseAndCheckFileInProject(doc.FilePath,0,text.ToString(),fsopts)
    return
        match checkAnswer with
        | FSharpCheckFileAnswer.Succeeded res -> Some res
        | res -> None
}


lib1proj.Documents |> Seq.find(fun doc -> doc.Name = "ProjectFileInfo")
|> fun doc ->
    let results = checkDoc doc |> Async.RunSynchronously
    if results.IsSome then
        (results.Value.GetAllUsesOfAllSymbolsInFile() |> Async.RunSynchronously)
        |> Array.iter ^ fun sym -> printfn "%s" sym.Symbol.FullName
    else
        printfn "didn't get back symbol results"


