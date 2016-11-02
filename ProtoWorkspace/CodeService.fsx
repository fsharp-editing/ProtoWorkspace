System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

#load "scripts/load-references-release.fsx"
#r "bin/release/protoworkspace.dll"

open System
open System.IO
open System.Collections.Generic
open Microsoft.CodeAnalysis
open Microsoft.FSharp.Compiler.SourceCodeServices
open ProtoWorkspace
open Microsoft.Build
open Microsoft.Build.Framework
open Microsoft.Build.Evaluation

let printsq sqs = sqs |> Seq.iter (printfn "%A")

let library1path = "data/projects/Library1/Library1.fsproj" |> Path.GetFullPath
let library2path = "data/projects/Library2/Library2.fsproj" |> Path.GetFullPath

let p = Project(library1path)

let lib1info = (ProjectFileInfo.create2 library1path) |> ProjectFileInfo.toProjectInfo

let checker = FSharpChecker.Create()

let fswork = new FSharpWorkspace()
;;
printsq fswork.Services.SupportedLanguages
;;
//fswork.Services.GetLanguageServices "FSharp"
;;
let lib1proj = fswork.AddProject lib1info
;;
printsq lib1proj.Documents 
;;

lib1proj.DocumentIds