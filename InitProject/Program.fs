open Fake.Core
open InitProject

[<EntryPoint>]
let main args =
    
    let initProjectContext = args |> Cli.parse |>  InitProjectContext.fromCliArguments
    
    []
    |> Context.FakeExecutionContext.Create false "InitProject.exe"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    Initializer.init initProjectContext |> Target.runOrDefault

    0