using System;
using System.Collections.Generic;
using System.Text;

namespace RemoteMaster.Host.Linux.Helpers
{
    /// <summary>
    /// A fully compliant SPA POD builder.
    /// 
    /// A POD is encoded as:
    ///   [Header] (8 bytes total)
    ///     - 4 bytes: Unsigned integer storing the size (in bytes) of the payload (body).
    ///     - 4 bytes: Unsigned integer storing the POD type.
    ///   [Payload]
    ///     - Contents vary by type (see docs) and are padded to 8-byte boundaries.
    /// 
    /// This builder supports writing basic types and containers such as Objects.
    /// 
    /// For example, an Object POD (POD type 15) has the following layout:
    ///   Header: size, type (15)
    ///   Body:
    ///     - object_type (4 bytes)
    ///     - object_id   (4 bytes)
    ///     - For each property:
    ///           key (4 bytes)
    ///           flags (4 bytes)
    ///           POD value (complete POD, with header, payload, and padding)
    /// 
    /// Additional types (Bool, Int, String, etc.) are built following the documented layouts.
    /// </summary>
    public class SpaPodBuilder
    {
        private byte[] buffer;
        private int position;

        /// <summary>
        /// Initializes the builder with a given capacity (rounded up to an 8-byte multiple).
        /// </summary>
        /// <param name="capacity">The capacity (in bytes) for the internal buffer.</param>
        public void InitBuilder(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException("Capacity must be positive.", nameof(capacity));
            }

            int alignedCapacity = ((capacity + 7) / 8) * 8;
            buffer = new byte[alignedCapacity];
            position = 0;
        }

        /// <summary>
        /// Returns a copy of the internal buffer up to the current write position.
        /// </summary>
        public byte[] GetBuffer()
        {
            if (buffer == null)
            {
                throw new InvalidOperationException("Builder is not initialized.");
            }

            var result = new byte[position];
            Array.Copy(buffer, result, position);
            return result;
        }

        #region High-Level POD Write Methods

        /// <summary>
        /// Writes a basic Int POD (type 4) with the given 32-bit signed value.
        /// Layout:
        ///   Header: [size=4][type=4]
        ///   Body: [value (int32)] + padding.
        /// </summary>
        public void WriteInt(int value)
        {
            // Payload size is 4 bytes.
            WritePodHeader(4, 4);
            WriteInt32(value);
            PadTo8Bytes();
        }

        /// <summary>
        /// Writes a basic Float POD (type 6) with the given 32-bit float value.
        /// </summary>
        public void WriteFloat(float value)
        {
            WritePodHeader(4, 6);
            WriteFloat32(value);
            PadTo8Bytes();
        }

