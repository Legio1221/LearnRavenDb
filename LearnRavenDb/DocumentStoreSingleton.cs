namespace LearnRavenDb
{
    using Raven.Client.Documents;
    using System;

    public static class DocumentStoreSingleton
    {
        // --------------------- Singleton Initialization --------------------- //
        // Singleton pattern ensures RavenDb does not have to worry about
        // locking or thread safety.
        private static readonly Lazy<IDocumentStore> LazyStore =
            new Lazy<IDocumentStore>(() =>
           {
               var store = new DocumentStore
               {
                   Urls = new string[] { "http://localhost:8080" },
                   Database = "Northwind"
               };

               return store.Initialize();
           });

        public static IDocumentStore Store => LazyStore.Value;
        // --------------------- END Singleton Initialization
    }
}
