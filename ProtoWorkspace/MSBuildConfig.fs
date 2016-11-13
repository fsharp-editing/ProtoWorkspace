module ProtoWorkspace.MSBuildInfo

open ProtoWorkspace
open System
open System.Collections.Generic
open Microsoft.Build.Framework
open Microsoft.Extensions.Logging
open FSharp.Control

type MSBuildDiagnosticsMessage =
    { LogLevel : string
      FileName : string
      Text : string
      StartLine : int
      Endline : int
      StartColumn : int
      EndColumn : int }

type MSBuildProjectDiagnostics =
    { FileName : string
      Warnings : MSBuildDiagnosticsMessage []
      Errors : MSBuildDiagnosticsMessage [] }

type MSBuildLogForwarder(logger : ILogger, diagnostics : MSBuildDiagnosticsMessage ICollection) as self =
    let mutable disposables : ResizeArray<IDisposable> = ResizeArray()

    let onError (args : // TODO - Add other/loose configuration properties?
                        // Properties : string []
                        BuildErrorEventArgs) =
        logger.LogError args.Message
        diagnostics.Add { LogLevel = "Error"
                          FileName = args.File
                          Text = args.Message
                          StartLine = args.LineNumber
                          Endline = args.ColumnNumber
                          StartColumn = args.EndLineNumber
                          EndColumn = args.EndColumnNumber }

    let onWarning (args : BuildWarningEventArgs) =
        logger.LogError args.Message
        diagnostics.Add { LogLevel = "Warning"
                          FileName = args.File
                          Text = args.Message
                          StartLine = args.LineNumber
                          Endline = args.ColumnNumber
                          StartColumn = args.EndLineNumber
                          EndColumn = args.EndColumnNumber }

    member val Parameters = "" with get, set
    member val Verbosity = LoggerVerbosity.Normal with get, set

    member __.Initialize(eventSource : IEventSource) =
        eventSource.ErrorRaised.Subscribe onError |> disposables.Add
        eventSource.WarningRaised.Subscribe onWarning |> disposables.Add

    member __.Shutdown() =
        disposables |> Seq.iter dispose
        disposables.Clear()

    interface Microsoft.Build.Framework.ILogger with
        member __.Initialize eventSource = self.Initialize eventSource
        member __.Shutdown() = self.Shutdown()

        member __.Parameters
            with get () = self.Parameters
            and set v = self.Parameters <- v

        member __.Verbosity
            with get () = self.Verbosity
            and set v = self.Verbosity <- v

type MSBuildOptions =
    { ToolsVersion : string
      VisualStudioVersion : string
      WaitForDebugger : bool
      MSBuildExtensionsPath : string }

type MSBuildProject =
    { ProjectGuid : Guid
      Path : string
      AssemblyName : string
      TargetPath : string
      TargetFramework : string
      SourceFiles : string IList }
