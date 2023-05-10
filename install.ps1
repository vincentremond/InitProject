dotnet pack --configuration Release
dotnet tool uninstall InitProject --global
dotnet tool install InitProject --add-source .\InitProject\nupkg\ --global
