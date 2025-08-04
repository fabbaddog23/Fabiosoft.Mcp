namespace MyMcpServer.Common

open FSharpPlus

module Operators =

    let inline (>!>) f g = f >> map g