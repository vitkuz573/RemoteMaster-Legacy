// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.SourceGenerators;

/// <summary>
/// Attribute used to mark domain entities for which repository classes should be generated.
/// The DbContext parameter specifies the EF Core DbContext type that should be used.
/// Optionally, UnitOfWorkGroup, UnitOfWorkPropertyName, Includes, and DbSetPropertyName can be provided.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateRepositoryAttribute(Type dbContext) : Attribute
{
    /// <summary>
    /// Gets the DbContext type to be used in the generated repository.
    /// </summary>
    public Type DbContext { get; } = dbContext;

    /// <summary>
    /// Optional group name.
    /// </summary>
    public string? UnitOfWorkGroup { get; set; }

    /// <summary>
    /// Optional property name for the generated UnitOfWork.
    /// </summary>
    public string? UnitOfWorkPropertyName { get; set; }

    /// <summary>
    /// Optional list of navigation properties to include in queries.
    /// </summary>
    public string[]? Includes { get; set; }

    /// <summary>
    /// Optional override for the DbSet property name in the DbContext.
    /// If not provided, a default name is computed from the entity name.
    /// </summary>
    public string? DbSetPropertyName { get; set; }
}
