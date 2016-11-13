module ProtoWorkspace.ServiceInterfaces

open System
open System.Composition
open System.Threading
open System.Threading.Tasks
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.Host
open Microsoft.CodeAnalysis.Classification
open Microsoft.CodeAnalysis.Formatting
open Microsoft.FSharp.Compiler.SourceCodeServices


/// TODO - delete this later, temporary usages for impl
let Checker = FSharpChecker.Create ()



type [<Struct>] BraceMatchingResult (leftSpan:TextSpan, rightSpan:TextSpan) =
    member __.LeftSpan  = leftSpan
    member __.RightSpan = rightSpan


let braceMatchingResult leftSpan rightSpan = BraceMatchingResult (leftSpan, rightSpan)


type IBraceMatcher =
    abstract FindBracesAsync:
        document:Document * position:int * cancellationToken:CancellationToken -> Async<BraceMatchingResult option>


type IBraceMatchingService =
    abstract member GetMatchingBracesAsync:
        document:Document * position:int * cancellationToken:CancellationToken -> Async<BraceMatchingResult option>


[<MetadataAttribute; AttributeUsage (AttributeTargets.Class)>]
type ExportBraceMatcherAttribute (language:string) =
    inherit ExportAttribute (typeof<IBraceMatcher>)
    do
        if isNull language then raise ^ ArgumentNullException language
    member __.Language = language



type IHighlightingService =
    abstract member GetHighlights: position:int -> TextSpan seq



/// <summary>
/// An indentation result represents where the indent should be placed.  It conveys this through
/// a pair of values.  A position in the existing document where the indent should be relative,
/// and the number of columns after that the indent should be placed at.
///
/// This pairing provides flexibility to the implementor to compute the indentation results in
/// a variety of ways.  For example, one implementation may wish to express indentation of a
/// newline as being four columns past the start of the first token on a previous line.  Another
/// may wish to simply express the indentation as an absolute amount from the start of the
/// current line.  With this tuple, both forms can be expressed, and the implementor does not
/// have to convert from one to the other.
/// </summary>
[<Struct>]
type IndentationResult (basePosition:int, offset:int) =
    /// <summary>
    /// The base position in the document that the indent should be relative to.  This position
    /// can occur on any line (including the current line, or a previous line).
    /// </summary>
    member __.BasePosition = basePosition

    /// <summary>
    /// The number of columns the indent should be at relative to the BasePosition's column.
    /// </summary>
    member __.Offset = offset



type IIndentationService =
    inherit ILanguageService
    abstract member GetDesiredIndentation:
        document:Document * lineNumber:int * cancellationToken:CancellationToken -> Async<IndentationResult option>


type ISynchronousIndentationService =
    inherit ILanguageService
    abstract member GetDesiredIndentation:
        document:Document * lineNumber:int * cancellationToken:CancellationToken -> Async<IndentationResult option>



