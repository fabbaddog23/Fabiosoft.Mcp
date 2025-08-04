open MyMcpServer

[<EntryPoint>]
let main argv =
    
    printfn "Starting F# MCP Server..."
    eprintfn "F# MCP Server started - communicating via stdio"
    
    McpServer.runStdio () |> Async.RunSynchronously
    0