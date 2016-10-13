namespace ProtoWorkspace

open System
open System.IO
open System.Text



type LineScanner (line:string) =
    let mutable line = line
    let mutable currentPosition = 0

    /// Return the text following the scanner's current position
    member __.ReadRest() =
        let rest = line.Substring currentPosition
        currentPosition <- line.Length
        rest

    /// Return the text between the scanner's current position and 
    /// the provided delimiter. 
    member self.ReadUpToAndEat (delimiter:string) =
        match line.IndexOf(delimiter, currentPosition) with
        | -1    -> self.ReadRest() 
        | index -> 
            let upToDelimiter = line.Substring(currentPosition, index - currentPosition)
            currentPosition <- index + delimiter.Length
            upToDelimiter


type SectionBlock = {
    BlockType         : string
    ParenthesizedName : string
    Value             : string
    KeyMap            : (string*string) seq
}


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
module SectionBlock =

    let parse (reader:TextReader) =
        let rec findStart ln =
            match reader.ReadLine() with 
            | null -> ln 
            | line -> 
                let startline = line.TrimStart [||]
                if startline <> String.Empty then startline 
                else findStart startline
        let startline         = reader.ReadLine() |> findStart
        let scanner           = LineScanner startline
        let blockType         = scanner.ReadUpToAndEat "("
        let parenthesizedName = scanner.ReadUpToAndEat ") = "
        let sectionValue      = scanner.ReadRest()
        
        let rec findPairs () = seq {
            match reader.ReadLine() with
            | null -> () 
            | txt when txt = "End" + blockType  -> ()
            | "" ->  yield! findPairs()
            | line ->
                let scanner = LineScanner line
                let key = scanner.ReadUpToAndEat " = "
                let value = scanner.ReadRest() 
                yield (key,value)
                yield! findPairs()
        }
        let keyMap = findPairs()

        {   BlockType         = blockType
            ParenthesizedName = parenthesizedName
            Value             = sectionValue    
            KeyMap            = keyMap
        }


    let getText indent (block:SectionBlock) =
        let builder = 
            StringBuilder().Append('\t',indent)
                .Append(sprintf "%s(%s) = " block.BlockType block.ParenthesizedName)
                .AppendLine block.Value
        
        for (key,value) in block.KeyMap do
            builder.Append('\t',indent+1)
                .Append(key).Append(" = ").AppendLine value |> ignore

        builder.Append('\t',indent)
            .Append(sprintf "End%s" block.BlockType)
            .AppendLine() |> string


type ProjectBlock = {
    ProjectTypeGuid : Guid
    ProjectName : string
    ProjectPath : string
    ProjectGuid : Guid
    ProjectSections : SectionBlock seq
} 


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
module ProjectBlock = 

    let parse (reader:TextReader) : ProjectBlock =
        let startLine = reader.ReadLine().TrimStart [||]
        let scanner = LineScanner startLine

        let invalid() = raise <| exn "Invalid Project Block in Solution"

        if scanner.ReadUpToAndEat "(\"" <> "Project" then invalid()

        let projectTypeGuid = 
            Guid.Parse <| scanner.ReadUpToAndEat "\")"

        if scanner.ReadUpToAndEat("\"").Trim() <> "=" then invalid()

        let projectName = 
            scanner.ReadUpToAndEat "\""

        if scanner.ReadUpToAndEat("\"").Trim() <> "," then invalid()
        
        let projectPath = 
            scanner.ReadUpToAndEat "\""

        if scanner.ReadUpToAndEat("\"").Trim() <> "," then invalid()

        let projectGuid = 
            Guid.Parse <| scanner.ReadUpToAndEat "\""

        let rec getSections() = seq {
            if not (Char.IsWhiteSpace <| char (reader.Peek())) then () else
            yield SectionBlock.parse reader
            yield! getSections()
        }
        let projectSections = getSections()

        let peekChar c = (reader.Peek() |> char) <> c
        if peekChar 'P' && peekChar 'G' then invalid()

        {   ProjectTypeGuid = projectTypeGuid
            ProjectName     = projectName
            ProjectPath     = projectPath
            ProjectGuid     = projectGuid
            ProjectSections = projectSections
        }


    let getText (projectBlock:ProjectBlock) =
        let typeGuid = projectBlock.ProjectTypeGuid.ToString("B").ToUpper()
        let projGuid = projectBlock.ProjectGuid.ToString("B").ToUpper()
        
        let builder = 
            StringBuilder().Append(
                sprintf "Project(\"%s\") = \"%s\", \"%s\", \"%s\"" 
                    typeGuid  projectBlock.ProjectName projectBlock.ProjectPath projGuid
                ).AppendLine()
        
        for section in projectBlock.ProjectSections do
            SectionBlock.getText 1 section |> builder.Append |> ignore
        
        builder.AppendLine "EndProject" |> string


