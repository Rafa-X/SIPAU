namespace UPZMG.Implementations.Permissions;

/// <summary>
/// What restoring the catalogue did. Role assignments are never part of it: adding a missing
/// permission must not change who holds what.
/// </summary>
/// <param name="Added">Catalogue permissions the table did not have.</param>
/// <param name="Updated">Rows whose display name or description no longer matched the code.</param>
/// <param name="Unknown">Rows that are not in the catalogue, so no endpoint reads them.</param>
public sealed record CatalogueRestoreResult(int Added, int Updated, int Unknown);
