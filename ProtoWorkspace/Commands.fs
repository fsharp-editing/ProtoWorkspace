namespace ProtoWorkspace

open System
open System.IO
open System.Composition
open Microsoft.CodeAnalysis
open Newtonsoft.Json
open ProtoWorkspace.Text

[<RequireQualifiedAccess>]
/// Contains the literals for the strings representing Editing Command Endpoints
module Command =
    let [<Literal>] GotoDefinition       = "/gotodefinition"
    let [<Literal>] FindSymbols          = "/findsymbols"
    let [<Literal>] UpdateBuffer         = "/updatebuffer"
    let [<Literal>] ChangeBuffer         = "/changebuffer"
    let [<Literal>] CodeCheck            = "/codecheck"
    let [<Literal>] FilesChanged         = "/filesChanged"
    let [<Literal>] FormatAfterKeystroke = "/formatAfterKeystroke"
    let [<Literal>] FormatRange          = "/formatRange"
    let [<Literal>] CodeFormat           = "/codeformat"
    let [<Literal>] Highlight            = "/highlight"
    let [<Literal>] AutoComplete         = "/autocomplete"
    let [<Literal>] FindImplementations  = "/findimplementations"
    let [<Literal>] FindUsages           = "/findusages"
    let [<Literal>] GotoFile             = "/gotofile"
    let [<Literal>] GotoRegion           = "/gotoregion"
    let [<Literal>] NavigateUp           = "/navigateup"
    let [<Literal>] NavigateDown         = "/navigatedown"
    let [<Literal>] TypeLookup           = "/typelookup"
    let [<Literal>] GetCodeAction        = "/getcodeactions"
    let [<Literal>] RunCodeAction        = "/runcodeaction"
    let [<Literal>] Rename               = "/rename"
    let [<Literal>] SignatureHelp        = "/signatureHelp"
    let [<Literal>] MembersTree          = "/currentfilemembersastree"
    let [<Literal>] MembersFlat          = "/currentfilemembersasflat"
    let [<Literal>] TestCommand          = "/gettestcontext"
    let [<Literal>] Metadata             = "/metadata"
    let [<Literal>] PackageSource        = "/packagesource"
    let [<Literal>] PackageSearch        = "/packagesearch"
    let [<Literal>] PackageVersion       = "/packageversion"
    let [<Literal>] WorkspaceInformation = "/projects"
    let [<Literal>] ProjectInformation   = "/project"
    let [<Literal>] FixUsings            = "/fixusings"
    let [<Literal>] CheckAliveStatus     = "/checkalivestatus"
    let [<Literal>] CheckReadyStatus     = "/checkreadystatus"
    let [<Literal>] StopServer           = "/stopserver"
    let [<Literal>] Open                 = "/open"
    let [<Literal>] Close                = "/close"
    let [<Literal>] Diagnostics          = "/diagnostics"


type IRequest = interface end
type IRequestHandler = interface end


[<MetadataAttribute>]
/// MEF Exports an IRequestHandler
type CommandHandlerAttribute(commandName:string, language:string) =
    inherit ExportAttribute(typeof<IRequestHandler>)
    member __.CommandName = commandName
    member __.Language = language


type CommandDescriptor<'Request,'Response> (commandName:string) =
    member val RequestType  = typeof<'Request> with get
    member val ResponseType = typeof<'Response> with get
    member val CommandName  = commandName with get


[<MetadataAttribute>]
/// MEF Exports an IRequest
type EditorCommandAttribute (commandName:string, request:Type,response:Type) =
    inherit ExportAttribute(typeof<IRequest>)
    member val RequestType  = request  with get
    member val ResponseType = response with get
    member val CommandName  = commandName with get


type RequestHandler<'Request,'Response> =
    inherit IRequestHandler
    abstract member Handle: request:'Request -> 'Response Async


type IAggregateResponse =
    abstract member Merge : response:IAggregateResponse -> IAggregateResponse


type Request () =
    let mutable fileName = ""

    interface IRequest

    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val Line : int = 0 with get, set

    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val Column  : int = 0 with get, set
    member val Buffer  : string = "" with get, set
    member val Changes : LinePositionSpanTextChange seq = Seq.empty with get, set

    member __.FileName
        with get () =
            fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
        and set v = if isNull v then fileName <- String.Empty else fileName <- v


type ModifiedFileResponse (fileName:string) =
    member val FileName = fileName with get, set
    member val Buffer = "" with get, set
    member val Changes : LinePositionSpanTextChange seq = Seq.empty with get, set
    new () = ModifiedFileResponse String.Empty


// File Open

type FileOpenResponse () =
    interface IAggregateResponse with
        member __.Merge response = response

[<EditorCommand(Command.Open, typeof<FileOpenRequest>,typeof<FileOpenResponse>)>]
type FileOpenRequest() =
    inherit Request()

// File Close

type FileCloseResponse () =
    interface IAggregateResponse with
        member __.Merge response = response

[<EditorCommand(Command.Close, typeof<FileCloseRequest>,typeof<FileCloseResponse>)>]
type FileCloseRequest() =
    inherit Request()


// Rename

type RenameResponse () =
    member val Changes : ModifiedFileResponse seq = Seq.empty with get, set
    member val ErrorMessage = "" with get, set

[<EditorCommand(Command.Rename, typeof<RenameRequest>,typeof<RenameResponse>)>]
type RenameRequest () =
    inherit Request ()
    ///  When true, return just the text changes.
    member val WantsTextChanges = false with get, set
    ///  When true, apply changes immediately on the server.
    member val ApplyTextChanges = true with get, set
    member val RenameTo = "" with get, set




