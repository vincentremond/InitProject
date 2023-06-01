open Fake.Core
open InitProject

let args = System.Environment.GetCommandLineArgs().[1..]
let initProjectContext = args |> Cli.parse |> InitProjectContext.fromCliArguments

[]
|> Context.FakeExecutionContext.Create false "InitProject.exe"
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

Initializer.init initProjectContext |> Target.runOrDefault
