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
open ProtoWorkspace.MSBuildInfo
open ProtoWorkspace.XLinq

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

    override self.ToString() =
        self |> function
        | Exe -> Constants.Exe
        | Winexe -> Constants.Winexe
        | Library -> Constants.Library
        | Module -> Constants.Module

    static member Parse text =
        text |> function
        | EqualsIC Constants.Exe -> Exe
        | EqualsIC Constants.Winexe -> Winexe
        | EqualsIC Constants.Library -> Library
        | EqualsIC Constants.Module -> Module
        | _ -> failwithf "Could not parse '%s' into a `OutputType`" text

    static member TryParse text =
        text |> function
        | EqualsIC Constants.Exe -> Some Exe
        | EqualsIC Constants.Winexe -> Some Winexe
        | EqualsIC Constants.Library -> Some Library
        | EqualsIC Constants.Module -> Some Module
        | _ -> None

// TODO - add another field to store `AdditionalDocuments` for use during ProjectInfo creation
type ProjectFileInfo = {
    ProjectId                 : ProjectId
    ProjectGuid               : Guid option
    Name                      : string
    ProjectFilePath           : string
    TargetFramework           : FrameworkName Option
    AssemblyName              : string
    TargetPath                : string
    OutputType                : OutputType
    SignAssembly              : bool
    AssemblyOriginatorKeyFile : string
    GenerateXmlDocumentation  : string
    PreprocessorySymbolNames  : string ResizeArray
    SourceFiles               : string ResizeArray
    References                : string ResizeArray
    /// Collection of paths fsproj files for the project references
    ProjectReferences         : string ResizeArray
//    ProjectReferences         : ProjectFileInfo ResizeArray
    Analyzers                 : string ResizeArray
} with
    (* Unsure how to convert these
    public LanguageVersion SpecifiedLanguageVersion { get; }
*)
    member self.ProjectDirectory = Path.GetDirectoryName self.ProjectFilePath

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ProjectFileInfo =
    let getFullPath (projectItem : ProjectItemInstance) = projectItem.GetMetadataValue MetadataNames.FullPath
    let referenceSourceTargetIsProjectReference (projectItem : ProjectItemInstance) : bool =
        projectItem.GetMetadataValue(MetadataNames.ReferenceSourceTarget)
                   .Equals(ItemNames.ProjectReference, StringComparison.OrdinalIgnoreCase)

    // TODO - Rewrite so it can work with all args besides `projectFilePath` as options
    let create (projectFilePath : string) (solutionDirectory : string) (logger : ILogger) (options : MSBuildOptions)
        (diagnostics : MSBuildDiagnosticsMessage ICollection) =
        if not (File.Exists projectFilePath) then failwithf "No project file found at '%s'" projectFilePath
        let globalProperties =
            dict [ PropertyNames.DesignTimeBuild, "true"
                   PropertyNames.BuildProjectReferences, "false"
                   PropertyNames.ResolveReferenceDependencies, "true"
                   PropertyNames.SolutionDir, solutionDirectory + string Path.DirectorySeparatorChar ]
        if not ^ String.IsNullOrWhiteSpace options.MSBuildExtensionsPath then
            globalProperties.Add (PropertyNames.MSBuildExtensionsPath, options.MSBuildExtensionsPath)
        if not ^ String.IsNullOrWhiteSpace options.VisualStudioVersion then
            globalProperties.Add (PropertyNames.VisualStudioVersion, options.VisualStudioVersion)
        let collection = new ProjectCollection(globalProperties)
        logger.LogInfofn "Using toolset %s for '%s'" (options.ToolsVersion ?|? collection.DefaultToolsVersion)
            projectFilePath
        let project =
            if String.IsNullOrEmpty options.ToolsVersion then collection.LoadProject projectFilePath
            else collection.LoadProject(projectFilePath, options.ToolsVersion)

        let projectInstance = project.CreateProjectInstance()

        let buildResult : bool =
            let loggers = [ MSBuildLogForwarder(logger, diagnostics) :> Microsoft.Build.Framework.ILogger ] :> seq<_>
            projectInstance.Build (TargetNames.ResolveReferences, loggers)

        // if not buildResult then null else
        let sourceFiles =
            projectInstance.GetItems ItemNames.Compile
            |> Seq.map getFullPath
            |> ResizeArray

        let references =
            projectInstance.GetItems ItemNames.ReferencePath
            |> Seq.filter referenceSourceTargetIsProjectReference
            |> Seq.map getFullPath
            |> ResizeArray

        let projectReferences =
            projectInstance.GetItems ItemNames.ProjectReference
            |> Seq.filter referenceSourceTargetIsProjectReference
            |> Seq.map getFullPath
            |> ResizeArray

        let analyzers =
            projectInstance.GetItems ItemNames.Analyzer
            |> Seq.map getFullPath
            |> ResizeArray

        {   ProjectFilePath = projectFilePath
            ProjectId = ProjectId.CreateNewId()
            ProjectGuid = projectInstance.GetPropertyValue PropertyNames.ProjectGuid |> PropertyConverter.toGuid |> Some
            Name = projectInstance.GetPropertyValue PropertyNames.ProjectName
            TargetFramework = FrameworkName(projectInstance.GetPropertyValue PropertyNames.TargetFrameworkMoniker)|> Some
            AssemblyName = projectInstance.GetPropertyValue PropertyNames.AssemblyName
            TargetPath = projectInstance.GetPropertyValue PropertyNames.TargetPath
            OutputType = OutputType.Parse <| projectInstance.GetPropertyValue PropertyNames.OutputType
            SignAssembly = PropertyConverter.toBoolean <| projectInstance.GetPropertyValue PropertyNames.SignAssembly
            AssemblyOriginatorKeyFile = projectInstance.GetPropertyValue PropertyNames.AssemblyOriginatorKeyFile
            GenerateXmlDocumentation = projectInstance.GetPropertyValue PropertyNames.DocumentationFile
            PreprocessorySymbolNames =
                projectInstance.GetPropertyValue PropertyNames.DefineConstants
                |> PropertyConverter.toDefineConstants
                |> ResizeArray
            SourceFiles = sourceFiles
            References = references
            ProjectReferences = projectReferences
//            ProjectReferences = ResizeArray()
            Analyzers = analyzers
        }

    let create2 (projectFilePath : string) =
        if not (File.Exists projectFilePath) then failwithf "No project file found at '%s'" projectFilePath
        let globalProperties =
            dict [
                PropertyNames.DesignTimeBuild, "true"
                PropertyNames.BuildProjectReferences, "false"
                PropertyNames.ResolveReferenceDependencies, "true"
            ]
        let collection = new ProjectCollection(globalProperties)
        let project : Project = collection.LoadProject projectFilePath
        project.GlobalProperties.Add("BuildingInsideVisualStudio", "true")

        let projectInstance = project.CreateProjectInstance()

        // if not buildResult then null else
        let sourceFiles =
            projectInstance.GetItems ItemNames.Compile
            |> Seq.map getFullPath
            |> ResizeArray

        let references =
            projectInstance.GetItems ItemNames.ReferencePath
            |> Seq.filter referenceSourceTargetIsProjectReference
            |> Seq.map getFullPath
            |> ResizeArray

        let projectReferences =
            projectInstance.GetItems ItemNames.ProjectReference
            |> Seq.filter referenceSourceTargetIsProjectReference
            |> Seq.map getFullPath
            |> ResizeArray

        let analyzers =
            projectInstance.GetItems ItemNames.Analyzer
            |> Seq.map getFullPath
            |> ResizeArray

        {   ProjectFilePath = projectFilePath
            ProjectId = ProjectId.CreateFromSerialized(PropertyNames.ProjectGuid |> PropertyConverter.toGuid)
            ProjectGuid = projectInstance.GetPropertyValue PropertyNames.ProjectGuid |> PropertyConverter.toGuid |> Some
            Name = projectInstance.GetPropertyValue PropertyNames.ProjectName
            TargetFramework = FrameworkName(projectInstance.GetPropertyValue PropertyNames.TargetFrameworkMoniker) |> Some
            AssemblyName = projectInstance.GetPropertyValue PropertyNames.AssemblyName
            TargetPath = projectInstance.GetPropertyValue PropertyNames.TargetPath
            OutputType = OutputType.Parse <| projectInstance.GetPropertyValue PropertyNames.OutputType
            SignAssembly = PropertyConverter.toBoolean <| projectInstance.GetPropertyValue PropertyNames.SignAssembly
            AssemblyOriginatorKeyFile = projectInstance.GetPropertyValue PropertyNames.AssemblyOriginatorKeyFile
            GenerateXmlDocumentation = projectInstance.GetPropertyValue PropertyNames.DocumentationFile
            PreprocessorySymbolNames =
                projectInstance.GetPropertyValue PropertyNames.DefineConstants
                |> PropertyConverter.toDefineConstants
                |> ResizeArray
            SourceFiles = sourceFiles
            References = references
            ProjectReferences = projectReferences
//            ProjectReferences = ResizeArray()
            Analyzers = analyzers
        }


    // this is a temporary approach due to msbuild issues, it will need to be replaced with an msbuild approach later
    let fromXDoc projectFilePath =
        let rec generate projectFilePath =
            let projectDir = Path.GetDirectoryName projectFilePath
            let workingDir = System.Environment.CurrentDirectory

            let xdoc = projectFilePath |> File.ReadAllText |> XDocument.Parse
            let xdoc = xdoc.Root

            let filterElems elemName xdoc =
                xdoc |> XElem.elements
                |> Seq.filter (XElem.isNamed elemName)


            let itemGroupElems = filterElems "ItemGroup" xdoc

            let propertyGroupElems =
                filterElems "PropertyGroup" xdoc
                |> Seq.collect XElem.elements

            let x:String = ""

            let projectReferenceElems = filterElems  "ProjectReference" xdoc

            let collectInculdeAttr elemName xelemsqs : string seq =
                xelemsqs |> Seq.collect (filterElems elemName)
                |> Seq.choose (XElem.tryGetAttributeValue "Include")

            let sourceFiles =
                collectInculdeAttr "Compile"  itemGroupElems
                |> ResizeArray

            let references =
                collectInculdeAttr "Reference"  itemGroupElems
                |> ResizeArray


            let projectReferences =
                System.IO.Directory.SetCurrentDirectory projectDir
                let projectPaths =
                    projectReferenceElems
                    |> Seq.map (XElem.getAttributeValue "Include" >> Path.GetDirectoryName)
                System.IO.Directory.SetCurrentDirectory workingDir
                projectPaths
                //|> Seq.map generate
                |> ResizeArray

            let analyzers =
                collectInculdeAttr "Analyzer"  itemGroupElems
                |> ResizeArray


            let getProperty propName =
                propertyGroupElems
                |> Seq.tryFind (XElem.isNamed propName)
                |> function  Some x -> XElem.value x | None -> String.Empty


            let projectGuid =
                getProperty "ProjectGuid"
                |> fun x ->
                    try PropertyConverter.toGuid x |> Some
                    with _ -> None


            {   ProjectFilePath = projectFilePath
                ProjectId = ProjectId.CreateNewId()
                ProjectGuid = projectGuid
                Name = getProperty "Name"
                TargetFramework = None
                AssemblyName = getProperty "AssemblyName"
                TargetPath = getProperty "OutputPath"
                OutputType = getProperty "OutputType" |> OutputType.Parse
                SignAssembly =  getProperty "SignAssembly" |> PropertyConverter.toBoolean
                AssemblyOriginatorKeyFile = String.Empty
                GenerateXmlDocumentation = String.Empty
                PreprocessorySymbolNames =
                    getProperty "DefineConstants"
                    |> PropertyConverter.toDefineConstants
                    |> ResizeArray
                SourceFiles = sourceFiles
                References = references
                ProjectReferences = projectReferences
                Analyzers = analyzers
            }
        generate projectFilePath


    let inline private createDocs projectFileInfo srcCodeKind =

        projectFileInfo.SourceFiles |> Seq.map (fun path ->
            let fullpath = Path.Combine(projectFileInfo.ProjectDirectory,path)
            DocumentInfo.Create
                (   DocumentId.CreateNewId projectFileInfo.ProjectId
                ,   Path.GetFileNameWithoutExtension path
                ,   sourceCodeKind=srcCodeKind
                ,   filePath = fullpath
                ,   loader=FileTextLoader(fullpath,Text.Encoding.UTF8)
                ,   isGenerated=false
                )
        )

    let createSrcDocInfo projectFileInfo =
        createDocs projectFileInfo SourceCodeKind.Regular

    let createScriptDocInfo projectFileInfo =
        createDocs projectFileInfo SourceCodeKind.Script


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
                    [ for projId in projIds -> ProjectReference(projId) ]
                    [ for path in paths -> ProjectReference(ProjectId.CreateNewId()) ]

        let projDict =
            let dict = Dictionary<_,_>()
            workspace.CurrentSolution.Projects
            |> Seq.iter(fun proj -> dict.Add(proj.Name,proj.Id))

        ProjectInfo.Create
            (   projectFileInfo.ProjectId
            ,   VersionStamp.Create()
            ,   projectFileInfo.Name
            ,   projectFileInfo.AssemblyName
            ,   "FSharp"
            ,   projectFileInfo.ProjectFilePath
            ,   outputFilePath=projectFileInfo.TargetPath
            (*  - TODO -
                Correctly adding projectreferences is going to be an issue
                ProjectReference is created using a project id, which means a collection of
                projectFileInfos should be passed to this function to prevent the creation
                of duplicate projectfile infos for referenced projects that have different ids
            *)
            ,   projectReferences= projectRefs
            ,   metadataReferences=seq[]
            ,   analyzerReferences=seq[]
            ,   documents = createSrcDocInfo projectFileInfo
            ,   additionalDocuments=seq[]
            //,   compilationOptions=
            //,   parseOptions=
            //,   isSubmission=
            //,   hostObjectType=
            )


    open Microsoft.FSharp.Compiler.SourceCodeServices

    let toFSharpProjectOptions (workspace:'a when 'a :> Workspace) (projectFileInfo:ProjectFileInfo) : FSharpProjectOptions =
        (projectFileInfo |> toProjectInfo workspace).ToFSharpProjectOptions workspace



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




//  with
//    static member Create (projectInfo:Proje)
