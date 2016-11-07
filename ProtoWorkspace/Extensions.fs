[<AutoOpen>]
module ProtoWorkspace.Extensions
open System
open System.Collections.Generic
open Microsoft.Extensions.Logging


type ILogger with
    member self.LogInfofn msg     = Printf.ksprintf (fun s -> self.LogInformation(s, [||])) msg
    member self.LogCriticalfn msg = Printf.ksprintf (fun s -> self.LogCritical(s, [||])) msg
    member self.LogDebugfn msg    = Printf.ksprintf (fun s -> self.LogDebug(s, [||])) msg
    member self.LogTracefn msg    = Printf.ksprintf (fun s -> self.LogTrace(s, [||])) msg
    member self.LogErrorfn msg    = Printf.ksprintf (fun s -> self.LogError(s, [||])) msg
    member self.LogWarningfn msg  = Printf.ksprintf (fun s -> self.LogWarning(s, [||])) msg

open Microsoft.CodeAnalysis

type Workspace with

    member self.ProjectDictionary() =
        let dict = Dictionary<_,_>()
        self.CurrentSolution.Projects
        |> Seq.iter(fun proj -> dict.Add(proj.FilePath,proj.Id))
        dict


    member self.ProjectPaths() =
        self.CurrentSolution.Projects |> Seq.map(fun proj -> proj.FilePath)


    member self.GetProjectIdFromPath path : ProjectId option =
        let dict = self.ProjectDictionary()
        Dict.tryFind path dict


    /// checks the workspace for projects located at the provided paths.
    /// returns a mapping of the projectId and path of projects inside the workspace
    /// and a list of the paths to projects the workspace doesn't include
    member self.GetProjectIdsFromPaths paths =
        let dict = self.ProjectDictionary()
        let pathsInside,pathsOutside = paths |> List.ofSeq |> List.partition (fun path -> dict.ContainsKey path)
        let idmap = pathsInside |> Seq.map (fun path -> dict.[path])
        idmap, pathsOutside


type Solution with

    member self.TryGetProject (projId:ProjectId) =
        if self.ContainsProject projId then Some (self.GetProject projId) else None


open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices


type Document with

    member self.ToDocumentInfo () =
        DocumentInfo.Create
            (   id=self.Id
            ,   name=self.Name
            ,   folders=self.Folders
            ,   sourceCodeKind=self.SourceCodeKind
            ,   loader=FileTextLoader(self.FilePath,Text.Encoding.UTF8)
            ,   filePath=self.FilePath
            )


type TextDocument with

    member self.ToDocumentInfo () =
        DocumentInfo.Create
            (   id=self.Id
            ,   name=self.Name
            ,   folders=self.Folders
            ,   loader=FileTextLoader(self.FilePath,Text.Encoding.UTF8)
            ,   filePath=self.FilePath
            )


type Project with

    member self.ToProjectInfo () =
        ProjectInfo.Create
            (   self.Id
            ,   self.Version
            ,   self.Name
            ,   self.AssemblyName
            ,   self.Language
            ,   self.FilePath
            ,   outputFilePath=self.OutputFilePath
            ,   projectReferences= self.ProjectReferences
            ,   metadataReferences=self.MetadataReferences
            ,   analyzerReferences=self.AnalyzerReferences
            ,   documents = (self.Documents |> Seq.map(fun doc -> doc.ToDocumentInfo()))
            ,   additionalDocuments= (self.AdditionalDocuments |> Seq.map(fun doc -> doc.ToDocumentInfo()))
            ,   compilationOptions=self.CompilationOptions
            ,   parseOptions=self.ParseOptions
            ,   isSubmission=self.IsSubmission
            )


let internal toFSharpProjectOptions (workspace:'a :> Workspace) (projInfo:ProjectInfo): FSharpProjectOptions =
    let projectStore = Dictionary<ProjectId,FSharpProjectOptions>()

    let rec generate (projInfo:ProjectInfo) : FSharpProjectOptions =
        let getProjectRefs (projInfo:ProjectInfo): (string * FSharpProjectOptions)[] =
            projInfo.ProjectReferences
            |> Seq.choose (fun pref -> workspace.CurrentSolution.TryGetProject pref.ProjectId)
            |> Seq.map(fun proj ->
                let proj = proj.ToProjectInfo()
                if projectStore.ContainsKey(proj.Id) then
                    (proj.OutputFilePath, projectStore.[proj.Id])
                else
                    let fsinfo = generate proj
                    projectStore.Add(proj.Id,fsinfo)
                    (proj.OutputFilePath, fsinfo)
            )|> Array.ofSeq

        {   ProjectFileName = projInfo.FilePath
            ProjectFileNames = projInfo.Documents |> Seq.map(fun doc -> doc.FilePath) |> Array.ofSeq
            OtherOptions = [||]
            ReferencedProjects =  getProjectRefs projInfo
            IsIncompleteTypeCheckEnvironment = false
            UseScriptResolutionRules = false
            LoadTime = System.DateTime.Now
            UnresolvedReferences = None
        }
    let fsprojOptions = generate projInfo
    projectStore.Clear()
    fsprojOptions



type ProjectInfo with

    member self.ToFSharpProjectOptions (workspace:'a :> Workspace) : FSharpProjectOptions =
        toFSharpProjectOptions workspace self


type Project with

    member self.ToFSharpProjectOptions (workspace:'a :> Workspace) : FSharpProjectOptions =
        self.ToProjectInfo().ToFSharpProjectOptions workspace


open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.FSharp.Compiler.Range


