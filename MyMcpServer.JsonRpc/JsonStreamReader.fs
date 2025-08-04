namespace MyMcpServer

open System
open System.IO
open System.Text
open System.Threading
open FSharp.Data
open FSharp.Control
open FsToolkit.ErrorHandling
open FSharpx
open MyMcpServer.Common
open System.Collections.Generic
open FSharpPlus

open Operators

type Character =
    | Char of char
    | EndOfStream

type UnexpectedCharacter = {
    Expected: char
    Actual: Character
    Position: int
}

[<RequireQualifiedAccess>]
type JsonReaderError =
     | UnexpectedCharacter of UnexpectedCharacter
     | InvalidJson of string    

type private CharacterStream = {
    Source: IAsyncEnumerator<char option>
    Destination: StringBuilder
}

[<RequireQualifiedAccess>]
module private CharacterStream =
    
    let consume (c: char) (stream: CharacterStream) =
        
        stream.Destination.Append(c) |> ignore        
        stream.Source |> AsyncEnumerator.next |> Async.map (fun _ -> stream)

    let peek (stream: CharacterStream) =
        AsyncEnumerator.peek stream.Source

    let position (stream: CharacterStream) =
        stream.Destination.Length

module private JsonReader =
    
    let private peek stream =
    
        stream
        |> CharacterStream.peek
        |> function
           | None -> EndOfStream
           | Some c -> Char c
        
    let private mapCharacter f stream =
        stream
        |> peek 
        |> function
           | EndOfStream -> stream |> Async.returnM
           | Char c -> f c stream

    let private consumeCharacter = mapCharacter CharacterStream.consume

    let rec private consumeAsLong condition =
        
        mapCharacter (fun c ->
            if condition c then
                consumeCharacter >=> consumeAsLong condition
            else
                Async.returnM
        )
         
    let private consumeWhitespace = consumeAsLong Char.IsWhiteSpace

    let private unexpectedCharacter expected stream =
        JsonReaderError.UnexpectedCharacter {
            Expected = expected
            Actual = stream |> peek
            Position = CharacterStream.position stream
        }
        |> Error
    
    let private expect expected stream =
        
        async {
            
            do! consumeWhitespace stream |> Async.Ignore

            let result =
                peek stream
                |> function
                   | Char c when c = expected ->
                        stream |> consumeCharacter |> Async.map Ok
                   | _ ->    
                        unexpectedCharacter expected stream |> Async.returnM
            
            do! consumeWhitespace stream |> Async.Ignore
            
            return! result
        }
        
    let rec private waitFor expected timeout stream =
        
        let interval = TimeSpan.FromMilliseconds(100)
        
        if timeout |> Option.exists (fun t -> t <= TimeSpan.Zero) then
            TimeoutException() |> raise

        async {
            match peek stream with
            | Char c when c = expected -> return! consumeCharacter stream |> Async.map Ok
            | Char c ->
                if Char.IsWhiteSpace c then
                    return! stream
                            |> consumeCharacter
                            >>= waitFor expected timeout
                else
                    return unexpectedCharacter expected stream
            | EndOfStream ->
                do! Async.Sleep(interval)
                let timeout = timeout |> Option.map (fun t -> t - interval)
                return! waitFor expected timeout stream
        }
        
    let rec private readUntil endChars stream =
        
        match peek stream with
        | EndOfStream -> stream |> Async.returnM
        | Char c when (List.contains c endChars) -> stream |> Async.returnM
        | Char _ ->
            stream
            |> consumeCharacter
            >>= readUntil endChars

    let rec private matchJsonObject timeout =                
        
        waitFor '{' timeout
        >> AsyncResult.bind (readUntil ['{';'}'] >!> Ok)
        >> AsyncResult.bind (fun s ->
            match peek s with
            | Char '}' -> s |> Ok |> Async.returnM
            | Char '{' -> matchJsonObject timeout s
            | _ -> unexpectedCharacter '}' s |> Async.returnM
            )            
        >> AsyncResult.bind (expect '}')
    
    let waitForJsonObject timeout source =
        
        {
            Source = source
            Destination = StringBuilder()
        }
        |> matchJsonObject timeout
        |> AsyncResult.map _.Destination.ToString()

type JsonStreamReader(stream: Stream, timeout: TimeSpan) =
        
    let bufferSize = 1024
    
    let reader = new StreamReader(stream)

    let charSequence=
            asyncSeq {
            
                let buffer = Memory.create bufferSize
                use timeoutToken = new TimeoutToken(timeout)
                
                while not timeoutToken.IsTimedOut do

                    let! readResult = TextReader.read buffer timeoutToken.Token reader
                
                    match readResult with
                    | Ok bytesRead ->
                        if bytesRead > 0 then
                            timeoutToken.Reset()
                        for i in 0 .. bytesRead - 1 do
                            yield buffer.Span[i] |> Some                             
                    | Error Cancelled -> ()

                yield None
            }

    let initializeChars () =
        let result = charSequence |> AsyncSeq.enumerator
        AsyncEnumerator.next result
        |> Async.map (fun _ -> result)
        
    let chars = 
        initializeChars ()
        |> Async.RunSynchronously
        
    let parseJson str =

        try
            str |> JsonValue.Parse |> Ok
        with ex ->
            ex.Message |> JsonReaderError.InvalidJson |> Error

    member _.ReadJsonObject(?timeout: TimeSpan) =
        chars |> JsonReader.waitForJsonObject timeout |> Async.map (Result.bind parseJson)

    interface IDisposable with
    
        member _.Dispose() = reader.Dispose()