type SolutionFile = {
    HeaderLines         : string seq
    VSVersionLineOpt    : string
    MinVSVersionLineOpt : string
    ProjectBlocks       : ProjectBlock seq
    GlobalSectionBlocks : SectionBlock seq
}
        
        
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix); RequireQualifiedAccess>]
module SolutionFile =         
       
    let getText (sln:SolutionFile) =
        let builder = StringBuilder().AppendLine()

        for line in sln.HeaderLines do
            builder.AppendLine line |> ignore
        
        for block in sln.ProjectBlocks do
            block |> ProjectBlock.getText 
            |> builder.Append  |> ignore
       
        builder.AppendLine "Global" |> ignore

        for section in sln.GlobalSectionBlocks do
            section |> SectionBlock.getText 1  
            |> builder.Append |> ignore
        
        builder.AppendLine "EndGlobal" 
        |> string


    let private getNextNonEmptyLine (reader:TextReader) =
        let rec getLine (line:string) =
            if isNull line || line.Trim() = String.Empty then line else
            getLine <| reader.ReadLine()
        getLine <| reader.ReadLine()


    let private consumeEmptyLines (reader:TextReader) =
        while   reader.Peek() <> -1 
            && "\r\n".Contains(reader.Peek()|>char|>string) do
            reader.ReadLine() |> ignore


    let parseGlobal (reader:TextReader) : SectionBlock seq =
        if reader.Peek() = -1 then Seq.empty 
        elif getNextNonEmptyLine reader <> "Global" then raise(exn "invalid global section") else

        let rec getBlocks() = seq {
            if reader.Peek() = -1 || Char.IsWhiteSpace(reader.Peek()|>char) then () else
            yield SectionBlock.parse reader
            yield! getBlocks()
        }
        let globalSectionBlocks = getBlocks()
        
        if getNextNonEmptyLine reader <> "EndGlobal" then raise(exn "invalid global section") else
        consumeEmptyLines reader
        globalSectionBlocks


    let parse (reader:TextReader) = 
        let headerLines = ResizeArray()
        let headerLine1 = getNextNonEmptyLine reader

        if isNull headerLine1 || not(headerLine1.StartsWith("Microsoft Visual Studio Solution File")) then
            raise(exn "invalid global section")

        /// skip comment lines and empty lines
        let rec getLines() = seq {
            if reader.Peek() = -1 || "#\r\n".Contains(reader.Peek()|>char|>string) then () else
            yield reader.ReadLine()
            yield! getLines()
        }
        let headerLines = Seq.append [headerLine1] (getLines())

        let visualStudioVersionLineOpt =
            if char(reader.Peek()) = 'V' then 
                let line = getNextNonEmptyLine reader
                if not (line.StartsWith "VisualStudioVersion") then 
                    raise(exn "invalid global section")
                line
            else String.Empty // should this be null?

        let minimumVisualStudioVersionLineOpt = 
            if char(reader.Peek()) = 'M' then
                let line = getNextNonEmptyLine reader
                if not(line.StartsWith "MinimumVisualStudioVersion") then
                    raise(exn "invalid global section")
                line
            else String.Empty // should this be null?

        // Parse project blocks while we have them
        let rec getBlocks() = seq {
            if char(reader.Peek()) <> 'P' then () else
            yield ProjectBlock.parse reader
            // Comments and Empty Lines between the Project Blocks are skipped
            getLines() |> ignore
            yield! getBlocks()        
        }
        // Parse project blocks while we have them
        let projectBlocks = getBlocks()

        // We now have a global block
        let globalSectionBlocks = parseGlobal reader

        if reader.Peek() <> -1 then
            raise(exn "Should be at the end of file")

        {   HeaderLines         = headerLines
            VSVersionLineOpt    = visualStudioVersionLineOpt
            MinVSVersionLineOpt = minimumVisualStudioVersionLineOpt
            ProjectBlocks       = projectBlocks
            GlobalSectionBlocks = globalSectionBlocks
        }


