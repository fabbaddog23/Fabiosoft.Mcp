namespace MyMcpServer.Common

open FSharp.Control

[<RequireQualifiedAccess>]
module AsyncSeq =
    
    let enumerator (asyncSeq: AsyncSeq<'T>) =
        asyncSeq |> AsyncSeq.toAsyncEnum |> _.GetAsyncEnumerator()        