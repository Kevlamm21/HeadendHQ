namespace HeadendHQ.Core.Catalog.Sources;

/// <summary>
/// A catalog source asked the client to back off (a 429, or a self-imposed per-run budget). Both are
/// terminal for the current run — the respectful reading is to stop and resume later, not try harder.
/// </summary>
public class CatalogSourceThrottledException(string message) : Exception(message);
