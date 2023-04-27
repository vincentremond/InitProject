module InitProject.Tests

open NUnit.Framework
open InitProject

[<Test>]
let CliTests1 () =
    Assert.AreEqual({ Destination = Some "MyProject1" }, Cli.parse [| "--destination"; "MyProject1" |])

[<Test>]
let CliTests2 () =
    Assert.AreEqual({ Destination = Some "MyProject1" }, Cli.parse [| "MyProject1" |])

[<Test>]
let CliTests3 () =
    Assert.AreEqual({ Destination = None }, Cli.parse [||])
