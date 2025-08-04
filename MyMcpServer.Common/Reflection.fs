namespace MyMcpServer.Common

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

[<RequireQualifiedAccess>]
module Reflection =
    
    let rec extractName (expr: Expr) : string =
        match expr with
        | Var(var) -> var.Name
        | PropertyGet(_, propInfo, _) -> propInfo.Name
        | FieldGet(_, fieldInfo) -> fieldInfo.Name
        | ValueWithName(_, _, name) -> name
        | Call(_, methodInfo, _) -> methodInfo.Name
        | Application(func, _) -> extractName func
        | Lambda(var, _) -> var.Name
        | Let(var, _, _) -> var.Name
        | _ -> failwith $"Could not extract name from expression '{expr}'"