        /// <summary>
        /// Writes a String POD (type 8) for a given string.
        /// The string is encoded in ASCII (or UTF8) and stored with a terminating 0.
        /// The size field is the string length including the 0 byte.
        /// </summary>
        public void WriteString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            // Get bytes and add terminating null.
            byte[] strBytes = Encoding.UTF8.GetBytes(s);
            int payloadSize = strBytes.Length + 1;
            WritePodHeader((uint)payloadSize, 8);
            WriteBytes(strBytes);
            WriteByte(0); // 0 terminator.
            PadTo8Bytes();
        }

        /// <summary>
        /// Writes an Object POD (type 15) with the specified object type, object id, and a set of properties.
        /// Each property is a triple: (key, flags, Action that writes the POD value).
        /// The Action should invoke one of the POD writing methods so that a full nested POD is built.
        /// </summary>
        public void WriteObject(uint objectType, uint objectId, params (uint key, uint flags, Action valueWriter)[] properties)
        {
            ArgumentNullException.ThrowIfNull(properties);

            // Remember the start position for header.
            AlignPosition();
            int headerStart = position;

            // Reserve header space: 8 bytes.
            WriteUInt32(0);         // Placeholder for body size.
            WriteUInt32(15);        // POD type for Object is 15.

            // Begin object body:
            WriteUInt32(objectType);  // Object type (4 bytes)
            WriteUInt32(objectId);    // Object id (4 bytes)

            // For each property: write key, flags, then nested POD value.
            foreach (var (key, flags, valueWriter) in properties)
            {
                WriteUInt32(key);
                WriteUInt32(flags);
                // The nested POD value must be written at an 8-byte–aligned position.
                AlignPosition();
                // Capture the current position so that the nested POD is self-contained.
                valueWriter();
            }

            // Calculate body size = current position - headerStart - 8 bytes header.
            uint bodySize = (uint)(position - headerStart - 8);
            // Go back and update header size.
            WriteUInt32At(headerStart, bodySize);

            // Pad overall POD to 8-byte boundary.
            PadTo8Bytes();
        }

        /// <summary>
        /// Writes a generic POD header.
        /// This writes an 8-byte header: first 4 bytes = payload size, second 4 bytes = POD type.
        /// The payload will be written next.
        /// </summary>
        /// <param name="payloadSize">Size of the payload in bytes.</param>
        /// <param name="podType">POD type identifier (see docs).</param>
        public void WritePodHeader(uint payloadSize, uint podType)
        {
            AlignPosition();
            int headerStart = position;
            WriteUInt32(payloadSize); // Placeholder size (payload size only)
            WriteUInt32(podType);
            // Note: caller is responsible for writing payload and later ensuring overall POD is padded.
        }

        #endregion

        #region Low-Level Write Helpers

        /// <summary>
        /// Ensures that the internal buffer has at least the specified number of bytes available.
        /// If not, it doubles the buffer size (always maintaining an 8-byte alignment).
        /// </summary>
        private void EnsureCapacity(int bytesToWrite)
        {
            if (buffer == null)
            {
                throw new InvalidOperationException("Builder is not initialized.");
            }

            if (position + bytesToWrite > buffer.Length)
            {
                int newCapacity = buffer.Length;
                while (position + bytesToWrite > newCapacity)
                {
                    newCapacity *= 2;
                }

                newCapacity = ((newCapacity + 7) / 8) * 8;
                Array.Resize(ref buffer, newCapacity);
            }
        }

        /// <summary>
        /// Aligns the current write position to an 8-byte boundary by writing zero-padding bytes.
        /// </summary>
        private void AlignPosition()
        {
            int misalignment = position % 8;
            if (misalignment != 0)
            {
                int padding = 8 - misalignment;
                EnsureCapacity(padding);
                for (int i = 0; i < padding; i++)
                {
                    buffer[position++] = 0;
                }
            }
        }

        /// <summary>
        /// Pads the current POD (or nested POD) so that the total size is a multiple of 8 bytes.
        /// </summary>
        private void PadTo8Bytes()
        {
            int misalignment = position % 8;
            if (misalignment != 0)
            {
                int padding = 8 - misalignment;
                EnsureCapacity(padding);
                for (int i = 0; i < padding; i++)
                {
                    buffer[position++] = 0;
                }
            }
        }

        /// <summary>
        /// Writes a 4‑byte unsigned integer at the current position and advances the position.
        /// </summary>
        private void WriteUInt32(uint value)
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
        /// Writes a 4‑byte signed integer at the current position and advances the position.
        /// </summary>
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
        /// Writes a 4‑byte 32‑bit float at the current position and advances the position.
        /// </summary>
        private void WriteFloat32(float value)
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
        /// Writes a single byte at the current position.
        /// </summary>
        private void WriteByte(byte value)
        {
            EnsureCapacity(1);
            buffer[position++] = value;
        }

        /// <summary>
        /// Writes an array of bytes at the current position.
        /// </summary>
        private void WriteBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            EnsureCapacity(bytes.Length);
            Array.Copy(bytes, 0, buffer, position, bytes.Length);
            position += bytes.Length;
        }

        /// <summary>
        /// Writes a 4‑byte unsigned integer at a specified offset.
        /// Used to update a previously reserved header field.
        /// </summary>
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

        #endregion

        #region Example Usage Methods

        // The following example methods show how a client might use the builder.

        /// <summary>
        /// Example: Builds a POD representing an object with two properties.
        /// Property 1: key = 100, flags = 0, value is an Int POD with value 42.
        /// Property 2: key = 200, flags = 0, value is a Float POD with value 3.14.
        /// </summary>
        public void BuildExampleObject()
        {
            InitBuilder(128);

            WriteObject(
                objectType: 0x10,    // For example, SPA_TYPE_OBJECT_Example
                objectId: 0x01,
                // Property 1
                (key: 100u, flags: 0u, valueWriter: () => WriteInt(42)),
                // Property 2
                (key: 200u, flags: 0u, valueWriter: () => WriteFloat(3.14f))
            );
        }

        /// <summary>
        /// Example: Builds a POD representing a string.
        /// </summary>
        public void BuildExampleString()
        {
            InitBuilder(64);
            WriteString("Hello, SPA POD!");
        }

        #endregion
    }
}
