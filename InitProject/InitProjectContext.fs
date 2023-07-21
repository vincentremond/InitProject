namespace InitProject

open Fake.IO.FileSystemOperators

type SolutionFile = {
    Name: string
    Folder: string
    File: string
}

type InitProjectContext = {
    TargetFolder: string
    ProjectName: string
} with

    // get properties
    member this.Solution = {
        Name = this.ProjectName
        Folder = this.TargetFolder
        File =
            this.TargetFolder
            </> $"{this.ProjectName}.sln"
    }

    member this.MainProject = {
        Name = this.ProjectName
        Folder =
            this.TargetFolder
            </> this.ProjectName
        File =
            this.TargetFolder
            </> this.ProjectName
            </> $"{this.ProjectName}.fsproj"
    }

    member this.TestProject = {
        Name = $"{this.ProjectName}.Tests"
        Folder =
            this.TargetFolder
            </> $"{this.ProjectName}.Tests"
        File =
            this.TargetFolder
            </> $"{this.ProjectName}.Tests"
            </> $"{this.ProjectName}.Tests.fsproj"
    }

    static member fromCliArguments commandLineArguments =
        let target =
            match commandLineArguments.Destination with
            | Some destination -> destination
            | None -> System.Environment.CurrentDirectory

        let info = System.IO.DirectoryInfo(target)

        {
            TargetFolder = info.FullName
            ProjectName = info.Name
        }
