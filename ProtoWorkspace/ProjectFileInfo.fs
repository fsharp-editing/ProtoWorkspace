namespace ProtoWorkspace

open System
open System.IO
open System.Collections.Generic
open Microsoft.CodeAnalysis
open System.Runtime.Versioning
open Microsoft.Extensions.Logging
open Microsoft.Build
open Microsoft.Build.Evaluation
open Microsoft.Build.Execution
open System.Xml
open System.Xml.Linq

/// Specifies the version of the F# compiler that should be used
type LanguageVersion =
    | FSharp2
    | FSharp3
    | FSharp4

type PlatformType =
    | X86
    | X64
    | AnyCPU

    override self.ToString() =
        self |> function
        | X86 -> Constants.X86
        | X64 -> Constants.X64
        | AnyCPU -> Constants.AnyCPU

    static member Parse text =
        text |> function
        | EqualsIC Constants.X86 -> X86
        | EqualsIC Constants.X64 -> X64
        | EqualsIC "Any CPU" | EqualsIC Constants.AnyCPU -> AnyCPU
        | _ -> failwithf "Could not parse '%s' into a `PlatformType`" text

    static member TryParse text =
        text |> function
        | EqualsIC Constants.X86 -> Some X86
        | EqualsIC Constants.X64 -> Some X64
        | EqualsIC "Any CPU" | EqualsIC Constants.AnyCPU -> Some AnyCPU
        | _ -> Option.None

/// Determines the output of compiling the F# Project
type OutputType =
    ///  An .exe with an entry point and a console.
    | Exe
    ///   An .exe with an entry point but no console.
    | Winexe
    /// a dynamically linked library (.dll)
    | Library
    /// Build a module that can be added to another assembly (.netmodule)
    | Module

    override self.ToString() = self |> function
        | Exe     -> Constants.Exe
        | Winexe  -> Constants.Winexe
        | Library -> Constants.Library
        | Module  -> Constants.Module

    static member Parse text = text |> function
        | EqualsIC Constants.Exe     -> Exe
        | EqualsIC Constants.Winexe  -> Winexe
        | EqualsIC Constants.Library -> Library
        | EqualsIC Constants.Module  -> Module
        | _ -> failwithf "Could not parse '%s' into a `OutputType`" text

    static member TryParse text = text |> function
        | EqualsIC Constants.Exe     -> Some Exe
        | EqualsIC Constants.Winexe  -> Some Winexe
        | EqualsIC Constants.Library -> Some Library
        | EqualsIC Constants.Module  -> Some Module
        | _ -> None

// TODO - add another field to store `AdditionalDocuments` for use during ProjectInfo creation
type ProjectFileInfo = {
    ProjectId                 : ProjectId
    ProjectGuid               : Guid option
    Name                      : string option
    ProjectFilePath           : string
    TargetFramework           : FrameworkName option
    AssemblyName              : string
    TargetPath                : string
    OutputType                : OutputType
    SignAssembly              : bool
    AssemblyOriginatorKeyFile : string option
    GenerateXmlDocumentation  : string option
    PreprocessorSymbolNames   : string []
    SourceFiles               : string []
    ScriptFiles               : string []
    OtherFiles                : string []
    References                : string []
    /// Collection of paths fsproj files for the project references
    ProjectReferences         : string []
//    ProjectReferences         : ProjectFileInfo ResizeArray
    Analyzers                 : string []
} with
    (* Unsure how to convert these
    public LanguageVersion SpecifiedLanguageVersion { get; }
*)
    member self.ProjectDirectory = Path.GetDirectoryName self.ProjectFilePath
(* From Partial Clases

    public partial class ProjectFileInfo
    {
        private static class ItemNames
        {
            public const string Analyzer = nameof(Analyzer);
            public const string Compile = nameof(Compile);
            public const string ProjectReference = nameof(ProjectReference);
            public const string ReferencePath = nameof(ReferencePath);
        }
    }

    public partial class ProjectFileInfo
    {
        private static class MetadataNames
        {
            public const string FullPath = nameof(FullPath);
            public const string Project = nameof(Project);
            public const string ReferenceSourceTarget = nameof(ReferenceSourceTarget);
        }
    }

    public partial class ProjectFileInfo
    {
        private static class PropertyNames
        {
            AllowUnsafeBlocks             = nameof(AllowUnsafeBlocks);
            AssemblyName                  = nameof(AssemblyName);
            AssemblyOriginatorKeyFile     = nameof(AssemblyOriginatorKeyFile);
            BuildProjectReferences        = nameof(BuildProjectReferences);
            DefineConstants               = nameof(DefineConstants);
            DesignTimeBuild               = nameof(DesignTimeBuild);
            DocumentationFile             = nameof(DocumentationFile);
            LangVersion                   = nameof(LangVersion);
            OutputType                    = nameof(OutputType);
            MSBuildExtensionsPath         = nameof(MSBuildExtensionsPath);
            ProjectGuid                   = nameof(ProjectGuid);
            ProjectName                   = nameof(ProjectName);
            _ResolveReferenceDependencies = nameof(_ResolveReferenceDependencies);
            SignAssembly                  = nameof(SignAssembly);
            SolutionDir                   = nameof(SolutionDir);
            TargetFrameworkMoniker        = nameof(TargetFrameworkMoniker);
            TargetPath                    = nameof(TargetPath);
            VisualStudioVersion           = nameof(VisualStudioVersion);
        }
    }

    public partial class ProjectFileInfo
    {
        private static class TargetNames
        {
            public const string ResolveReferences = nameof(ResolveReferences);
        }
    }

}

