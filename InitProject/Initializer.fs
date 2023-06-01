namespace InitProject

open Fake.Core
open Fake.Core.TargetOperators

module Initializer =

    open Steps

    let steps = [
        nameof ``dotnet: Update new template``, ``dotnet: Update new template``
        nameof ``io: Create target directory``, ``io: Create target directory``
        nameof ``git: Init repository``, ``git: Init repository``
        nameof ``Add .gitignore``, ``Add .gitignore``
        nameof ``io: Create README.md``, ``io: Create README.md``
        nameof ``Init dotnet tool-manifest``, ``Init dotnet tool-manifest``
        nameof ``Install dotnet tool paket``, ``Install dotnet tool paket``
        nameof ``Install dotnet tool fantomas``, ``Install dotnet tool fantomas``
        nameof ``Init paket``, ``Init paket``
        nameof ``Create sln``, ``Create sln``
        nameof ``Create main project``, ``Create main project``
        nameof ``Create test project``, ``Create test project``
        nameof ``Add reference to main project on test project``, ``Add reference to main project on test project``
        nameof ``Add paket.references, AppendTargetFrameworkToOutputPath and enable FS0025 warning to projects``,
        ``Add paket.references, AppendTargetFrameworkToOutputPath and enable FS0025 warning to projects``
        nameof ``Create editorconfig file and apply config``, ``Create editorconfig file and apply config``
        nameof ``Create .build folder to sln``, ``Create .build folder to sln``
        nameof ``Open rider``, ``Open rider``
    ]

    let init context =
        steps |> List.iter (fun (name, step) -> Target.create name (step context))

        steps
        |> List.map fst
        |> List.pairwise
        |> List.map (fun (stepNameA, stepNameB) -> stepNameA ==> stepNameB)
        |> List.last
