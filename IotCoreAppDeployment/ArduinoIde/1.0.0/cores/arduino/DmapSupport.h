// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _DMAP_SUPPORT_H_
#define _DMAP_SUPPORT_H_

#include <Windows.h>

#include "public.h"

// Define the device name strings used to access the SPI, I2C and Fabric GPIO controllers with DMap on Galileo.
#define galileoSpi0DeviceName  L"\\\\.\\PCI#VEN_8086&DEV_0935&SUBSYS_09358086&REV_10#3&b1bfb68&0&A8#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define galileoSpi1DeviceName  L"\\\\.\\PCI#VEN_8086&DEV_0935&SUBSYS_09358086&REV_10#3&b1bfb68&0&A9#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define galileoI2cDeviceName   L"\\\\.\\PCI#VEN_8086&DEV_0934&SUBSYS_09348086&REV_10#3&b1bfb68&0&AA#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define galileoGpioDeviceName  L"\\\\.\\PCI#VEN_8086&DEV_0934&SUBSYS_09348086&REV_10#3&b1bfb68&0&AA#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\1"

// Define the device name string used to access the Legacy GPIO controller with DMap on Galileo.
#define galileoLegacyGpioDeviceName L"\\\\.\\ACPI#INT3488#4&431f7f5&0#{091a7d51-bb55-42e4-ae25-1d0b563fa177}"

// Define the device name strings used to access the controllers on the MBM.
#define mbmGpioS0DeviceName   L"\\\\.\\ACPI#INT33FC#1#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define mbmGpioS5DeviceName   L"\\\\.\\ACPI#INT33FC#3#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define mbmPwm0DeviceName     L"\\\\.\\ACPI#80860F09#1#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define mbmPwm1DeviceNmae     L"\\\\.\\ACPI#80860F09#2#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define mbmSpiDeviceName      L"\\\\.\\ACPI#80860F0E#0#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define mbmI2cDeviceName      L"\\\\.\\ACPI#80860F41#6#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"

// Define the device name strings used to access the controllers on the PI2.
#define pi2Spi0DeviceName     L"\\\\.\\ACPI#BCM2838#0#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define pi2Spi1DeviceName     L"\\\\.\\ACPI#BCM2839#1#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define pi2I2c0DeviceName     L"\\\\.\\ACPI#BCM2841#0#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define pi2I2c1DeviceName     L"\\\\.\\ACPI#BCM2841#1#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define pi2PwmDeviceName      L"\\\\.\\ACPI#BCM2844#0#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"
#define pi2GpioDeviceName     L"\\\\.\\ACPI#BCM2845#0#{109b86ad-f53d-4b76-aa5f-821e2ddf2141}\\0"

/// Routine to get the base address of a memory mapped controller with no sharing allowed.
HRESULT GetControllerBaseAddress(PWCHAR deviceName, HANDLE & handle, PVOID & baseAddress);

/// Routine to get the base address of a memory mapped controller with a sharing specification.
HRESULT GetControllerBaseAddress(PWCHAR deviceName, HANDLE & handle, PVOID & baseAddress, DWORD shareMode);

/// Routine to close a controller that has previously been opened.
void DmapCloseController(HANDLE & handle);

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/// Routine to open a controller device in the SOC.
HRESULT OpenControllerDevice(PWCHAR deviceName, HANDLE & handle, DWORD shareMode);
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)  // If building a UWP app.
HRESULT GetControllerLock(HANDLE & handle);
HRESULT ReleaseControllerLock(HANDLE & handle);
#endif

#endif // _DMAP_SUPPORT_H_