|| NOTE || - Omnisharp's 'ProjectFileInfo_Mono.cs' for Mono may need to be ported too

*)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ProjectFileInfo =
    open Microsoft.CodeAnalysis.Diagnostics
    open Microsoft.CodeAnalysis.CodeActions
    open ProtoWorkspace.Loaders

    let getFullPath (projectItem : ProjectItemInstance) = projectItem.GetMetadataValue MetadataName.FullPath
    let isProjectReference (projectItem : ProjectItemInstance) : bool =
        projectItem.GetMetadataValue(MetadataName.ReferenceSourceTarget)
                   .Equals(ItemName.ProjectReference, StringComparison.OrdinalIgnoreCase)

//    // TODO - Rewrite so it can work with all args besides `projectFilePath` as options
//    let evaluate (projectFilePath : string) (solutionDirectory : string) (logger : ILogger) (options : MSBuildOptions)
//        (diagnostics : MSBuildDiagnosticsMessage ICollection) =
//        if not (File.Exists projectFilePath) then failwithf "No project file found at '%s'" projectFilePath
//
//        let checkProp prop value =
//            if not ^ String.IsNullOrWhiteSpace value then [prop,value] else []
//
//        let globalProperties =
//            dict [
//                yield Property.DesignTimeBuild, "true"
//                yield Property.BuildProjectReferences, "false"
//                yield Property.ResolveReferenceDependencies, "true"
//                yield Property.SolutionDir, solutionDirectory + string Path.DirectorySeparatorChar
//                yield! checkProp Property.MSBuildExtensionsPath options.MSBuildExtensionsPath
//                yield! checkProp Property.VisualStudioVersion options.VisualStudioVersion
//            ]
//
//        let collection = new ProjectCollection(globalProperties)
//        logger.LogInfofn "Using toolset %s for '%s'" (options.ToolsVersion ?|? collection.DefaultToolsVersion)
//            projectFilePath
//        let project =
//            if String.IsNullOrEmpty options.ToolsVersion then collection.LoadProject projectFilePath
//            else collection.LoadProject(projectFilePath, options.ToolsVersion)
//
//        let projectInstance = project.CreateProjectInstance()
//
//        let buildResult : bool =
//            let loggers = [ MSBuildLogForwarder(logger, diagnostics) :> Microsoft.Build.Framework.ILogger ] :> seq<_>
//            projectInstance.Build (TargetName.ResolveReferences, loggers)
//
//        // if not buildResult then null else
//        let sourceFiles =
//            projectInstance.GetItems ItemName.Compile
//            |> Seq.map getFullPath
//            |> Array.ofSeq
//
//        let references =
//            projectInstance.GetItems ItemName.ReferencePath
//            |> Seq.filter isProjectReference
//            |> Seq.map getFullPath
//            |> Array.ofSeq
//
//        let projectReferences =
//            projectInstance.GetItems ItemName.ProjectReference
//            |> Seq.filter isProjectReference
//            |> Seq.map getFullPath
//            |> Array.ofSeq
//
//        let analyzers =
//            projectInstance.GetItems ItemName.Analyzer
//            |> Seq.map getFullPath
//            |> Array.ofSeq
//
//        {   ProjectFilePath = projectFilePath
//            ProjectId = ProjectId.CreateNewId()
//            ProjectGuid = projectInstance.GetPropertyValue Property.ProjectGuid |> PropertyConverter.toGuid
//            Name = projectInstance.TryGetPropertyValue Property.ProjectName
//            TargetFramework = projectInstance.TryGetPropertyValue Property.TargetFrameworkMoniker |> Option.map FrameworkName
//            AssemblyName = projectInstance.GetPropertyValue Property.AssemblyName
//            TargetPath = projectInstance.GetPropertyValue Property.TargetPath
//            OutputType = OutputType.Parse <| projectInstance.GetPropertyValue Property.OutputType
//            SignAssembly = PropertyConverter.toBoolean <| projectInstance.GetPropertyValue Property.SignAssembly
//            AssemblyOriginatorKeyFile = projectInstance.TryGetPropertyValue Property.AssemblyOriginatorKeyFile
//            GenerateXmlDocumentation = projectInstance.TryGetPropertyValue Property.DocumentationFile
//            PreprocessorySymbolNames =
//                projectInstance.GetPropertyValue Property.DefineConstants
//                |> PropertyConverter.toDefineConstants
//                |> Array.ofSeq
//            SourceFiles = sourceFiles
//            References = references
//            ProjectReferences = projectReferences
//            Analyzers = analyzers
//        }

    let create (projectFilePath:string) =
        if not (File.Exists projectFilePath) then failwithf "No project file found at '%s'" projectFilePath else

        let manager = BuildManager.DefaultBuildManager

        let buildParam = BuildParameters(DetailedSummary=true)
        let project = Project projectFilePath
        let projectInstance = project.CreateProjectInstance()


        let requestReferences =
            BuildRequestData (projectInstance,
                [|  "ResolveAssemblyReferences"
                    "ResolveProjectReferences"
                    "ResolveReferenceDependencies"
                |])

        let fromBuildRes (targetName:string) (result:BuildResult) =
            if not ^ result.ResultsByTarget.ContainsKey targetName then [||] else
            result.ResultsByTarget.[targetName].Items

        let result = manager.Build(buildParam,requestReferences)

        let getItemPaths itemName =
            projectInstance.GetItems itemName |> Seq.map getFullPath

        let filterItemPaths predicate itemName =
            projectInstance.GetItems itemName
            |> Seq.filter predicate
            |> Seq.map getFullPath

        let isScriptFile path =
            String.equalsIC (path |> Path.GetExtension) ".fsx"

        let sourceFiles = getItemPaths ItemName.Compile

        let otherFiles  =
            filterItemPaths (fun x -> not ^ isScriptFile x.EvaluatedInclude) ItemName.None

        let scriptFiles  =
            filterItemPaths (fun x -> isScriptFile x.EvaluatedInclude) ItemName.None

        let references =
            projectInstance.GetItems ItemName.ReferencePath
            |> Seq.filter (not<<isProjectReference)
            |> Seq.map getFullPath

        let projectReferences =
            projectInstance.GetItems ItemName.ProjectReference
            |> Seq.filter isProjectReference
            |> Seq.map getFullPath

        let analyzers = getItemPaths ItemName.Analyzer

        let projectGuid =
            projectInstance.TryGetPropertyValue Property.ProjectGuid
            |> Option.bind PropertyConverter.toGuid

        let projectId =
            defaultArg  (projectGuid |> Option.map ^ fun x -> ProjectId.CreateFromSerialized x)
                        (ProjectId.CreateNewId())

        let defineConstants =
            projectInstance.GetPropertyValue Property.DefineConstants
            |> PropertyConverter.toDefineConstants


        let projectName = projectInstance.TryGetPropertyValue Property.ProjectName
        let assemblyName = projectInstance.GetPropertyValue Property.AssemblyName
        let targetPath = projectInstance.GetPropertyValue Property.TargetPath
        let targetFramework = projectInstance.TryGetPropertyValue Property.TargetFrameworkMoniker |> Option.map FrameworkName
        let assemblyKeyFile = projectInstance.TryGetPropertyValue Property.AssemblyOriginatorKeyFile
        let signAssembly = PropertyConverter.toBoolean <| projectInstance.GetPropertyValue Property.SignAssembly
        let outputType = OutputType.Parse <| projectInstance.GetPropertyValue Property.OutputType
        let xmlDocs = projectInstance.TryGetPropertyValue Property.DocumentationFile

        {   ProjectFilePath           = projectFilePath
            ProjectId                 = projectId
            ProjectGuid               = projectGuid
            Name                      = projectName
            TargetFramework           = targetFramework
            AssemblyName              = assemblyName
            TargetPath                = targetPath
            OutputType                = outputType
            SignAssembly              = signAssembly
            AssemblyOriginatorKeyFile = assemblyKeyFile
            GenerateXmlDocumentation  = xmlDocs
            PreprocessorSymbolNames   = defineConstants |> Array.ofSeq
            SourceFiles               = sourceFiles |> Array.ofSeq
            ScriptFiles               = scriptFiles |> Array.ofSeq
            OtherFiles                = otherFiles |> Array.ofSeq
            References                = references |> Array.ofSeq
            ProjectReferences         = projectReferences |> Array.ofSeq
            Analyzers                 = analyzers |> Array.ofSeq
        }


    let private createSrcDocs directory projectId filePaths srcCodeKind =
        filePaths |> Seq.map ^ fun path ->
            let fullpath = Path.Combine(directory,path)
            DocumentInfo.Create
                (   DocumentId.CreateNewId projectId
                ,   Path.GetFileNameWithoutExtension path
                ,   sourceCodeKind = srcCodeKind
                ,   filePath = fullpath
                ,   loader = FileTextLoader(fullpath,Text.Encoding.UTF8)
                ,   isGenerated = false
                )

    let createSrcDocInfos (projectFileInfo:ProjectFileInfo) =
        createSrcDocs  projectFileInfo.ProjectDirectory
                    projectFileInfo.ProjectId
                    projectFileInfo.SourceFiles
                    SourceCodeKind.Regular

    let createScriptDocInfos (projectFileInfo:ProjectFileInfo) =
        createSrcDocs projectFileInfo.ProjectDirectory
                    projectFileInfo.ProjectId
                    projectFileInfo.ScriptFiles
                    SourceCodeKind.Script

    let createOtherDocInfos (projectFileInfo:ProjectFileInfo) =
        projectFileInfo.OtherFiles |> Seq.map ^ fun path ->
            let fullpath = Path.Combine(projectFileInfo.ProjectDirectory,path)
            DocumentInfo.Create
                (   DocumentId.CreateNewId projectFileInfo.ProjectId
                ,   Path.GetFileNameWithoutExtension path
                ,   filePath = fullpath
                ,   loader = FileTextLoader(fullpath,Text.Encoding.UTF8)
                ,   isGenerated = false
                )

    let createAdditionalDocuments projectFileInfo =
        Seq.append  (createScriptDocInfos projectFileInfo)
                    (createOtherDocInfos  projectFileInfo)

    let createAnalyzerReferences (projectFileInfo:ProjectFileInfo) =
        if projectFileInfo.Analyzers.Length = 0 then Seq.empty else
        projectFileInfo.Analyzers |> Seq.map ^ fun path ->
            AnalyzerFileReference(path,AnalyzerAssemblyLoader())
            :> AnalyzerReference


    /// Converts into the Microsoft.CodeAnalysis ProjectInfo used by workspaces
    // TODO -
    //  change the internals to a recusive generation of projectInfo for all project references
    //  without creating duplicate projects
    let toProjectInfo (workspace:'a when 'a :> Workspace) (projectFileInfo : ProjectFileInfo) =

        let projectRefs =
                let projIds, paths = (workspace :> Workspace).GetProjectIdsFromPaths projectFileInfo.ProjectReferences
                // TODO - this is a temporary impl, projectInfos need to be generated for the paths to projects
                // that aren't contained in the workspace
                Seq.append
                    [ for projId in projIds -> ProjectReference projId ]
                    [ for path in paths -> ProjectReference ^ ProjectId.CreateNewId() ]

        let projDict = workspace.ProjectDictionary()

        ProjectInfo.Create
            (   id                  = projectFileInfo.ProjectId
            ,   version             = VersionStamp.Create()
            ,   name                = defaultArg projectFileInfo.Name String.Empty
            ,   assemblyName        = projectFileInfo.AssemblyName
            ,   language            = "FSharp"
            ,   filePath            = projectFileInfo.ProjectFilePath
            ,   outputFilePath      = projectFileInfo.TargetPath
            (*  - TODO -
                Correctly adding projectreferences is going to be an issue
                ProjectReference is created using a project id, which means a collection of
                projectFileInfos should be passed to this function to prevent the creation
                of duplicate projectfile infos for referenced projects that have different ids
            *)
            ,   projectReferences   = projectRefs
            ,   metadataReferences  = seq[]
            ,   analyzerReferences  = createAnalyzerReferences projectFileInfo
            ,   documents           = createSrcDocInfos projectFileInfo
            ,   additionalDocuments = createAdditionalDocuments projectFileInfo
            //,   compilationOptions=
            //,   parseOptions=
            //,   isSubmission=
            //,   hostObjectType=
            )


    open Microsoft.FSharp.Compiler.SourceCodeServices

    let toFSharpProjectOptions (workspace:'a when 'a :> Workspace) (projectFileInfo:ProjectFileInfo) : FSharpProjectOptions =
        (projectFileInfo |> toProjectInfo workspace).ToFSharpProjectOptions workspace







//  with
//    static member Create (projectInfo:Proje)
