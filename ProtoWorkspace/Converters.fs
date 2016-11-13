[<AutoOpen>]
module ProtoWorkspace.Converters

open System
open System.Collections.Immutable
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Range
open Newtonsoft.Json


let fsharpRangeToTextSpan (sourceText:SourceText, range:range) =
    // Roslyn TextLineCollection is zero-based, F# range lines are one-based
    let startPosition = sourceText.Lines.[range.StartLine - 1].Start + range.StartColumn
    let endPosition = sourceText.Lines.[range.EndLine - 1].Start + range.EndColumn
    TextSpan (startPosition, endPosition - startPosition)

let getTaskAction (computation:Async<unit>) =
    // Shortcut due to nonstandard way of converting Async<unit> to Task
    let action () =
        try
            computation |> Async.RunSynchronously
        with ex ->
            Assert.Exception ^ ex.GetBaseException()
            raise ^ ex.GetBaseException()
    Action action

let getCompletedTaskResult (task:'Result Task)=
    if task.Status = TaskStatus.RanToCompletion then
        task.Result
    else
        Assert.Exception ^ task.Exception.GetBaseException()
        raise ^ task.Exception.GetBaseException()

let supportedDiagnostics () =
    // We are constructing our own descriptors at run-time. Compiler service is already doing error formatting and localization.
    let dummyDescriptor = DiagnosticDescriptor("0", String.Empty, String.Empty, String.Empty, DiagnosticSeverity.Error, true, null, null)
    ImmutableArray.Create<DiagnosticDescriptor> dummyDescriptor

let convertError(error: FSharpErrorInfo, location: Location) =
    let id = "FS" + error.ErrorNumber.ToString "0000"
    let emptyString = LocalizableString.op_Implicit ""
    let description = LocalizableString.op_Implicit error.Message
    let severity = if error.Severity = FSharpErrorSeverity.Error then DiagnosticSeverity.Error else DiagnosticSeverity.Warning
    let descriptor = DiagnosticDescriptor(id, emptyString, description, error.Subcategory, severity, true, emptyString, String.Empty, null)
    Diagnostic.Create(descriptor, location)



type ZeroBasedIndexConverter() =
    inherit JsonConverter()

    override __.CanConvert (objectType:Type) =
        objectType
            |>( (=) typeof<int>
            |?| (=) typeof<int Nullable>
            |?| (=) typeof<int seq>
            |?| (=) typeof<int []>
        )

    override __.ReadJson (reader:JsonReader, objectType:Type, existingValue:obj, serializer:JsonSerializer) =
        if reader.TokenType = JsonToken.Null then null
        elif objectType = typeof<int []> then
            serializer.Deserialize<int[]> reader :> obj
        elif objectType = typeof<int seq> then
            serializer.Deserialize<int seq> reader
            |> Seq.map ^ fun x -> x - 1
            :> obj
        elif objectType = typeof<int Nullable> then
            let result = serializer.Deserialize<int Nullable> reader
            if result.HasValue then
                result.Value :> obj
            else
                null
        else
            null

    (*  Omnisharp has a configuration on whether to use zerobasedindices or not
                if (Configuration.ZeroBasedIndices)
            {
                return serializer.Deserialize(reader, objectType);
            }
    *)


    override __.WriteJson(writer:JsonWriter, value: obj, serializer:JsonSerializer) =
        if isNull value then
            serializer.Serialize(writer,null)
        else
        let objectType = value.GetType()
        let results =
            if objectType = typeof<int[]> then
                let results = value :?> int[]
                for i=0 to results.Length-1 do
                    results.[i] <- results.[i] + 1
                results :> obj
            elif objectType = typeof<int seq> then
                let results = value :?> int seq
                results |> Seq.map ^ fun x -> x + 1
                :> obj
            elif objectType = typeof<int Nullable> then
                let result = value :?> int Nullable
                if result.HasValue then
                    result.Value + 1 :> obj
                else
                    null
            else
                null
        serializer.Serialize(writer,results)


(*

let FSharpRangeToTextSpan(sourceText: SourceText, range: range) =
    // Roslyn TextLineCollection is zero-based, F# range lines are one-based
    let startPosition = sourceText.Lines.[range.StartLine - 1].Start + range.StartColumn
    let endPosition = sourceText.Lines.[range.EndLine - 1].Start + range.EndColumn
    TextSpan(startPosition, endPosition - startPosition)





*)







