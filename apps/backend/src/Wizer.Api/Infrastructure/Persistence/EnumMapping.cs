using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Wizer.Api.Infrastructure.Persistence;

/// <summary>
/// Enums are stored as snake_case <c>text</c> with a <c>CHECK</c> constraint, never as integers
/// (<c>.ia/database.md</c> §Tipos). <c>PassFail</c> is stored as <c>pass_fail</c>.
/// </summary>
public static class EnumMapping
{
    public static string ToDbValue<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    public static TEnum FromDbValue<TEnum>(string value)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Single(candidate => ToDbValue(candidate) == value);

    public static PropertyBuilder<TEnum> HasSnakeCaseConversion<TEnum>(this PropertyBuilder<TEnum> property)
        where TEnum : struct, Enum =>
        property.HasConversion(new SnakeCaseEnumConverter<TEnum>()).HasMaxLength(MaxLength<TEnum>());

    public static PropertyBuilder<TEnum?> HasSnakeCaseConversion<TEnum>(this PropertyBuilder<TEnum?> property)
        where TEnum : struct, Enum =>
        property.HasConversion(new SnakeCaseEnumConverter<TEnum>()).HasMaxLength(MaxLength<TEnum>());

    /// <summary>Adds <c>ck_{table}_{column}</c> restricting the column to the enum values.</summary>
    public static void HasEnumCheck<TEnum>(this TableBuilder table, string column)
        where TEnum : struct, Enum
    {
        var values = string.Join(", ", Enum.GetValues<TEnum>().Select(value => $"'{ToDbValue(value)}'"));
        table.HasCheckConstraint($"ck_{table.Name}_{column}", $"{column} IN ({values})");
    }

    private static int MaxLength<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Max(value => ToDbValue(value).Length);

    private sealed class SnakeCaseEnumConverter<TEnum>()
        : ValueConverter<TEnum, string>(value => ToDbValue(value), value => FromDbValue<TEnum>(value))
        where TEnum : struct, Enum;
}
