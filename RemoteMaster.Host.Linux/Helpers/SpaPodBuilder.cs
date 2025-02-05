// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Linux.Helpers;

/// <summary>
/// This class implements a SPA POD builder that builds a binary blob
/// representing a POD object. The binary format is defined as follows:
/// 
///   [Header] (8 bytes total)
///     - 4 bytes: Unsigned integer that stores the size of the object body (excluding the header).
///     - 4 bytes: Unsigned integer that stores the object type.
/// 
///   [Body]
///     - 4 bytes: Unsigned integer representing the object id.
///     - A sequence of key-value pairs. Each key is a 4‑byte unsigned integer and each value is a 4‑byte integer.
///       (So each pair is 8 bytes.)
/// 
/// The builder reserves space for the header, writes the object id and key–value pairs,
/// computes the object body size (4 bytes for the object id + 8 * number_of_pairs),
/// and then goes back and updates the header with the correct body size.
/// 
/// This implementation mimics the original C API functions:
///   - spa_pod_builder_init: (here, InitBuilder)
///   - spa_pod_builder_add_object (two overloads)
///   - and GetBuffer returns the built binary blob.
/// 
/// Memory alignment is handled by ensuring all writes are 4‑byte quantities.
/// </summary>
public class SpaPodBuilder
{
    // The internal byte buffer.
    private byte[] buffer;

    // The current write position in the buffer.
    private int position;

    /// <summary>
    /// Initializes the builder with a given capacity. This mimics spa_pod_builder_init.
    /// </summary>
    /// <param name="capacity">The capacity (in bytes) for the internal byte buffer.</param>
    public void InitBuilder(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));
        }

        buffer = new byte[capacity];
        position = 0;
    }

    /// <summary>
    /// Adds an object to the builder using a variable number of key–value pairs.
    /// This overload expects exactly 5 key–value pairs.
    /// </summary>
    /// <param name="type">The object type (4-byte unsigned integer).</param>
    /// <param name="id">The object id (4-byte unsigned integer).</param>
    /// <param name="keyValues">A params array of key–value pairs; must have exactly 5 pairs.</param>
    public void AddObject(uint type, uint id, params (uint key, int value)[] keyValues)
    {
        if (keyValues == null || keyValues.Length != 5)
        {
            throw new ArgumentException("This overload requires exactly 5 key-value pairs.", nameof(keyValues));
        }

        WriteObject(type, id, keyValues);
    }

    /// <summary>
    /// Adds an object to the builder using exactly 7 key–value pairs.
    /// </summary>
    /// <param name="type">The object type (4-byte unsigned integer).</param>
    /// <param name="id">The object id (4-byte unsigned integer).</param>
    /// <param name="key1">Key-value pair 1.</param>
    /// <param name="key2">Key-value pair 2.</param>
    /// <param name="key3">Key-value pair 3.</param>
    /// <param name="key4">Key-value pair 4.</param>
    /// <param name="key5">Key-value pair 5.</param>
    /// <param name="key6">Key-value pair 6.</param>
    /// <param name="key7">Key-value pair 7.</param>
    public void AddObject(uint type, uint id,
                          (uint key, int value) key1,
                          (uint key, int value) key2,
                          (uint key, int value) key3,
                          (uint key, int value) key4,
                          (uint key, int value) key5,
                          (uint key, int value) key6,
                          (uint key, int value) key7)
    {
        // Create an array with the 7 pairs.
        var keyValues = new (uint key, int value)[]
        {
                key1, key2, key3, key4, key5, key6, key7
        };

        WriteObject(type, id, keyValues);
    }

    /// <summary>
    /// Returns the complete byte array built so far. The returned array contains exactly the bytes written.
    /// </summary>
    /// <returns>A byte array with the binary blob.</returns>
    public byte[] GetBuffer()
    {
        if (buffer == null)
        {
            throw new InvalidOperationException("Builder is not initialized.");
        }

        // Return a copy of the buffer up to the current write position.
        return buffer.Take(position).ToArray();
    }

    /// <summary>
    /// Writes an object into the internal buffer. This function reserves space for the header,
    /// writes the object id and all key–value pairs, calculates the size of the object body,
    /// and then goes back and updates the header’s size field.
    /// </summary>
    /// <param name="type">Object type (4 bytes, unsigned).</param>
    /// <param name="id">Object id (4 bytes, unsigned).</param>
    /// <param name="keyValues">An array of key–value pairs.</param>
    private void WriteObject(uint type, uint id, (uint key, int value)[] keyValues)
    {
        if (buffer == null)
        {
            throw new InvalidOperationException("Builder is not initialized.");
        }

        // Ensure alignment: our position should be a multiple of 4.
        if (position % 4 != 0)
        {
            throw new InvalidOperationException("Buffer is not aligned to 4 bytes.");
        }

        // Remember the starting position of this object.
        int headerStart = position;

        // Reserve space for header: 4 bytes for the body size (placeholder) and 4 bytes for the object type.
        WriteUInt32(0);    // Placeholder for object body size
        WriteUInt32(type); // Write object type

        // Write the object id (this is the first field in the object body).
        WriteUInt32(id);

        // Write the sequence of key–value pairs.
        foreach (var (key, value) in keyValues)
        {
            WriteUInt32(key); // 4 bytes key
            WriteInt32(value); // 4 bytes value
        }

        // Calculate the size of the object body.
        // The body consists of the object id (4 bytes) plus 8 bytes per key–value pair.
        int numPairs = keyValues.Length;
        uint bodySize = (uint)(4 + numPairs * 8);

        // Go back to the header start and update the first 4 bytes with the body size.
        WriteUInt32At(headerStart, bodySize);

        // The header is now complete and the object is built.
        // The current position remains at the end of the object.
    }

    #region Low-Level Write Helpers

    /// <summary>
    /// Writes a 4‑byte unsigned integer (uint) at the current position and advances the position.
    /// </summary>
    /// <param name="value">The unsigned integer value to write.</param>
    private void WriteUInt32(uint value)
    {
        EnsureCapacity(4);

        // Get the 4 bytes (little-endian). Reverse if necessary.
        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        Array.Copy(bytes, 0, buffer, position, 4);
        position += 4;
    }

    /// <summary>
    /// Writes a 4‑byte signed integer (int) at the current position and advances the position.
    /// </summary>
    /// <param name="value">The integer value to write.</param>
    private void WriteInt32(int value)
    {
        EnsureCapacity(4);

        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        Array.Copy(bytes, 0, buffer, position, 4);
        position += 4;
    }

    /// <summary>
    /// Writes a 4‑byte unsigned integer (uint) at a specified offset in the buffer.
    /// This is used to update the header once the object body size is known.
    /// </summary>
    /// <param name="offset">The offset at which to write (must be within the buffer).</param>
    /// <param name="value">The unsigned integer value to write.</param>
    private void WriteUInt32At(int offset, uint value)
    {
        if (offset < 0 || offset + 4 > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        Array.Copy(bytes, 0, buffer, offset, 4);
    }

    /// <summary>
    /// Ensures that there is enough space left in the internal buffer to write the specified number of bytes.
    /// Throws an exception if not enough capacity is available.
    /// </summary>
    /// <param name="bytesToWrite">Number of bytes that need to be written.</param>
    private void EnsureCapacity(int bytesToWrite)
    {
        if (buffer == null)
        {
            throw new InvalidOperationException("Builder is not initialized.");
        }

        if (position + bytesToWrite > buffer.Length)
        {
            throw new InvalidOperationException($"Buffer overflow: trying to write {bytesToWrite} bytes, but only {buffer.Length - position} bytes remain.");
        }
    }

    #endregion
}
