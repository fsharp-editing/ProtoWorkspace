System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

#r "System.Threading.Tasks"
#load "scripts/load-references-release.fsx"
#r "bin/release/protoworkspace.dll"

open System
open System.IO
open System.Collections.Generic
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.CodeAnalysis
open System.Xml
open System.Xml.Linq
open ProtoWorkspace
open ProtoWorkspace.ProjectFileInfo
open System.Threading

let internal toFSharpProjectOptions (workspace: 'a when 'a :> Workspace) (projInfo: ProjectInfo): FSharpProjectOptions =
    let projectStore = Dictionary<ProjectId, FSharpProjectOptions>()

    let rec generate (projInfo:ProjectInfo) : FSharpProjectOptions =
        let getProjectRefs (projInfo:ProjectInfo) : (string * FSharpProjectOptions) [] =
            projInfo.ProjectReferences
            |> Seq.choose ^ fun pref -> workspace.CurrentSolution.TryGetProject pref.ProjectId
            |> Seq.map ^ fun proj ->
                let proj = proj.ToProjectInfo()
                if projectStore.ContainsKey proj.Id then (proj.OutputFilePath, projectStore.[proj.Id])
                else
                    let fsinfo = generate proj
                    projectStore.Add (proj.Id, fsinfo)
                    (proj.OutputFilePath, fsinfo)
            |> Array.ofSeq
        {   ProjectFileName = projInfo.FilePath
            ProjectFileNames =
                projInfo.Documents
                |> Seq.map ^ fun doc -> doc.FilePath
                |> Array.ofSeq
            OtherOptions = [||]
            ReferencedProjects = getProjectRefs projInfo
            IsIncompleteTypeCheckEnvironment = false
            UseScriptResolutionRules = false
            LoadTime = System.DateTime.Now
            UnresolvedReferences = None
        }
    generate projInfo

type ProjectInfo with
    member self.ToFSharpProjectOptions(workspace: 'a when 'a :> Workspace): FSharpProjectOptions =
        toFSharpProjectOptions workspace self

type Project with
    member self.ToProjectInfo() =
        ProjectInfo.Create
            (self.Id, self.Version, self.Name, self.AssemblyName, self.Language, self.FilePath,
             outputFilePath = self.OutputFilePath,
             projectReferences = self.ProjectReferences,
             metadataReferences = self.MetadataReferences,
             analyzerReferences = self.AnalyzerReferences,
             documents = (self.Documents |> Seq.map ^ fun doc -> doc.ToDocumentInfo()),
             additionalDocuments = (self.AdditionalDocuments |> Seq.map ^ fun doc -> doc.ToDocumentInfo()),
             compilationOptions = self.CompilationOptions,
             parseOptions = self.ParseOptions,
             isSubmission = self.IsSubmission)
    member self.ToFSharpProjectOptions(workspace: 'a when 'a :> Workspace): FSharpProjectOptions =
        self.ToProjectInfo().ToFSharpProjectOptions workspace


let fswork = new FSharpWorkspace()

let protopath = "ProtoWorkspace.fsproj" |> Path.GetFullPath

let protoinfo = (ProjectFileInfo.create protopath) |> ProjectFileInfo.toProjectInfo fswork

let protoproj = fswork.AddProject protoinfo
;;
let protoOpts = protoinfo.ToFSharpProjectOptions fswork

