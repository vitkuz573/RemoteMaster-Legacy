// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System;
using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers
{
    // === From spa/pod/pod.h ===
    // Original header defines the basic POD as:
    //   struct spa_pod {
    //       uint32_t size;  // size of the body
    //       uint32_t type;  // type code
    //   };
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct SpaPod
    {
        public uint size; // Size of the body (in bytes)
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
        public SpaPod pod;         // From spa/pod/pod.h
        public uint format;        // Video format code (e.g. RGB)
        public uint width;
        public uint height;
        public uint framerate_num;
        public uint framerate_den;
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
        public SpaPod pod;         // From spa/pod/pod.h
        public uint buffers;       // Number of buffers (from SPA specs)
        public uint blocks;        // Number of blocks per buffer
        public uint size;          // Total size per buffer
        public uint stride;        // Stride (bytes per row)
        public uint align;         // Memory alignment required
        public int dataType;       // Data type flags (per SPA)
        public int metaType;       // Metadata type (if any)
    }

    public static class PipewireNative
    {
        private const string LIB = "libpipewire-0.3";

        #region PipeWire Core Functions
        // --- From pipewire.h and pipewire/stream.h ---
        // These functions initialize PipeWire, create a main loop,
        // and create/connect/destroy streams.
        [DllImport(LIB)]
        public static extern void pw_init(ref int argc, ref IntPtr argv);

        [DllImport(LIB)]
        public static extern nint pw_main_loop_new(nint properties);

        [DllImport(LIB)]
        public static extern nint pw_main_loop_get_loop(nint mainLoop);

        [DllImport(LIB)]
        public static extern int pw_main_loop_run(nint mainLoop);

        [DllImport(LIB)]
        public static extern void pw_main_loop_quit(nint mainLoop);

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
        public static extern void pw_stream_queue_buffer(nint stream, nint buffer);

        [DllImport(LIB)]
        public static extern void pw_stream_disconnect(nint stream);

        [DllImport(LIB)]
        public static extern void pw_stream_destroy(nint stream);
        #endregion

        #region Properties Functions
        // --- From pipewire/properties.h ---
        [DllImport(LIB, EntryPoint = "pw_properties_new")]
        public static extern nint pw_properties_new(
            [MarshalAs(UnmanagedType.LPStr)] string key1,
            [MarshalAs(UnmanagedType.LPStr)] string value1,
            [MarshalAs(UnmanagedType.LPStr)] string key2,
            [MarshalAs(UnmanagedType.LPStr)] string value2,
            nint end); // Use IntPtr.Zero as sentinel.

        [DllImport(LIB, EntryPoint = "pw_properties_set")]
        public static extern void pw_properties_set(nint properties,
                                                    [MarshalAs(UnmanagedType.LPStr)] string key,
                                                    [MarshalAs(UnmanagedType.LPStr)] string value);
        #endregion

        #region Building SPA PODs

        private const uint SPA_TYPE_Format = 0x100;         // Original header value? (verify)
        private const uint SPA_TYPE_BufferParam = 0x101;      // Original header value? (verify)
        private const uint SPA_VIDEO_FORMAT_RGB = 0x34325241; // From header: 'AR24' little-endian

        private static int RoundUp8(int size)
        {
            return (size + 7) / 8 * 8;
        }

        public static IntPtr BuildVideoFormatPod(uint width, uint height, uint framerateNum, uint framerateDen)
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

            if (padding <= 0)
            {
                return podPtr;
            }

            for (var i = 0; i < padding; i++)
            {
                Marshal.WriteByte(podPtr, structSize + i, 0);
            }

            return podPtr;
        }

        public static IntPtr BuildBufferParamPod(uint buffers, uint blocks, uint size, uint stride, uint align, int dataType, int metaType)
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

            if (padding <= 0)
            {
                return podPtr;
            }

            for (var i = 0; i < padding; i++)
            {
                Marshal.WriteByte(podPtr, structSize + i, 0);
            }

            return podPtr;
        }
        #endregion

        #region Buffer Data Extraction
        // IMPORTANT: From pipewire/stream.h, buffers are represented by struct spa_buffer,
        // and the actual data is stored in one or more spa_data elements.
        // Here we simply copy from the provided pointer, but in a correct implementation,
        // you must define and parse struct spa_buffer and struct spa_data.
        public static byte[] ExtractFrameData(nint bufferPtr, int expectedSize)
        {
            byte[] data = new byte[expectedSize];
            // This simplified approach may cause segmentation faults if bufferPtr is not a direct data pointer.
            Marshal.Copy(bufferPtr, data, 0, expectedSize);
            return data;
        }
        #endregion

        #region Constants and Delegates
        // --- From pipewire/stream.h ---
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
            public nint stateChanged; // from pw_stream_events.destroy / state_changed
            public nint process;      // process callback – required
            public nint addBuffer;    // optional callback
            public nint removeBuffer; // optional callback
            public nint drained;      // optional callback
        }
        #endregion
    }
}
