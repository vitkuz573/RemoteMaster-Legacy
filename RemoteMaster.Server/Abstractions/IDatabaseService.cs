// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Interface for database operations related to nodes.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Retrieves nodes based on the specified predicate.
    /// </summary>
    /// <typeparam name="T">The type of node.</typeparam>
    /// <param name="predicate">The predicate to filter nodes.</param>
    /// <returns>A list of nodes that match the predicate.</returns>
    Task<IList<T>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode;

    /// <summary>
    /// Adds a new node to the database.
    /// </summary>
    /// <typeparam name="T">The type of node.</typeparam>
    /// <param name="node">The node to add.</param>
    /// <returns>The ID of the added node.</returns>
    Task<T> AddNodeAsync<T>(T node) where T : class, INode;

    /// <summary>
    /// Removes the specified node from the database.
    /// </summary>
    /// <typeparam name="T">The type of node.</typeparam>
    /// <param name="node">The node to remove.</param>
    Task RemoveNodeAsync<T>(T node) where T : class, INode;

    /// <summary>
    /// Updates the specified node with the given update action.
    /// </summary>
    /// <typeparam name="T">The type of node.</typeparam>
    /// <param name="node">The node to update.</param>
    /// <param name="updateAction">The action to perform on the node for updating.</param>
    Task UpdateNodeAsync<T>(T node, Action<T> updateAction) where T : class, INode;

    /// <summary>
    /// Moves the specified node to a new parent.
    /// </summary>
    /// <typeparam name="TNode">The type of the node.</typeparam>
    /// <typeparam name="TParent">The type of the new parent node.</typeparam>
    /// <param name="node">The node to move.</param>
    /// <param name="newParent">The new parent node.</param>
    Task MoveNodeAsync<TNode, TParent>(TNode node, TParent newParent) where TNode : class, INode where TParent : class, INode;

    /// <summary>
    /// Gets the full path for the specified node.
    /// </summary>
    /// <typeparam name="T">The type of the node.</typeparam>
    /// <param name="node">The node to get the path for.</param>
    /// <returns>The full path of the node as an array of strings.</returns>
    Task<string[]> GetFullPathAsync<T>(T node) where T : class, INode;
}
