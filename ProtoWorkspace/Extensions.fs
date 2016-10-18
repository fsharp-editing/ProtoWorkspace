[<AutoOpen>]
module ProtoWorkspace.Extensions

open Microsoft.Extensions.Logging

type ILogger with
    member self.LogInfofn msg = Printf.ksprintf (fun s -> self.LogInformation(s, [||])) msg
    member self.LogCriticalfn msg = Printf.ksprintf (fun s -> self.LogCritical(s, [||])) msg
    member self.LogDebugfn msg = Printf.ksprintf (fun s -> self.LogDebug(s, [||])) msg
    member self.LogTracefn msg = Printf.ksprintf (fun s -> self.LogTrace(s, [||])) msg
    member self.LogErrorfn msg = Printf.ksprintf (fun s -> self.LogError(s, [||])) msg
    member self.LogWarningfn msg = Printf.ksprintf (fun s -> self.LogWarning(s, [||])) msg
