[<AutoOpen>]
module ProtoWorkspace.Prelude
open System
open System.Diagnostics

type FileName = string
type FilePath = string

let inline debugfn msg = Printf.kprintf Debug.WriteLine msg
let inline failfn msg = Printf.kprintf Debug.Fail msg
let inline isNull v = match v with | null -> true | _ -> false
let inline isNotNull v = not (isNull v)
let inline dispose (disposable:#IDisposable) = disposable.Dispose ()

let inline Ok a = Choice1Of2 a
let inline Fail a = Choice2Of2 a
let inline (|Ok|Fail|) a = a

/// String Equals Ordinal Ignore Case
let (|EqualsIC|_|) (str:string) arg =
  if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0
  then Some () else None

/// Null coalescing operator
let ( <?> ) a b = if isNull a then b else a

/// If arg is null raise an `ArgumentNullException` with the argname
let inline checkNullArg arg argName =
    if isNull arg then nullArg argName 


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
    let inline orElse v = function
        | Some x -> Some x
        | None -> v

    /// Gets the value if Some x, otherwise try to get another value by calling a function
    let inline getOrTry f = function
        | Some x -> x
        | None -> f()

    /// Gets the option if Some x, otherwise try to get another value
    let inline orTry f = function
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


[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module Array =
    let inline private checkNonNull argName arg = 
        match box arg with 
        | null -> nullArg argName 
        | _ -> ()

/// Optimized arrays equality. ~100x faster than `array1 = array2` on strings.
    /// ~2x faster for floats
    /// ~0.8x slower for ints
    let inline areEqual (xs: 'T []) (ys: 'T []) =
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
    let inline isSubArray (subArray: 'T []) (wholeArray:'T []) index = 
        if isNull subArray || isNull wholeArray then false
        elif subArray.Length = 0 then true
        elif subArray.Length > wholeArray.Length then false
        elif subArray.Length = wholeArray.Length then areEqual subArray wholeArray else
        let rec loop subidx idx =
            if subidx = subArray.Length then true 
            elif subArray.[subidx] = wholeArray.[idx] then loop (subidx+1) (idx+1) 
            else false
        loop 0 index

    /// Returns true if one array has another as its subset from index 0.
    let startsWith (prefix: _ []) (whole: _ []) =
        isSubArray prefix whole 0

    /// Returns true if one array has trailing elements equal to another's.
    let endsWith (suffix: _ []) (whole: _ []) =
        isSubArray suffix whole (whole.Length-suffix.Length)

    /// Returns a new array with an element replaced with a given value.
    let replace index value (array: _ []) =
        checkNonNull "array" array
        if index >= array.Length then raise (IndexOutOfRangeException "index")
        let res = Array.copy array
        res.[index] <- value
        res

    /// Returns all heads of a given array.
    /// For [|1;2;3|] it returns [|[|1; 2; 3|]; [|1; 2|]; [|1|]|]
    let heads (array: 'T []) =
        checkNonNull "array" array
        let res = Array.zeroCreate<'T[]> array.Length
        for i = array.Length - 1 downto 0 do
            res.[i] <- array.[0..i]
        res

    /// Fold over the array passing the index and element at that index to a folding function
    let foldi (folder: 'State -> int -> 'T -> 'State) (state: 'State) (array: 'T []) =
        checkNonNull "array" array
        if array.Length = 0 then state else
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt folder
        let mutable state:'State = state
        let len = array.Length
        for i = 0 to len - 1 do
            state <- folder.Invoke (state, i, array.[i])
        state

    /// pass an array byref to reverse it in place
    let revInPlace (array: 'T []) =
        checkNonNull "array" array
        if areEqual array [||] then () else
        let arrlen, revlen = array.Length-1, array.Length/2 - 1
        for idx in 0 .. revlen do
            let t1 = array.[idx] 
            let t2 = array.[arrlen-idx]
            array.[idx] <- t2
            array.[arrlen-idx] <- t1

    /// Map all elements of the array that satisfy the predicate
    let filterMap predicate mapfn (array: 'T [])  =
        checkNonNull "array" array
        if array.Length = 0 then [||] else
        let result = Array.zeroCreate array.Length
        let mutable count = 0
        for elm in array do
            if predicate elm then 
               result.[count] <- mapfn elm
               count <- count + 1
        if count = 0 then [||] else
        result.[0..count-1]


[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module String =
    let inline toCharArray (str:string) = str.ToCharArray()

    let lowerCaseFirstChar (str: string) =
        if String.IsNullOrEmpty str 
         || Char.IsLower(str, 0) then str else 
        let strArr = toCharArray str
        match Array.tryHead strArr with
        | None -> str
        | Some c  -> 
            strArr.[0] <- Char.ToLower c
            String strArr
    
    let inline contains (target:string) (str:string) =
        str.Contains target

    let inline equalsIgnoreCase (str1:string) (str2:string) =
        str1.Equals(str2, StringComparison.OrdinalIgnoreCase)

    let extractTrailingIndex (str: string) =
        match str with
        | null -> null, None
        | _ ->
            let charr = str.ToCharArray() 
            Array.revInPlace charr
            let digits = Array.takeWhile Char.IsDigit charr
            Array.revInPlace digits
            String digits |> function
            | "" -> str, None
            | index -> str.Substring (0, str.Length - index.Length), Some (int index)

    /// Remove all trailing and leading whitespace from the string
    /// return null if the string is null
    let trim (value: string) = if isNull value then null else value.Trim()
    
    /// Splits a string into substrings based on the strings in the array separators
    let split options (separator: string []) (value: string) = 
        if isNull value  then null else value.Split(separator, options)

    let (|StartsWith|_|) pattern value =
        if String.IsNullOrWhiteSpace value then None
        elif value.StartsWith pattern then Some ()
        else None

    let (|Contains|_|) pattern value =
        if String.IsNullOrWhiteSpace value then None
        elif value.Contains pattern then Some ()
        else None
    
    open System.IO

    let getLines (str: string) =
        use reader = new StringReader(str)
        [|  let line = ref (reader.ReadLine())
            while isNotNull (!line) do
                yield !line
                line := reader.ReadLine()
            if str.EndsWith "\n" then
            // last trailing space not returned
            // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
                yield String.Empty
        |]

    let getNonEmptyLines (str: string) =
        use reader = new StringReader(str)
        [|  let line = ref (reader.ReadLine())
            while isNotNull (!line) do
                if (!line).Length > 0 then yield !line
                line := reader.ReadLine()
        |]

    /// Parse a string to find the first nonempty line
    /// Return null if the string was null or only contained empty lines
    let firstNonEmptyLine (str: string) =
        use reader = new StringReader (str)
        let rec loop (line:string) =
            if isNull line then None 
            elif  line.Length > 0 then Some line
            else loop (reader.ReadLine())
        loop (reader.ReadLine())

open System.Text
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StringBuilder =
    /// Pipelining function for appending a string to a stringbuilder
    let inline append (str:string) (sb:StringBuilder) = sb.Append str

    /// Pipelining function for appending a string with a '\n' to a stringbuilder
    let inline appendLine (str:string) (sb:StringBuilder) = sb.AppendLine str
    
    /// SideEffecting function for appending a string to a stringbuilder
    let inline appendi (str:string) (sb:StringBuilder) = sb.Append str |> ignore

    /// SideEffecting function for appending a string with a '\n' to a stringbuilder
    let inline appendLinei (str:string) (sb:StringBuilder) = sb.AppendLine str |> ignore

[<RequireQualifiedAccess>]
module Dict = 
    open System.Collections.Generic

    let add key value (dict: #IDictionary<_,_>) =
        dict.[key] <- value
        dict

    let remove (key: 'k) (dict: #IDictionary<'k,_>) =
        dict.Remove key |> ignore
        dict

    let tryFind key (dict: #IDictionary<'k, 'v>) = 
        let mutable value = Unchecked.defaultof<_>
        if dict.TryGetValue (key, &value) then Some value
        else None

    let ofSeq (xs: ('k * 'v) seq) = 
        let dict = Dictionary()
        for k, v in xs do dict.[k] <- v
        dict


module PropertyConverter =

    // TODO - railway this
    let toGuid propertyValue =
        match Guid.TryParse propertyValue with
        | true, value -> value
        | _ -> failwithf "Couldn't parse '%s' into a Guid" propertyValue
    
    let toDefineConstants propertyValue =
        if String.IsNullOrWhiteSpace propertyValue then [||] else
        propertyValue.Split([|';'|], StringSplitOptions.RemoveEmptyEntries)



    // TODO - railway this
    let toBoolean propertyValue =
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



