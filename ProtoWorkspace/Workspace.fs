namespace ProtoWorkspace

open System
open System.Linq
open System.Threading
open System.Composition
open System.IO
open System.Collections.Generic
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.Host
open Microsoft.CodeAnalysis.Host.Mef
open ProtoWorkspace.HostServices
open FSharpVSPowerTools
open ProtoWorkspace.Text
open FSharp.Control
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices



[<Export; Shared>]
type FSharpWorkspace (hostServices : HostServices) as self =
    inherit Workspace(hostServices,Constants.FSharpLanguageName )
    let bufferManager = new BufferManager(self)
    let disposables = ResizeArray<IDisposable>()
    let checker = FSharpChecker.Create()

    let checkProjectId (projectId:ProjectId) =
        match self.CurrentSolution.ContainsProject projectId with
        | false -> failwithf "The workspace does not contain a project with the id '%s'" ^ string projectId.Id
        | true -> ()

    let projectOptions = Dictionary<ProjectId,FSharpProjectOptions>()

    // TODO -
    //  What additional data caches does the workspace need?
    //  * FSharpProjectOptions dictionary that updates on projectInfo change events?
    //  *

    do
        disposables.Add bufferManager

    new (aggregator:HostServicesAggregator) =
        new FSharpWorkspace (aggregator.CreateHostServices())

    new () =
        new FSharpWorkspace(FSharpHostService())



    member __.Checker : FSharpChecker = checker

    //let activeDocuments = HashSet<DocumentId>()
    //new() = new FSharpWorkspace(HostServicesAggregator(Seq.empty))
    override __.CanOpenDocuments = true
    override __.CanApplyChange _ = true

    /// Adds a document to the workspace.
    member self.AddDocument (documentInfo:DocumentInfo) =
        checkNullArg documentInfo "documentInfo"
        base.OnDocumentAdded documentInfo
        self.CurrentSolution.GetDocument documentInfo.Id

    /// Adds a document to a project in the workspace.
    member __.AddDocument (projectId:ProjectId, name:string, text:SourceText) =
        checkNullArg projectId "projectId"
        checkNullArg name "name"
        checkNullArg text "text"
        DocumentInfo.Create(
            DocumentId.CreateNewId projectId, name,
            loader = TextLoader.From ^ TextAndVersion.Create(text, VersionStamp.Create())
        )


    /// Puts the specified document into the open state.
    override __.OpenDocument (docId, activate) =
        let doc = base.CurrentSolution.GetDocument docId
        if isNull doc then ()
        else
            let task = doc.GetTextAsync CancellationToken.None
            task.Wait CancellationToken.None
            let text = task.Result
            base.OnDocumentOpened(docId, text.Container, activate)


    /// Puts the specified document into the closed state.
    override __.CloseDocument docId =
        let doc = base.CurrentSolution.GetDocument docId
        if isNull doc then ()
        else
            let task = doc.GetTextAsync CancellationToken.None
            task.Wait CancellationToken.None
            let text = task.Result
            let versionTask = doc.GetTextVersionAsync CancellationToken.None
            versionTask.Wait CancellationToken.None
            let version = versionTask.Result
            let loader = TextLoader.From ^ TextAndVersion.Create(text, version, doc.FilePath)
            base.OnDocumentClosed(docId, loader)


    /// Adds a project to the workspace. All previous projects remain intact.
    member self.AddProject projectInfo =
        checkNullArg projectInfo "projectInfo"
        base.OnProjectAdded projectInfo
        base.UpdateReferencesAfterAdd()
        self.CurrentSolution.GetProject projectInfo.Id

