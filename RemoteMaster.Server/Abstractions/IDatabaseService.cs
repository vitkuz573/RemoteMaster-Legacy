// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IDatabaseService
{
    Task<IList<T>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode;
    
    Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : class, INode;

    Task<Guid> AddNodeAsync<T>(T node) where T : class, INode;

    Task RemoveNodeAsync<T>(T node) where T : class, INode;

    Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName);

    Task MoveNodesAsync(IEnumerable<Guid> nodeIds, Guid newParentId);

    Task<string[]> GetFullPathForOrganizationalUnitAsync(Guid ouId);
}
