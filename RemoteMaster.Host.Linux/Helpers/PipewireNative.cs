// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers
{
    // === SPA POD Definitions ===
    // From spa/pod/pod.h:
    // The original header defines the basic POD as:
    //   struct spa_pod {
    //       uint32_t size;  // size of the body
    //       uint32_t type;  // type code
    //   };
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct SpaPod
    {
        public uint size; // Size of the body in bytes
        public uint type; // Type code (as defined in SPA)
    }

    // === VideoFormatPod ===
    // Typically defined in SPA headers (or derived from builder examples)
    // Example (C header):
    //   struct video_format_pod {
    //       struct spa_pod pod;
    //       uint32_t format;
    //       uint32_t width;
    //       uint32_t height;
    //       uint32_t framerate_num;
    //       uint32_t framerate_den;
    //   };
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct VideoFormatPod
    {
        public SpaPod pod;         // Base spa_pod header
        public uint format;        // Video format code (e.g. RGB)
        public uint width;         // Video width
        public uint height;        // Video height
        public uint framerate_num; // Frame rate numerator
        public uint framerate_den; // Frame rate denominator
    }

    // === BufferParamPod ===
    // Example C header definition might be:
    //   struct buffer_param_pod {
    //       struct spa_pod pod;
    //       uint32_t buffers;
    //       uint32_t blocks;
    //       uint32_t size;
    //       uint32_t stride;
    //       uint32_t align;
    //       int dataType;
    //       int metaType;
    //   };
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct BufferParamPod
    {
        public SpaPod pod;      // Base spa_pod header
        public uint buffers;    // Number of buffers (from SPA specs)
        public uint blocks;     // Number of blocks per buffer
        public uint size;       // Total size per buffer
        public uint stride;     // Stride (bytes per row)
        public uint align;      // Required memory alignment
        public int dataType;    // Data type flags (per SPA)
        public int metaType;    // Metadata type (if any)
    }

    // === SPA Buffer Structures for Data Extraction ===
    // The PipeWire buffer (spa_buffer) contains an array of spa_data elements.
    // Here we define a simplified version of these structures.

    // Represents a PipeWire buffer (spa_buffer)
    [StructLayout(LayoutKind.Sequential)]
    public struct SpaBuffer
    {
        public uint n_metas;   // Number of metadata elements
        public uint n_datas;   // Number of spa_data elements
        public nint metas;   // Pointer to an array of spa_meta (not used in this example)
        public nint datas;   // Pointer to an array of spa_data elements
    }

    // Represents a data block descriptor (spa_data)
    [StructLayout(LayoutKind.Sequential)]
    public struct SpaData
    {
        public uint type;       // Memory type (e.g. SPA_DATA_MemPtr, SPA_DATA_DmaBuf, etc.)
        public uint flags;      // Data flags (e.g. SPA_DATA_FLAG_READABLE, SPA_DATA_FLAG_MAPPABLE, etc.)
        public long fd;         // File descriptor (if applicable, e.g. for DMA-BUF)
        public uint mapoffset;  // Offset for mmap (usually page aligned)
        public uint maxsize;    // Maximum size of the data block
        public nint data;     // Direct pointer to data (if available)
        public nint chunk;    // Pointer to a spa_chunk structure with valid data info (optional)
    }

    // Represents a valid data chunk descriptor (spa_chunk)
    [StructLayout(LayoutKind.Sequential)]
    public struct SpaChunk
    {
        public uint offset; // Offset of the valid data within the memory block
        public uint size;   // Size of the valid data
        public int stride;  // Stride (bytes per row, if applicable)
        public int flags;   // Flags (e.g. SPA_CHUNK_FLAG_EMPTY, etc.)
    }

    public static class PipewireNative
    {
        private const string LIB = "libpipewire-0.3";

        #region PipeWire Core Functions
        // Functions from pipewire.h and pipewire/stream.h
        // These functions initialize PipeWire, create a main loop,
        // and create/connect/destroy streams.
        [DllImport(LIB)]
        public static extern void pw_init(ref int argc, ref nint argv);

        [DllImport(LIB)]
        public static extern nint pw_main_loop_new(nint properties);

        [DllImport(LIB)]
        public static extern nint pw_main_loop_get_loop(nint mainLoop);

        [DllImport(LIB)]
        public static extern int pw_main_loop_run(nint mainLoop);

        [DllImport(LIB)]
        public static extern int pw_main_loop_quit(nint mainLoop);

        [DllImport(LIB)]
        public static extern void pw_main_loop_destroy(nint mainLoop);

        [DllImport(LIB)]
        public static extern nint pw_stream_new_simple(nint loop,
                                                       [MarshalAs(UnmanagedType.LPStr)] string name,
                                                       nint properties,
                                                       ref pw_stream_events events,
                                                       nint userData);

        [DllImport(LIB)]
        public static extern int pw_stream_connect(nint stream,
                                                   int direction,
                                                   uint target_id,
                                                   uint flags,
                                                   nint parameters,
                                                   uint n_params);

        [DllImport(LIB)]
        public static extern nint pw_stream_dequeue_buffer(nint stream);

        [DllImport(LIB)]
        public static extern int pw_stream_queue_buffer(nint stream, nint buffer);

        [DllImport(LIB)]
        public static extern int pw_stream_disconnect(nint stream);

        [DllImport(LIB)]
        public static extern void pw_stream_destroy(nint stream);
        #endregion

        #region Properties Functions
        // Functions from pipewire/properties.h
        [DllImport(LIB, EntryPoint = "pw_properties_new")]
        public static extern nint pw_properties_new(
            [MarshalAs(UnmanagedType.LPStr)] string key1,
            [MarshalAs(UnmanagedType.LPStr)] string value1,
            [MarshalAs(UnmanagedType.LPStr)] string key2,
            [MarshalAs(UnmanagedType.LPStr)] string value2,
            nint end); // Use nint.Zero as sentinel.

        [DllImport(LIB, EntryPoint = "pw_properties_set")]
        public static extern void pw_properties_set(nint properties,
                                                    [MarshalAs(UnmanagedType.LPStr)] string key,
                                                    [MarshalAs(UnmanagedType.LPStr)] string value);
        #endregion

        #region Building SPA PODs
        private const uint SPA_TYPE_Format = 0x100;         // (verify against your SPA headers)
        private const uint SPA_TYPE_BufferParam = 0x101;      // (verify against your SPA headers)
        private const uint SPA_VIDEO_FORMAT_RGB = 0x34325241; // 'AR24' in little-endian

        private static int RoundUp8(int size)
        {
            return (size + 7) / 8 * 8;
        }

        /// <summary>
        /// Builds a VideoFormatPod for the given parameters.
        /// </summary>
        public static nint BuildVideoFormatPod(uint width, uint height, uint framerateNum, uint framerateDen)
        {
            var structSize = Marshal.SizeOf<VideoFormatPod>();
            var headerSize = Marshal.SizeOf<SpaPod>();
            var payloadSize = (uint)(structSize - headerSize);

            var pod = new VideoFormatPod
            {
                pod = new SpaPod
                {
                    size = payloadSize,
                    type = SPA_TYPE_Format
                },
                format = SPA_VIDEO_FORMAT_RGB,
                width = width,
                height = height,
                framerate_num = framerateNum,
                framerate_den = framerateDen
            };

            var paddedSize = RoundUp8(structSize);
            var podPtr = Marshal.AllocHGlobal(paddedSize);

            Marshal.StructureToPtr(pod, podPtr, false);

            var padding = paddedSize - structSize;
            for (var i = 0; i < padding; i++)
            {
                Marshal.WriteByte(podPtr, structSize + i, 0);
            }

            return podPtr;
        }

        /// <summary>
        /// Builds a BufferParamPod with the given parameters.
        /// </summary>
        public static nint BuildBufferParamPod(uint buffers, uint blocks, uint size, uint stride, uint align, int dataType, int metaType)
        {
            var structSize = Marshal.SizeOf<BufferParamPod>();
            var headerSize = Marshal.SizeOf<SpaPod>();
            var payloadSize = (uint)(structSize - headerSize);

            var pod = new BufferParamPod
            {
                pod = new SpaPod
                {
                    size = payloadSize,
                    type = SPA_TYPE_BufferParam
                },
                buffers = buffers,
                blocks = blocks,
                size = size,
                stride = stride,
                align = align,
                dataType = dataType,
                metaType = metaType
            };

            var paddedSize = RoundUp8(structSize);
            var podPtr = Marshal.AllocHGlobal(paddedSize);

            Marshal.StructureToPtr(pod, podPtr, false);

            var padding = paddedSize - structSize;
            for (var i = 0; i < padding; i++)
            {
                Marshal.WriteByte(podPtr, structSize + i, 0);
            }

            return podPtr;
        }
        #endregion

        #region Buffer Data Extraction (Improved)
        /// <summary>
        /// Extracts frame data from a PipeWire buffer.
        /// This function parses a spa_buffer and then extracts the data from the first spa_data element.
        /// It uses the spa_chunk (if available) to determine the valid data size.
        /// </summary>
        /// <param name="bufferPtr">Pointer to a spa_buffer (e.g. obtained from pw_stream_dequeue_buffer)</param>
        /// <param name="expectedSize">Expected size of the frame data (used to limit the copy size)</param>
        /// <returns>A byte array containing the frame data</returns>
        public static byte[] ExtractFrameData(nint bufferPtr, int expectedSize)
        {
            if (bufferPtr == nint.Zero)
            {
                throw new ArgumentException("bufferPtr is null", nameof(bufferPtr));
            }

            // Read the spa_buffer structure
            var buffer = Marshal.PtrToStructure<SpaBuffer>(bufferPtr);

            if (buffer.n_datas < 1)
            {
                throw new Exception("No spa_data elements found in spa_buffer.");
            }

            // Calculate the size of a SpaData structure
            var spaDataSize = Marshal.SizeOf<SpaData>();

            // Get pointer to the first spa_data element in the array
            var dataArrayPtr = buffer.datas;

            if (dataArrayPtr == nint.Zero)
            {
                throw new Exception("Pointer to spa_data array is null.");
            }

            var spaData = Marshal.PtrToStructure<SpaData>(dataArrayPtr);

            // Determine the data size.
            // If a spa_chunk is provided, use its 'size' field; otherwise, fallback to spaData.maxsize.
            var dataSize = spaData.maxsize;
            
            if (spaData.chunk != nint.Zero)
            {
                var chunk = Marshal.PtrToStructure<SpaChunk>(spaData.chunk);
                
                if (chunk.size > 0)
                {
                    dataSize = chunk.size;
                }
            }

            // Limit data size to expectedSize if necessary
            if (dataSize > expectedSize)
            {
                dataSize = (uint)expectedSize;
            }

            if (spaData.data == nint.Zero)
            {
                throw new Exception("spa_data.data is null.");
            }

            var data = new byte[dataSize];
            
            Marshal.Copy(spaData.data, data, 0, (int)dataSize);
            
            return data;
        }
        #endregion

        #region Constants and Delegates
        // Constants and delegates from pipewire/stream.h
        public const int PW_DIRECTION_INPUT = 0;
        public const uint PW_ID_ANY = 0;
        public const uint PW_STREAM_FLAG_AUTOCONNECT = 1;
        public const string PW_KEY_MEDIA_TYPE = "media.type";
        public const string PW_KEY_MEDIA_CATEGORY = "media.category";
        public const string PW_KEY_MEDIA_ROLE = "media.role";
        public const string PW_KEY_MEDIA_CLASS = "media.class";
        public const int PW_VERSION_STREAM_EVENTS = 2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PwStreamProcessDelegate(nint userData);

        // Original structure from pipewire/stream.h:
        //   struct pw_stream_events {
        //       uint32_t version;
        //       void (*stateChanged)(void *data, enum pw_stream_state old, enum pw_stream_state state, const char *error);
        //       void (*process)(void *data);
        //       void (*addBuffer)(void *data, struct pw_buffer *buffer);
        //       void (*removeBuffer)(void *data, struct pw_buffer *buffer);
        //       void (*drained)(void *data);
        //   };
        [StructLayout(LayoutKind.Sequential)]
        public struct pw_stream_events
        {
            public uint version;
            public nint stateChanged; // Callback for state change events
            public nint process;      // Process callback – required
            public nint addBuffer;    // Optional callback when a buffer is added
            public nint removeBuffer; // Optional callback when a buffer is removed
            public nint drained;      // Optional callback when buffers are drained
        }
        #endregion
    }
}