//    /// Adds a project to the workspace. All previous projects remain intact.
//    member self.AddProject(name : string) =
//        ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), name, name, "FSharp")
//        |> self.AddProject

    /// Adds multiple projects to the workspace at once. All existing projects remain intact.
    member self.AddProjects (projectInfos:seq<_>) =
        checkNullArg projectInfos "projectInfos"
        for info in projectInfos do
            self.OnProjectAdded info
        base.UpdateReferencesAfterAdd()


    /// Adds an entire solution to the workspace, replacing any existing solution.
    member self.AddSolutionInfo (solutionInfo:SolutionInfo) =
        checkNullArg solutionInfo "solutionInfo"
        base.OnSolutionAdded solutionInfo
        base.UpdateReferencesAfterAdd()
        self.CurrentSolution


//    /// Adds an entire solution to the workspace, replacing any existing solution.
//    member self.AddSolution (solution:Solution) =
//        checkNullArg solution "solution"
//        let fn (arg:WorkspaceChangeEventArgs) =
//            arg.
//        base.OnSolutionAdded solution
//        base.UpdateReferencesAfterAdd()
//        self.CurrentSolution



    member __.AddProjectReference (projectId, projectReference) =
        base.OnProjectReferenceAdded (projectId, projectReference)


    member __.AddMetadataReference (projectId, metadataReference) =
        base.OnMetadataReferenceAdded (projectId, metadataReference)


    member __.RemoveMetadataReference (projectId, metadataReference) =
        base.OnMetadataReferenceRemoved (projectId, metadataReference)

    //    member __. AddDocument documentInfo =
    //        base.OnDocumentAdded documentInfo
    member __.RemoveDocument documentId = base.OnDocumentRemoved documentId

    member __.RemoveProject projectId = base.OnProjectRemoved projectId

    member __.SetCompilationOptions (projectId, options) = base.OnCompilationOptionsChanged(projectId, options)

    member __.SetParseOptions (projectId, parseOptions) = base.OnParseOptionsChanged(projectId, parseOptions)

    member __.OnDocumentChanged (documentId, text) =
        base.OnDocumentTextChanged(documentId, text, PreservationMode.PreserveIdentity)


    member __.TryGetDocumentId filePath =
        let documentIds = base.CurrentSolution.GetDocumentIdsWithFilePath filePath
        match documentIds.FirstOrDefault() with
        | null -> None
        | docId -> Some docId


    member self.GetDocuments filePath =
        base.CurrentSolution.GetDocumentIdsWithFilePath(filePath)
            .Select(fun docId -> self.CurrentSolution.GetDocument docId)


    member self.TryGetDocument filePath =
        self.TryGetDocumentId filePath |> Option.map (fun docId -> self.CurrentSolution.GetDocument docId)

    interface IDisposable with
        member __.Dispose() =
            disposables |> Seq.iter dispose
            disposables.Clear()

