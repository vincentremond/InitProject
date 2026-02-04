namespace InitProject

open System.IO
open Fake.DotNet

[<AutoOpen>]
module PathOperators =
    let (<//?>) (d: DirectoryInfo) (sd: string) = Path.Join(d.FullName, sd) |> DirectoryInfo
    let (</?>) (d: DirectoryInfo) (f: string) = Path.Join(d.FullName, f) |> FileInfo

type Language =
    | FSharp
    | CSharp

    static member tryParse(lang: string) =
        match lang with
        | "fsharp" -> Ok FSharp
        | "csharp" -> Ok CSharp
        | _ -> Error $"Language %s{lang} not supported (possible values: fsharp, csharp)"

    member this.toNewOption =
        match this with
        | FSharp -> DotNet.NewLanguage.FSharp
        | CSharp -> DotNet.NewLanguage.CSharp

type CliArguments = {
    ProjectName: string option
    Language: Language
    NoTestProject: bool
}
