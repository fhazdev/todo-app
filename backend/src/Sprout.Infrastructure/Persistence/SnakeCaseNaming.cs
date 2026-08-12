using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Sprout.Infrastructure.Persistence;

/// <summary>
/// Renames every table, column, key, index and foreign key in the model to
/// snake_case.
/// <para>
/// Flyway owns the schema, so the SQL is hand-written and read often; snake_case
/// keeps it free of quoted "PascalCase" identifiers. Doing it by walking the model
/// covers the ASP.NET Identity tables too, which we do not configure by hand.
/// </para>
/// </summary>
internal static class SnakeCaseNaming
{
    public static void ApplySnakeCaseNames(this ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.GetTableName() is { } table)
            {
                entity.SetTableName(ToSnakeCase(table));
            }

            var storeObject = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);

            foreach (var property in entity.GetProperties())
            {
                var current = storeObject is { } target
                    ? property.GetColumnName(target)
                    : property.GetColumnName();

                property.SetColumnName(ToSnakeCase(current ?? property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? string.Empty));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName() ?? string.Empty));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ?? string.Empty));
            }
        }
    }

    /// <summary>
    /// "TodoListId" becomes "todo_list_id"; "AspNetUserClaims" becomes
    /// "asp_net_user_claims"; runs of capitals stay together, so "TokenSHA" becomes
    /// "token_sha" rather than "token_s_h_a".
    /// </summary>
    internal static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];

            if (ch == '_')
            {
                builder.Append('_');
                continue;
            }

            var boundary = i > 0
                && char.IsUpper(ch)
                && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));

            if (boundary && builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }
}
