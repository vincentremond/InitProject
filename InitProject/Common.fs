namespace InitProject

open Fake.DotNet

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
}
