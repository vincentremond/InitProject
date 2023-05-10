namespace InitProject

open System
open System.Xml.Linq
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Tools.Git
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators

module Steps =

    let ``dotnet: Update new template`` (_: InitProjectContext) _ = DotNetCli.exec "new" [ "update" ]

    let ``io: Create target directory`` (initProjectContext: InitProjectContext) _ =
        Trace.tracefn $"Creating target directory {initProjectContext.Solution.Folder}"
        Directory.ensure initProjectContext.Solution.Folder

        Trace.tracefn $"Setting current directory to {initProjectContext.Solution.Folder}"
        Environment.CurrentDirectory <- initProjectContext.Solution.Folder

    let ``git: Init repository`` (initProjectContext: InitProjectContext) _ =
        Trace.tracefn $"Initializing git repository"
        let bare = false
        let shared = false
        Repository.init initProjectContext.Solution.Folder bare shared

    let ``Add .gitignore`` (_: InitProjectContext) _ =
        DotNetCli.exec "new" [ "gitignore" ]

        ".gitignore"
        |> File.tryFixFile (fun lines ->
            if not (lines |> Seq.contains ".idea/") then
                Trace.tracefn "Adding .idea/ to .gitignore"
                lines |> StringList.appendAfter "# JetBrains Rider" ".idea/" |> Some
            else
                None)

    let ``io: Create README.md`` (initProjectContext: InitProjectContext) _ =
        [ $"# {initProjectContext.Solution.Name}"; "" ]
        |> File.writeNew (initProjectContext.Solution.Folder </> "README.md")

    let ``Init dotnet tool-manifest`` (_: InitProjectContext) _ =
        DotNetCli.exec "new" [ "tool-manifest" ]

    let ``Install dotnet tool paket`` (_: InitProjectContext) _ =
        DotNetCli.exec "tool" [ "install"; "paket" ]

    let ``Install dotnet tool fantomas`` (_: InitProjectContext) _ =
        DotNetCli.exec "tool" [ "install"; "fantomas" ]

    let ``Create sln`` (initProjectContext: InitProjectContext) _ =
        DotNetCli.exec "new" [ "sln"; "--name"; initProjectContext.Solution.Name ]

    let ``Init paket`` (_: InitProjectContext) _ =
        DotNetCli.exec "paket" [ "init" ]

        "paket.dependencies"
        |> File.fixFile (fun lines ->
            let lines =
                lines
                |> List.map (function
                    | StartsWith "framework:" _ -> "framework: auto-detect"
                    | line -> line)

            let lines =
                lines
                @ [ ""
                    "group Tests"
                    ""
                    "storage: none"
                    "source https://api.nuget.org/v3/index.json"
                    "" ]

            lines)


    let ``Create main project`` (initProjectContext: InitProjectContext) _ =
        DotNet.newFromTemplate "console" (fun o ->
            { o with
                Name = Some initProjectContext.MainProject.Name })

        DotNetCli.exec "sln" [ "add"; initProjectContext.MainProject.File ]

        DotNetCli.exec "paket" [ "add"; "FSharp.Core"; "--project"; initProjectContext.MainProject.File ]

    let ``Create test project`` (initProjectContext: InitProjectContext) _ =
        DotNet.newFromTemplate "nunit" (fun o ->
            { o with
                Name = Some initProjectContext.TestProject.Name })

        DotNetCli.exec "sln" [ "add"; initProjectContext.TestProject.File ]

        DotNetCli.exec
            "paket"
            [ "add"
              "FSharp.Core"
              "--project"
              initProjectContext.TestProject.File
              "--group"
              "Tests" ]

        let xDoc = initProjectContext.TestProject.File |> XDocument.load

        let nugetReferences =
            xDoc.Root.Elements("ItemGroup").Elements("PackageReference") |> Seq.toArray

        let nugetPackagesToAdd =
            nugetReferences.Attributes("Include")
            |> Seq.map (fun x -> x.Value)
            |> Seq.toArray

        nugetReferences.Remove()

        xDoc.Root.Element("PropertyGroup").Element("GenerateProgramFile").Value <- "true"

        xDoc.Root.Element("ItemGroup").Elements("Compile")
        |> Seq.where (fun compile -> compile.Attribute("Include").Value = "Program.fs")
        |> Seq.iter (fun compile -> compile.Remove())

        File.delete (initProjectContext.TestProject.Folder </> "Program.fs")


        xDoc.Save(initProjectContext.TestProject.File)

        nugetPackagesToAdd
        |> Seq.iter (fun package ->
            DotNetCli.exec "paket" [ "add"; package; "--project"; initProjectContext.TestProject.File; "--group"; "Tests" ])

    let ``Add reference to main project on test project`` (initProjectContext: InitProjectContext) _ =
        DotNetCli.exec "add" [ initProjectContext.TestProject.File; "reference"; initProjectContext.MainProject.File ]

    let ``Add paket.references and enable FS0025 warning to projects`` (_: InitProjectContext) _ =
        let fixFsProj (path: string) =
            let xDoc = path |> XDocument.Load
            xDoc.Root.Element("PropertyGroup").Add(XElement("WarningsAsErrors", "FS0025"))

            xDoc.Root
                .Element("ItemGroup")
                .AddFirst(XElement("Content", [ XAttribute("Include", "paket.references") ]))

            xDoc.Save(path)

        // get all fsproj files
        (!! "**/*.fsproj") |> Seq.iter fixFsProj

    let ``Create editorconfig file and apply config`` (initProjectContext: InitProjectContext) _ =
        File.writeNew
            (initProjectContext.Solution.Folder </> ".editorconfig")
            [ "[*.{fs,fsx}]"
              "fsharp_multiline_block_brackets_on_same_column = true"
              "fsharp_experimental_stroustrup_style = true" ]

        DotNetCli.exec "fantomas" [ "." ]

    let ``Create .build folder to sln`` (initProjectContext: InitProjectContext) _ =
        let folderProjectTypeGuid =
            "2150E333-8FDC-42A3-9474-1A3956D46DE8" |> Guid |> Guid.toStringUC

        let projectUniqueGuid = Guid.NewGuid() |> Guid.toStringUC

        initProjectContext.Solution.File
        |> File.fixFile (
            StringList.insertManyBefore
                "Global"
                [ $"Project(\"%s{folderProjectTypeGuid}\") = \".build\", \".build\", \"%s{projectUniqueGuid}\""
                  "\tProjectSection(SolutionItems) = preProject"
                  "\t\t.editorconfig = .editorconfig"
                  "\t\t.gitignore = .gitignore"
                  "\t\tpaket.dependencies = paket.dependencies"
                  "\t\tpaket.lock = paket.lock"
                  "\t\tREADME.md = README.md"
                  "\tEndProjectSection"
                  "EndProject" ]
        )


    let ``Open rider`` (initProjectContext: InitProjectContext) _ =
        let jetBrainsRiderPath =
            !! @"C:\Program Files (x86)\JetBrains\*\bin\rider64.exe" |> Seq.sort |> Seq.last

        (jetBrainsRiderPath, Arguments.ofList [ initProjectContext.Solution.File ])
        |> Command.RawCommand
        |> CreateProcess.fromCommand
        |> Proc.start
        |> ignore
