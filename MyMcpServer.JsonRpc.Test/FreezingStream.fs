namespace MyMcpServer

open System.IO
open System.Threading
open System.Threading.Tasks

type FreezingStream() =
    inherit Stream()
    
    override _.CanRead = true
    override _.CanSeek = false
    override _.CanWrite = false
    override _.Length = 0L
    override _.Position with get() = 0L and set(_) = ()
    
    override _.Flush() = ()
    override _.Seek(_, _) = 0L
    override _.SetLength(_) = ()
    override _.Write(_, _, _) = ()
    
    override _.Read(_, _, _) = 0
    
    override _.ReadAsync(_, _, _, cancellationToken) =
        task {
            do! Task.Delay(Timeout.Infinite, cancellationToken)
            return 0
        }