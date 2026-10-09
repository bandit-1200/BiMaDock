using Xunit;

// Alle UI-Tests teilen sich Maus, Tastatur und Bildschirm und dürfen nicht parallel laufen.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
