namespace Core.Storage;

using Core.Abstractions;

public static class StoreFactory
{
    public static ILibraryStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

        ILibraryStore baseStore = useFile
            ? new FileLibraryStore(dataPath)
            : new InMemoryLibraryStore(SampleData.Books());

        return new CachingLibraryStore(baseStore);
    }
}