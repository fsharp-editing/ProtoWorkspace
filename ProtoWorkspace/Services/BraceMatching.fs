module ProtoWorkspace.BraceMatching

open System
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharpVSPowerTools
open ProtoWorkspace.ServiceInterfaces
open ProtoWorkspace.LanguageService


[<ExportBraceMatcher (Constants.FSharpLanguageName)>]
type FSharpBraceMatchingService () =

    static member GetBraceMatchingResult (sourceText, fileName, options, position) = async {
        let isPositionInRange range =
            let span = Converters.fsharpRangeToTextSpan (sourceText, range)
            span.Start <= position && position < span.End
        let! matchedBraces = Checker.MatchBracesAlternate (fileName, sourceText.ToString(), options)

        return
            matchedBraces |> Seq.tryFind ^ fun (left, right) -> isPositionInRange left || isPositionInRange right
    }

    interface IBraceMatcher with
        member this.FindBracesAsync (document, position, cancellationToken) = async {
            match FSharpLanguageService.GetOptions document.Project.Id with
            | Some options ->
                let! sourceText = document.GetTextAsync cancellationToken |> Async.AwaitTask
                let! result = FSharpBraceMatchingService.GetBraceMatchingResult (sourceText, document.Name, options, position)
                return
                    match result with
                    | None -> None
                    | Some (left, right) ->
                        Some ^ braceMatchingResult
                                    (Converters.fsharpRangeToTextSpan (sourceText, left))
                                    (Converters.fsharpRangeToTextSpan (sourceText, right))
            | None -> return None
        }