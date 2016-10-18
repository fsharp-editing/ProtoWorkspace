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
open ProtoWorkspace.MSBuildInfo

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

type ProjectFileInfo = 
    { ProjectId : ProjectId
      ProjectGuid : Guid
      Name : string
      ProjectFilePath : string
      TargetFramework : FrameworkName
      AssemblyName : string
      TargetPath : string
      OutputType : OutputType
      SignAssembly : bool
      AssemblyOriginatorKeyFile : string
      GenerateXmlDocumentation : string
      PreprocessorySymbolNames : string ResizeArray
      SourceFiles : string ResizeArray
      References : string ResizeArray
      ProjectReferences : string ResizeArray
      Analyzers : string ResizeArray }
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
    
    let create (projectFilePath : string) (solutionDirectory : string) (logger : ILogger) (options : MSBuildOptions) 
        (diagnostics : MSBuildDiagnosticsMessage ICollection) = 
        if not (File.Exists projectFilePath) then failwithf "No project file found at '%s'" projectFilePath
        let globalProperties = 
            dict [ PropertyNames.DesignTimeBuild, "true"
                   PropertyNames.BuildProjectReferences, "false"
                   PropertyNames.ResolveReferenceDependencies, "true"
                   PropertyNames.SolutionDir, solutionDirectory + string Path.DirectorySeparatorChar ]
        if not (String.IsNullOrWhiteSpace options.MSBuildExtensionsPath) then 
            globalProperties.Add(PropertyNames.MSBuildExtensionsPath, options.MSBuildExtensionsPath)
        if not (String.IsNullOrWhiteSpace options.VisualStudioVersion) then 
            globalProperties.Add(PropertyNames.VisualStudioVersion, options.VisualStudioVersion)
        let collection = new ProjectCollection(globalProperties)
        logger.LogInfofn "Using toolset %s for '%s'" (options.ToolsVersion <?> collection.DefaultToolsVersion) 
            projectFilePath
        let project = 
            if String.IsNullOrEmpty options.ToolsVersion then collection.LoadProject projectFilePath
            else collection.LoadProject(projectFilePath, options.ToolsVersion)
        
        let projectInstance = project.CreateProjectInstance()
        
        let buildResult : bool = 
            let loggers = [ MSBuildLogForwarder(logger, diagnostics) :> Microsoft.Build.Framework.ILogger ] :> seq<_>
            projectInstance.Build(TargetNames.ResolveReferences, loggers)
        
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
        
        { ProjectFilePath = projectFilePath
          ProjectId = ProjectId.CreateNewId()
          ProjectGuid = PropertyConverter.toGuid <| projectInstance.GetPropertyValue PropertyNames.ProjectGuid
          Name = projectInstance.GetPropertyValue PropertyNames.ProjectName
          TargetFramework = FrameworkName(projectInstance.GetPropertyValue PropertyNames.TargetFrameworkMoniker)
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
          Analyzers = analyzers }
    
    let toProjectInfo (projectFileInfo : ProjectFileInfo) = 
        ProjectInfo.Create
            (projectFileInfo.ProjectId, VersionStamp.Create(), projectFileInfo.Name, projectFileInfo.AssemblyName, 
             "FSharp", projectFileInfo.ProjectFilePath)
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
