// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

// Temporary solution until https://github.com/dotnet/runtime/issues/112111 is resolved.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace RemoteMaster.Server.Attributes;

/// <summary>
/// Specifies the minimum length of collection or string data allowed in a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class CustomMinLengthAttribute : ValidationAttribute
{
    /// <summary>
    /// Gets the minimum allowable length.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomMinLengthAttribute"/> class.
    /// </summary>
    /// <param name="length">
    /// The minimum allowable length of collection or string data.
    /// Value must be greater than or equal to zero.
    /// </param>
    public CustomMinLengthAttribute(int length) : base("The field {0} must be a string or collection type with a minimum length of {1}.")
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
        }

        Length = length;
    }

    /// <summary>
    /// Determines whether the specified value is valid.
    /// Null values are considered valid. To enforce non-null values, use the <see cref="RequiredAttribute"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>
    /// true if the value is null or its length is greater than or equal to the specified minimum; otherwise, false.
    /// </returns>
    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        int actualLength;

        if (value is string str)
        {
            actualLength = str.Length;
        }
        else if (value is ICollection collection)
        {
            actualLength = collection.Count;
        }
        else if (value is IEnumerable enumerable)
        {
            actualLength = 0;

            foreach (var _ in enumerable)
            {
                actualLength++;

                if (actualLength >= Length)
                {
                    break;
                }
            }
        }
        else
        {
            throw new InvalidCastException(string.Format(CultureInfo.CurrentCulture, "The type '{0}' is not a valid type for CustomMinLengthAttribute. It must be a string or a collection type.", value.GetType()));
        }

        return actualLength >= Length;
    }

    /// <summary>
    /// Applies formatting to a specified error message.
    /// </summary>
    /// <param name="name">The name to include in the formatted string.</param>
    /// <returns>A localized string to describe the error.</returns>
    public override string FormatErrorMessage(string name)
    {
        return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Length);
    }
}
