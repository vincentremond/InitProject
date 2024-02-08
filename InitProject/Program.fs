open Fake.Core
open InitProject
open Steps
open FakeOperators
open Fargo

let cliParser =
    fargo {
        let! destination = opt "name" null "name" "Project name"
        let! noTestProject = flag "no-test-project" null "Don't create test project"

        and! language =
            opt "lang" null "lang" "Language (csharp or fsharp)"
            |> optParse Language.tryParse
            |> defaultValue FSharp

        return {
            ProjectName = destination
            Language = language
            NoTestProject = noTestProject
        }
    }

[<EntryPoint>]
let main args =

    run
        "QuickStart"
        cliParser
        args
        (fun _ args ->
            task {
                []
                |> Context.FakeExecutionContext.Create false "InitProject.exe"
                |> Context.RuntimeContext.Fake
                |> Context.setExecutionContext

                let ctx = InitProjectContext.fromCliArguments args

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
                    ===> <@
                        ``Add paket.references, AppendTargetFrameworkToOutputPath and enable FS0025 warning to projects``
                            ctx
                    @>
                    ===> <@ ``Create editorconfig file and apply config`` ctx @>
                    ===> <@ ``Create .build folder to sln`` ctx @>
                    ===> <@ ``Add license file`` ctx @>
                    ===> <@ ``Open rider`` ctx @>
                )

                return 0
            }
        )
