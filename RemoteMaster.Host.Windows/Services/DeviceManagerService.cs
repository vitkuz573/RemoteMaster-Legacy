// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.DTOs;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class DeviceManagerService(ILogger<DeviceManagerService> logger) : IDeviceManagerService
{
    private const int DefaultBufferSize = 512;

    public List<DeviceDto> GetDeviceList()
    {
        var devices = new List<DeviceDto>();

        using var deviceInfoSetHandle = SetupDiGetClassDevs(null, (string)null!, default, SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_ALLCLASSES);

        if (deviceInfoSetHandle.IsInvalid)
        {
            throw new Exception("Failed to retrieve device information.");
        }

        var deviceInfoData = new SP_DEVINFO_DATA
        {
            cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA))
        };

        uint deviceIndex = 0;

        while (SetupDiEnumDeviceInfo(deviceInfoSetHandle, deviceIndex, ref deviceInfoData))
        {
            var deviceInstanceId = GetDeviceInstanceId(deviceInfoSetHandle, deviceInfoData);
            var deviceName = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC);
            var hardwareId = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_HARDWAREID);
            var compatibleIds = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_COMPATIBLEIDS);
            var manufacturer = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_MFG);
            var friendlyName = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_FRIENDLYNAME);
            var locationInfo = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_LOCATION_INFORMATION);
            var service = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_SERVICE);
            var classGuid = GetDevicePropertyString(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_CLASSGUID);

            if (string.IsNullOrWhiteSpace(classGuid))
            {
                deviceIndex++;
                continue;
            }

            string className;

            try
            {
                className = GetClassNameFromGuid(Guid.Parse(classGuid));
            }
            catch (FormatException)
            {
                deviceIndex++;
                continue;
            }

            var configFlags = GetConfigFlags(deviceInfoSetHandle, deviceInfoData);
            var isEnabled = (configFlags & SETUP_DI_DEVICE_CONFIGURATION_FLAGS.CONFIGFLAG_DISABLED) == 0;

            devices.Add(new DeviceDto(!string.IsNullOrEmpty(friendlyName) ? friendlyName : deviceName, className, manufacturer, hardwareId, compatibleIds, locationInfo, service, deviceInstanceId, isEnabled));

            deviceIndex++;
        }

        return devices;
    }

    public bool DisableDeviceByInstanceId(string deviceInstanceId)
    {
        try
        {
            unsafe
            {
                fixed (char* pDeviceId = deviceInstanceId)
                {
                    var result = CM_Locate_DevNode(out var devInst, pDeviceId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL);

                    if (result != CONFIGRET.CR_SUCCESS)
                    {
                        return false;
                    }

                    result = CM_Disable_DevNode(devInst, CM_DISABLE_PERSIST);

                    return result == CONFIGRET.CR_SUCCESS;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool EnableDeviceByInstanceId(string deviceInstanceId)
    {
        try
        {
            unsafe
            {
                fixed (char* pDeviceId = deviceInstanceId)
                {
                    var result = CM_Locate_DevNode(out var devInst, pDeviceId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL);

                    if (result != CONFIGRET.CR_SUCCESS)
                    {
                        return false;
                    }

                    result = CM_Enable_DevNode(devInst, 0);

                    return result == CONFIGRET.CR_SUCCESS;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool StopDeviceByInstanceId(string deviceInstanceId)
    {
        PNP_VETO_TYPE vetoType = default;
        var vetoNameBuffer = new char[256];

        try
        {
            unsafe
            {
                fixed (char* pDeviceId = deviceInstanceId)
                {
                    var result = CM_Locate_DevNode(out var devInst, pDeviceId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL);

                    if (result != CONFIGRET.CR_SUCCESS)
                    {
                        return false;
                    }

                    result = CM_Get_DevNode_Status(out _, out _, devInst, 0);

                    if (result != CONFIGRET.CR_SUCCESS)
                    {
                        return false;
                    }

                    fixed (char* vetoNamePtr = vetoNameBuffer)
                    {
                        result = CM_Request_Device_Eject(devInst, &vetoType, vetoNamePtr, (uint)vetoNameBuffer.Length, 0);

                        return result == CONFIGRET.CR_SUCCESS;
                    }
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool StartDeviceByInstanceId(string deviceInstanceId)
    {
        try
        {
            unsafe
            {
                fixed (char* pDeviceId = deviceInstanceId)
                {
                    var result = CM_Locate_DevNode(out var devInst, pDeviceId, CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL);

                    if (result != CONFIGRET.CR_SUCCESS)
                    {
                        return false;
                    }

                    result = CM_Reenumerate_DevNode(devInst, 0);

                    return result == CONFIGRET.CR_SUCCESS;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool UpdateDeviceDriver(string hardwareId, string infFilePath)
    {
        BOOL rebootRequired = default;

        try
        {
            var hwndParent = HWND.Null;

            const UPDATEDRIVERFORPLUGANDPLAYDEVICES_FLAGS installFlags = UPDATEDRIVERFORPLUGANDPLAYDEVICES_FLAGS.INSTALLFLAG_FORCE | UPDATEDRIVERFORPLUGANDPLAYDEVICES_FLAGS.INSTALLFLAG_NONINTERACTIVE;

            bool result;

            unsafe
            {
                result = UpdateDriverForPlugAndPlayDevices(hwndParent, hardwareId, infFilePath, installFlags, &rebootRequired);
            }

            if (result)
            {
                if (rebootRequired)
                {
                    logger.LogInformation("Reboot is required to complete the driver update.");
                }

                return true;
            }

            logger.LogError("Driver update failed.");

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred during driver update: {Message}", ex.Message);

            return false;
        }
    }

    private static string GetDevicePropertyString(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData, SETUP_DI_REGISTRY_PROPERTY property)
    {
        var buffer = new byte[DefaultBufferSize];

        return TryGetDeviceProperty(deviceInfoSetHandle, deviceInfoData, property, buffer, out var result) ? CleanString(result) : string.Empty;
    }

    private static bool TryGetDeviceProperty(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData, SETUP_DI_REGISTRY_PROPERTY property, Span<byte> buffer, out byte[] result)
    {
        uint requiredSize = 0;
        uint propertyRegDataType = 0;

        bool success;

        unsafe
        {
            success = SetupDiGetDeviceRegistryProperty(deviceInfoSetHandle, in deviceInfoData, property, &propertyRegDataType, buffer, &requiredSize);
        }

        if (success && requiredSize <= buffer.Length)
        {
            result = buffer[..(int)requiredSize].ToArray();

            return true;
        }

        result = [];

        return false;
    }

    private static string GetDeviceInstanceId(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData)
    {
        var instanceIdBuffer = new char[DefaultBufferSize];

        uint requiredSize = 0;

        unsafe
        {
            fixed (char* pInstanceId = instanceIdBuffer)
            {
                if (SetupDiGetDeviceInstanceId(deviceInfoSetHandle, deviceInfoData, pInstanceId, (uint)instanceIdBuffer.Length, &requiredSize))
                {
                    return new string(pInstanceId, 0, (int)requiredSize - 1);
                }
            }
        }

        return string.Empty;
    }

    private static string GetClassNameFromGuid(Guid classGuid)
    {
        var classNameBuffer = new char[DefaultBufferSize];
        uint requiredSize = 0;

        unsafe
        {
            fixed (char* classNamePtr = classNameBuffer)
            {
                var result = SetupDiClassNameFromGuid(classGuid, classNamePtr, (uint)classNameBuffer.Length, &requiredSize);

                if (result == 0)
                {
                    throw new Exception("Failed to retrieve class name for the specified GUID.");
                }

                return new string(classNamePtr, 0, (int)requiredSize - 1);
            }
        }
    }

    private static SETUP_DI_DEVICE_CONFIGURATION_FLAGS GetConfigFlags(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData)
    {
        var buffer = new byte[4];

        return TryGetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_CONFIGFLAGS, buffer, out var result)
            ? (SETUP_DI_DEVICE_CONFIGURATION_FLAGS)BitConverter.ToUInt32(result, 0)
            : default;
    }

    private static string CleanString(byte[] buffer)
    {
        var str = Encoding.Unicode.GetString(buffer);

        return str.Split('\0')[0].Trim();
    }
}
