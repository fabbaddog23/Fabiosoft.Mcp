namespace MyMcpServer.Common

open System.Collections.Generic
open FSharpx

[<RequireQualifiedAccess>]
module AsyncEnumerator =
    
    let next (enumerator: IAsyncEnumerator<'T>) =
            enumerator.MoveNextAsync()
            |> _.AsTask()
            |> Async.AwaitTask
            |> Async.map (fun hasNext -> if hasNext then Some enumerator.Current else None)
            
    let peek (enumerator: IAsyncEnumerator<'T>) = enumerator.Current