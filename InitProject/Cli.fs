namespace InitProject

open Fake.Core

type CliArguments = { Destination: string option }

[<RequireQualifiedAccess>]
module Cli =

    let private cli =
        """
InitProject

Usage:
    InitProject.exe
    InitProject.exe <destination>
    InitProject.exe --destination <destination>
"""


    let parse args =
        let parsedArguments = args |> Seq.toArray |> Docopt(cli).Parse

        printfn $"Parsed arguments: %A{parsedArguments}"

        let destination =
            match parsedArguments |> Map.tryFind "<destination>" with
            | Some(Argument s) -> Some s
            | Some other -> failwithf $"Unexpected argument type: %A{other}"
            | _ -> None

        { Destination = destination }
