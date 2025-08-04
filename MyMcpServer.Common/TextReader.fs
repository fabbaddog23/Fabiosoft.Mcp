namespace MyMcpServer.Common

open System
open System.IO
open FSharpx

type Cancelled = | Cancelled

[<RequireQualifiedAccess>]
module TextReader =
    
    let read buffer cancellationToken (reader: TextReader) =
    
        async {
            try    
                return! reader.ReadAsync(buffer, cancellationToken)
                        |> _.AsTask()
                        |> Async.AwaitTask
                        |> Async.map Ok
            with
            | :? OperationCanceledException -> return Cancelled |> Error
        }