and internal BufferManager (workspace:FSharpWorkspace) as self =
    let transientDocuments = Dictionary<string, DocumentId list>(StringComparer.OrdinalIgnoreCase)
    let transientDocumentIds = HashSet<DocumentId>()
    let lockObj = obj()
    let workspaceEvent = workspace.WorkspaceChanged
    let subscriptions = ResizeArray<IDisposable>()
    do
        workspaceEvent.Subscribe self.OnWorkspaceChanged |> subscriptions.Add

    let tryAddTransientDocument (fileName : string) (fileContent : string) =
        if String.IsNullOrWhiteSpace fileName then false
        else
        let projects = workspace.CurrentSolution.Projects
        if projects.Count() = 0 then false
        else
        let sourceText = SourceText.From fileContent

        let documents : DocumentInfo list =
            (projects, []) ||> Seq.foldBack ^ fun project docs ->
                let docId = DocumentId.CreateNewId project.Id
                let version = VersionStamp.Create()
                let docInfo =
                    DocumentInfo.Create
                        (docId, fileName, filePath = fileName,
                        loader = TextLoader.From(TextAndVersion.Create(sourceText, version)))
                docInfo :: docs
        lock lockObj ^ fun () ->
            let docIds = documents |> List.map ^ fun doc -> doc.Id
            transientDocuments.Add(fileName, docIds)
            transientDocumentIds.UnionWith docIds
        documents |> List.iter ^ fun doc -> workspace.AddDocument doc |> ignore
        true


    member __.UpdateBuffer (request:Request) = async {
        let buffer =
            match request with
            | :? UpdateBufferRequest as request ->
                if request.FromDisk then File.ReadAllText request.FileName else request.Buffer
            | _ -> request.Buffer

        let changes = request.Changes
        let documentIds = workspace.CurrentSolution.GetDocumentIdsWithFilePath request.FileName
        if not documentIds.IsEmpty then
            if Seq.isEmpty changes then
                let sourceText = SourceText.From buffer
                documentIds |> Seq.iter ^ fun docId -> workspace.OnDocumentChanged(docId, sourceText)
            else
            for docId in documentIds do
                let doc = workspace.CurrentSolution.GetDocument docId
                let! sourceText = doc.GetTextAsync() |> Async.AwaitTask
                let sourceText =
                    (sourceText, changes) ||> Seq.fold ^ fun sourceText change ->
                        let startOffset =
                            sourceText.Lines.GetPosition ^ LinePosition (change.StartLine, change.StartLine)
                        let endOffset =
                            sourceText.Lines.GetPosition ^ LinePosition (change.EndLine, change.EndColumn)
                        sourceText.WithChanges
                            [| TextChange(TextSpan(startOffset, endOffset - startOffset), change.NewText) |]
                workspace.OnDocumentChanged(docId, sourceText)
        else
        if isNotNull buffer then tryAddTransientDocument request.FileName buffer |> ignore
    }


    member __.UpdateChangeBuffer(request : ChangeBufferRequest) = async {
        if isNull request.FileName then ()
        else
        let documentIds = workspace.CurrentSolution.GetDocumentIdsWithFilePath(request.FileName)
        if not documentIds.IsEmpty then
            for docId in documentIds do
                let doc = workspace.CurrentSolution.GetDocument docId
                let! sourceText = doc.GetTextAsync() |> Async.AwaitTask
                let startOffset =
                    sourceText.Lines.GetPosition ^ LinePosition(request.StartLine, request.StartLine)
                let endOffset = sourceText.Lines.GetPosition ^ LinePosition(request.EndLine, request.EndColumn)
                let sourceText =
                    sourceText.WithChanges
                        [| TextChange(TextSpan(startOffset, endOffset - startOffset), request.NewText) |]
                workspace.OnDocumentChanged(docId, sourceText)
        else
            tryAddTransientDocument request.FileName request.NewText |> ignore
    }

    member __.OnWorkspaceChanged (args:WorkspaceChangeEventArgs) =
        let filename =
            match args.Kind with
            | WorkspaceChangeKind.DocumentAdded   -> (args.NewSolution.GetDocument args.DocumentId).FilePath
            | WorkspaceChangeKind.DocumentRemoved -> (args.OldSolution.GetDocument args.DocumentId).FilePath
            | _ -> String.Empty
        if String.IsNullOrEmpty filename then ()
        else
        lock lockObj ^ fun () ->
            match Dict.tryFind filename transientDocuments with
            | None -> ()
            | Some docIds ->
                transientDocuments.Remove filename |> ignore
                for docId in docIds do
                    workspace.RemoveDocument docId |> ignore
                    transientDocumentIds.Remove docId |> ignore


    member self.FindProjectsByFileName (fileName:string) =
        let dirInfo = (FileInfo fileName).Directory
        let candidates =
            workspace.CurrentSolution.Projects
                .GroupBy(fun project -> (FileInfo project.FilePath).Directory.FullName)
                .ToDictionary((fun grouping -> grouping.Key),( fun grouping -> List.ofSeq grouping))
        let rec loop (dirInfo:DirectoryInfo) =
            if isNull dirInfo then [] else
            match candidates.TryGetValue dirInfo.FullName with
            | true, ps -> ps
            | _, _ -> loop dirInfo.Parent
        loop dirInfo :> _ seq


    interface IDisposable with
        member __.Dispose() =
            subscriptions |> Seq.iter dispose
            subscriptions.Clear()
