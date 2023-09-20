namespace InitProject

type Language =
    | FSharp
    | CSharp

    static member tryParse(lang: string) =
        match lang with
        | "fsharp" -> Ok FSharp
        | "csharp" -> Ok CSharp
        | _ -> Error $"Language %s{lang} not supported (possible values: fsharp, csharp)"

type CliArguments = {
    ProjectName: string option
    Language: Language
}
