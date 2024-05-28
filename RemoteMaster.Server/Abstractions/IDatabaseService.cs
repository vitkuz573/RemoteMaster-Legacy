// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IDatabaseService
{
    Task<IList<Node>> GetNodesAsync(Expression<Func<Node, bool>>? predicate = null);

    Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : Node;

    Task<Guid> AddNodeAsync(Node node);

    Task RemoveNodeAsync(Node node);

    Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName);

    Task MoveNodesAsync(IEnumerable<Guid> nodeIds, Guid newParentId);

    Task<string[]> GetFullPathForOrganizationalUnitAsync(Guid ouId);

    Task<List<Guid>> GetAllowedOrganizationalUnitsForViewerAsync(string userName);
}
