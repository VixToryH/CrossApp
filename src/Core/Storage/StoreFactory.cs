namespace Core.Storage;

using Core.Abstractions;

public static class StoreFactory
{
    public static ILibraryStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

        ILibraryStore store = useFile
            ? new FileLibraryStore(dataPath)
            : new InMemoryLibraryStore(SampleData.Books());

        if (useCache)
        {
            store = new CachingLibraryStore(store);
        }

        return store;
    }
}