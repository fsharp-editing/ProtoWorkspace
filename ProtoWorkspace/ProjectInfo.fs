namespace ProtoWorkspace
open System
open System.IO
open System.Collections.Generic
open Microsoft.CodeAnalysis
open System.Runtime.Versioning


type ProjectFileInfo = {
    ProjectId : ProjectId
    ProjectGuid : Guid
    Name : string
    ProjectFilePath : string
    TargetFramework : FrameworkName
(* Unsure how to convert these
    public LanguageVersion SpecifiedLanguageVersion { get; }
    public string ProjectDirectory => Path.GetDirectoryName(ProjectFilePath);
*)


}

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
            public const string AllowUnsafeBlocks = nameof(AllowUnsafeBlocks);
            public const string AssemblyName = nameof(AssemblyName);
            public const string AssemblyOriginatorKeyFile = nameof(AssemblyOriginatorKeyFile);
            public const string BuildProjectReferences = nameof(BuildProjectReferences);
            public const string DefineConstants = nameof(DefineConstants);
            public const string DesignTimeBuild = nameof(DesignTimeBuild);
            public const string DocumentationFile = nameof(DocumentationFile);
            public const string LangVersion = nameof(LangVersion);
            public const string OutputType = nameof(OutputType);
            public const string MSBuildExtensionsPath = nameof(MSBuildExtensionsPath);
            public const string ProjectGuid = nameof(ProjectGuid);
            public const string ProjectName = nameof(ProjectName);
            public const string _ResolveReferenceDependencies = nameof(_ResolveReferenceDependencies);
            public const string SignAssembly = nameof(SignAssembly);
            public const string SolutionDir = nameof(SolutionDir);
            public const string TargetFrameworkMoniker = nameof(TargetFrameworkMoniker);
            public const string TargetPath = nameof(TargetPath);
            public const string VisualStudioVersion = nameof(VisualStudioVersion);
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





type MSBuildProject = {
    ProjectGuid : Guid
    Path : string
    AssemblyName : string
    TargetPath : string
    TargetFramework : string
    SourceFiles : string IList
} 
//  with
//    static member Create (projectInfo:Proje)


