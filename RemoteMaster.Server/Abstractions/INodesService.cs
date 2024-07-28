// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines a contract for database operations related to node entities,
/// including CRUD operations, moving nodes, and retrieving their full path.
/// </summary>
public interface INodesService
{
    /// <summary>
    /// Retrieves nodes based on the specified predicate.
    /// </summary>
    /// <typeparam name="T">The type of node. Must implement <see cref="INode"/>.</typeparam>
    /// <param name="predicate">The predicate to filter nodes.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with a <see cref="Result{T}"/> containing a list of nodes that match the predicate.
    /// </returns>
    Task<Result<IList<T>>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode;

    /// <summary>
    /// Adds multiple new nodes to the database.
    /// </summary>
    /// <typeparam name="T">The type of nodes. Must implement <see cref="INode"/>.</typeparam>
    /// <param name="nodes">The nodes to add.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with a <see cref="Result{T}"/> containing the added nodes.
    /// </returns>
    Task<Result<IList<T>>> AddNodesAsync<T>(IEnumerable<T> nodes) where T : class, INode;

    /// <summary>
    /// Removes multiple nodes from the database.
    /// </summary>
    /// <typeparam name="T">The type of nodes. Must implement <see cref="INode"/>.</typeparam>
    /// <param name="nodes">The nodes to remove.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with a <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> RemoveNodesAsync<T>(IEnumerable<T> nodes) where T : class, INode;

    /// <summary>
    /// Updates the specified node with the given update action.
    /// </summary>
    /// <typeparam name="T">The type of node. Must implement <see cref="INode"/>.</typeparam>
    /// <param name="node">The node to update.</param>
    /// <param name="updateFunction">The function to perform on the node for updating.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with a <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> UpdateNodeAsync<T>(T node, Func<T, T> updateFunction) where T : class, INode;

    /// <summary>
    /// Moves the specified node to a new parent.
    /// </summary>
    /// <typeparam name="TNode">The type of the node. Must implement <see cref="INode"/>.</typeparam>
    /// <typeparam name="TParent">The type of the new parent node. Must implement <see cref="INode"/>.</typeparam>
    /// <param name="node">The node to move.</param>
    /// <param name="newParent">The new parent node.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with a <see cref="Result"/> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> MoveNodeAsync<TNode, TParent>(TNode node, TParent newParent) where TNode : class, INode where TParent : class, INode;

    /// <summary>
    /// Gets the full path for the specified node.
    /// </summary>
    /// <typeparam name="T">The type of the node. Must implement <see cref="INode"/>.</typeparam>
    /// <param name="node">The node to get the path for.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, with a <see cref="Result{T}"/> containing the full path of the node as an array of strings.
    /// </returns>
    Task<Result<string[]>> GetFullPathAsync<T>(T node) where T : class, INode;
}
