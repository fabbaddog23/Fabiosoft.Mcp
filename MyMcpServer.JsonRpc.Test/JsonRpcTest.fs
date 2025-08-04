module JsonRpcTest

open System
open System.IO
open System.Threading
open MyMcpServer
open Xunit
open Faqt
open Faqt.Operators

[<Fact>]
let ``Input of null throws exception`` ()=
    
    // Arrange
    let input = null
    use output = new MemoryStream()
    let methods = Object()
    
    // Act
    let act = fun () -> JsonRpc.createServer input output methods |> Async.RunSynchronously
    
    // Assert
    act.Should().Throw<ArgumentNullException,_>().Whose.ParamName.Should().Be("input")
    
[<Fact>]
let ``Output of null throws exception`` ()=
    
    // Arrange
    use input = new MemoryStream()
    let output = null
    let methods = Object()
    
    // Act
    let act = fun () -> JsonRpc.createServer input output methods |> Async.RunSynchronously
    
    // Assert
    act.Should().Throw<ArgumentNullException,_>().Whose.ParamName.Should().Be("output")
    
[<Fact>]
let ``Methods of null throws exception`` ()=
    
    // Arrange
    use input = new MemoryStream()
    use output = new MemoryStream()
    let methods = null
    
    // Act
    let act = fun () -> JsonRpc.createServer input output methods |> Async.RunSynchronously
    
    // Assert
    act.Should().Throw<ArgumentNullException,_>().Whose.ParamName.Should().Be("methods")
    
[<Fact>]
let ``Non json string returns error`` ()=
    
    // Arrange
    use input = new MemoryStream()
    use output = new MemoryStream()
    use inputWriter = new StreamWriter(input)
    use outputReader = new JsonStreamReader(output, TimeSpan.FromMilliseconds(100))
    use cancellationTokenSource = new CancellationTokenSource()
    let methods = Object()
    let server = JsonRpc.createServer input output methods
    Async.Start(server, cancellationTokenSource.Token)
    
    // Act
    inputWriter.Write("something")
    let result = outputReader.ReadJsonObject (TimeSpan.FromSeconds(1)) |> Async.RunSynchronously
    
    // Assert
    %result.Should().BeOfCase(Error).WhoseValue
           .Should().BeOfCase(JsonReaderError.UnexpectedCharacter).WhoseValue
           .Should().Be({
                Position = 1
                Expected = '{'
                Actual = Char 's'
           })
    cancellationTokenSource.Cancel()