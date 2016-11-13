module ProtoWorkspace.Text

open System
open System.Collections.Generic
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.Classification
open Microsoft.CodeAnalysis.Editing
open Microsoft.CodeAnalysis.Differencing
open Newtonsoft.Json

type LinePositionSpanTextChange () =

    member val NewText     : string  = "" with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val StartLine   : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val StartColumn : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndLine     : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndColumn   : int     = 0  with get, set

    override self.Equals obj =
        match obj with
        | :? LinePositionSpanTextChange as other ->
            self.NewText        = other.NewText
            && self.StartLine   = other.StartLine
            && self.StartColumn = other.StartColumn
            && self.EndLine     = other.EndLine
            && self.EndColumn   = other.EndColumn
        | _ -> false


    override self.GetHashCode() =
        self.NewText.GetHashCode()
        * (23 + self.StartLine)
        * (29 + self.StartColumn)
        * (31 + self.EndLine)
        * (37 + self.EndColumn)


    override self.ToString () =
        if isNull self.NewText then String.Empty else
        let displayText = self.NewText.Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t")
        sprintf "StartLine=%i, StartColumn=%i, Endline=%i, EndColumn=%i,NewText='%s'"
            self.StartLine self.StartColumn self.EndLine self.EndColumn self.NewText


    member __.Convert (document:Document, changes: TextChange seq) : LinePositionSpanTextChange seq Async = async {
        let! (text:SourceText)  = document.GetTextAsync()

        return changes
        |> Seq.sortWithDescending ^ fun c1 c2 -> c1.Span.CompareTo c2.Span
        |> Seq.map ^ fun change ->
            let span = change.Span
            let newText = change.NewText
            let span, prefix, suffix =
                if newText.Length <= 0 then span, String.Empty, String.Empty else
                // Roslyn computes text changes on character arrays. So it might happen that a
                // change starts inbetween \r\n which is OK when you are offset-based but a problem
                // when you are line,column-based. This code extends text edits which just overlap
                // a with a line break to its full line break
                let span, prefix =
                    if span.Start > 0 && newText.[0] = '\n' && text.[span.Start - 1] = '\r' then
                        // text: foo\r\nbar\r\nfoo
                        // edit:      [----)
                        TextSpan.FromBounds(span.Start - 1, span.End), "\r"
                    else
                        span, String.Empty
                let span, suffix =
                    if span.End < text.Length - 1 && newText.[newText.Length - 1] = '\r' && text.[span.End] = '\n' then
                        // text: foo\r\nbar\r\nfoo
                        // edit:        [----)
                        TextSpan.FromBounds(span.Start, span.End + 1), "\n"
                    else
                        span, String.Empty
                span, prefix, suffix
            let linePositionSpan = text.Lines.GetLinePositionSpan span
            LinePositionSpanTextChange
                (   NewText     = prefix + newText + suffix
                ,   StartLine   = linePositionSpan.Start.Line
                ,   StartColumn = linePositionSpan.Start.Character
                ,   EndLine     = linePositionSpan.End.Line
                ,   EndColumn   = linePositionSpan.End.Character
                )
    }


type QuickFix () =
    member val Text     : string  = "" with get, set
    member val FileName : string  = "" with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val Line   : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val Column : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndLine     : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndColumn   : int     = 0  with get, set
    // NOTE - this is an ICollection in Omnisharp
    member val Projects : string [] = [||] with get, set

    override self.Equals other =
        if isNull other then false else
        match other with
        | :? QuickFix as other ->
            self.FileName = other.FileName
            && self.Line = other.Line
            && self.Column = other.Column
            && self.EndLine = other.EndLine
            && self.EndColumn = other.EndColumn
            && self.Text = other.Text
        | _ -> false

    override self.GetHashCode () =
        17 * 23 + (hash self.FileName)
        |> (*) 23 |> (+) ^ hash self.Line
        |> (*) 23 |> (+) ^ hash self.Column
        |> (*) 23 |> (+) ^ hash self.EndLine
        |> (*) 23 |> (+) ^ hash self.EndColumn
        |> (*) 23 |> (+) ^ hash self.Text



//
//type QuickFix = {
//    Text     : string
//    FileName : string
//    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
//    Line   : int
//    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
//    Column : int
//    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
//    EndLine     : int
//    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
//    EndColumn   : int
//    Projects : string seq
//}

type SymbolLocation () =
    inherit QuickFix()
    member val Kind = "" with get, set


type SyntaxFeature  = {
    Name : string
    Data : string
}


