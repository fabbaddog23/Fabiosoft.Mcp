namespace MyMcpServer.Common

open System

[<RequireQualifiedAccess>]
module Memory =
    
    let inline create<'T> size = Array.zeroCreate<'T> size |> Memory