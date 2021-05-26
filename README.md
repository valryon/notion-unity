# notion-unity: a Notion database parser for Unity3D

Download Notion databases directly in Unity!

## Required Packages

- [UniTask](https://github.com/Cysharp/UniTask) (can be replaced by coroutines)
- [JSON.NET for Unity](https://github.com/jilleJr/Newtonsoft.Json-for-Unity) (mandatory)

## Usage

```csharp
    var result = await NotionDownloader.GetTableAsync(databaseID);
    foreach(var line in result.Lines) 
    {
        var cell = line.Get("Name");
    }
```

