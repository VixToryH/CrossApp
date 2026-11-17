namespace Core.Storage;

using Core.Abstractions;

public static class StoreFactory
{
    public static ILibraryStore Create(string[] args, string resolvedFilePath)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        ILibraryStore store = useFile
            ? new FileLibraryStore(resolvedFilePath)
            : new InMemoryLibraryStore(SampleData.Books());

        if (useCache)
        {
            store = new CachingLibraryStore(store);
        }

        return store;
    }
}