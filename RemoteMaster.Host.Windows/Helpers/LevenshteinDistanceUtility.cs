// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Helpers;

public static class LevenshteinDistanceUtility
{
    public static int ComputeLevenshteinDistance(string source1, string source2)
    {
        ArgumentNullException.ThrowIfNull(source1);
        ArgumentNullException.ThrowIfNull(source2);

        var matrix = new int[source1.Length + 1, source2.Length + 1];

        for (var i = 0; i <= source1.Length; matrix[i, 0] = i++) { }
        for (var j = 0; j <= source2.Length; matrix[0, j] = j++) { }

        for (var i = 1; i <= source1.Length; i++)
        {
            for (var j = 1; j <= source2.Length; j++)
            {
                var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source1.Length, source2.Length];
    }
}