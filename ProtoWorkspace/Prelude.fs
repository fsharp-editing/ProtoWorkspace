[<AutoOpen>]
module ProtoWorkspace.Prelude

open System
open System.IO
open System.Diagnostics

type FileName = string

type FilePath = string

let inline debugfn msg = Printf.kprintf Debug.WriteLine msg
let inline failfn msg = Printf.kprintf Debug.Fail msg

let inline isNull v =
    match v with
    | null -> true
    | _ -> false

let inline isNotNull v = not (isNull v)
let inline dispose (disposable : #IDisposable) = disposable.Dispose()
let inline Ok a = Choice1Of2 a
let inline Fail a = Choice2Of2 a
let inline (|Ok|Fail|) a = a

/// String Equals Ordinal Ignore Case
let (|EqualsIC|_|) (str : string) arg =
    if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some()
    else None


let tryCast<'T> (o: obj): 'T option =
    match o with
    | null -> None
    | :? 'T as a -> Some a
    | _ ->
        debugfn "Cannot cast %O to %O" (o.GetType()) typeof<'T>.Name
        None

/// Null coalescing operator
let (?|?) a b = if isNull a then b else a

let (^) = (<|)

/// OR predicate combinator
let inline (|?|) (pred1:'a->bool) (pred2:'a->bool)  =
    fun a -> pred1 a || pred2 a

/// AND predicate combinator
let inline (|&|) (pred1:'a->bool) (pred2:'a->bool)  =
    fun a -> pred1 a && pred2 a

let (</>) path1 path2 = Path.Combine (path1, path2)


/// If arg is null raise an `ArgumentNullException` with the argname
let inline checkNullArg arg argName =
    if isNull arg then nullArg argName


/// Load times used to reset type checking properly on script/project load/unload. It just has to be unique for each project load/reload.
/// Not yet sure if this works for scripts.
let fakeDateTimeRepresentingTimeLoaded x = DateTime(abs (int64 (match x with null -> 0 | _ -> x.GetHashCode())) % 103231L)

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Array =
    let inline private checkNonNull argName arg =
        match box arg with
        | null -> nullArg argName
        | _ -> ()

    /// Optimized arrays equality. ~100x faster than `array1 = array2` on strings.
    /// ~2x faster for floats
    /// ~0.8x slower for ints
    let inline areEqual (xs : 'T []) (ys : 'T []) =
        match xs, ys with
        | null, null -> true
        | [||], [||] -> true
        | null, _ | _, null -> false
        | _ when xs.Length <> ys.Length -> false
        | _ ->
            let mutable break' = false
            let mutable i = 0
            let mutable result = true
            while i < xs.Length && not break' do
                if xs.[i] <> ys.[i] then
                    break' <- true
                    result <- false
                i <- i + 1
            result

    /// check if subArray is found in the wholeArray starting
    /// at the provided index
    let inline isSubArray (subArray : 'T []) (wholeArray : 'T []) index =
        if isNull subArray || isNull wholeArray then false
        elif subArray.Length = 0 then true
        elif subArray.Length > wholeArray.Length then false
        elif subArray.Length = wholeArray.Length then areEqual subArray wholeArray
        else
            let rec loop subidx idx =
                if subidx = subArray.Length then true
                elif subArray.[subidx] = wholeArray.[idx] then loop (subidx + 1) (idx + 1)
                else false
            loop 0 index

    /// Returns true if one array has another as its subset from index 0.
    let startsWith (prefix : _ []) (whole : _ []) = isSubArray prefix whole 0

    /// Returns true if one array has trailing elements equal to another's.
    let endsWith (suffix : _ []) (whole : _ []) = isSubArray suffix whole (whole.Length - suffix.Length)

    /// Returns a new array with an element replaced with a given value.
    let replace index value (array : _ []) =
        checkNonNull "array" array
        if index >= array.Length then raise (IndexOutOfRangeException "index")
        let res = Array.copy array
        res.[index] <- value
        res

    /// Returns all heads of a given array.
    /// For [|1;2;3|] it returns [|[|1; 2; 3|]; [|1; 2|]; [|1|]|]
    let heads (array : 'T []) =
        checkNonNull "array" array
        let res = Array.zeroCreate<'T []> array.Length
        for i = array.Length - 1 downto 0 do
            res.[i] <- array.[0..i]
        res

    /// Fold over the array passing the index and element at that index to a folding function
    let foldi (folder : 'State -> int -> 'T -> 'State) (state : 'State) (array : 'T []) =
        checkNonNull "array" array
        if array.Length = 0 then state
        else
            let folder = OptimizedClosures.FSharpFunc<_, _, _, _>.Adapt folder
            let mutable state : 'State = state
            let len = array.Length
            for i = 0 to len - 1 do
                state <- folder.Invoke(state, i, array.[i])
            state

    /// pass an array byref to reverse it in place
    let revInPlace (array : 'T []) =
        checkNonNull "array" array
        if areEqual array [||] then ()
        else
            let arrlen, revlen = array.Length - 1, array.Length / 2 - 1
            for idx in 0..revlen do
                let t1 = array.[idx]
                let t2 = array.[arrlen - idx]
                array.[idx] <- t2
                array.[arrlen - idx] <- t1

    /// Map all elements of the array that satisfy the predicate
    let filterMap predicate mapfn (array : 'T []) =
        checkNonNull "array" array
        if array.Length = 0 then [||]
        else
            let result = Array.zeroCreate array.Length
            let mutable count = 0
            for elm in array do
                if predicate elm then
                    result.[count] <- mapfn elm
                    count <- count + 1
            if count = 0 then [||]
            else result.[0..count - 1]

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module String =
    let inline toCharArray (str : string) = str.ToCharArray()

    let lowerCaseFirstChar (str : string) =
        if String.IsNullOrEmpty str || Char.IsLower(str, 0) then str
        else
            let strArr = toCharArray str
            match Array.tryHead strArr with
            | None -> str
            | Some c ->
                strArr.[0] <- Char.ToLower c
                String strArr

    let inline contains (target : string) (str : string) = str.Contains target
    let inline equalsIgnoreCase (str1 : string) (str2 : string) = str1.Equals(str2, StringComparison.OrdinalIgnoreCase)

    let extractTrailingIndex (str : string) =
        match str with
        | null -> null, None
        | _ ->
            let charr = str.ToCharArray()
            Array.revInPlace charr
            let digits = Array.takeWhile Char.IsDigit charr
            Array.revInPlace digits
            String digits |> function
            | "" -> str, None
            | index -> str.Substring(0, str.Length - index.Length), Some(int index)

    /// Remove all trailing and leading whitespace from the string
    /// return null if the string is null
    let trim (value : string) =
        if isNull value then null
        else value.Trim()

    /// Splits a string into substrings based on the strings in the array separators
    let split options (separator : string []) (value : string) =
        if isNull value then null
        else value.Split(separator, options)

    let (|StartsWith|_|) pattern value =
        if String.IsNullOrWhiteSpace value then None
        elif value.StartsWith pattern then Some()
        else None

    let (|Contains|_|) pattern value =
        if String.IsNullOrWhiteSpace value then None
        elif value.Contains pattern then Some()
        else None

    open System.IO

    let getLines (str : string) =
        use reader = new StringReader(str)
        [| let line = ref (reader.ReadLine())
           while isNotNull (!line) do
               yield !line
               line := reader.ReadLine()
           if str.EndsWith "\n" then
               // last trailing space not returned
               // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
               yield String.Empty |]

    let getNonEmptyLines (str : string) =
        use reader = new StringReader(str)
        [| let line = ref (reader.ReadLine())
           while isNotNull (!line) do
               if (!line).Length > 0 then yield !line
               line := reader.ReadLine() |]

    /// Parse a string to find the first nonempty line
    /// Return null if the string was null or only contained empty lines
    let firstNonEmptyLine (str : string) =
        use reader = new StringReader(str)

        let rec loop (line : string) =
            if isNull line then None
            elif line.Length > 0 then Some line
            else loop (reader.ReadLine())
        loop (reader.ReadLine())

open System.Text

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StringBuilder =
    /// Pipelining function for appending a string to a stringbuilder
    let inline append (str : string) (sb : StringBuilder) = sb.Append str

    /// Pipelining function for appending a string with a '\n' to a stringbuilder
    let inline appendLine (str : string) (sb : StringBuilder) = sb.AppendLine str

    /// SideEffecting function for appending a string to a stringbuilder
    let inline appendi (str : string) (sb : StringBuilder) = sb.Append str |> ignore

    /// SideEffecting function for appending a string with a '\n' to a stringbuilder
    let inline appendLinei (str : string) (sb : StringBuilder) = sb.AppendLine str |> ignore

[<RequireQualifiedAccess>]
module Dict =
    open System.Collections.Generic

    let add key value (dict : #IDictionary<_, _>) =
        dict.[key] <- value
        dict

    let remove (key : 'k) (dict : #IDictionary<'k, _>) =
        dict.Remove key |> ignore
        dict

    let tryFind key (dict : #IDictionary<'k, 'v>) =
        let mutable value = Unchecked.defaultof<_>
        if dict.TryGetValue(key, &value) then Some value
        else None

    let ofSeq (xs : ('k * 'v) seq) =
        let dict = Dictionary()
        for k, v in xs do
            dict.[k] <- v
        dict

module PropertyConverter =
    // TODO - railway this
    let toGuid propertyValue =
        match Guid.TryParse propertyValue with
        | true, value -> value
        | _ -> failwithf "Couldn't parse '%s' into a Guid" propertyValue

    let toDefineConstants propertyValue =
        if String.IsNullOrWhiteSpace propertyValue then [||]
        else propertyValue.Split([| ';' |], StringSplitOptions.RemoveEmptyEntries)

    // TODO - railway this
    let toBoolean propertyValue =
        if propertyValue = String.Empty then false else
        match Boolean.TryParse propertyValue with
        | true, value -> value
        | _ -> failwithf "Couldn't parse '%s' into a Boolean" propertyValue

    let toBooleanOr propertyValue defaultArg =
        match Boolean.TryParse propertyValue with
        | true, value -> value
        | _ -> defaultArg
(*
    Omnisharp does it like this

            public static bool? ToBoolean(string propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                return null;
            }

            try
            {
                return Convert.ToBoolean(propertyValue);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static bool ToBoolean(string propertyValue, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                return defaultValue;
            }

            try
            {
                return Convert.ToBoolean(propertyValue);
            }
            catch (FormatException)
            {
                return defaultValue;
            }
        }
*)




type internal Assert() =
    /// Display a good exception for this error message and then rethrow.
    static member Exception(e:Exception) =
        System.Diagnostics.Debug.Assert(false, "Unexpected exception seen in language service", e.ToString())

module internal CommonRoslynHelpers =
    open System
    open System.Collections.Immutable
    open System.Threading.Tasks
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.Text
    open Microsoft.FSharp.Compiler
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open Microsoft.FSharp.Compiler.Range

    let fsharpRangeToTextSpan(sourceText: SourceText, range: range) =
        // Roslyn TextLineCollection is zero-based, F# range lines are one-based
        let startPosition = sourceText.Lines.[range.StartLine - 1].Start + range.StartColumn
        let endPosition = sourceText.Lines.[range.EndLine - 1].Start + range.EndColumn
        TextSpan(startPosition, endPosition - startPosition)

    let getTaskAction(computation: Async<unit>) =
        // Shortcut due to nonstandard way of converting Async<unit> to Task
        let action() =
            try
                computation |> Async.RunSynchronously
            with ex ->
                Assert.Exception(ex.GetBaseException())
                raise(ex.GetBaseException())
        Action action

    let getCompletedTaskResult(task: Task<'TResult>) =
        if task.Status = TaskStatus.RanToCompletion then
            task.Result
        else
            Assert.Exception(task.Exception.GetBaseException())
            raise(task.Exception.GetBaseException())

    let supportedDiagnostics() =
        // We are constructing our own descriptors at run-time. Compiler service is already doing error formatting and localization.
        let dummyDescriptor = DiagnosticDescriptor("0", String.Empty, String.Empty, String.Empty, DiagnosticSeverity.Error, true, null, null)
        ImmutableArray.Create<DiagnosticDescriptor>(dummyDescriptor)

    let convertError(error: FSharpErrorInfo, location: Location) =
        let id = "FS" + error.ErrorNumber.ToString("0000")
        let emptyString = LocalizableString.op_Implicit("")
        let description = LocalizableString.op_Implicit(error.Message)
        let severity = if error.Severity = FSharpErrorSeverity.Error then DiagnosticSeverity.Error else DiagnosticSeverity.Warning
        let descriptor = new DiagnosticDescriptor(id, emptyString, description, error.Subcategory, severity, true, emptyString, String.Empty, null)
        Diagnostic.Create(descriptor, location)


[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Option =
    let inline ofNull value =
        if obj.ReferenceEquals(value, null) then None else Some value

    let inline ofNullable (value: Nullable<'T>) =
        if value.HasValue then Some value.Value else None

    let inline toNullable (value: 'T option) =
        match value with
        | Some x -> Nullable<_> x
        | None -> Nullable<_> ()

    let inline attempt (f: unit -> 'T) = try Some <| f() with _ -> None

    /// Gets the value associated with the option or the supplied default value.
    let inline getOrElse v =
        function
        | Some x -> x
        | None -> v

    /// Gets the option if Some x, otherwise the supplied default value.
    let inline orElse v =
        function
        | Some x -> Some x
        | None -> v

    /// Gets the value if Some x, otherwise try to get another value by calling a function
    let inline getOrTry f =
        function
        | Some x -> x
        | None -> f()

    /// Gets the option if Some x, otherwise try to get another value
    let inline orTry f =
        function
        | Some x -> Some x
        | None -> f()

    /// Some(Some x) -> Some x | None -> None
    let inline flatten x =
        match x with
        | Some x -> x
        | None -> None

    let inline toList x =
        match x with
        | Some x -> [x]
        | None -> []

    let inline iterElse someAction noneAction opt =
        match opt with
        | Some x -> someAction x
        | None   -> noneAction ()

// Async helper functions copied from https://github.com/jack-pappas/ExtCore/blob/master/ExtCore/ControlCollections.Async.fs
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Async =
    /// Transforms an Async value using the specified function.
    [<CompiledName("Map")>]
    let map (mapping : 'T -> 'U) (value : Async<'T>) : Async<'U> =
        async {
            // Get the input value.
            let! x = value
            // Apply the mapping function and return the result.
            return mapping x
        }

    // Transforms an Async value using the specified Async function.
    [<CompiledName("Bind")>]
    let bind (binding : 'T -> Async<'U>) (value : Async<'T>) : Async<'U> =
        async {
            // Get the input value.
            let! x = value
            // Apply the binding function and return the result.
            return! binding x
        }

    [<RequireQualifiedAccess>]
    module Array =
        /// Async implementation of Array.map.
        let map (mapping : 'T -> Async<'U>) (array : 'T[]) : Async<'U[]> =
            let len = Array.length array
            let result = Array.zeroCreate len

            async { // Apply the mapping function to each array element.
                for i in 0 .. len - 1 do
                    let! mappedValue = mapping array.[i]
                    result.[i] <- mappedValue

                // Return the completed results.
                return result
            }

        /// Async implementation of Array.mapi.
        let mapi (mapping : int -> 'T -> Async<'U>) (array : 'T[]) : Async<'U[]> =
            let len = Array.length array
            let result = Array.zeroCreate len

            async {
                // Apply the mapping function to each array element.
                for i in 0 .. len - 1 do
                    let! mappedValue = mapping i array.[i]
                    result.[i] <- mappedValue

                // Return the completed results.
                return result
            }

        /// Async implementation of Array.exists.
        let exists (predicate : 'T -> Async<bool>) (array : 'T[]) : Async<bool> =
            let len = Array.length array
            let rec loop i =
                async {
                    if i >= len then
                        return false
                    else
                        let! found = predicate array.[i]
                        if found then
                            return true
                        else
                            return! loop (i + 1)
                }
            loop 0

    [<RequireQualifiedAccess>]
    module List =
        let rec private mapImpl (mapping, mapped : 'U list, pending : 'T list) =
            async {
                match pending with
                | [] ->
                    // Reverse the list of mapped values before returning it.
                    return List.rev mapped

                | el :: pending ->
                    // Apply the current list element to the mapping function.
                    let! mappedEl = mapping el

                    // Cons the result to the list of mapped values, then continue
                    // mapping the rest of the pending list elements.
                    return! mapImpl (mapping, mappedEl :: mapped, pending)
                }

        /// Async implementation of List.map.
        let map (mapping : 'T -> Async<'U>) (list : 'T list) : Async<'U list> =
            mapImpl (mapping, [], list)



/// Maybe computation expression builder, copied from ExtCore library
/// https://github.com/jack-pappas/ExtCore/blob/master/ExtCore/Control.fs
[<Sealed>]
type MaybeBuilder () =
    // 'T -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Return value: 'T option = Some value

    // M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.ReturnFrom value: 'T option = value

    // unit -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Zero (): unit option =  Some ()     // TODO: Should this be None?

    // (unit -> M<'T>) -> M<'T>
    [<DebuggerStepThrough>]
    member __.Delay (f: unit -> 'T option): 'T option = f ()

    // M<'T> -> M<'T> -> M<'T>
    // or
    // M<unit> -> M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Combine (r1, r2: 'T option): 'T option =
        match r1 with
        | None -> None
        | Some () -> r2

    // M<'T> * ('T -> M<'U>) -> M<'U>
    [<DebuggerStepThrough>]
    member inline __.Bind (value, f: 'T -> 'U option): 'U option =  Option.bind f value

    // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
    [<DebuggerStepThrough>]
    member __.Using (resource: ('T :> System.IDisposable), body: _ -> _ option): _ option =
        try body resource
        finally
            if not <| obj.ReferenceEquals (null, box resource) then
                resource.Dispose ()

    // (unit -> bool) * M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member x.While (guard, body: _ option): _ option =
        if guard () then
            // OPTIMIZE: This could be simplified so we don't need to make calls to Bind and While.
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    // seq<'T> * ('T -> M<'U>) -> M<'U>
    // or
    // seq<'T> * ('T -> M<'U>) -> seq<M<'U>>
    [<DebuggerStepThrough>]
    member x.For (sequence: seq<_>, body: 'T -> unit option): _ option =
        // OPTIMIZE: This could be simplified so we don't need to make calls to Using, While, Delay.
        using (sequence.GetEnumerator()) (fun enum ->
            x.While (enum.MoveNext,
                x.Delay (fun () -> body enum.Current)))



[<Sealed>]
type AsyncMaybeBuilder () =
    [<DebuggerStepThrough>]
    member __.Return value : Async<'T option> = Some value |> async.Return

    [<DebuggerStepThrough>]
    member __.ReturnFrom value : Async<'T option> = value

    [<DebuggerStepThrough>]
    member __.ReturnFrom (value: 'T option) : Async<'T option> = async.Return value

    [<DebuggerStepThrough>]
    member __.Zero () : Async<unit option> = Some () |> async.Return

    [<DebuggerStepThrough>]
    member __.Delay (f : unit -> Async<'T option>) : Async<'T option> = f ()

    [<DebuggerStepThrough>]
    member __.Combine (r1, r2 : Async<'T option>) : Async<'T option> =
        async {
            let! r1' = r1
            match r1' with
            | None -> return None
            | Some () -> return! r2
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: Async<'T option>, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            let! value' = value
            match value' with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: 'T option, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            match value with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Using (resource : ('T :> IDisposable), body : _ -> Async<_ option>) : Async<_ option> =
        try body resource
        finally
            if isNotNull resource then resource.Dispose ()

    [<DebuggerStepThrough>]
    member x.While (guard, body : Async<_ option>) : Async<_ option> =
        if guard () then
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    [<DebuggerStepThrough>]
    member x.For (sequence : seq<_>, body : 'T -> Async<unit option>) : Async<_ option> =
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (enum.MoveNext, x.Delay (fun () -> body enum.Current)))

    [<DebuggerStepThrough>]
    member inline __.TryWith (computation : Async<'T option>, catchHandler : exn -> Async<'T option>) : Async<'T option> =
            async.TryWith (computation, catchHandler)

    [<DebuggerStepThrough>]
    member inline __.TryFinally (computation : Async<'T option>, compensation : unit -> unit) : Async<'T option> =
            async.TryFinally (computation, compensation)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncMaybe =
    let inline liftAsync (async : Async<'T>) : Async<_ option> =
        async |> Async.map Some


let maybe = MaybeBuilder()
let asyncMaybe = AsyncMaybeBuilder()

