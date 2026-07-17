using Xunit;

// Принудительно заставляет Visual Studio запускать разные классы параллельно
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, MaxParallelThreads = 4)]
