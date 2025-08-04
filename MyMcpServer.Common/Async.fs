namespace MyMcpServer.Common

module Async =

    let tryWith (handler: exn -> 'T) (computation: Async<'T>) = async {
        try
            return! computation
        with
        | ex -> return handler ex
    }        