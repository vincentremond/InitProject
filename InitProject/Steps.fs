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

    let ``dotnet: Update new template`` (_: InitProjectContext) = DotNetCli.exec "new" [ "update" ]

    let ``io: Create target directory`` (initProjectContext: InitProjectContext) =
        Trace.tracefn $"Creating target directory {initProjectContext.Solution.Folder}"
        Directory.ensure initProjectContext.Solution.Folder

        Trace.tracefn $"Setting current directory to {initProjectContext.Solution.Folder}"
        Environment.CurrentDirectory <- initProjectContext.Solution.Folder

    let ``git: Init repository`` (initProjectContext: InitProjectContext) =
        Trace.tracefn $"Initializing git repository"
        let bare = false
        let shared = false
        Repository.init initProjectContext.Solution.Folder bare shared
        Branches.checkout initProjectContext.Solution.Folder true "main"

    let ``Add .gitignore`` (_: InitProjectContext) =
        DotNetCli.exec "new" [ "gitignore" ]

        ".gitignore"
        |> File.tryFixFile (fun lines ->
            if not (lines |> Seq.contains ".idea/") then
                Trace.tracefn "Adding .idea/ to .gitignore"

                lines
                |> StringList.appendAfter "# JetBrains Rider" ".idea/"
                |> Some
            else
                None
        )

    let ``io: Create README.md`` (initProjectContext: InitProjectContext) =
        [
            $"# {initProjectContext.Solution.Name}"
            ""
        ]
        |> File.writeNew (
            initProjectContext.Solution.Folder
            </> "README.md"
        )

    let ``Init dotnet tool-manifest`` (_: InitProjectContext) =
        DotNetCli.exec "new" [ "tool-manifest" ]

    let ``Install dotnet tool paket`` (_: InitProjectContext) =
        DotNetCli.exec "tool" [
            "install"
            "paket"
        ]

    let ``Install dotnet tool fantomas`` (ctx: InitProjectContext) =
        if ctx.Language = FSharp then
            DotNetCli.exec "tool" [
                "install"
                "fantomas"
            ]

    let ``Create sln`` (initProjectContext: InitProjectContext) =
        DotNetCli.exec "new" [
            "sln"
            "--name"
            initProjectContext.Solution.Name
        ]

    let ``Init paket`` (_: InitProjectContext) =
        DotNetCli.exec "paket" [ "init" ]

        "paket.dependencies"
        |> File.fixFile (fun lines ->
            let lines =
                lines
                |> List.map (
                    function
                    | StartsWith "framework:" _ -> "framework: auto-detect"
                    | line -> line
                )

            let lines =
                lines
                @ [
                    ""
                    "group Tests"
                    ""
                    "storage: none"
                    "source https://api.nuget.org/v3/index.json"
                    ""
                ]

            lines
        )

    let ``Create main project`` (ctx: InitProjectContext) =

        DotNet.newFromTemplate
            "console"
            (fun o -> {
                o with
                    Name = Some ctx.MainProject.Name
                    Language = ctx.Language.toNewOption
            })

        DotNetCli.exec "sln" [
            "add"
            ctx.MainProject.File
        ]

        if ctx.Language = FSharp then
            DotNetCli.exec "paket" [
                "add"
                "FSharp.Core"
                "--project"
                ctx.MainProject.File
            ]

    let ``Create test project`` (ctx: InitProjectContext) =
        DotNet.newFromTemplate
            "nunit"
            (fun o -> {
                o with
                    Name = Some ctx.TestProject.Name
                    Language = ctx.Language.toNewOption
            })

        DotNetCli.exec "sln" [
            "add"
            ctx.TestProject.File
        ]

        if ctx.Language = FSharp then
            DotNetCli.exec "paket" [
                "add"
                "FSharp.Core"
                "--project"
                ctx.TestProject.File
                "--group"
                "Tests"
            ]

        let xDoc =
            ctx.TestProject.File
            |> XDocument.load

        printfn $"Test project loaded {ctx.TestProject.File}"

        let nugetReferences =
            xDoc.Root
                .Elements("ItemGroup")
                .Elements("PackageReference")
            |> Seq.toArray

        printfn $"Found %d{nugetReferences.Length} nuget references"

        let nugetPackagesToAdd =
            nugetReferences.Attributes("Include")
            |> Seq.map (fun x -> x.Value)
            |> Seq.toArray

        printfn $"Found %d{nugetPackagesToAdd.Length} nuget packages to add"

        nugetReferences.Remove()
        printfn "Removed old nuget references"

        xDoc.Root
            .Elementtt("PropertyGroup")
            .Elementtt("GenerateProgramFile")
            .Value <- "true"

        xDoc.Root
            .Elementtt("ItemGroup")
            .Elements("Compile")
        |> Seq.where (fun compile ->
            compile
                .Attribute("Include")
                .Value = "Program.fs"
        )
        |> Seq.iter (fun compile -> compile.Remove())

        File.delete ctx.TestProject.ProgramFile

        xDoc.Save(ctx.TestProject.File)

        nugetPackagesToAdd
        |> Seq.iter (fun package ->
            DotNetCli.exec "paket" [
                "add"
                package
                "--project"
                ctx.TestProject.File
                "--group"
                "Tests"
            ]
        )

    let ``Add reference to main project on test project`` (initProjectContext: InitProjectContext) =
        DotNetCli.exec "add" [
            initProjectContext.TestProject.File
            "reference"
            initProjectContext.MainProject.File
        ]

    let ``Add paket.references, AppendTargetFrameworkToOutputPath and enable FS0025 warning to projects``
        (ctx: InitProjectContext)
        =
        let fixProjectFile (path: string) =
            let xDoc = path |> XDocument.Load
            let propertyGroup = xDoc.Root.Element("PropertyGroup")

            if ctx.Language = FSharp then
                // FS0025: Incomplete pattern matches on this expression
                propertyGroup.Add(XElement("WarningsAsErrors", "FS0025"))

            // Disable AppendTargetFrameworkToOutputPath to allow simpler paths (for example for shortcuts)
            propertyGroup.Add(XElement("AppendTargetFrameworkToOutputPath", false))

            if ctx.Language = FSharp then
                xDoc.Root
                    .Element("ItemGroup")
                    .AddFirst(XElement("None", [ XAttribute("Include", "paket.references") ]))

            xDoc.Save(path)

        // get all fsproj files
        (!! @"**/*.?sproj")
        |> Seq.iter fixProjectFile

    let ``Create editorconfig file and apply config`` (initProjectContext: InitProjectContext) =
        File.writeNew
            (initProjectContext.Solution.Folder
             </> ".editorconfig")
            [
                "root = true"
                ""
                "[paket.*]"
                "insert_final_newline = false"
                ""
                "[*.{fs,fsx}]"
                "fsharp_multiline_bracket_style = stroustrup"
                "fsharp_multi_line_lambda_closing_newline = true"
                "fsharp_max_infix_operator_expression = 30"
                "fsharp_max_dot_get_expression_width = 30"

                "fsharp_bar_before_discriminated_union_declaration = true"
                "fsharp_keep_max_number_of_blank_lines = 1"

                "fsharp_record_multiline_formatter = number_of_items"
                "fsharp_max_record_number_of_items = 1"

                "fsharp_array_or_list_multiline_formatter = number_of_items"
                "fsharp_max_array_or_list_number_of_items = 1"
            ]

        DotNetCli.exec "fantomas" [ "." ]

    let ``Create .build folder to sln`` (initProjectContext: InitProjectContext) =
        let folderProjectTypeGuid =
            "2150E333-8FDC-42A3-9474-1A3956D46DE8"
            |> Guid
            |> Guid.toStringUC

        let projectUniqueGuid =
            Guid.NewGuid()
            |> Guid.toStringUC

        initProjectContext.Solution.File
        |> File.fixFile (
            StringList.insertManyBefore "Global" [
                $"Project(\"%s{folderProjectTypeGuid}\") = \".build\", \".build\", \"%s{projectUniqueGuid}\""
                "\tProjectSection(SolutionItems) = preProject"
                "\t\t.editorconfig = .editorconfig"
                "\t\t.gitignore = .gitignore"
                "\t\tpaket.dependencies = paket.dependencies"
                "\t\tpaket.lock = paket.lock"
                "\t\tREADME.md = README.md"
                "\tEndProjectSection"
                "EndProject"
            ]
        )

    let ``Open rider`` (initProjectContext: InitProjectContext) =
        let jetBrainsRiderPath =
            !! @"C:\Program Files (x86)\JetBrains\*\bin\rider64.exe"
            |> Seq.sort
            |> Seq.last

        (jetBrainsRiderPath, Arguments.ofList [ initProjectContext.Solution.File ])
        |> Command.RawCommand
        |> CreateProcess.fromCommand
        |> Proc.start
        |> ignore
