// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Helpers;

/// <summary>
/// Provides a method to compute the Levenshtein distance between two strings.
/// </summary>
public static class LevenshteinDistance
{
    /// <summary>
    /// Computes the Levenshtein distance between two strings.
    /// </summary>
    /// <param name="source1">The first string to compare.</param>
    /// <param name="source2">The second string to compare.</param>
    /// <returns>The Levenshtein distance between the two strings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either source1 or source2 is null.</exception>
    public static int Compute(string source1, string source2)
    {
        ArgumentNullException.ThrowIfNull(source1);
        ArgumentNullException.ThrowIfNull(source2);

        if (source1.Length == 0)
        {
            return source2.Length;
        }

        if (source2.Length == 0)
        {
            return source1.Length;
        }

        Span<int> distances = stackalloc int[source2.Length + 1];

        for (var j = 0; j <= source2.Length; j++)
        {
            distances[j] = j;
        }

        for (var i = 1; i <= source1.Length; i++)
        {
            var previousDistance = distances[0];

            distances[0] = i;

            for (var j = 1; j <= source2.Length; j++)
            {
                var temp = distances[j];

                distances[j] = Math.Min(Math.Min(distances[j] + 1, distances[j - 1] + 1), previousDistance + (source1[i - 1] == source2[j - 1] ? 0 : 1));
                previousDistance = temp;
            }
        }

        return distances[source2.Length];
    }
}
