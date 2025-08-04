namespace MyMcpServer.Common

open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations

[<RequireQualifiedAccess>]
module Guard =
    
    let inline isNotNull (expr: Expr<'T>) =
        
        let name = Reflection.extractName expr        
        let value = expr |> LeafExpressionConverter.EvaluateQuotation :?> 'T
        if isNull (box value) then nullArg name