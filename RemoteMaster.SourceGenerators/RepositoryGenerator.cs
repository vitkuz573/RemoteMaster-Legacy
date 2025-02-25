// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RemoteMaster.SourceGenerators;

[Generator]
public class RepositoryGenerator : IIncrementalGenerator
{
    private record RepositoryMetadata(
        string EntityName,
        string EntityFullName,
        string RepositoryName,
        string DefaultDbSetName,
        string? DbSetPropertyName,
        string DbContextName,
        string UnitOfWorkGroup,
        string? UnitOfWorkPropertyName,
        ImmutableArray<string> Includes,
        string IdType);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configProvider = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith("RepositoryGeneratorConfig.json"))
            .Select(static (additionalText, cancellationToken) =>
            {
                var text = additionalText.GetText(cancellationToken)?.ToString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return new RepositoryGeneratorConfig();
                }
                try
                {
                    return JsonSerializer.Deserialize<RepositoryGeneratorConfig>(text) ?? new RepositoryGeneratorConfig();
                }
                catch
                {
                    return new RepositoryGeneratorConfig();
                }
            })
            .Collect()
            .Select(static (configs, _) => configs.FirstOrDefault() ?? new RepositoryGeneratorConfig());

        var baseNamespaceProvider = configProvider.Select(static (cfg, _) =>
            string.IsNullOrWhiteSpace(cfg.GeneratedNamespace)
                ? "Vitkuz573.SourceGenerators.Repository"
                : cfg.GeneratedNamespace);

        var repositoryMetadataProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => ExtractRepositoryMetadata(ctx))
            .Where(static meta => meta is not null)
            .Select(static (meta, _) => meta!);

        var repositoryMetadataCollection = repositoryMetadataProvider.Collect();

        var combinedReposWithConfig = repositoryMetadataCollection.Combine(configProvider)
            .Combine(baseNamespaceProvider);

        context.RegisterSourceOutput(combinedReposWithConfig, static (spc, tuple) =>
        {
            var ((metadataCollection, cfg), baseNamespace) = tuple;
            if (cfg.GenerateRepositoriesInSeparateFiles)
            {
                foreach (var meta in metadataCollection)
                {
                    var source = GenerateSingleRepository(meta, baseNamespace);
                    spc.AddSource($"{meta.RepositoryName}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
            else
            {
                var source = GenerateRepositories(metadataCollection, baseNamespace);
                spc.AddSource("GeneratedRepositories.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        });

        var combinedRepoInterfaces = repositoryMetadataCollection.Combine(configProvider)
            .Combine(baseNamespaceProvider);

        context.RegisterSourceOutput(combinedRepoInterfaces, static (spc, tuple) =>
        {
            var ((metadataCollection, cfg), baseNamespace) = tuple;

            if (cfg.GenerateRepositoriesInSeparateFiles)
            {
                foreach (var meta in metadataCollection)
                {
                    var source = GenerateRepositoryInterface(meta, baseNamespace);
                    spc.AddSource($"I{meta.RepositoryName}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
            else
            {
                var source = GenerateRepositoryInterfaces(metadataCollection, baseNamespace);
                spc.AddSource("GeneratedRepositoryInterfaces.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        });

        var combinedUow = repositoryMetadataCollection.Combine(configProvider)
            .Combine(baseNamespaceProvider);

        context.RegisterSourceOutput(combinedUow, static (spc, tuple) =>
        {
            var ((metadataCollection, cfg), baseNamespace) = tuple;

            if (cfg.GenerateUnitOfWorkInSeparateFiles)
            {
                var unitOfWorkSources = GenerateUnitOfWorkSeparateFiles(metadataCollection, baseNamespace);

                foreach (var (fileName, source) in unitOfWorkSources)
                {
                    spc.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
                }
            }
            else
            {
                var unitOfWorkSource = GenerateUnitOfWork(metadataCollection, baseNamespace);
                spc.AddSource("UnitOfWork.g.cs", SourceText.From(unitOfWorkSource, Encoding.UTF8));
            }
        });

        context.RegisterSourceOutput(baseNamespaceProvider, (spc, baseNamespace) =>
        {
            var interfaceSource = GenerateInterfaces(baseNamespace);
            spc.AddSource("IRepository.g.cs", SourceText.From(interfaceSource, Encoding.UTF8));
        });
    }

    private static RepositoryMetadata? ExtractRepositoryMetadata(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        foreach (var attribute in classSymbol.GetAttributes()
                     .Where(attribute => attribute.AttributeClass?.ToDisplayString() == "RemoteMaster.SourceGenerators.GenerateRepositoryAttribute"))
        {
            if (attribute.ConstructorArguments.Length != 1 ||
                attribute.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
                attribute.ConstructorArguments[0].Value is not INamedTypeSymbol dbContextSymbol)
            {
                continue;
            }

            var dbContextName = dbContextSymbol.ToDisplayString();

            string? unitOfWorkGroup = null;
            string? unitOfWorkPropertyName = null;
            string? customDbSetName = null;

            var includes = ImmutableArray<string>.Empty;

            foreach (var namedArg in attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "UnitOfWorkGroup" when namedArg.Value.Value is string group:
                        unitOfWorkGroup = group;
                        break;
                    case "UnitOfWorkPropertyName" when namedArg.Value.Value is string propName:
                        unitOfWorkPropertyName = propName;
                        break;
                    case "Includes" when namedArg.Value.Kind == TypedConstantKind.Array:
                        includes = namedArg.Value.Values
                            .Where(x => x.Value is string)
                            .Select(x => (string)x.Value!)
                            .ToImmutableArray();
                        break;
                    case "DbSetPropertyName" when namedArg.Value.Value is string customName:
                        customDbSetName = customName;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(unitOfWorkGroup))
            {
                var shortName = dbContextSymbol.Name;

                const string suffix = "DbContext";

                if (shortName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    shortName = shortName.Substring(0, shortName.Length - suffix.Length);
                }
                unitOfWorkGroup = shortName;
            }

            var entityName = classSymbol.Name;
            var entityFullName = classSymbol.ToDisplayString();
            var repositoryName = entityName + "Repository";
            var defaultDbSetName = entityName.EndsWith("s") ? entityName : entityName + "s";

            IPropertySymbol? idProperty = null;

            for (var type = classSymbol; type != null; type = type.BaseType)
            {
                idProperty = type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == "Id");

                if (idProperty != null)
                {
                    break;
                }
            }

            var idType = idProperty?.Type.ToDisplayString() ?? "int";

            return new RepositoryMetadata(
                EntityName: entityName,
                EntityFullName: entityFullName,
                RepositoryName: repositoryName,
                DefaultDbSetName: defaultDbSetName,
                DbSetPropertyName: customDbSetName,
                DbContextName: dbContextName,
                UnitOfWorkGroup: unitOfWorkGroup!,
                UnitOfWorkPropertyName: unitOfWorkPropertyName,
                Includes: includes,
                IdType: idType);
        }

        return null;
    }

    #region Helper Methods

    private static string GetPlural(string text) => text.EndsWith("s") ? text : text + "s";

    private static string GetUnitOfWorkPropertyName(RepositoryMetadata meta) =>
        !string.IsNullOrEmpty(meta.UnitOfWorkPropertyName)
            ? meta.UnitOfWorkPropertyName
            : GetPlural(meta.EntityName);

    private static void AppendAutoGeneratedHeader(StringBuilder sb)
    {
        sb.AppendLine("// ------------------------------------------------------------------------------");
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("//     This file was generated automatically by the RemoteMaster RepositoryGenerator.");
        sb.AppendLine("//     Any manual changes may be lost upon regeneration.");
        sb.AppendLine("//     Created with passion and precision.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("// ------------------------------------------------------------------------------");
        sb.AppendLine();
    }

    private static void AppendUsings(StringBuilder sb, params string[] usings)
    {
        foreach (var u in usings)
        {
            sb.AppendLine($"using {u};");
        }
    }

    private static void AppendRepositoryClass(StringBuilder sb, RepositoryMetadata meta)
    {
        var dbSetNameToUse = string.IsNullOrEmpty(meta.DbSetPropertyName)
            ? meta.DefaultDbSetName
            : meta.DbSetPropertyName;

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Auto-generated repository for the {meta.EntityName} entity.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public partial class {meta.RepositoryName} : I{meta.RepositoryName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        private readonly {meta.DbContextName} _context;");
        sb.AppendLine();
        sb.AppendLine($"        public {meta.RepositoryName}({meta.DbContextName} context) => _context = context;");
        sb.AppendLine();
        sb.AppendLine($"        public async Task<{meta.EntityFullName}?> GetByIdAsync({meta.IdType} id)");
        sb.AppendLine("        {");

        if (meta.Includes.Any())
        {
            sb.AppendLine($"            var query = _context.{dbSetNameToUse};");

            foreach (var include in meta.Includes)
            {
                sb.AppendLine($"            query = query.Include(x => x.{include});");
            }

            sb.AppendLine("            return await query.FirstOrDefaultAsync(x => x.Id == id);");
        }
        else
        {
            sb.AppendLine($"            return await _context.{dbSetNameToUse}.FirstOrDefaultAsync(x => x.Id == id);");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public async Task<IEnumerable<{meta.EntityFullName}>> GetByIdsAsync(IEnumerable<{meta.IdType}> ids)");
        sb.AppendLine("        {");
        sb.AppendLine($"            return await _context.{dbSetNameToUse}.Where(x => ids.Contains(x.Id)).ToListAsync();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public async Task<IEnumerable<{meta.EntityFullName}>> GetAllAsync()");
        sb.AppendLine("        {");
        sb.AppendLine($"            return await _context.{dbSetNameToUse}.ToListAsync();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public async Task<IEnumerable<{meta.EntityFullName}>> FindAsync(Expression<Func<{meta.EntityFullName}, bool>> predicate)");
        sb.AppendLine("        {");

        if (meta.Includes.Any())
        {
            sb.AppendLine($"            var query = _context.{dbSetNameToUse};");

            foreach (var include in meta.Includes)
            {
                sb.AppendLine($"            query = query.Include(x => x.{include});");
            }

            sb.AppendLine("            query = query.Where(predicate);");
            sb.AppendLine("            return await query.ToListAsync();");
        }
        else
        {
            sb.AppendLine($"            return await _context.{dbSetNameToUse}.Where(predicate).ToListAsync();");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public async Task AddAsync({meta.EntityFullName} entity)");
        sb.AppendLine("        {");
        sb.AppendLine($"            await _context.{dbSetNameToUse}.AddAsync(entity);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public void Update({meta.EntityFullName} entity)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _context.{dbSetNameToUse}.Update(entity);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public void Delete({meta.EntityFullName} entity)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _context.{dbSetNameToUse}.Remove(entity);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void AppendRepositoryInterface(StringBuilder sb, RepositoryMetadata meta)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Repository interface for the {meta.EntityName} entity.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public partial interface I{meta.RepositoryName} : IRepository<{meta.EntityFullName}, {meta.IdType}>");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
    }

    private static string GenerateUnitOfWorkGroupCode(IEnumerable<RepositoryMetadata> group, string dbContextName, string unitOfWorkGroup)
    {
        var sb = new StringBuilder();

        var unitOfWorkClassName = (unitOfWorkGroup.StartsWith("I") && unitOfWorkGroup.Length > 1
            ? unitOfWorkGroup.Substring(1)
            : unitOfWorkGroup) + "UnitOfWork";

        var unitOfWorkInterfaceName = "I" + unitOfWorkClassName;

        sb.AppendLine($"    public interface {unitOfWorkInterfaceName} : IUnitOfWork");
        sb.AppendLine("    {");

        foreach (var repo in group)
        {
            var repositoryInterface = "I" + repo.RepositoryName;
            var propertyName = GetUnitOfWorkPropertyName(repo);

            sb.AppendLine($"        {repositoryInterface} {propertyName} {{ get; }}");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public class {unitOfWorkClassName} : UnitOfWork<{dbContextName}>, {unitOfWorkInterfaceName}");
        sb.AppendLine("    {");

        foreach (var repo in group)
        {
            var repositoryInterface = "I" + repo.RepositoryName;
            var propertyName = GetUnitOfWorkPropertyName(repo);

            sb.AppendLine($"        public {repositoryInterface} {propertyName} {{ get; }}");
        }

        sb.AppendLine();

        sb.Append($"        public {unitOfWorkClassName}(");
        sb.Append($"{dbContextName} context, IDomainEventDispatcher domainEventDispatcher, ");
        sb.Append($"ILogger<UnitOfWork<{dbContextName}>> logger");

        foreach (var repo in group)
        {
            var repositoryInterface = "I" + repo.RepositoryName;
            var parameterName = char.ToLowerInvariant(repo.RepositoryName[0]) + repo.RepositoryName.Substring(1);

            sb.Append($", {repositoryInterface} {parameterName}");
        }

        sb.AppendLine(") : base(context, domainEventDispatcher, logger)");
        sb.AppendLine("        {");

        foreach (var repo in group)
        {
            var propertyName = GetUnitOfWorkPropertyName(repo);
            var parameterName = char.ToLowerInvariant(repo.RepositoryName[0]) + repo.RepositoryName.Substring(1);

            sb.AppendLine($"            {propertyName} = {parameterName};");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");

        return sb.ToString();
    }

    #endregion

    #region Repository Generation

    private static string GenerateSingleRepository(RepositoryMetadata meta, string baseNamespace)
    {
        var sb = new StringBuilder();
        AppendAutoGeneratedHeader(sb);
        AppendUsings(sb,
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Threading.Tasks",
            "Microsoft.EntityFrameworkCore",
            $"{baseNamespace}.Abstractions");
        sb.AppendLine();
        sb.AppendLine($"namespace {baseNamespace}.Repositories");
        sb.AppendLine("{");
        AppendRepositoryClass(sb, meta);
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateRepositories(ImmutableArray<RepositoryMetadata> metadataArray, string baseNamespace)
    {
        var sb = new StringBuilder();

        AppendAutoGeneratedHeader(sb);
        AppendUsings(sb,
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Threading.Tasks",
            "Microsoft.EntityFrameworkCore",
            $"{baseNamespace}.Abstractions");
        sb.AppendLine();
        sb.AppendLine($"namespace {baseNamespace}.Repositories");
        sb.AppendLine("{");

        foreach (var meta in metadataArray)
        {
            AppendRepositoryClass(sb, meta);
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    #endregion

    #region Repository Interface Generation

    private static string GenerateRepositoryInterface(RepositoryMetadata meta, string baseNamespace)
    {
        var sb = new StringBuilder();

        AppendAutoGeneratedHeader(sb);
        AppendUsings(sb,
            "System",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "System.Threading.Tasks",
            "Microsoft.EntityFrameworkCore",
            $"{baseNamespace}.Abstractions");
        sb.AppendLine();
        sb.AppendLine($"namespace {baseNamespace}.Repositories");
        sb.AppendLine("{");
        AppendRepositoryInterface(sb, meta);
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateRepositoryInterfaces(ImmutableArray<RepositoryMetadata> metadataArray, string baseNamespace)
    {
        var sb = new StringBuilder();

        AppendAutoGeneratedHeader(sb);
        AppendUsings(sb,
            "System",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "System.Threading.Tasks",
            "Microsoft.EntityFrameworkCore",
            $"{baseNamespace}.Abstractions");
        sb.AppendLine();
        sb.AppendLine($"namespace {baseNamespace}.Repositories");
        sb.AppendLine("{");

        foreach (var meta in metadataArray)
        {
            AppendRepositoryInterface(sb, meta);
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    #endregion

    #region Interface Generation

    private static string GenerateInterfaces(string baseNamespace)
    {
        var sb = new StringBuilder();

        AppendAutoGeneratedHeader(sb);
        AppendUsings(sb, "System", "System.Collections.Generic", "System.Linq.Expressions", "System.Threading.Tasks");

        sb.AppendLine();
        sb.AppendLine($"namespace {baseNamespace}.Abstractions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Interface for domain events.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public interface IDomainEvent { }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Base interface for aggregate roots.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public interface IAggregateRoot");
        sb.AppendLine("    {");
        sb.AppendLine("        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }");
        sb.AppendLine();
        sb.AppendLine("        void AddDomainEvent(IDomainEvent domainEvent);");
        sb.AppendLine();
        sb.AppendLine("        void ClearDomainEvents();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Generic repository interface for domain entities.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <typeparam name=\"TEntity\">The type of the entity.</typeparam>");
        sb.AppendLine("    /// <typeparam name=\"TId\">The type of the entity identifier.</typeparam>");
        sb.AppendLine("    public interface IRepository<TEntity, in TId> where TEntity : class, IAggregateRoot");
        sb.AppendLine("    {");
        sb.AppendLine("        Task<TEntity?> GetByIdAsync(TId id);");
        sb.AppendLine();
        sb.AppendLine("        Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<TId> ids);");
        sb.AppendLine();
        sb.AppendLine("        Task<IEnumerable<TEntity>> GetAllAsync();");
        sb.AppendLine();
        sb.AppendLine("        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);");
        sb.AppendLine();
        sb.AppendLine("        Task AddAsync(TEntity entity);");
        sb.AppendLine();
        sb.AppendLine("        void Update(TEntity entity);");
        sb.AppendLine();
        sb.AppendLine("        void Delete(TEntity entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public interface IUnitOfWork : IDisposable, IAsyncDisposable");
        sb.AppendLine("    {");
        sb.AppendLine("        bool IsInTransaction { get; }");
        sb.AppendLine("        Task<int> CommitAsync(CancellationToken cancellationToken = default);");
        sb.AppendLine("        Task BeginTransactionAsync(CancellationToken cancellationToken = default);");
        sb.AppendLine("        Task CommitTransactionAsync(CancellationToken cancellationToken = default);");
        sb.AppendLine("        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    #endregion

    #region UnitOfWork Generation

    private static string GenerateUnitOfWork(ImmutableArray<RepositoryMetadata> metadataArray, string baseNamespace)
    {
        var sb = new StringBuilder();

        AppendAutoGeneratedHeader(sb);
        AppendUsings(sb,
            "System",
            "System.Threading",
            "System.Threading.Tasks",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Extensions.Logging",
            $"{baseNamespace}.Abstractions");

        sb.AppendLine();
        sb.AppendLine($"namespace {baseNamespace}.UnitOfWork");
        sb.AppendLine("{");

        var groups = metadataArray.GroupBy(meta => new { meta.UnitOfWorkGroup, meta.DbContextName }).ToList();

        foreach (var group in groups)
        {
            var groupKey = group.Key;

            sb.AppendLine(GenerateUnitOfWorkGroupCode(group, groupKey.DbContextName, groupKey.UnitOfWorkGroup));
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static ImmutableArray<(string fileName, string source)> GenerateUnitOfWorkSeparateFiles(ImmutableArray<RepositoryMetadata> metadataArray, string baseNamespace)
    {
        var builder = ImmutableArray.CreateBuilder<(string, string)>();
        var groups = metadataArray.GroupBy(meta => new { meta.UnitOfWorkGroup, meta.DbContextName }).ToList();

        foreach (var group in groups)
        {
            var sb = new StringBuilder();

            AppendAutoGeneratedHeader(sb);
            AppendUsings(sb,
                "System",
                "System.Threading",
                "System.Threading.Tasks",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions.Logging",
                $"{baseNamespace}.Abstractions");

            sb.AppendLine();
            sb.AppendLine($"namespace {baseNamespace}.UnitOfWork");
            sb.AppendLine("{");

            var groupKey = group.Key;

            sb.AppendLine(GenerateUnitOfWorkGroupCode(group, groupKey.DbContextName, groupKey.UnitOfWorkGroup));
            sb.AppendLine("}");

            var unitOfWorkClassName = (groupKey.UnitOfWorkGroup.StartsWith("I") && groupKey.UnitOfWorkGroup.Length > 1
                ? groupKey.UnitOfWorkGroup.Substring(1)
                : groupKey.UnitOfWorkGroup) + "UnitOfWork";

            builder.Add(($"{unitOfWorkClassName}.g.cs", sb.ToString()));
        }

        return builder.ToImmutable();
    }

    #endregion
}
