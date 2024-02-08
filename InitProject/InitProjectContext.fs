namespace InitProject

open Fake.IO.FileSystemOperators

type ProjectFile = {
    Name: string
    Folder: string
    File: string
    ProgramFile: string
}

type InitProjectContext = {
    Language: Language
    TargetFolder: string
    ProjectName: string
    NoTestProject: bool
} with

    member this.ProjectExtension =
        match this.Language with
        | Language.FSharp -> ".fsproj"
        | Language.CSharp -> ".csproj"

    member this.SourceFileExtension =
        match this.Language with
        | Language.FSharp -> ".fs"
        | Language.CSharp -> ".cs"

    // get properties
    member this.Solution = {|
        Name = this.ProjectName
        Folder = this.TargetFolder
        File = this.TargetFolder </> $"{this.ProjectName}.sln"
    |}

    member this.MainProject = {
        Name = this.ProjectName
        Folder = this.TargetFolder </> this.ProjectName
        File =
            this.TargetFolder
            </> this.ProjectName
            </> $"{this.ProjectName}{this.ProjectExtension}"
        ProgramFile = this.TargetFolder </> this.ProjectName </> $"Program{this.SourceFileExtension}"
    }

    member this.TestProject =
        if this.NoTestProject then
            None
        else
            Some {
                Name = $"{this.ProjectName}.Tests"
                Folder = this.TargetFolder </> $"{this.ProjectName}.Tests"
                File =
                    this.TargetFolder
                    </> $"{this.ProjectName}.Tests"
                    </> $"{this.ProjectName}.Tests{this.ProjectExtension}"
                ProgramFile =
                    this.TargetFolder
                    </> $"{this.ProjectName}.Tests"
                    </> $"Program{this.SourceFileExtension}"
            }

    static member fromCliArguments(args: CliArguments) =
        let target =
            match args.ProjectName with
            | Some projectName -> projectName
            | None -> System.Environment.CurrentDirectory

        let info = System.IO.DirectoryInfo(target)

        {
            Language = args.Language
            NoTestProject = args.NoTestProject
            TargetFolder = info.FullName
            ProjectName = info.Name
        }
