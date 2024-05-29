// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IDatabaseService
{
    Task<IList<INode>> GetNodesAsync(Expression<Func<INode, bool>>? predicate = null);

    Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : INode;

    Task<Guid> AddNodeAsync(INode node);

    Task RemoveNodeAsync(INode node);

    Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName);

    Task MoveNodesAsync(IEnumerable<Guid> nodeIds, Guid newParentId);

    Task<string[]> GetFullPathForOrganizationalUnitAsync(Guid ouId);

    Task<List<Guid>> GetAllowedOrganizationalUnitsForViewerAsync(string userName);
}
