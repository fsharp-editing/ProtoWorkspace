# ProtoWorkspace

An implementation of a Roslyn workspace to serve as the foundation for a xplat Language Service and editor tools in FSharp.Editing

## Roslyn Architecture

There is often confusion about what Roslyn is and why it would be used with F#. This project has 
no relation to the Roslyn the C# Compiler. ProtoWorkspace is implementing a Roslyn Workspace from the
Microsoft.CodeAnalysis API to take advantage of its capabilites for managing projects, solutions, document tracking, dirty buffers, and its infrastructure for implmenting editor tooling features (e.g. intellisense, refactoring, code fixes).
The workspace for F# needs to be built from the ground up as the existing Roslyn workspaces are incompatible with F#.

![](https://github.com/dotnet/roslyn/wiki/images/alex-api-layers.png)

For the F# workspace the compiler layer is fulfilled by the **FSharp.Compiler.Service**



## Workspace Architecture

![](https://github.com/dotnet/roslyn/wiki/images/workspace-obj-relations.png)

Host Environment -
Event System -
SourceText Change sets -
FSharp Language service integration -


**[For examples of workspace implmentations see the links in Reference.md](reference.md)**