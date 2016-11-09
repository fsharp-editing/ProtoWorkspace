module ProtoWorkspace.Loaders

open Microsoft.CodeAnalysis
open System.Reflection

type AnalyzerAssemblyLoader() as self =

    member __.AddDependencyLocation(fullPath: string): unit = ()

    member __.LoadFromPath(fullPath: string): System.Reflection.Assembly =
        Assembly.Load(AssemblyName.GetAssemblyName fullPath)

    interface IAnalyzerAssemblyLoader with
        member __.AddDependencyLocation fullPath = self.AddDependencyLocation fullPath
        member __.LoadFromPath fullPath = self.LoadFromPath fullPath

