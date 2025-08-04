namespace MyMcpServer

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

type ServerCapabilities = {
    tools: obj option
    resources: obj option
    prompts: obj option
    logging: obj option
}

type InitializeResult = {
    protocolVersion: string
    capabilities: ServerCapabilities
    serverInfo: {| name: string; version: string |}
}

type Tool = {
    name: string
    description: string
    inputSchema: obj
}

type Resource = {
    uri: string
    name: string
    description: string option
    mimeType: string option
}

module McpServer =
    
    let mutable private initialized = false
    let private tools = ResizeArray<Tool>()
    let private resources = ResizeArray<Resource>()
    
    do
        tools.Add({
            name = "echo"
            description = "Echo back the input text"
            inputSchema = {|
                ``type`` = "object"
                properties = {|
                    text = {|
                        ``type`` = "string"
                        description = "Text to echo back"
                    |}
                |}
                required = [| "text" |]
            |}
        })
    
    do
        resources.Add({
            uri = "file://example.txt"
            name = "Example File"
            description = Some "A sample text file"
            mimeType = Some "text/plain"
        })
    //     
    // let private handleToolCall (request: JsonRpcRequest) : JsonRpcResponse =
    //     
    //     try
    //         match request.parameters with
    //         | Some params ->
    //             if params.ContainsKey("name") then
    //                 let toolName = params["name"].GetString()
    //                 match toolName with
    //                 | "echo" ->
    //                     let args = params["arguments"]
    //                     let text = args.GetProperty("text").GetString()
    //                     {
    //                         jsonrpc = "2.0"
    //                         id = request.id
    //                         result = Some({|
    //                             content = [|
    //                                 {|
    //                                     ``type`` = "text"
    //                                     text = $"Echo: {text}"
    //                                 |}
    //                             |]
    //                             isError = false
    //                         |} :> obj)
    //                         error = None
    //                     }
    //                 | _ ->
    //                     {
    //                         jsonrpc = "2.0"
    //                         id = request.id
    //                         result = None
    //                         error = Some({
    //                             code = -32602
    //                             message = "Unknown tool"
    //                             data = None
    //                         } :> obj)
    //                     }
    //             else
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = None
    //                     error = Some({
    //                         code = -32602
    //                         message = "Missing tool name"
    //                         data = None
    //                     } :> obj)
    //                 }
    //         | None ->
    //             {
    //                 jsonrpc = "2.0"
    //                 id = request.id
    //                 result = None
    //                 error = Some({
    //                     code = -32602
    //                     message = "Invalid parameters"
    //                     data = None
    //                 } :> obj)
    //             }
    //     with
    //     | ex ->
    //         {
    //             jsonrpc = "2.0"
    //             id = request.id
    //             result = None
    //             error = Some({
    //                 code = -32603
    //                 message = "Tool execution error"
    //                 data = Some(ex.Message :> obj)
    //             } :> obj)
    //         }
    //
    // let private handleResourceRead (request: JsonRpcRequest) : JsonRpcResponse =
    //     try
    //         match request.parameters with
    //         | Some params ->
    //             if params.ContainsKey("uri") then
    //                 let uri = params["uri"].GetString()
    //                 match uri with
    //                 | "file://example.txt" ->
    //                     {
    //                         jsonrpc = "2.0"
    //                         id = request.id
    //                         result = Some({|
    //                             contents = [|
    //                                 {|
    //                                     uri = uri
    //                                     mimeType = "text/plain"
    //                                     text = "This is example content from the F# MCP server!"
    //                                 |}
    //                             |]
    //                         |} :> obj)
    //                         error = None
    //                     }
    //                 | _ ->
    //                     {
    //                         jsonrpc = "2.0"
    //                         id = request.id
    //                         result = None
    //                         error = Some({
    //                             code = -32602
    //                             message = "Resource not found"
    //                             data = None
    //                         } :> obj)
    //                     }
    //             else
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = None
    //                     error = Some({
    //                         code = -32602
    //                         message = "Missing URI"
    //                         data = None
    //                     } :> obj)
    //                 }
    //         | None ->
    //             {
    //                 jsonrpc = "2.0"
    //                 id = request.id
    //                 result = None
    //                 error = Some({
    //                     code = -32602
    //                     message = "Invalid parameters"
    //                     data = None
    //                 } :> obj)
    //             }
    //     with
    //     | ex ->
    //         {
    //             jsonrpc = "2.0"
    //             id = request.id
    //             result = None
    //             error = Some({
    //                 code = -32603
    //                 message = "Resource read error"
    //                 data = Some(ex.Message :> obj)
    //             } :> obj)
    //         }
    //
    // let handleRequest (request: JsonRpcRequest) : JsonRpcResponse =
    //     
    //     try
    //         match request.method with
    //         | "initialize" ->
    //             initialized <- true
    //             {
    //                 jsonrpc = "2.0"
    //                 id = request.id
    //                 result = Some({
    //                     protocolVersion = "2024-11-05"
    //                     capabilities = {
    //                         tools = Some {| listChanged = false |}
    //                         resources = Some {| subscribe = false; listChanged = false |}
    //                         prompts = None
    //                         logging = None
    //                     }
    //                     serverInfo = {| name = "F# MCP Server"; version = "1.0.0" |}
    //                 } :> obj)
    //                 error = None
    //             }
    //         
    //         | "notifications/initialized" ->
    //             // Client confirms initialization is complete
    //             {
    //                 jsonrpc = "2.0"
    //                 id = request.id
    //                 result = Some(obj())
    //                 error = None
    //             }
    //         
    //         | "tools/list" ->
    //             if not initialized then
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = None
    //                     error = Some({
    //                         code = -32002
    //                         message = "Server not initialized"
    //                         data = None
    //                     } :> obj)
    //                 }
    //             else
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = Some({| tools = tools.ToArray() |} :> obj)
    //                     error = None
    //                 }
    //         
    //         | "tools/call" ->
    //             if not initialized then
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = None
    //                     error = Some({
    //                         code = -32002
    //                         message = "Server not initialized"
    //                         data = None
    //                     } :> obj)
    //                 }
    //             else
    //                 handleToolCall request
    //         
    //         | "resources/list" ->
    //             if not initialized then
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = None
    //                     error = Some({
    //                         code = -32002
    //                         message = "Server not initialized"
    //                         data = None
    //                     } :> obj)
    //                 }
    //             else
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = Some({| resources = resources.ToArray() |} :> obj)
    //                     error = None
    //                 }
    //         
    //         | "resources/read" ->
    //             if not initialized then
    //                 {
    //                     jsonrpc = "2.0"
    //                     id = request.id
    //                     result = None
    //                     error = Some({
    //                         code = -32002
    //                         message = "Server not initialized"
    //                         data = None
    //                     } :> obj)
    //                 }
    //             else
    //                 handleResourceRead request
    //         
    //         | _ ->
    //             {
    //                 jsonrpc = "2.0"
    //                 id = request.id
    //                 result = None
    //                 error = Some({
    //                     code = -32601
    //                     message = "Method not found"
    //                     data = None
    //                 } :> obj)
    //             }
    //     with
    //     | ex ->
    //         {
    //             jsonrpc = "2.0"
    //             id = request.id
    //             result = None
    //             error = Some({
    //                 code = -32603
    //                 message = "Internal error"
    //                 data = Some(ex.Message :> obj)
    //             } :> obj)
    //         }
    //
    
    
    let runStdio () =
        async {
            
            return ()
            
            // let jsonOptions = JsonSerializerOptions()
            // jsonOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
            // jsonOptions.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
            //
            // use stdin = Console.OpenStandardInput()
            // use stdout = Console.OpenStandardOutput()
            // use reader = new StreamReader(stdin)
            // use writer = new StreamWriter(stdout)
            // writer.AutoFlush <- true
            //
            // while not reader.EndOfStream do
            //     try
            //         let! line = reader.ReadLineAsync() |> Async.AwaitTask
            //         if not (String.IsNullOrWhiteSpace(line)) then
            //             let request = JsonSerializer.Deserialize<JsonRpcRequest>(line, jsonOptions)
            //             let response = handleRequest request
            //             let responseJson = JsonSerializer.Serialize(response, jsonOptions)
            //             do! writer.WriteLineAsync(responseJson) |> Async.AwaitTask
            //     with
            //     | ex ->
            //         eprintfn "Error processing request: %s" ex.Message
        }
