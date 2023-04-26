namespace InitProject

open Fake.Core

[<RequireQualifiedAccess>]
module Cli =

    let private cli =
        """
usage: InitProject.exe --destination=<destination>

--destination=<destination>  Target destination, if not set current directory will be used
"""

    type Arguments = { Destination: string option }

    let parse args =
        let parsedArguments = args |> Seq.toArray |> Docopt(cli).Parse

        printfn $"Parsed arguments: %A{parsedArguments}"

        let destination =
            match parsedArguments |> Map.tryFind "--destination" with
            | Some(Argument s) -> Some s
            | Some other -> failwithf $"Unexpected argument type: %A{other}"
            | _ -> None

        { Destination = destination }
