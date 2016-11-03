System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

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
open ProtoWorkspace.XLinq


let printsq sqs = sqs |> Seq.iter (printfn "%A")


let library1path = "../data/projects/Library1/Library1.fsproj" |> Path.GetFullPath
let library2path = "../data/projects/Library2/Library2.fsproj" |> Path.GetFullPath


let xdoc = (library1path |> File.ReadAllText |> XDocument.Parse).Root

let lib1info = (ProjectFileInfo.fromXDoc library1path) |> ProjectFileInfo.toProjectInfo

let fswork = new FSharpWorkspace()
;;
printsq fswork.Services.SupportedLanguages
;;
let lib1proj = fswork.AddProject lib1info
;;
lib1proj.Documents
|> Seq.iter (fun doc -> printfn "%s - %s" doc.Name doc.FilePath)

// Below is for the MsBuild Approach

//System.Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH","../packages/MSBuild/runtimes/any/native/MSBuild.exe")

//let collection = new ProjectCollection()
////collection.GlobalProperties.Add("BuildingInsideVisualStudio", "true")
//collection.SetGlobalProperty("BuildingInsideVisualStudio", "true")
//
//collection.DefaultToolsVersion <- "14.0"
//let proj = Project(library1path,null,null,collection,ProjectLoadSettings.IgnoreMissingImports)
