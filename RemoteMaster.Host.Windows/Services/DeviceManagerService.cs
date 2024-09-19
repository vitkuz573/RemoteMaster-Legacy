// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using System.Text;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class DeviceManagerService : IDeviceManagerService
{
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
            var instanceIdBuffer = new char[512];

            var deviceInstanceId = GetDeviceInstanceId(deviceInfoSetHandle, deviceInfoData, instanceIdBuffer);

            var deviceName = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC);
            var hardwareId = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_HARDWAREID);
            var compatibleIds = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_COMPATIBLEIDS);
            var manufacturer = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_MFG);
            var friendlyName = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_FRIENDLYNAME);
            var locationInfo = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_LOCATION_INFORMATION);
            var service = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_SERVICE);
            var classGuid = GetDeviceProperty(deviceInfoSetHandle, deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_CLASSGUID);

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
            var isEnabled = (configFlags & 0x00000001) == 0;

            devices.Add(new DeviceDto(!string.IsNullOrEmpty(friendlyName) ? friendlyName : deviceName, className, manufacturer, hardwareId, compatibleIds, locationInfo, service, deviceInstanceId, isEnabled));

            deviceIndex++;
        }

        return devices;
    }

    public unsafe bool DisableDeviceByInstanceId(string deviceInstanceId)
    {
        try
        {
            fixed (char* pDeviceId = deviceInstanceId)
            {
                var deviceInstancePtr = new PWSTR(pDeviceId);

                const CM_LOCATE_DEVNODE_FLAGS flags = CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL;
                var result = CM_Locate_DevNode(out var devInst, deviceInstancePtr, flags);

                if (result != CONFIGRET.CR_SUCCESS)
                {
                    return false;
                }

                result = CM_Disable_DevNode(devInst, CM_DISABLE_PERSIST);

                return result == CONFIGRET.CR_SUCCESS;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public unsafe bool EnableDeviceByInstanceId(string deviceInstanceId)
    {
        try
        {
            fixed (char* pDeviceId = deviceInstanceId)
            {
                var deviceInstancePtr = new PWSTR(pDeviceId);

                const CM_LOCATE_DEVNODE_FLAGS flags = CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL;
                var result = CM_Locate_DevNode(out var devInst, deviceInstancePtr, flags);

                if (result != CONFIGRET.CR_SUCCESS)
                {
                    return false;
                }

                result = CM_Enable_DevNode(devInst, 0);

                return result == CONFIGRET.CR_SUCCESS;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public unsafe bool StopDeviceByInstanceId(string deviceInstanceId)
    {
        try
        {
            fixed (char* pDeviceId = deviceInstanceId)
            {
                var deviceInstancePtr = new PWSTR(pDeviceId);

                const CM_LOCATE_DEVNODE_FLAGS flags = CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL;
                var result = CM_Locate_DevNode(out var devInst, deviceInstancePtr, flags);

                if (result != CONFIGRET.CR_SUCCESS)
                {
                    return false;
                }

                result = CM_Get_DevNode_Status(out _, out _, devInst, 0);

                if (result != CONFIGRET.CR_SUCCESS)
                {
                    return false;
                }

                PNP_VETO_TYPE vetoType = default;
                var vetoNameBuffer = new char[256];

                fixed (char* vetoNamePtr = vetoNameBuffer)
                {
                    var vetoNamePwstr = new PWSTR(vetoNamePtr);
                    result = CM_Request_Device_Eject(devInst, &vetoType, vetoNamePwstr, (uint)vetoNameBuffer.Length, 0);

                    return result == CONFIGRET.CR_SUCCESS;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public unsafe bool StartDeviceByInstanceId(string deviceInstanceId)
    {
        try
        {
            fixed (char* pDeviceId = deviceInstanceId)
            {
                var deviceInstancePtr = new PWSTR(pDeviceId);

                const CM_LOCATE_DEVNODE_FLAGS flags = CM_LOCATE_DEVNODE_FLAGS.CM_LOCATE_DEVNODE_NORMAL;
                var result = CM_Locate_DevNode(out var devInst, deviceInstancePtr, flags);

                if (result != CONFIGRET.CR_SUCCESS)
                {
                    return false;
                }

                result = CM_Reenumerate_DevNode(devInst, 0);

                return result == CONFIGRET.CR_SUCCESS;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static unsafe string GetDeviceInstanceId(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData, char[] instanceIdBuffer)
    {
        uint requiredSize = 0;

        fixed (char* pInstanceId = instanceIdBuffer)
        {
            var deviceInstancePtr = new PWSTR(pInstanceId);

            if (SetupDiGetDeviceInstanceId(deviceInfoSetHandle, deviceInfoData, deviceInstancePtr, (uint)instanceIdBuffer.Length, &requiredSize))
            {
                return new string(pInstanceId, 0, (int)requiredSize - 1);
            }
        }

        return string.Empty;
    }

    private static unsafe string GetDeviceProperty(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData, SETUP_DI_REGISTRY_PROPERTY property)
    {
        var buffer = new byte[512];

        uint requiredSize = 0;
        uint propertyRegDataType = 0;

        return SetupDiGetDeviceRegistryProperty(deviceInfoSetHandle, in deviceInfoData, property, &propertyRegDataType, buffer.AsSpan(), &requiredSize) ? CleanString(buffer) : string.Empty;
    }

    private static unsafe uint GetConfigFlags(SafeHandle deviceInfoSetHandle, SP_DEVINFO_DATA deviceInfoData)
    {
        var buffer = new byte[4];

        uint requiredSize = 0;
        uint propertyRegDataType = 0;

        return SetupDiGetDeviceRegistryProperty(deviceInfoSetHandle, in deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_CONFIGFLAGS, &propertyRegDataType, buffer.AsSpan(), &requiredSize) ? BitConverter.ToUInt32(buffer, 0) : (uint)0;
    }

    private static string CleanString(byte[] buffer)
    {
        var str = Encoding.Unicode.GetString(buffer);

        return str.Split('\0')[0].Trim();
    }

    private static unsafe string GetClassNameFromGuid(Guid classGuid)
    {
        var classNameBuffer = new char[256];
        uint requiredSize = 0;

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
