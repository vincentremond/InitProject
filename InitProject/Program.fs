open Fake.Core
open InitProject
open Steps
open FakeOperators

let args = System.Environment.GetCommandLineArgs().[1..]

let ctx =
    args
    |> Cli.parse
    |> InitProjectContext.fromCliArguments

[]
|> Context.FakeExecutionContext.Create false "InitProject.exe"
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

Target.runOrDefault (
    <@ ``dotnet: Update new template`` ctx @>
    ==!> <@ ``io: Create target directory`` ctx @>
    ===> <@ ``git: Init repository`` ctx @>
    ===> <@ ``Add .gitignore`` ctx @>
    ===> <@ ``io: Create README.md`` ctx @>
    ===> <@ ``Init dotnet tool-manifest`` ctx @>
    ===> <@ ``Install dotnet tool paket`` ctx @>
    ===> <@ ``Install dotnet tool fantomas`` ctx @>
    ===> <@ ``Init paket`` ctx @>
    ===> <@ ``Create sln`` ctx @>
    ===> <@ ``Create main project`` ctx @>
    ===> <@ ``Create test project`` ctx @>
    ===> <@ ``Add reference to main project on test project`` ctx @>
    ===> <@ ``Add paket.references, AppendTargetFrameworkToOutputPath and enable FS0025 warning to projects`` ctx @>
    ===> <@ ``Create editorconfig file and apply config`` ctx @>
    ===> <@ ``Create .build folder to sln`` ctx @>
    ===> <@ ``Open rider`` ctx @>
)
