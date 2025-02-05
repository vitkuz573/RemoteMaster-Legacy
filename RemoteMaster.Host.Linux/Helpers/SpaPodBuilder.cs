using System.Text;

namespace RemoteMaster.Host.Linux.Helpers;

/// <summary>
/// A fully compliant SPA POD builder.
/// 
/// A POD (Plain Old Data) is encoded as follows:
///   [Header] (8 bytes total)
///     - 4 bytes: Unsigned integer storing the size (in bytes) of the payload.
///     - 4 bytes: Unsigned integer storing the POD type.
///   [Payload]
///     - Varies by type (see documentation) and is padded to an 8-byte boundary.
/// 
/// This builder supports writing both primitive types (Bool, Int, etc.) and containers
/// (Array, Struct, Object, Sequence, etc.) as defined by the SPA type system.
/// </summary>
public class SpaPodBuilder
{
    // Internal buffer to store the POD data.
    private byte[] _buffer = [];

    // Current write position within the buffer.
    private int _position;

    #region Initialization and Buffer Retrieval

    /// <summary>
    /// Initializes the builder with a given capacity (rounded up to a multiple of 8 bytes).
    /// </summary>
    /// <param name="capacity">The capacity (in bytes) for the internal buffer.</param>
    public void InitBuilder(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));
        }

        // Calculate the capacity aligned to 8 bytes.
        var alignedCapacity = ((capacity + 7) / 8) * 8;

        _buffer = new byte[alignedCapacity];
        _position = 0;
    }

    /// <summary>
    /// Returns a copy of the internal buffer up to the current write position.
    /// The returned buffer contains the fully constructed POD.
    /// </summary>
    public byte[] GetBuffer()
    {
        if (_buffer == null)
        {
            throw new InvalidOperationException("Builder is not initialized.");
        }

        var result = new byte[_position];

        Array.Copy(_buffer, result, _position);

        return result;
    }

    #endregion

    #region Primitive POD Types

    /// <summary>
    /// Writes a POD of type None (SPA_TYPE_None, type 1).
    /// This indicates a NULL value. It has no payload.
    /// Format:
    ///   Header: [size = 0][type = 1]
    ///   No payload.
    /// </summary>
    public void WriteNone()
    {
        WritePodHeader(0, 1);
        // Even with no payload, we pad to 8-byte boundary.
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Bool POD (SPA_TYPE_Bool, type 2).
    /// The boolean value is stored as a 32-bit integer (0 = false, non-zero = true).
    /// Format:
    ///   Header: [size = 4][type = 2]
    ///   Payload: [value (int32)]
    ///   Followed by padding to 8 bytes.
    /// </summary>
    public void WriteBool(bool value)
    {
        WritePodHeader(4, 2);
        WriteInt32(value ? 1 : 0);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes an Id POD (SPA_TYPE_Id, type 3).
    /// An Id is stored as a 32-bit unsigned integer.
    /// Format:
    ///   Header: [size = 4][type = 3]
    ///   Payload: [id (uint32)]
    /// </summary>
    public void WriteId(uint id)
    {
        WritePodHeader(4, 3);
        WriteUInt32(id);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes an Int POD (SPA_TYPE_Int, type 4).
    /// A 32-bit signed integer.
    /// Format:
    ///   Header: [size = 4][type = 4]
    ///   Payload: [value (int32)]
    /// </summary>
    public void WriteInt(int value)
    {
        WritePodHeader(4, 4);
        WriteInt32(value);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Long POD (SPA_TYPE_Long, type 5).
    /// A 64-bit signed integer.
    /// Format:
    ///   Header: [size = 8][type = 5]
    ///   Payload: [value (int64)]
    /// </summary>
    public void WriteLong(long value)
    {
        WritePodHeader(8, 5);
        WriteInt64(value);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Float POD (SPA_TYPE_Float, type 6).
    /// A 32-bit floating point value.
    /// Format:
    ///   Header: [size = 4][type = 6]
    ///   Payload: [value (float32)]
    /// </summary>
    public void WriteFloat(float value)
    {
        WritePodHeader(4, 6);
        WriteFloat32(value);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Double POD (SPA_TYPE_Double, type 7).
    /// A 64-bit floating point value.
    /// Format:
    ///   Header: [size = 8][type = 7]
    ///   Payload: [value (float64)]
    /// </summary>
    public void WriteDouble(double value)
    {
        WritePodHeader(8, 7);
        WriteDouble64(value);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a String POD (SPA_TYPE_String, type 8).
    /// The string is encoded in UTF8 and stored with a terminating 0 byte.
    /// The size field equals the length of the string including the 0 byte.
    /// Format:
    ///   Header: [size = string length + 1][type = 8]
    ///   Payload: [string bytes] followed by a 0 terminator, then padding.
    /// </summary>
    public void WriteString(string s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        var strBytes = Encoding.UTF8.GetBytes(s);
        var payloadSize = strBytes.Length + 1; // +1 for the null terminator
        
        WritePodHeader((uint)payloadSize, 8);
        WriteBytes(strBytes);
        WriteByte(0); // null terminator
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Bytes POD (SPA_TYPE_Bytes, type 9).
    /// Represents an array of raw bytes.
    /// Format:
    ///   Header: [size = number of bytes][type = 9]
    ///   Payload: [raw byte array] followed by padding.
    /// </summary>
    public void WriteBytesPod(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        WritePodHeader((uint)data.Length, 9);
        WriteBytes(data);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Rectangle POD (SPA_TYPE_Rectangle, type 10).
    /// A rectangle defined by width and height.
    /// Format:
    ///   Header: [size = 8][type = 10]
    ///   Payload: [width (uint32)] [height (uint32)] followed by padding.
    /// </summary>
    public void WriteRectangle(uint width, uint height)
    {
        WritePodHeader(8, 10);
        WriteUInt32(width);
        WriteUInt32(height);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Fraction POD (SPA_TYPE_Fraction, type 11).
    /// A fraction with numerator and denominator.
    /// Format:
    ///   Header: [size = 8][type = 11]
    ///   Payload: [numerator (uint32)] [denom (uint32)] followed by padding.
    /// </summary>
    public void WriteFraction(uint numerator, uint denominator)
    {
        WritePodHeader(8, 11);
        WriteUInt32(numerator);
        WriteUInt32(denominator);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Bitmap POD (SPA_TYPE_Bitmap, type 12).
    /// A bitmap stored as an array of uint8 bits.
    /// Format:
    ///   Header: [size = number of bytes with bits][type = 12]
    ///   Payload: [bit data] followed by padding.
    /// </summary>
    public void WriteBitmap(byte[] bits)
    {
        if (bits == null)
        {
            throw new ArgumentNullException(nameof(bits));
        }

        WritePodHeader((uint)bits.Length, 12);
        WriteBytes(bits);
        PadTo8Bytes();
    }

    #endregion

    #region Container POD Types

    /// <summary>
    /// Writes an Array POD (SPA_TYPE_Array, type 13).
    /// An array of PODs of equal size and type.
    /// Format:
    ///   Header: [size][type = 13]
    ///   Payload:
    ///     - child_size (4 bytes)
    ///     - child_type (4 bytes)
    ///     - A series of child PODs (each exactly child_size bytes).
    /// The children PODs are written by the provided action.
    /// </summary>
    public void WriteArray(uint childSize, uint childType, Action childrenWriter)
    {
        if (childrenWriter == null)
        {
            throw new ArgumentNullException(nameof(childrenWriter));
        }

        // Align to an 8-byte boundary before writing the header.
        AlignPosition();
        
        var headerStart = _position;

        // Reserve header space: write payload size placeholder and type (13)
        WriteUInt32(0);       // placeholder for payload size
        WriteUInt32(13);      // POD type for Array is 13

        // Write additional header fields for Array: child_size and child_type.
        WriteUInt32(childSize);
        WriteUInt32(childType);

        // Write the children PODs.
        childrenWriter();

        // Calculate payload size (excluding the 8-byte header) and update header.
        var bodySize = (uint)(_position - headerStart - 8);
        
        WriteUInt32At(headerStart, bodySize);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Struct POD (SPA_TYPE_Struct, type 14).
    /// A collection of PODs concatenated together.
    /// Format:
    ///   Header: [size][type = 14]
    ///   Payload: concatenated PODs (each already padded to 8 bytes).
    /// </summary>
    public void WriteStruct(Action childrenWriter)
    {
        if (childrenWriter == null)
        {
            throw new ArgumentNullException(nameof(childrenWriter));
        }

        AlignPosition();
        
        var headerStart = _position;
        
        WriteUInt32(0);      // placeholder for payload size
        WriteUInt32(14);     // POD type for Struct is 14

        childrenWriter();

        var bodySize = (uint)(_position - headerStart - 8);
        
        WriteUInt32At(headerStart, bodySize);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes an Object POD (SPA_TYPE_Object, type 15).
    /// An object consists of an object type, an object id, and a series of properties.
    /// Each property is written as: key (uint32), flags (uint32), followed by a nested POD.
    /// Format:
    ///   Header: [size][type = 15]
    ///   Payload:
    ///     - object_type (4 bytes)
    ///     - object_id (4 bytes)
    ///     - For each property:
    ///         key (4 bytes)
    ///         flags (4 bytes)
    ///         nested POD value (must start at an 8-byte boundary)
    /// </summary>
    public void WriteObject(uint objectType, uint objectId, params (uint key, uint flags, Action valueWriter)[] properties)
    {
        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        AlignPosition();
        
        var headerStart = _position;
        
        WriteUInt32(0);      // placeholder for payload size
        WriteUInt32(15);     // POD type for Object is 15

        // Write object type and object id.
        WriteUInt32(objectType);
        WriteUInt32(objectId);

        // Write each property.
        foreach (var (key, flags, valueWriter) in properties)
        {
            WriteUInt32(key);
            WriteUInt32(flags);
            // Ensure the nested POD starts at an 8-byte aligned position.
            AlignPosition();
            valueWriter();
        }

        var bodySize = (uint)(_position - headerStart - 8);
        WriteUInt32At(headerStart, bodySize);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Sequence POD (SPA_TYPE_Sequence, type 16).
    /// A sequence is used to store a series of timed events (e.g., MIDI or control updates).
    /// Format:
    ///   Header: [size][type = 16]
    ///   Payload:
    ///     - unit (4 bytes)
    ///     - pad (4 bytes, currently 0)
    ///     - A series of controls, each consisting of:
    ///         offset (4 bytes), type (4 bytes), and a nested POD for the control value.
    /// The controls are written by the provided action.
    /// </summary>
    public void WriteSequence(uint unit, Action controlsWriter)
    {
        if (controlsWriter == null)
        {
            throw new ArgumentNullException(nameof(controlsWriter));
        }

        AlignPosition();
        
        var headerStart = _position;
        
        WriteUInt32(0);      // placeholder for payload size
        WriteUInt32(16);     // POD type for Sequence is 16

        WriteUInt32(unit);
        WriteUInt32(0);      // pad field (currently 0)

        controlsWriter();

        var bodySize = (uint)(_position - headerStart - 8);
        
        WriteUInt32At(headerStart, bodySize);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Pointer POD (SPA_TYPE_Pointer, type 17).
    /// Represents a typed pointer in memory.
    /// Format:
    ///   Header: [size][type = 17]
    ///   Payload:
    ///     - pointer type (uint32)
    ///     - 4 bytes of padding (must be 0)
    ///     - native pointer value (IntPtr.Size bytes)
    /// </summary>
    public void WritePointer(uint pointerType, IntPtr pointer)
    {
        // Calculate payload size: 4 bytes for pointer type, 4 bytes for padding, plus native pointer size.
        var payloadSize = 4 + 4 + IntPtr.Size;
        
        WritePodHeader((uint)payloadSize, 17);
        WriteUInt32(pointerType);
        WriteUInt32(0); // padding must be 0

        if (IntPtr.Size == 8)
        {
            WriteInt64(pointer.ToInt64());
        }
        else
        {
            WriteUInt32((uint)pointer.ToInt32());
        }

        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Fd POD (SPA_TYPE_Fd, type 18).
    /// A file descriptor stored as a 64-bit integer.
    /// Format:
    ///   Header: [size = 8][type = 18]
    ///   Payload: [fd (int64)] followed by padding.
    /// </summary>
    public void WriteFd(long fd)
    {
        WritePodHeader(8, 18);
        WriteInt64(fd);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Choice POD (SPA_TYPE_Choice, type 19).
    /// A choice contains an array of possible values.
    /// Format:
    ///   Header: [size][type = 19]
    ///   Payload:
    ///     - choice type (uint32)
    ///     - flags (uint32, must be 0)
    ///     - child_size (uint32)
    ///     - child_type (uint32)
    ///     - An array of child PODs (each exactly child_size bytes).
    /// The child PODs are written by the provided action.
    /// </summary>
    public void WriteChoice(uint choiceType, uint childSize, uint childType, Action childrenWriter)
    {
        if (childrenWriter == null)
        {
            throw new ArgumentNullException(nameof(childrenWriter));
        }

        AlignPosition();
        
        var headerStart = _position;
        
        WriteUInt32(0);      // placeholder for payload size
        WriteUInt32(19);     // POD type for Choice is 19

        WriteUInt32(choiceType);
        WriteUInt32(0);      // flags (must be 0)
        WriteUInt32(childSize);
        WriteUInt32(childType);

        childrenWriter();

        var bodySize = (uint)(_position - headerStart - 8);
        
        WriteUInt32At(headerStart, bodySize);
        PadTo8Bytes();
    }

    /// <summary>
    /// Writes a Pod POD (SPA_TYPE_Pod, type 20).
    /// This wraps a nested POD.
    /// Format:
    ///   Header: [size][type = 20]
    ///   Payload: a nested POD written by the provided action.
    /// </summary>
    public void WritePod(Action podWriter)
    {
        if (podWriter == null)
        {
            throw new ArgumentNullException(nameof(podWriter));
        }

        AlignPosition();
        
        var headerStart = _position;
        
        WriteUInt32(0);      // placeholder for payload size
        WriteUInt32(20);     // POD type for Pod is 20

        podWriter();

        var bodySize = (uint)(_position - headerStart - 8);
        
        WriteUInt32At(headerStart, bodySize);
        PadTo8Bytes();
    }

    #endregion

    #region Low-Level Write Helpers

    /// <summary>
    /// Ensures that the internal buffer has at least the specified number of bytes available.
    /// If not, the buffer size is doubled (while maintaining 8-byte alignment).
    /// </summary>
    /// <param name="bytesToWrite">Number of bytes to be written.</param>
    private void EnsureCapacity(int bytesToWrite)
    {
        if (_buffer == null)
        {
            throw new InvalidOperationException("Builder is not initialized.");
        }

        if (_position + bytesToWrite <= _buffer.Length)
        {
            return;
        }

        var newCapacity = _buffer.Length;
        
        while (_position + bytesToWrite > newCapacity)
        {
            newCapacity *= 2;
        }

        newCapacity = (newCapacity + 7) / 8 * 8;

        Array.Resize(ref _buffer, newCapacity);
    }

    /// <summary>
    /// Aligns the current write position to an 8-byte boundary by writing zero-padding bytes.
    /// </summary>
    private void AlignPosition()
    {
        var misalignment = _position % 8;
        
        if (misalignment == 0)
        {
            return;
        }

        var padding = 8 - misalignment;
        
        EnsureCapacity(padding);
        
        for (var i = 0; i < padding; i++)
        {
            _buffer[_position++] = 0;
        }
    }

    /// <summary>
    /// Pads the current POD (or nested POD) so that the total size is a multiple of 8 bytes.
    /// </summary>
    private void PadTo8Bytes()
    {
        var misalignment = _position % 8;
        
        if (misalignment == 0)
        {
            return;
        }

        var padding = 8 - misalignment;
        
        EnsureCapacity(padding);
        
        for (var i = 0; i < padding; i++)
        {
            _buffer[_position++] = 0;
        }
    }

    /// <summary>
    /// Writes a 4-byte unsigned integer at the current position and advances the position.
    /// </summary>
    private void WriteUInt32(uint value)
    {
        EnsureCapacity(4);
        
        var bytes = BitConverter.GetBytes(value);
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        Array.Copy(bytes, 0, _buffer, _position, 4);
        
        _position += 4;
    }

    /// <summary>
    /// Writes a 4-byte signed integer at the current position and advances the position.
    /// </summary>
    private void WriteInt32(int value)
    {
        EnsureCapacity(4);
        
        var bytes = BitConverter.GetBytes(value);
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        Array.Copy(bytes, 0, _buffer, _position, 4);
        
        _position += 4;
    }

    /// <summary>
    /// Writes a 4-byte floating point value at the current position and advances the position.
    /// </summary>
    private void WriteFloat32(float value)
    {
        EnsureCapacity(4);
        
        var bytes = BitConverter.GetBytes(value);
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        Array.Copy(bytes, 0, _buffer, _position, 4);
        
        _position += 4;
    }

    /// <summary>
    /// Writes a single byte at the current position.
    /// </summary>
    private void WriteByte(byte value)
    {
        EnsureCapacity(1);

        _buffer[_position++] = value;
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
        
        Array.Copy(bytes, 0, _buffer, _position, bytes.Length);
        
        _position += bytes.Length;
    }

    /// <summary>
    /// Writes a 4-byte unsigned integer at a specified offset.
    /// This is used to update a previously reserved header field.
    /// </summary>
    private void WriteUInt32At(int offset, uint value)
    {
        if (offset < 0 || offset + 4 > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        var bytes = BitConverter.GetBytes(value);
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        Array.Copy(bytes, 0, _buffer, offset, 4);
    }

    /// <summary>
    /// Writes an 8-byte signed integer at the current position and advances the position.
    /// </summary>
    private void WriteInt64(long value)
    {
        EnsureCapacity(8);
        
        var bytes = BitConverter.GetBytes(value);
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        Array.Copy(bytes, 0, _buffer, _position, 8);

        _position += 8;
    }

    /// <summary>
    /// Writes an 8-byte double value at the current position and advances the position.
    /// </summary>
    private void WriteDouble64(double value)
    {
        EnsureCapacity(8);
        
        var bytes = BitConverter.GetBytes(value);
        
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        Array.Copy(bytes, 0, _buffer, _position, 8);
        
        _position += 8;
    }

    /// <summary>
    /// Writes the POD header.
    /// This method writes an 8-byte header consisting of:
    ///   - 4 bytes: payload size (in bytes)
    ///   - 4 bytes: POD type
    /// After calling this method, the caller should write the payload and then call PadTo8Bytes().
    /// </summary>
    /// <param name="payloadSize">The size of the payload (excluding the header).</param>
    /// <param name="podType">The POD type identifier.</param>
    private void WritePodHeader(uint payloadSize, uint podType)
    {
        // Ensure that the POD starts at an 8-byte aligned position.
        AlignPosition();
        var headerStart = _position;
        
        WriteUInt32(payloadSize);
        WriteUInt32(podType);
        // The header is now written; the payload will follow.
    }

    #endregion
}
