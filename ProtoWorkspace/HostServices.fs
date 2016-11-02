module ProtoWorkspace.HostServices



open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Host
open Microsoft.CodeAnalysis.Host.Mef

// based on - https://gist.github.com/praeclarum/953629b2f80860e54747

type FSharpHostLanguageService (workspace:Workspace) =
    inherit HostLanguageServices()

    override __.Language = "FSharp"
    override __.WorkspaceServices with get () = workspace.Services
    override __.GetService<'a when 'a :> ILanguageService>() = 
        Unchecked.defaultof<'a>


type FSharpHostWorkspaceService (workspace:Workspace,baseServices:HostWorkspaceServices) =
    inherit HostWorkspaceServices()

    let languageService = FSharpHostLanguageService workspace

    override __.GetService<'a when 'a :> IWorkspaceService >()  = 
        baseServices.GetService<'a>()

    override __.HostServices with get() = workspace.Services.HostServices

    override __.Workspace = workspace

    override __.IsSupported languageName = languageName = "FSharp"

    override __.SupportedLanguages = seq ["FSharp"]

    override __.GetLanguageServices _ = languageService :> HostLanguageServices

    override __.FindLanguageServices filter  = base.FindLanguageServices filter


type FSharpHostService () =
    inherit HostServices()
    let baseWorkspace = new AdhocWorkspace()

    override __.CreateWorkspaceServices workspace =
        FSharpHostWorkspaceService(workspace,baseWorkspace.Services) :> HostWorkspaceServices

