/*++

Module Name:

    public.h

Abstract:

    This module contains the common declarations shared by driver
    and user applications.

Environment:

    user and kernel

--*/

#define DMAP_NAME L"DmapGpio"

#define DMAP_SYMBOLIC_NAME L"\\DosDevices\\" DMAP_NAME
#define DMAP_USERMODE_PATH L"\\\\.\\" DMAP_NAME
#define DMAP_USERMODE_PATH_SIZE sizeof(DMAP_USERMODE_PATH)


//
// Define an Interface Guid so that app can find the device and talk to it.
// {109b86ad-f53d-4b76-aa5f-821e2ddf2141}
//
#define DMAP_INTERFACE {0x109b86ad,0xf53d,0x4b76,0xaa,0x5f,0x82,0x1e,0x2d,0xdf,0x21,0x41}
DEFINE_GUID(GUID_DEVINTERFACE_DMap, { 0x109b86ad,0xf53d,0x4b76,0xaa,0x5f,0x82,0x1e,0x2d,0xdf,0x21,0x41 });

#define FILE_DEVICE_DMAP 0x423

#define IOCTL_DMAP_MAPMEMORY              CTL_CODE(FILE_DEVICE_DMAP, 0x100, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_DMAP_WRITEPORT              CTL_CODE(FILE_DEVICE_DMAP, 0x101, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_DMAP_READPORT               CTL_CODE(FILE_DEVICE_DMAP, 0x102, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_DMAP_LOCK                   CTL_CODE(FILE_DEVICE_DMAP, 0x103, METHOD_NEITHER, FILE_ANY_ACCESS)
#define IOCLT_DMAP_UNLOCK                 CTL_CODE(FILE_DEVICE_DMAP, 0x104, METHOD_NEITHER, FILE_ANY_ACCESS)

typedef struct _DMAP_MAPMEMORY_OUTPUT_BUFFER
{
    PVOID Address;
    ULONG Length;
} DMAP_MAPMEMORY_OUTPUT_BUFFER;

typedef struct _DMAP_WRITEPORT_INPUT_BUFFER
{
    ULONG Address;
    UCHAR Value;
} DMAP_WRITEPORT_INPUT_BUFFER;
