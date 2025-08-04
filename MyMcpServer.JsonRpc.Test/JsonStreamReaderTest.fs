module JsonStreamReaderTests

open System
open System.Text
open System.Threading
open FSharp.Data
open System.IO
open MyMcpServer
open Xunit
open Faqt
open Faqt.Operators

[<Fact>]
let ``Reading without input returns timeout`` () =
    
    // Arrange
    use stream = new MemoryStream()
    use jsonReader = new JsonStreamReader(stream, TimeSpan.FromMilliseconds(50))
    
    // Act
    let act = fun () -> Async.RunSynchronously(jsonReader.ReadJsonObject(), 100, CancellationToken.None)

    // Assert
    %act.Should().Throw<TimeoutException, _>()
    
type WhitespaceData() as this =
    inherit TheoryData<string>()
    do
        this.Add("")
        this.Add(" ")
        this.Add("\t")
        this.Add("\n")
        this.Add("\r")
        this.Add("   ")
        this.Add("\t\t\t")
        this.Add("\n\n\n")
        this.Add("\r\r\r")
        this.Add(" \t\n\r ")
        this.Add("   \t\n\r   ")
    
[<Theory>]
[<ClassData(typeof<WhitespaceData>)>]
let ``Reading with whitespace input returns timeout`` (input: string) =
    
    // Arrange
    let data = Encoding.UTF8.GetBytes(input)
    use stream = new MemoryStream(data)
    use jsonReader = new JsonStreamReader(stream, TimeSpan.FromMilliseconds(50))
    
    // Act
    let act = fun () -> Async.RunSynchronously(jsonReader.ReadJsonObject(), 100, CancellationToken.None)

    // Assert
    %act.Should().Throw<TimeoutException, _>()    

type ValidJsonObjectData() as this =
    inherit TheoryData<string, (string * JsonValue) array>()
    do
        this.Add("{}", [||])
        this.Add("   {}   ", [||])
        this.Add("\t{} \t ", [||])
        this.Add("\n{}\n", [||])
        this.Add("\r{}\r", [||])
        this.Add("""{ "abcdefg": "123" }""", [| "abcdefg", JsonValue.String "123" |])
        this.Add("""{ "abcdefg": 123 }""", [| "abcdefg", JsonValue.Number 123m |])
        this.Add("""{"arr": []}""", [| "arr", JsonValue.Array [||] |])
        this.Add("""{"arr": [1, 2, 3]}""", [| "arr", JsonValue.Array [| JsonValue.Number 1m; JsonValue.Number 2m; JsonValue.Number 3m |] |])
        this.Add("""{"nested": {"key": "value"}}""", [| "nested", JsonValue.Record [| "key", JsonValue.String "value" |] |])
        this.Add("""{"mixed": {"key1": "value1", "key2": 42, "key3": [1, 2, 3]}}""",
                 [| "mixed", JsonValue.Record [| "key1", JsonValue.String "value1"; "key2", JsonValue.Number 42m; "key3", JsonValue.Array [| JsonValue.Number 1m; JsonValue.Number 2m; JsonValue.Number 3m |] |] |])

[<Theory>]
[<ClassData(typeof<ValidJsonObjectData>)>]
let ``Reading with valid object returns object`` (str: string, expected: (string * JsonValue) array) =
    
    // Arrange
    let data = Encoding.UTF8.GetBytes(str)
    let stream = new MemoryStream(data)
    use jsonReader = new JsonStreamReader(stream, TimeSpan.FromMilliseconds(100))
    
    // Act
    let result = jsonReader.ReadJsonObject() |> Async.RunSynchronously
    
    // Assert
    %result.Should().BeOfCase(Ok).WhoseValue.Should().Be(JsonValue.Record expected)
    
