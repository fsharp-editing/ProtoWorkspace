namespace ProtoWorkspace

[<AutoOpen>]
module Environment = 
    open System
    
    /// Are we running on the Mono platform?
    let runningOnMono = 
        try 
            System.Type.GetType "Mono.Runtime" <> null
        with _ -> false
    
    let ``namewith.to`` = ""
    
    /// Target framework (used to find the right version of F# binaries)
    type FSharpTargetFramework = 
        | NET_2_0
        | NET_3_0
        | NET_3_5
        | NET_4_0
        | NET_4_5
        | NET_4_6
    
    type FSharpCompilerVersion = 
        // F# 2.0
        | FSharp_2_0
        // F# 3.0
        | FSharp_3_0
        // F# 3.1
        | FSharp_3_1
        // F# 4.0
        | FSharp_4_0
        // F# 4.1
        | FSharp_4_1
        
        override x.ToString() = 
            match x with
            | FSharp_2_0 -> "4.0.0.0"
            | FSharp_3_0 -> "4.3.0.0"
            | FSharp_3_1 -> "4.3.1.0"
            | FSharp_4_0 -> "4.4.0.0"
            | FSharp_4_1 -> "4.4.1.0"
        
        /// The current requested language version can be overridden by the user using environment variable.
        static member LatestKnown = 
            match System.Environment.GetEnvironmentVariable "FSHARP_PREFERRED_VERSION" with
            | "4.0.0.0" -> FSharp_2_0
            | "4.3.0.0" -> FSharp_3_0
            | "4.3.1.0" -> FSharp_3_1
            | "4.4.0.0" -> FSharp_4_0
            | "4.4.1.0" -> FSharp_4_1
            | null | _ -> FSharp_4_0
    
    let maxPath = 260
    let maxDataLength = System.Text.UTF32Encoding().GetMaxByteCount maxPath
    let KEY_WOW64_DEFAULT = 0x0000
    let KEY_WOW64_32KEY = 0x0200
    let HKEY_LOCAL_MACHINE = UIntPtr 0x80000002u
    let KEY_QUERY_VALUE = 0x1
    let REG_SZ = 1u
