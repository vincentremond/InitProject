namespace InitProject

open System
open System.Xml.Linq
open Fake.Core
open Fake.DotNet
open Fake.IO

[<AutoOpen>]
module String =
    // Active pattern for string startsWith
    let (|StartsWith|_|) (prefix: string) (str: string) =
        if str.StartsWith(prefix) then Some str else None

    // equals active pattern
    let (|Equals|_|) (str1: string) (str2: string) = if str1 = str2 then Some() else None

[<RequireQualifiedAccess>]
module DotNetCli =
    let exec command ars =
        let args = ars |> Seq.map (sprintf "%s") |> String.concat " "
        let result = DotNet.exec (fun options -> options) command args

        match result with
        | { ExitCode = 0 } -> result.Results |> Seq.iter (string >> (Trace.tracefn "%s"))
        | _ ->
            result.Errors |> Seq.iter (Trace.traceErrorfn "%s")
            failwithf $"DotNetCLI.exec failed with exit code %d{result.ExitCode}"

[<RequireQualifiedAccess>]
module Guid =

    let toStringUC (guid: Guid) = guid.ToString("B").ToUpperInvariant()


[<RequireQualifiedAccess>]
module StringList =
    let appendAfter before after =
        List.collect (
            function
            | Equals before -> [ before; after ]
            | line -> [ line ]
        )

    let insertManyBefore beforeWhat contents =
        List.collect (
            function
            | Equals beforeWhat -> contents @ [ beforeWhat ]
            | line -> [ line ]
        )

[<RequireQualifiedAccess>]
module File =
    let fixFile (fix: string list -> string list) path =
        let lines = path |> File.read |> Seq.toList
        let lines = fix lines
        File.writeNew path lines

    let tryFixFile (f: string list -> string list option) path =
        let lines = path |> File.read |> Seq.toList
        let lines = f lines

        match lines with
        | Some lines -> File.writeNew path lines
        | None -> ()

    let writeNewEmpty path = File.writeNew path []

[<RequireQualifiedAccess>]
module Seq =
    let trySingle f s =
        match s |> Seq.where f |> Seq.toList with
        | [ x ] -> Some x
        | _ -> None

[<RequireQualifiedAccess>]
module XDocument =
    let load (path: string) = XDocument.Load(path)

[<RequireQualifiedAccess>]
module XContainer =
    let element name (x: XContainer) = x.Element(XName.Get(name))
    let elements name (x: XContainer) = x.Elements(XName.Get(name))

[<RequireQualifiedAccess>]
module XElement =
    let attribute name (x: XElement) = x.Attribute(XName.Get(name))

[<RequireQualifiedAccess>]
module XAttribute =
    let value (x: XAttribute) = x.Value