type FileMemberElement () =
    member val ChildNodes : FileMemberElement seq = Seq.empty with get, set
    member val Location = QuickFix() with get, set
    member val Kind = "" with get, set
    member val Features : SyntaxFeature seq = Seq.empty with get
    // NOTE - this is an ICollection in Omnisharp
    member val Projects : string [] = [||] with get, set

    member self.CompareTo (other:FileMemberElement) =
        if   other.Location.Line      < self.Location.Line      then 1
        elif other.Location.Line      > self.Location.Line      then -1
        elif other.Location.Column    < self.Location.Column    then 1
        elif other.Location.Column    > self.Location.Column    then -1
        elif other.Location.EndLine   < self.Location.EndLine   then 1
        elif other.Location.EndLine   > self.Location.EndLine   then -1
        elif other.Location.EndColumn < self.Location.EndColumn then 1
        elif other.Location.EndColumn > self.Location.EndColumn then -1
        else 0

    override self.Equals (other:obj) =
        if isNull other then false else
        match other with
        | :? FileMemberElement as other ->
            self.Location.Line = other.Location.Line
            && self.Location.Column = other.Location.Column
            && self.Location.EndLine = other.Location.EndLine
            && self.Location.EndColumn = other.Location.EndColumn
        | _ -> false

    override self.GetHashCode() =
        13 * self.Location.Line +
        17 * self.Location.Column +
        23 * self.Location.EndLine +
        31 * self.Location.EndColumn

    interface IComparable with
        member self.CompareTo (other:obj) =
            match other with
            | :? FileMemberElement as other -> self.CompareTo other
            | _ -> failwith "'other' was not a FileMemberElement"


type FileMemberTree () =
    member val TopLevelTypeDefinitions : FileMemberElement seq = Seq.empty with get, set

type HighlightSpan () =

    member val Kind : string  = "" with get, set
    member val Projects : string seq = Seq.empty with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val StartLine   : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val StartColumn : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndLine     : int     = 0  with get, set
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    member val EndColumn   : int     = 0  with get, set


    member self.FromClassifiedSpan(span:ClassifiedSpan, lines:TextLineCollection, projects:string seq) =
        let linePos = lines.GetLinePositionSpan(span.TextSpan)
        HighlightSpan
            (   Kind = span.ClassificationType
            ,   Projects = projects
            ,   StartLine = linePos.Start.Line
            ,   EndLine = linePos.End.Line
            ,   StartColumn = linePos.Start.Character
            ,   EndColumn = linePos.End.Character
            )


    member self.CompareTo (other:HighlightSpan) =
        if   other.StartLine   < self.StartLine    then 1
        elif other.StartLine   > self.StartLine    then -1
        elif other.StartColumn < self.StartColumn  then 1
        elif other.StartColumn > self.StartColumn  then -1
        elif other.EndLine     < self.EndLine      then 1
        elif other.EndLine     > self.EndLine      then -1
        elif other.EndColumn   < self.EndColumn    then 1
        elif other.EndColumn   > self.EndColumn    then -1
        else 0


    override self.Equals (other:obj) =
        if isNull other then false else
        match other with
        | :? HighlightSpan as other ->
            self.StartLine = other.StartLine
            && self.StartColumn = other.StartColumn
            && self.EndLine = other.EndLine
            && self.EndColumn = other.EndColumn
        | _ -> false

    override self.GetHashCode() =
        13 * self.StartLine +
        17 * self.StartColumn +
        23 * self.EndLine +
        31 * self.EndColumn

    interface IComparable with
        member self.CompareTo (other:obj) =
            match other with
            | :? HighlightSpan as other -> self.CompareTo other
            | _ -> failwith "'other' was not a HighLightSpan"


type HighlightClassification =
    | Name                   = 1
    | Comment                = 2
    | String                 = 3
    | Operator               = 4
    | Punctuation            = 5
    | Keyword                = 6
    | Number                 = 7
    | Identifier             = 8
    | PreprocessorKeyword    = 9
    | ExcludedCode           = 10


type SignatureHelpParameter = {
    Name          : string
    Label         : string
    Documentation : string
}


type SignatureHelpItem = {
    Name          : string
    Label         : string
    Documentation : string
    Parameters    : SignatureHelpParameter list
}


type SignatureHelp = {
    Signatures      : SignatureHelpItem list
    ActiveSignature : int
    ActiveParameter : int
}


type MetadataSource = {
    AssemblyName  : string
    TypeName      : string
    ProjectName   : string
    VersionNumber : string
    Language      : string
}


type CodeAction = {
    Identifier : string
    Name : string
}

type Point = {
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    Line : int
    [<JsonConverter(typeof<ZeroBasedIndexConverter>)>]
    Column : int
}

type Range = {
    Start : Point
    End   : Point
}


