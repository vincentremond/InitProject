namespace InitProject

[<RequireQualifiedAccess>]
module InitProjectContext =

    let private arguments = System.Environment.GetCommandLineArgs() |> Array.skip 1

    let private commandLineArguments = Cli.parse arguments

    let targetFolder, ProjectName =
        let target =
            match commandLineArguments.Destination with
            | Some destination -> destination
            | None -> System.Environment.CurrentDirectory

        let info = System.IO.DirectoryInfo(target)
        (info.FullName, info.Name)
