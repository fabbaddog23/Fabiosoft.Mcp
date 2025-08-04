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

[<RequireQualifiedAccess>]
type JsonWriterError =
     | InvalidJsonObject of string * JsonValue 

type JsonStreamWriter(stream: Stream) =

    let writer =
        let writer = new StreamWriter(stream, Encoding.UTF8, 1024, true)
        writer.AutoFlush <- true
        writer
        
    let validateProperties (properties: (string * JsonValue) array) =
        
        match properties with
        | [||] ->
            failwith "Json object must have at least one property!"
        
        | _ when Array.exists (fun (key, _) -> String.IsNullOrWhiteSpace key) properties ->
            failwith "Json object properties must have non-empty keys!"
            
        | v when Array.distinctBy fst v |> Array.length <> v.Length ->
            failwith "Json object properties must have unique keys!"
        | _ -> ()

    member _.WriteJsonObject(json: JsonValue) : Async<Result<unit, JsonWriterError>> =
        
        match json with
        | JsonValue.Record properties as record ->
            try
                validateProperties properties
                record.WriteTo(writer, JsonSaveOptions.None) |> AsyncResult.ok
            with ex ->
                JsonWriterError.InvalidJsonObject (ex.Message,json) |> AsyncResult.error
            
        | v -> JsonWriterError.InvalidJsonObject ("Only json objects are allowed!",v) |> AsyncResult.error

    interface IDisposable with

        member _.Dispose() = writer.Dispose()