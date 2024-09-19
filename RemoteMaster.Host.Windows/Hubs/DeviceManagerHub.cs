// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Hubs;

public class DeviceManagerHub : Hub<IDeviceManagerClient>
{
    public async Task GetDevices()
    {
        var devices = await Task.Run(GetDeviceList);

        await Clients.All.ReceiveDeviceList(devices);
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

    private static List<DeviceDto> GetDeviceList()
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

        var deviceInfoSet = (HDEVINFO)deviceInfoSetHandle.DangerousGetHandle();

        while (SetupDiEnumDeviceInfo(deviceInfoSetHandle, deviceIndex, ref deviceInfoData))
        {
            var buffer = new byte[512];

            var deviceName = string.Empty;
            var hardwareId = string.Empty;
            var compatibleIds = string.Empty;
            var manufacturer = string.Empty;
            var friendlyName = string.Empty;
            var locationInfo = string.Empty;
            var service = string.Empty;
            var className = string.Empty;
            var deviceInstanceId = string.Empty;
            bool isEnabled;

            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    uint propertyRegDataType = 0;
                    uint requiredSize = 0;

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        deviceName = CleanString(buffer);
                    }

                    uint instanceIdRequiredSize = 0;
                    var instanceIdBuffer = new char[512];

                    fixed (char* pInstanceId = instanceIdBuffer)
                    {
                        var deviceInstancePtr = new PWSTR(pInstanceId);
                        
                        if (SetupDiGetDeviceInstanceId(deviceInfoSetHandle, deviceInfoData, deviceInstancePtr, (uint)instanceIdBuffer.Length, &instanceIdRequiredSize))
                        {
                            deviceInstanceId = new string(pInstanceId, 0, (int)instanceIdRequiredSize - 1);
                        }
                    }

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_HARDWAREID, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        hardwareId = CleanString(buffer);
                    }

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_COMPATIBLEIDS, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        compatibleIds = CleanString(buffer);
                    }

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_MFG, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        manufacturer = CleanString(buffer);
                    }

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_FRIENDLYNAME, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        friendlyName = CleanString(buffer);
                    }

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_LOCATION_INFORMATION, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        locationInfo = CleanString(buffer);
                    }

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_SERVICE, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        service = CleanString(buffer);
                    }

                    Array.Clear(buffer, 0, buffer.Length);

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_CLASSGUID, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        var classGuid = CleanString(buffer);
                        className = GetClassNameFromGuid(Guid.Parse(classGuid));
                    }

                    uint configFlags = 0;

                    if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, &deviceInfoData, SETUP_DI_REGISTRY_PROPERTY.SPDRP_CONFIGFLAGS, &propertyRegDataType, pBuffer, (uint)buffer.Length, &requiredSize))
                    {
                        configFlags = BitConverter.ToUInt32(buffer, 0);
                    }

                    isEnabled = (configFlags & 0x00000001) == 0;

                    fixed (char* pInstanceId = instanceIdBuffer)
                    {
                        var deviceInstancePtr = new PWSTR(pInstanceId);
                        
                        if (SetupDiGetDeviceInstanceId(deviceInfoSetHandle, deviceInfoData, deviceInstancePtr, (uint)instanceIdBuffer.Length, &instanceIdRequiredSize))
                        {
                            deviceInstanceId = new string(pInstanceId, 0, (int)instanceIdRequiredSize - 1);
                        }
                    }
                }
            }

            devices.Add(new DeviceDto(!string.IsNullOrEmpty(friendlyName) ? friendlyName : deviceName, className, manufacturer, hardwareId, compatibleIds, locationInfo, service, deviceInstanceId, isEnabled));

            deviceIndex++;
        }

        return devices;
    }

    public async Task DisableDevice(string deviceInstanceId)
    {
        var result = await Task.Run(() => DisableDeviceByInstanceId(deviceInstanceId));

        if (result)
        {
            await Clients.All.NotifyDeviceDisabled(deviceInstanceId);
        }
        else
        {
            await Clients.All.NotifyDeviceDisableFailed(deviceInstanceId);
        }
    }

    public async Task EnableDevice(string deviceInstanceId)
    {
        var result = await Task.Run(() => EnableDeviceByInstanceId(deviceInstanceId));

        if (result)
        {
            await Clients.All.NotifyDeviceEnabled(deviceInstanceId);
        }
        else
        {
            await Clients.All.NotifyDeviceEnableFailed(deviceInstanceId);
        }
    }

    private static unsafe bool DisableDeviceByInstanceId(string deviceInstanceId)
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

    private static unsafe bool EnableDeviceByInstanceId(string deviceInstanceId)
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

    private static unsafe bool StopDeviceByInstanceId(string deviceInstanceId)
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

    private static unsafe bool StartDeviceByInstanceId(string deviceInstanceId)
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
}
