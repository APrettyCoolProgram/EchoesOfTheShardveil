<div align="center">

  <img src="../../logo/RepositoryTemplate-336x220.png" alt="Repository Template">

  <h1>Scratch Pad</h1>
  
</div>

```
dotnet clean && dotnet build TheCards.slnx && dotnet test && dotnet run --project TheCards.Desktop/TheCards.Desktop.csproj
```

```
dotnet clean && dotnet build TheCards.slnx && dotnet test && dotnet publish TheCards.Desktop/TheCards.Desktop.csproj -c Release -r osx-arm64 --self-contained
```