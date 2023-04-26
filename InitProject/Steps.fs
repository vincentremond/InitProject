namespace InitProject

open System
open System.Xml.Linq
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Tools.Git
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators

module Paths =
    type SolutionFile =
        { Name: string
          Folder: string
          File: string }

    let Solution =
        { Name = InitProjectContext.ProjectName
          Folder = InitProjectContext.targetFolder
          File = InitProjectContext.targetFolder </> $"{InitProjectContext.ProjectName}.sln" }

    let MainProject =
        { Name = InitProjectContext.ProjectName
          Folder = InitProjectContext.targetFolder </> InitProjectContext.ProjectName
          File =
            InitProjectContext.targetFolder
            </> InitProjectContext.ProjectName
            </> $"{InitProjectContext.ProjectName}.fsproj" }

    let TestProject =
        { Name = $"{InitProjectContext.ProjectName}.Tests"
          Folder = InitProjectContext.targetFolder </> $"{InitProjectContext.ProjectName}.Tests"
          File =
            InitProjectContext.targetFolder
            </> $"{InitProjectContext.ProjectName}.Tests"
            </> $"{InitProjectContext.ProjectName}.Tests.fsproj" }

module Steps =

    let ``dotnet: Update new template`` _ = DotNetCli.exec "new" [ "update" ]

    let ``TEMP: Clean target directory`` _ =
        Trace.tracefn $"Cleaning target directory {Paths.Solution.Folder}"

        if
            Paths.Solution.Folder = @"D:\TMP\2023.04.25-InitProject\MyProject1"
            || Paths.Solution.Folder = @"D:\VRM\Projects\InitProject\InitProject\InitProject\bin\Debug\net7.0\TestProject11"
        then
            Directory.delete Paths.Solution.Folder

    let ``io: Create target directory`` _ =
        Trace.tracefn $"Creating target directory {Paths.Solution.Folder}"
        Directory.ensure Paths.Solution.Folder

        Trace.tracefn $"Setting current directory to {Paths.Solution.Folder}"
        System.Environment.CurrentDirectory <- Paths.Solution.Folder

    let ``git: Init repository`` _ =
        Trace.tracefn $"Initializing git repository"
        let bare = false
        let shared = false
        Repository.init Paths.Solution.Folder bare shared

    let ``Add .gitignore`` _ =
        DotNetCli.exec "new" [ "gitignore" ]

        ".gitignore"
        |> File.tryFixFile (fun lines ->
            if not (lines |> Seq.contains ".idea/") then
                Trace.tracefn "Adding .idea/ to .gitignore"
                lines |> StringList.appendAfter "# JetBrains Rider" ".idea/" |> Some
            else
                None)

    let ``io: Create README.md`` _ =
        [ $"# {Paths.Solution.Name}"; "" ]
        |> File.writeNew (Paths.Solution.Folder </> "README.md")

    let ``Init dotnet tool-manifest`` _ =
        DotNetCli.exec "new" [ "tool-manifest" ]

    let ``Install dotnet tool paket`` _ =
        DotNetCli.exec "tool" [ "install"; "paket" ]

    let ``Install dotnet tool fantomas`` _ =
        DotNetCli.exec "tool" [ "install"; "fantomas" ]

    let ``Create sln`` _ =
        DotNetCli.exec "new" [ "sln"; "--name"; Paths.Solution.Name ]

    let ``Init paket`` _ =
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


    let ``Create main project`` _ =
        DotNet.newFromTemplate "console" (fun o ->
            { o with
                Name = Some Paths.MainProject.Name })

        DotNetCli.exec "sln" [ "add"; Paths.MainProject.File ]

        DotNetCli.exec "paket" [ "add"; "FSharp.Core"; "--project"; Paths.MainProject.File ]

    let ``Create test project`` _ =
        DotNet.newFromTemplate "nunit" (fun o ->
            { o with
                Name = Some Paths.TestProject.Name })

        DotNetCli.exec "sln" [ "add"; Paths.TestProject.File ]

        DotNetCli.exec
            "paket"
            [ "add"
              "FSharp.Core"
              "--project"
              Paths.TestProject.File
              "--group"
              "Tests" ]

        let xDoc = Paths.TestProject.File |> XDocument.load

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

        File.delete (Paths.TestProject.Folder </> "Program.fs")


        xDoc.Save(Paths.TestProject.File)

        nugetPackagesToAdd
        |> Seq.iter (fun package ->
            DotNetCli.exec "paket" [ "add"; package; "--project"; Paths.TestProject.File; "--group"; "Tests" ])

    let ``Add reference to main project on test project`` _ =
        DotNetCli.exec "add" [ Paths.TestProject.File; "reference"; Paths.MainProject.File ]

    let ``Add paket.references and enable FS0025 warning to projects`` _ =
        let fixFsProj (path: string) =
            let xDoc = path |> XDocument.Load
            xDoc.Root.Element("PropertyGroup").Add(XElement("WarningsAsErrors", "FS0025"))

            xDoc.Root
                .Element("ItemGroup")
                .AddFirst(XElement("Content", [ XAttribute("Include", "paket.references") ]))

            xDoc.Save(path)

        // get all fsproj files
        (!! "**/*.fsproj") |> Seq.iter fixFsProj

    let ``Create editorconfig file and apply config`` _ =
        File.writeNew
            (Paths.Solution.Folder </> ".editorconfig")
            [ "[*.{fs,fsx}]"
              "fsharp_multiline_block_brackets_on_same_column = true"
              "fsharp_experimental_stroustrup_style = true" ]

        DotNetCli.exec "fantomas" [ "."; "--recurse" ]

    let ``Create .build folder to sln`` _ =
        let folderProjectTypeGuid =
            "2150E333-8FDC-42A3-9474-1A3956D46DE8" |> Guid |> Guid.toStringUC

        let projectUniqueGuid = Guid.NewGuid() |> Guid.toStringUC

        Paths.Solution.File
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


    let ``Open rider`` _ =
        let jetBrainsRiderPath =
            !! @"C:\Program Files (x86)\JetBrains\*\bin\rider64.exe" |> Seq.sort |> Seq.last

        (jetBrainsRiderPath, Arguments.ofList [ Paths.Solution.File ])
        |> Command.RawCommand
        |> CreateProcess.fromCommand
        |> Proc.start
        |> ignore
