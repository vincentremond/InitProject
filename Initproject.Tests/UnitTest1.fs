module Initproject.Tests

open System.IO
open AutoBogus
open Bogus
open InitProject
open NUnit.Framework
open FsUnit

let faker = Faker()

[<Test>]
let Test1 () =
    // Arrange
    let tempDir = Directory.CreateTempSubdirectory()
    let solutionContent = "<Solution />"

    let expectedContent =
        """<Solution>
  <Folder Name="/.build/">
    <File Path=".editorconfig" />
    <File Path=".gitignore" />
    <File Path="paket.dependencies" />
    <File Path="paket.lock" />
    <File Path="README.md" />
  </Folder>
</Solution>"""

    let context: InitProjectContext = {
        Language = AutoFaker.Generate<Language>()
        ProjectName = faker.Commerce.ProductName()
        TargetFolder = tempDir
        NoTestProject = AutoFaker.Generate<bool>()
    }

    File.WriteAllText(context.Solution.File.FullName, solutionContent)

    // Act
    Steps.``Create .build folder to slnx`` context

    // Assert
    let actualContent = File.ReadAllText context.Solution.File.FullName
    actualContent |> should equal expectedContent
