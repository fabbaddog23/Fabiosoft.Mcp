namespace MyMcpServer

open System.IO
open MyMcpServer.Common

[<RequireQualifiedAccess>]
module JsonRpc =
    
    let createServer<'T> (input: Stream) (output: Stream) (methods: 'T) : Async<unit> =
  
        async {
            Guard.isNotNull <@ input @>   
            Guard.isNotNull <@ output @>
            Guard.isNotNull <@ methods @>
        }
        
        // use inputReader = new JsonStreamReader(input)
        
        
        