type UnexpectedCharacterData() as this =
    inherit TheoryData<string, Character, char, int>()
    do
        this.Add("{", EndOfStream, '}', 1)
        this.Add("}", Char '}', '{', 0)
        this.Add("ieirtang", Char 'i', '{', 0)
        this.Add("{", EndOfStream, '}', 1)
        this.Add("""{ "abcdefg": """, EndOfStream, '}', 13)
        this.Add("""{ "abcdefg": "123" """, EndOfStream, '}', 19)
        this.Add("{{ ", EndOfStream, '}', 3)
        this.Add("{{} ", EndOfStream, '}', 4)
        this.Add("""{  "abcdefg": "123" {""", EndOfStream, '}', 21)

[<Theory>]
[<ClassData(typeof<UnexpectedCharacterData>)>]
let ``Reading with unexpected character`` (input: string, unexpectedCharacter: Character, expected: char, index: int) =
    
    // Arrange
    let data = Encoding.UTF8.GetBytes(input)
    let stream = new MemoryStream(data)
    use jsonReader = new JsonStreamReader(stream, TimeSpan.FromMilliseconds(100))
    
    // Act
    let result = jsonReader.ReadJsonObject() |> Async.RunSynchronously
    
    // Assert
    %result.Should().BeOfCase(Error).WhoseValue.Should().BeOfCase(JsonReaderError.UnexpectedCharacter).WhoseValue.Should().Be(
        { Actual = unexpectedCharacter
          Expected = expected
          Position = index })

type InvalidJsonData() as this =
    inherit TheoryData<string, int>()
    do
        this.Add("""{ "test": a }""", 10)
        this.Add("""{ "test": 123, "test2": }""", 24)
        this.Add("""{ "test": 123 "test2": "value" }""", 14)
        this.Add("""{"test":[}""", 9)
        this.Add("""{ "test": 123, "test2": "value", }""", 33)
        this.Add("""{ "test": 123, "test2": "value", "test3": }""", 42)

[<Theory>]
[<ClassData(typeof<InvalidJsonData>)>]
let ``Reading with invalid json`` (input: string, index: int) =
    
    // Arrange
    let data = Encoding.UTF8.GetBytes(input)
    let stream = new MemoryStream(data)
    use jsonReader = new JsonStreamReader(stream, TimeSpan.FromMilliseconds(100))
    
    // Act
    let result = jsonReader.ReadJsonObject() |> Async.RunSynchronously
    
    // Assert
    %result.Should().BeOfCase(Error)
         .WhoseValue.Should().BeOfCase(JsonReaderError.InvalidJson)
         .WhoseValue.Should().Contain(index.ToString()).And.Contain(input)

[<Fact>]
let ``Reading with freezing stream returns timeout`` () =
    
    // Arrange
    let stream = new FreezingStream()
    use jsonReader = new JsonStreamReader(stream, TimeSpan.FromMilliseconds(50))
    
    // Act
    let act = fun () -> Async.RunSynchronously(jsonReader.ReadJsonObject(), 100, CancellationToken.None)

    // Assert
    %act.Should().Throw<TimeoutException, _>()
    
[<Fact>]
let ``Read three json objects`` () =
    
    // Arrange    
    let data = Encoding.UTF8.GetBytes("""{"key1": "value1"}{"key2": "value2"}{"key3": "value3"}""")
    let stream = new MemoryStream(data)
    use jsonReader = new JsonStreamReader(stream, TimeSpan.FromMilliseconds(100))
    
    // Act
    let result1 = jsonReader.ReadJsonObject() |> Async.RunSynchronously
    let result2 = jsonReader.ReadJsonObject() |> Async.RunSynchronously
    let result3 = jsonReader.ReadJsonObject() |> Async.RunSynchronously
    
    // Assert
    %result1.Should().BeOfCase(Ok).WhoseValue.Should().Be(JsonValue.Record [| "key1", JsonValue.String "value1" |])
    %result2.Should().BeOfCase(Ok).WhoseValue.Should().Be(JsonValue.Record [| "key2", JsonValue.String "value2" |])
    %result3.Should().BeOfCase(Ok).WhoseValue.Should().Be(JsonValue.Record [| "key3", JsonValue.String "value3" |])