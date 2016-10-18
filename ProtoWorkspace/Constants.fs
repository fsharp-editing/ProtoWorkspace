module ProtoWorkspace.Constants

open System

[<Literal>]
let FSharpProjectGuidStr = "F2A71F9B-5D33-465A-A702-920D77279786"

let FSharpProjectGuid = Guid FSharpProjectGuidStr

// Common Constants
[<Literal>]
let Name = "Name"

[<Literal>]
let None = "None"

[<Literal>]
let Reference = "Reference"

// Platform Constants
[<Literal>]
let X86 = "x86"

[<Literal>]
let X64 = "x64"

[<Literal>]
let AnyCPU = "AnyCPU"

// BuildAction Constants
[<Literal>]
let Compile = "Compile"

[<Literal>]
let Content = "Content"

[<Literal>]
let Resource = "Resource"

[<Literal>]
let EmbeddedResource = "EmbeddedResource"

// CopyToOutputDirectory Constants
[<Literal>]
let Never = "Never"

[<Literal>]
let Always = "Always"

[<Literal>]
let PreserveNewest = "PreserveNewest"

// DebugType Constants
[<Literal>]
let PdbOnly = "PdbOnly"

[<Literal>]
let Full = "Full"

// OutputType Constants
[<Literal>]
let Exe = "Exe"

[<Literal>]
let Winexe = "Winexe"

[<Literal>]
let Library = "Library"

[<Literal>]
let Module = "Module"

// XML Attribute Name Constants
[<Literal>]
let DefaultTargets = "DefaultTargets"

[<Literal>]
let ToolsVersion = "ToolsVersion"

[<Literal>]
let Include = "Include"

[<Literal>]
let Condition = "Condition"

// MSBuild XML Element Constants
[<Literal>]
let Project = "Project"

[<Literal>]
let ItemGroup = "ItemGroup"

[<Literal>]
let PropertyGroup = "PropertyGroup"

[<Literal>]
let ProjectReference = "ProjectReference"

// XML Property Constants (found in PropertyGroups)
[<Literal>]
let AssemblyName = "AssemblyName"

[<Literal>]
let RootNamespace = "RootNamespace"

[<Literal>]
let Configuration = "Configuration"

[<Literal>]
let Platform = "Platform"

[<Literal>]
let SchemaVersion = "SchemaVersion"

[<Literal>]
let ProjectGuid = "ProjectGuid"

[<Literal>]
let ProjectType = "ProjectType"

[<Literal>]
let OutputType = "OutputType"

[<Literal>]
let TargetFrameworkVersion = "TargetFrameworkVersion"

[<Literal>]
let TargetFrameworkProfile = "TargetFrameworkProfile"

[<Literal>]
let AutoGenerateBindingRedirects = "AutoGenerateBindingRedirects"

[<Literal>]
let TargetFSharpCoreVersion = "TargetFSharpCoreVersion"

[<Literal>]
let DebugSymbols = "DebugSymbols"

[<Literal>]
let DebugType = "DebugType"

[<Literal>]
let Optimize = "Optimize"

[<Literal>]
let Tailcalls = "Tailcalls"

[<Literal>]
let OutputPath = "OutputPath"

[<Literal>]
let CompilationConstants = "DefineConstants"

[<Literal>]
let WarningLevel = "WarningLevel"

[<Literal>]
let PlatformTarget = "PlatformTarget"

[<Literal>]
let DocumentationFile = "DocumentationFile"

[<Literal>]
let Prefer32Bit = "Prefer32Bit"

[<Literal>]
let OtherFlags = "OtherFlags"

// XML Elements
[<Literal>]
let CopyToOutputDirectory = "CopyToOutputDirectory"

[<Literal>]
let HintPath = "HintPath"

[<Literal>]
let Private = "Private"

[<Literal>]
let SpecificVersion = "SpecificVersion"

[<Literal>]
let Link = "Link"

[<Literal>]
let Paket = "Paket"

[<Literal>]
let XmlDecl = @"<?xml version='1.0' encoding='utf-8'?>"

[<Literal>]
let Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003"
