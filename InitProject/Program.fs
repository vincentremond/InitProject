open Fake.Core
open InitProject

[]
|> Context.FakeExecutionContext.Create false "InitProject.exe"
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

Initializer.init () |> Target.runOrDefault
