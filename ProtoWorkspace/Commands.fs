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


type RequestHandler<'Request,'Response> =
    inherit IRequestHandler
    abstract member Handle: request:'Request -> 'Response Async


type IAggregateResponse =
    abstract member Merge : response: IAggregateResponse -> IAggregateResponse


type QuickFixResponse () =
    member val QuickFixes : QuickFix seq = Seq.empty with get, set

    new (quickFixes:QuickFix seq) as self =
        QuickFixResponse() then
        self.QuickFixes <- quickFixes

    member self.Merge (response:QuickFixResponse) =
        Seq.append self.QuickFixes response.QuickFixes
        |> QuickFixResponse

    interface IAggregateResponse with
        member self.Merge response =
            self.Merge (response :?> QuickFixResponse)
            :> IAggregateResponse



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


// Buffer

[<EditorCommand(Command.UpdateBuffer, typeof<UpdateBufferRequest>,typeof<obj>)>]
type UpdateBufferRequest () =
    inherit Request()
    /// Instead of updating the buffer from the editor,
    /// set this to allow updating from disk
    member val FromDisk = false with get, set

[<EditorCommand(Command.ChangeBuffer, typeof<ChangeBufferRequest>,typeof<obj>)>]
type ChangeBufferRequest () =
    interface IRequest

    member val NewText  : string  = "" with get, set
    member val FileName : string  = "" with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val StartLine   : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val StartColumn : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndLine     : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndColumn   : int     = 0  with get, set


// Signature Help

[<EditorCommand(Command.SignatureHelp, typeof<SignatureHelpRequest>,typeof<SignatureHelp>)>]
type SignatureHelpRequest () = inherit Request()


// Fix Usings

type FixUsingsResponse () =
    member val Buffer = "" with get, set
    member val AmbiguousResults : QuickFix seq = Seq.empty with get, set
    member val Changes : LinePositionSpanTextChange seq = Seq.empty with get, set


[<EditorCommand(Command.FixUsings, typeof<FixUsingsRequest>,typeof<FixUsingsResponse>)>]
type FixUsingsRequest () =
    inherit Request()
    member val WantsTextChanges = false with get, set
    member val ApplyTextChanges = true with get, set



// AutoComplete

type AutoCompleteResponse () =
    /// The text to be "completed", that is, the text that will be inserted in the
    member val CompletionText = "" with get, set
    member val Description = "" with get, set
    /// The text that should be displayed in the auto-complete UI.
    member val DisplayText = "" with get, set
    member val RequiredNamespaceImport = "" with get, set
    member val MethodHandler = "" with get, set
    member val ReturnType = "" with get, set
    member val Snippet = "" with get, set
    member val Kind = "" with get, set

    override self.Equals (other:obj) =
        if isNull other then false else
        match other with
        | :? AutoCompleteResponse as other ->
            self.DisplayText = other.DisplayText
            && self.Snippet = other.Snippet
        | _ -> false

    override self.GetHashCode() =
        17 * hash self.DisplayText
        + (if isNotNull self.Snippet then 31 * hash self.Snippet else 0)


[<EditorCommand(Command.AutoComplete, typeof<AutoCompleteRequest>,typeof<AutoCompleteResponse>)>]
type AutoCompleteRequest () =
    inherit Request()
    let mutable wordToComplete = ""

    member __.WordToComplete
        with get () = wordToComplete ?|? ""
        and  set v  = wordToComplete <- v

    ///   Specifies whether to return the code documentation for
    ///   each and every returned autocomplete result.
    member val WantDocumentationForEveryCompletionResult = false with get, set

    ///   Specifies whether to return importable types. Defaults to
    ///   false. Can be turned off to get a small speed boost.
    member val WantImportableTypes = false with get, set

    /// Returns a 'method header' for working with parameter templating.
    member val WantMethodHeader = false with get, set

    /// Returns a snippet that can be used by common snippet libraries
    /// to provide parameter and type parameter placeholders
    member val WantSnippet = false with get, set

    /// Returns the return type
    member val WantReturnType = false with get, set

    /// Returns the kind (i.e Method, Property, Field)
    member val WantKind = false with get, set

