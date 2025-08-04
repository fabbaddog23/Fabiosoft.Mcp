module JsonStreamWriterTests

open FSharp.Data
open System.IO
open MyMcpServer
open Xunit
open Faqt
open Faqt.Operators

type InvalidJsonObjectData() as this =
    inherit TheoryData<JsonValue>()
    do
        this.Add(JsonValue.Null)
        this.Add(JsonValue.Array [||])
        this.Add(JsonValue.Boolean false)
        this.Add(JsonValue.Boolean true)
        this.Add(JsonValue.Float 1.0)
        this.Add(JsonValue.Number 5.0M)
        this.Add(JsonValue.String "test")
        this.Add(JsonValue.Record [|
            ("", JsonValue.Null)
        |])
        this.Add(JsonValue.Record [|
            ("p", JsonValue.Number 5M)
            ("p", JsonValue.Number 5M)
        |])

[<Theory>]
[<ClassData(typeof<InvalidJsonObjectData>)>]
let ``Writing invalid json value returns error`` (jsonValue: JsonValue) =
    
    // Arrange
    use stream = new MemoryStream()
    use jsonWriter = new JsonStreamWriter(stream)
    
    // Act
    let result = jsonWriter.WriteJsonObject jsonValue |> Async.RunSynchronously

    // Assert
    %result.Should().BeOfCase(Error).WhoseValue
           .Should().BeOfCase(JsonWriterError.InvalidJsonObject).WhoseValue
           .Should().Transform(snd).That.Should().Be(jsonValue)

[<Theory>]
[<ClassData(typeof<InvalidJsonObjectData>)>]
let ``Writing invalid json value does not change stream`` (jsonValue: JsonValue) =
    
    // Arrange
    use stream = new MemoryStream()
    use jsonWriter = new JsonStreamWriter(stream)
    
    // Act
    let _ = jsonWriter.WriteJsonObject jsonValue |> Async.RunSynchronously

    // Assert
    stream.Position <- 0L
    use reader = new StreamReader(stream)
    let jsonString = reader.ReadToEnd()
    
    %jsonString.Should().Be("")

type ValidJsonObjectData() as this =
    inherit TheoryData<JsonValue>()
    do
        this.Add(JsonValue.Record [|
            ("p", JsonValue.String "v")
        |])
        this.Add(JsonValue.Record [|
            ("p1", JsonValue.String "v")
            ("p2", JsonValue.Number 123M)
            ("p3", JsonValue.Boolean true)
        |])
        this.Add(JsonValue.Record [|
            ("p", JsonValue.Null)
        |])

[<Theory>]
[<ClassData(typeof<ValidJsonObjectData>)>]
let ``Writing valid json returns ok`` (jsonValue: JsonValue) =
    
    // Arrange
    use stream = new MemoryStream()
    use jsonWriter = new JsonStreamWriter(stream)
    
    // Act
    let result = jsonWriter.WriteJsonObject jsonValue |> Async.RunSynchronously

    // Assert
    %result.Should().BeOfCase(Ok)
    
[<Theory>]
[<ClassData(typeof<ValidJsonObjectData>)>]
let ``Writing valid json writes json to stream`` (jsonValue: JsonValue) =
    
    // Arrange
    use stream = new MemoryStream()
    use jsonWriter = new JsonStreamWriter(stream)
    
    // Act
    let _ = jsonWriter.WriteJsonObject jsonValue |> Async.RunSynchronously

    // Assert
    stream.Position <- 0L
    use reader = new StreamReader(stream)
    let jsonString = reader.ReadToEnd()
    let json = JsonValue.Parse(jsonString)
    
    %json.Should().Be(jsonValue)