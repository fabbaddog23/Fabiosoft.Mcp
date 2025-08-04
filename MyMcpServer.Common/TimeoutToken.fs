namespace MyMcpServer.Common

open System
open System.Threading

type TimeoutToken(timeout: TimeSpan) =
    let mutable tokenSource = new CancellationTokenSource(timeout)

    member _.Token = tokenSource.Token

    member _.IsTimedOut = tokenSource.IsCancellationRequested

    member _.Reset() =
        tokenSource.Dispose()
        tokenSource <- new CancellationTokenSource(timeout)

    interface IDisposable with
        member _.Dispose() = tokenSource.Dispose()