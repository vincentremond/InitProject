@ECHO OFF

dotnet tool restore
dotnet build -- %*

AddToPath ./InitProject/bin/Debug
