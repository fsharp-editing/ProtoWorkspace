module ProtoWorkspace.Utilities

[<Sealed>]
/// <summary>
/// A lightweight mutual exclusion object which supports waiting with cancellation and prevents
/// recursion (i.e. you may not call Wait if you already hold the lock)
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="NonReentrantLock"/> provides a lightweight mutual exclusion class that doesn't
/// use Windows kernel synchronization primitives.
/// </para>
/// <para>
/// The implementation is distilled from the workings of <see cref="SemaphoreSlim"/>
/// The basic idea is that we use a regular sync object (Monitor.Enter/Exit) to guard the setting
/// of an 'owning thread' field. If, during the Wait, we find the lock is held by someone else
/// then we register a cancellation callback and enter a "Monitor.Wait" loop. If the cancellation
/// callback fires, then it "pulses" all the waiters to wake them up and check for cancellation.
/// Waiters are also "pulsed" when leaving the lock.
/// </para>
/// <para>
/// All public members of <see cref="NonReentrantLock"/> are thread-safe and may be used concurrently
/// from multiple threads.
/// </para>
/// </remarks>
type internal NonReentrantLock(?useThisInstanceForSynchronization:bool) =
    let useThisInstanceForSynchronization = defaultArg useThisInstanceForSynchronization false
    /// <summary>
    /// A synchronization object to protect access to the <see cref="_owningThreadId"/> field and to be pulsed
    /// when <see cref="Release"/> is called and during cancellation.
    /// </summary>
    let syncLock = obj()

