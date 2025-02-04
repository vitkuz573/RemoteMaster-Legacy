// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct spa_pod
{
    public uint size;
    public uint type;
}

[StructLayout(LayoutKind.Sequential)]
public struct spa_buffer
{
    public uint n_metas;
    public uint n_datas;
    public nint metas;  
    public nint datas;  
}

[StructLayout(LayoutKind.Sequential)]
public struct spa_data
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
public struct spa_chunk
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

    #region SPA POD Builder Functions

    [DllImport(LibraryName, EntryPoint = "spa_pod_builder_init", CallingConvention = CallingConvention.Cdecl)]
    public static extern void spa_pod_builder_init(nint builder, nint data, uint size);

    [DllImport(LibraryName, EntryPoint = "spa_pod_builder_add_object", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint spa_pod_builder_add_object(nint builder, uint type, uint id, uint key1, nint value1, uint key2, nint value2, uint key3, nint value3, uint key4, nint value4, uint key5, nint value5);

    [DllImport(LibraryName, EntryPoint = "spa_pod_builder_add_object", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint spa_pod_builder_add_object(nint builder, uint type, uint id, uint key1, nint value1, uint key2, nint value2, uint key3, nint value3, uint key4, nint value4, uint key5, nint value5, uint key6, nint value6, uint key7, nint value7);

    #endregion

    #region Building SPA PODs

    private const uint SPA_TYPE_OBJECT_Format = 0x40003;
    private const uint SPA_TYPE_OBJECT_ParamBuffers = 0x40004;

    private const uint SPA_VIDEO_FORMAT_RGB = 15;

    public static nint BuildVideoFormatPod(uint width, uint height)
    {
        var buffer = Marshal.AllocHGlobal(1024);
        var builder = Marshal.AllocHGlobal(Marshal.SizeOf<nint>());

        spa_pod_builder_init(builder, buffer, 1024);

        var pod = spa_pod_builder_add_object(
            builder, SPA_TYPE_OBJECT_Format, SPA_PARAM_Format,
            SPA_FORMAT_mediaType, (nint)SPA_MEDIA_TYPE_video,
            SPA_FORMAT_mediaSubtype, (nint)SPA_MEDIA_SUBTYPE_raw,
            SPA_FORMAT_VIDEO_format, (nint)SPA_VIDEO_FORMAT_RGB,
            SPA_FORMAT_VIDEO_width, (nint)width,
            SPA_FORMAT_VIDEO_height, (nint)height
        );

        Marshal.FreeHGlobal(builder);

        return pod;
    }

    public static nint BuildBufferParamPod(uint buffers, uint blocks, uint size, uint stride, uint align, int dataType, int metaType)
    {
        var buffer = Marshal.AllocHGlobal(1024);
        var builder = Marshal.AllocHGlobal(Marshal.SizeOf<nint>());

        spa_pod_builder_init(builder, buffer, 1024);

        var pod = spa_pod_builder_add_object(
            builder, SPA_TYPE_OBJECT_ParamBuffers, SPA_PARAM_Buffers,
            SPA_PARAM_BUFFERS_buffers, (nint)buffers,
            SPA_PARAM_BUFFERS_blocks, (nint)blocks,
            SPA_PARAM_BUFFERS_size, (nint)size,
            SPA_PARAM_BUFFERS_stride, (nint)stride,
            SPA_PARAM_BUFFERS_align, (nint)align,
            SPA_PARAM_BUFFERS_dataType, (nint)dataType,
            SPA_PARAM_BUFFERS_metaType, (nint)metaType
        );

        Marshal.FreeHGlobal(builder);

        return pod;
    }

    #endregion

    #region Buffer Data Extraction

    public static byte[] ExtractFrameData(nint bufferPtr, int expectedSize)
    {
        if (bufferPtr == nint.Zero)
        {
            throw new ArgumentException("bufferPtr is null", nameof(bufferPtr));
        }

        var buffer = Marshal.PtrToStructure<spa_buffer>(bufferPtr);

        if (buffer.n_datas < 1)
        {
            throw new Exception("No spa_data elements found in spa_buffer.");
        }

        var spaDataSize = Marshal.SizeOf<spa_data>();

        var dataArrayPtr = buffer.datas;

        if (dataArrayPtr == nint.Zero)
        {
            throw new Exception("Pointer to spa_data array is null.");
        }

        var spaData = Marshal.PtrToStructure<spa_data>(dataArrayPtr);
        var dataSize = spaData.maxsize;
        
        if (spaData.chunk != nint.Zero)
        {
            var chunk = Marshal.PtrToStructure<spa_chunk>(spaData.chunk);
            
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

    #region SPA_PARAM_BUFFERS_* Values

    private const uint SPA_PARAM_BUFFERS_buffers = 1;
    private const uint SPA_PARAM_BUFFERS_blocks = 2;
    private const uint SPA_PARAM_BUFFERS_size = 3;
    private const uint SPA_PARAM_BUFFERS_stride = 4;
    private const uint SPA_PARAM_BUFFERS_align = 5;
    private const uint SPA_PARAM_BUFFERS_dataType = 6;
    private const uint SPA_PARAM_BUFFERS_metaType = 7;

    #endregion

    #region SPA_FORMAT_* Values

    private const uint SPA_FORMAT_mediaType = 1;
    private const uint SPA_FORMAT_mediaSubtype = 2;
    private const uint SPA_FORMAT_VIDEO_format = 3;
    private const uint SPA_FORMAT_VIDEO_width = 4;
    private const uint SPA_FORMAT_VIDEO_height = 5;

    #endregion

    #region SPA_MEDIA_TYPE Values

    private const uint SPA_MEDIA_TYPE_unknown = 0;
    private const uint SPA_MEDIA_TYPE_audio = 1;
    private const uint SPA_MEDIA_TYPE_video = 2;
    private const uint SPA_MEDIA_TYPE_image = 3;
    private const uint SPA_MEDIA_TYPE_binary = 4;
    private const uint SPA_MEDIA_TYPE_stream = 5;
    private const uint SPA_MEDIA_TYPE_application = 6;

    #endregion

    #region SPA_MEDIA_SUBTYPE Values

    private const uint SPA_MEDIA_SUBTYPE_unknown = 0;
    private const uint SPA_MEDIA_SUBTYPE_raw = 1;
    private const uint SPA_MEDIA_SUBTYPE_dsp = 2;
    private const uint SPA_MEDIA_SUBTYPE_iec958 = 3;
    private const uint SPA_MEDIA_SUBTYPE_dsd = 4;

    #endregion

    #region SPA_PARAM_* Values

    private const uint SPA_PARAM_Invalid = 0;
    private const uint SPA_PARAM_PropInfo = 1;
    private const uint SPA_PARAM_Props = 2;
    private const uint SPA_PARAM_EnumFormat = 3;
    private const uint SPA_PARAM_Format = 4;
    private const uint SPA_PARAM_Buffers = 5;
    private const uint SPA_PARAM_Meta = 6;
    private const uint SPA_PARAM_IO = 7;
    private const uint SPA_PARAM_EnumProfile = 8;
    private const uint SPA_PARAM_Profile = 9;
    private const uint SPA_PARAM_EnumPortConfig = 10;
    private const uint SPA_PARAM_PortConfig = 11;
    private const uint SPA_PARAM_EnumRoute = 12;
    private const uint SPA_PARAM_Route = 13;
    private const uint SPA_PARAM_Control = 14;
    private const uint SPA_PARAM_Latency = 15;
    private const uint SPA_PARAM_ProcessLatency = 16;
    private const uint SPA_PARAM_Tag = 17;

    #endregion

    #region Constants and Delegates

    public const uint PW_STREAM_FLAG_AUTOCONNECT = 1;

    public const int PW_DIRECTION_INPUT = 0;

    public const uint PW_ID_ANY = 0;

    public const int PW_VERSION_STREAM_EVENTS = 2;

    public const string PW_KEY_MEDIA_TYPE = "media.type";
    public const string PW_KEY_MEDIA_CATEGORY = "media.category";
    public const string PW_KEY_MEDIA_CLASS = "media.class";
    public const string PW_KEY_MEDIA_ROLE = "media.role";

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
