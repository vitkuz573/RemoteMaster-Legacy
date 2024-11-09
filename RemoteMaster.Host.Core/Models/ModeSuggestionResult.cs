// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Models;

/// <summary>
/// Represents a suggestion result for a launch mode, including similarity metrics.
/// </summary>
public record ModeSuggestionResult(ModeSuggestion Suggestion, double Similarity);
