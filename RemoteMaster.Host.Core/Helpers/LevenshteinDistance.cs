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

        var len1 = source1.Length;
        var len2 = source2.Length;

        if (len1 == 0)
        {
            return len2;
        }

        if (len2 == 0)
        {
            return len1;
        }

        var previousDistances = new int[len2 + 1];

        for (var j = 0; j <= len2; j++)
        {
            previousDistances[j] = j;
        }

        for (var i = 1; i <= len1; i++)
        {
            var currentDistances = new int[len2 + 1];
            currentDistances[0] = i;

            for (var j = 1; j <= len2; j++)
            {
                var cost = source1[i - 1] == source2[j - 1] ? 0 : 1;

                currentDistances[j] = Math.Min(Math.Min(previousDistances[j] + 1, currentDistances[j - 1] + 1), previousDistances[j - 1] + cost);
            }

            previousDistances = currentDistances;
        }

        return previousDistances[len2];
    }
}
