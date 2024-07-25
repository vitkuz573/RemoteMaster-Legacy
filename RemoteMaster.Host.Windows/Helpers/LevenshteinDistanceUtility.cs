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

        var len1 = source1.Length;
        var len2 = source2.Length;

        var prevRow = new int[len2 + 1];
        var currentRow = new int[len2 + 1];

        for (var j = 0; j <= len2; j++)
        {
            prevRow[j] = j;
        }

        for (var i = 1; i <= len1; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= len2; j++)
            {
                var cost = (source1[i - 1] == source2[j - 1]) ? 0 : 1;

                currentRow[j] = Math.Min(Math.Min(prevRow[j] + 1, currentRow[j - 1] + 1), prevRow[j - 1] + cost);
            }

            (prevRow, currentRow) = (currentRow, prevRow);
        }

        return prevRow[len2];
    }
}