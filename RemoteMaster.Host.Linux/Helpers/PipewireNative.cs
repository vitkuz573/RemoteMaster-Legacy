// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct SpaPod
{
    public uint size;
    public uint type;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct VideoFormatPod
{
    public SpaPod pod;        
    public uint format;       
    public uint width;        
    public uint height;       
    public uint framerate_num;
    public uint framerate_den;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct BufferParamPod
{
    public SpaPod pod;  
    public uint buffers;
    public uint blocks; 
    public uint size;   
    public uint stride; 
    public uint align;  
    public int dataType;
    public int metaType;
}

[StructLayout(LayoutKind.Sequential)]
public struct SpaBuffer
{
    public uint n_metas;
    public uint n_datas;
    public nint metas;  
    public nint datas;  
}

[StructLayout(LayoutKind.Sequential)]
public struct SpaData
{
    public uint type;     
    public uint flags;    
    public long fd;       
    public uint mapoffset;
    public uint maxsize;  
    public nint data;     
    public nint chunk;    
}

[StructLayout(LayoutKind.Sequential)]
public struct SpaChunk
{
    public uint offset;
    public uint size;  
    public int stride; 
    public int flags;  
}

public static class PipewireNative
{
    private const string LibraryName = "libpipewire-0.3";

    #region PipeWire Core Functions

    [DllImport(LibraryName, EntryPoint = "pw_init", CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_init(ref int argc, ref nint argv);

    [DllImport(LibraryName, EntryPoint = "pw_main_loop_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_main_loop_new(nint properties);

    [DllImport(LibraryName, EntryPoint = "pw_main_loop_get_loop", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_main_loop_get_loop(nint mainLoop);

    [DllImport(LibraryName, EntryPoint = "pw_main_loop_run", CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_main_loop_run(nint mainLoop);

    [DllImport(LibraryName, EntryPoint = "pw_main_loop_quit", CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_main_loop_quit(nint mainLoop);

    [DllImport(LibraryName, EntryPoint = "pw_main_loop_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_destroy(nint mainLoop);

    [DllImport(LibraryName, EntryPoint = "pw_stream_new_simple", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_new_simple(nint loop, [MarshalAs(UnmanagedType.LPStr)] string name, nint properties, ref pw_stream_events events, nint userData);

    [DllImport(LibraryName, EntryPoint = "pw_stream_connect", CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_connect(nint stream, int direction, uint target_id, uint flags, nint parameters, uint n_params);

    [DllImport(LibraryName, EntryPoint = "pw_stream_dequeue_buffer", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_dequeue_buffer(nint stream);

    [DllImport(LibraryName, EntryPoint = "pw_stream_queue_buffer", CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_queue_buffer(nint stream, nint buffer);

    [DllImport(LibraryName, EntryPoint = "pw_stream_disconnect", CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_disconnect(nint stream);

    [DllImport(LibraryName, EntryPoint = "pw_stream_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_destroy(nint stream);
   
    #endregion

    #region Properties Functions

    [DllImport(LibraryName, EntryPoint = "pw_properties_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_properties_new([MarshalAs(UnmanagedType.LPStr)] string key1, [MarshalAs(UnmanagedType.LPStr)] string value1, [MarshalAs(UnmanagedType.LPStr)] string key2, [MarshalAs(UnmanagedType.LPStr)] string value2, nint end);

    [DllImport(LibraryName, EntryPoint = "pw_properties_set", CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_properties_set(nint properties, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value);
    
    #endregion

    #region Building SPA PODs
    
    private const uint SPA_TYPE_Format = 0x100;         
    private const uint SPA_TYPE_BufferParam = 0x101;     
    private const uint SPA_VIDEO_FORMAT_RGB = 0x34325241;

    private static int RoundUp8(int size)
    {
        return (size + 7) / 8 * 8;
    }

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

    #region Buffer Data Extraction

    public static byte[] ExtractFrameData(nint bufferPtr, int expectedSize)
    {
        if (bufferPtr == nint.Zero)
        {
            throw new ArgumentException("bufferPtr is null", nameof(bufferPtr));
        }

        var buffer = Marshal.PtrToStructure<SpaBuffer>(bufferPtr);

        if (buffer.n_datas < 1)
        {
            throw new Exception("No spa_data elements found in spa_buffer.");
        }

        var spaDataSize = Marshal.SizeOf<SpaData>();

        var dataArrayPtr = buffer.datas;

        if (dataArrayPtr == nint.Zero)
        {
            throw new Exception("Pointer to spa_data array is null.");
        }

        var spaData = Marshal.PtrToStructure<SpaData>(dataArrayPtr);
        var dataSize = spaData.maxsize;
        
        if (spaData.chunk != nint.Zero)
        {
            var chunk = Marshal.PtrToStructure<SpaChunk>(spaData.chunk);
            
            if (chunk.size > 0)
            {
                dataSize = chunk.size;
            }
        }

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

    public const int PW_DIRECTION_INPUT = 0;
    public const uint PW_ID_ANY = 0;
    public const uint PW_STREAM_FLAG_AUTOCONNECT = 1;
    public const string PW_KEY_MEDIA_TYPE = "media.type";
    public const string PW_KEY_MEDIA_CATEGORY = "media.category";
    public const string PW_KEY_MEDIA_ROLE = "media.role";
    public const string PW_KEY_MEDIA_CLASS = "media.class";
    public const int PW_VERSION_STREAM_EVENTS = 2;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamDestroyDelegate(nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamStateChangedDelegate(nint userData, int oldState, int newState, [MarshalAs(UnmanagedType.LPUTF8Str)] string error);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamControlInfoDelegate(nint userData, uint id, nint control);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamIoChangedDelegate(nint userData, uint id, nint area, uint size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamParamChangedDelegate(nint userData, uint id, nint param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamBufferDelegate(nint userData, nint buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamProcessDelegate(nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamCommandDelegate(nint userData, nint command);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamTriggerDoneDelegate(nint userData);

    [StructLayout(LayoutKind.Sequential)]
    public struct pw_stream_events
    {
        public uint version;
        public nint stateChanged;
        public nint process;     
        public nint addBuffer;   
        public nint removeBuffer;
        public nint drained;     
    }

    #endregion
}
