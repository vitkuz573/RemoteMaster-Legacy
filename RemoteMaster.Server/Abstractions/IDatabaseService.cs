// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Server.Abstractions;

public interface IDatabaseService
{
    Task<IList<T>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode;
    
    Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : class, INode;

    Task<Guid> AddNodeAsync<T>(T node) where T : class, INode;

    Task RemoveNodeAsync<T>(T node) where T : class, INode;

    Task UpdateNodeAsync<T>(T node, Action<T> updateAction) where T : class, INode;

    Task MoveNodeAsync<TNode, TParent>(TNode node, TParent newParent) where TNode : class, INode where TParent : class, INode;

    Task<string[]> GetFullPathAsync<T>(Guid nodeId) where T : class, INode;
}
