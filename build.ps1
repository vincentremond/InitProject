$ErrorActionPreference = "Stop"

dotnet tool restore
dotnet build

AddToPath ./InitProject/bin/Debug
