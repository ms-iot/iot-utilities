// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _GPIO_CONTROLLER_H_
#define _GPIO_CONTROLLER_H_

#include <Windows.h>
#include "ErrorCodes.h"

#include "ArduinoCommon.h"
#include "DmapSupport.h"
#include "HiResTimer.h"
#include "concrt.h"

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
#include "quarklgpio.h"
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/// Class used to interact with the Quark Fabric GPIO hardware.
class QuarkFabricGpioControllerClass
{
public:
    /// Constructor.
    QuarkFabricGpioControllerClass()
    {
        m_hController = INVALID_HANDLE_VALUE;
        m_registers = nullptr;
    }

    /// Destructor.
    virtual ~QuarkFabricGpioControllerClass()
    {
        DmapCloseController(m_hController);
        m_registers = nullptr;
    }

    /// Method to map the Fabric GPIO controller registers if they are not already mapped.
    /**
    \return HRESULT error or success code.
    */
    inline HRESULT mapIfNeeded()
    {
        if (m_hController == INVALID_HANDLE_VALUE)
        {
            return _mapController();
        }
        else
        {
            return S_OK;
        }
    }

    /// Method to set the state of a Fabric GPIO port bit.
    inline HRESULT setPinState(ULONG portBit, ULONG state);

    /// Method to read the state of a Fabric GPIO bit.
    inline HRESULT getPinState(ULONG portBit, ULONG & state);

    /// Method to set the direction (input or output) of a Fabric GPIO port bit.
    inline HRESULT setPinDirection(ULONG portBit, ULONG mode);

    /// Method to get the direction (input or output) of a Fabric GPIO port bit.
    inline HRESULT getPinDirection(ULONG portBit, ULONG & mode);

private:

    #pragma warning(push)
    #pragma warning(disable : 4201) // Ignore nameless struct/union warnings

    /// Port A Data Register.
    typedef union {
        struct {
            ULONG GPIO_SWPORTA_DR : 8;      ///< Port Data
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_SWPORTA_DR;

    /// Port A Data Direction Register.
    typedef union {
        struct {
            ULONG GPIO_SWPORTA_DDR : 8;     ///< Port Data Direction
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_SWPORTA_DDR;

    /// Interrupt Enable Register for Port A.
    typedef union {
        struct {
            ULONG GPIO_INTEN : 8;           ///< Interrupt Enable
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_INTEN;

    /// Interrupt Mask Register for Port A.
    typedef union {
        struct {
            ULONG GPIO_INTMASK : 8;         ///< Interrupt Mask
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_INTMASK;

    /// Interrupt Type Register for Port A.
    typedef union {
        struct {
            ULONG GPI_INTTYPE_LEVEL : 8;    ///< Interrupt Type
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_INTTYPE_LEVEL;

    /// Interrupt Polarity Register for Port A.
    typedef union {
        struct {
            ULONG GPIO_INT_POLARITY : 8;    ///< Interrupt Polarity
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_INT_POLARITY;

    /// Interrupt Status Register for Port A.
    typedef union {
        struct {
            ULONG GPIO_INTSTATUS : 8;       ///< Interrupt Status
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_INTSTATUS;

    /// Raw Interrupt Status Register for Port A.
    typedef union {
        struct {
            ULONG GPIO_RAW_INSTATUS : 8;    ///< Raw Interrupt Status
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_RAW_INTSTATUS;

    /// Debounce Enable Register for Port A.
    typedef union {
        struct {
            ULONG GPIO_DEBOUNCE : 8;        ///< Debounce Enable
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_DEBOUNCE;

    /// Clear Interrupt Register for Port A.
    typedef union {
        struct {
            ULONG GPIO_PORTA_EOI : 8;       ///< Clear Interrupt
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_PORTA_EOI;

    /// Port A External Port Register.
    typedef union {
        struct {
            ULONG GPIO_EXT_PORTA : 8;       ///< External Port
            ULONG _reserved : 24;
        };
        ULONG ALL_BITS;
    } _GPIO_EXT_PORTA;

    /// Register controls Synchronization of Level-Sensitive interrupts.
    typedef union {
        struct {
            ULONG GPIO_LS_SYNC : 1;         ///< Synchronization Level
            ULONG _reserved : 31;
        };
        ULONG ALL_BITS;
    } _GPIO_LS_SYNC;

    #pragma warning( pop )

    /// Layout of the Quark Fabric GPIO Controller registers in memory.
    typedef struct _FABRIC_GPIO {
        _GPIO_SWPORTA_DR        GPIO_SWPORTA_DR;    ///< 0x00 - Port A Data
        _GPIO_SWPORTA_DDR       GPIO_SWPORTA_DDR;   ///< 0x04 - Port A Data Direction
        ULONG                   _reserved1[0x0A];   ///< 0x08 - 0x2F
        _GPIO_INTEN             GPIO_INTEN;         ///< 0x30 - Interrupt Enable
        _GPIO_INTMASK           GPIO_INTMASK;       ///< 0x34 - Interrupt Mask
        _GPIO_INTTYPE_LEVEL     GPIO_INTTYPE_LEVEL; ///< 0x38 - Interrupt Type
        _GPIO_INT_POLARITY      GPIO_INT_POLARITY;  ///< 0x3C - Interrupt Polarity
        _GPIO_INTSTATUS         GPIO_INTSTATUS;     ///< 0x40 - Interrupt Status
        _GPIO_RAW_INTSTATUS     GPIO_RAW_INTSTATUS; ///< 0x44 - Raw Interrupt Status
        _GPIO_DEBOUNCE          GPIO_DEBOUNCE;      ///< 0x48 - Debounce enable
        _GPIO_PORTA_EOI         GPIO_PORTA_EOI;     ///< 0x4C - Clear Interrupt
        _GPIO_EXT_PORTA         GPIO_EXT_PORTA;     ///< 0x50 - Port A External Port
        ULONG                   _reserved[0x03];    ///< 0x54 - 0x5F
        _GPIO_LS_SYNC           GPIO_LS_SYNC;       ///< 0x60 - Synchronization Level
    } volatile FABRIC_GPIO, *PFABRIC_GPIO;

    //
    // QuarkFabricGpioControllerClass private data members.
    //

    /// Handle to the controller device.
    /**
    This handle can be used to map Fabric GPIO Controller registers in to user
    memory address space.
    */
    HANDLE m_hController;

    /// Pointer to the controller object in this process' address space.
    /**
    This controller object is used to access the Fabric GPIO regiseters after
    they are mapped into this process' virtual address space.
    */
    PFABRIC_GPIO m_registers;

    //
    // QuarkFabricGpioControllerClass private methods.
    //

    /// Method to map the Fabric GPIO Controller into this process' virtual address space.
    HRESULT _mapController();

    /// Method to set a Fabric GPIO pin as an input.
    inline void _setPinInput(ULONG portBit)
    {
        // Clear the appropriate Data Direction Register bit.  This operation is atomic, 
        // and therefore thread-safe on a single core processor (such as the Quark X1000).
        _bittestandreset((LONG*)&m_registers->GPIO_SWPORTA_DDR.ALL_BITS, portBit);
    }

    /// Method to set a Fabric GPIO pin as an output
    inline void _setPinOutput(ULONG portBit)
    {
        // Set the appropriate Data Direction Register bit.
        _bittestandset((LONG*)&m_registers->GPIO_SWPORTA_DDR.ALL_BITS, portBit);
    }
};
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/// The global object used to interact with the Fabric GPIO hardware.
__declspec (selectany) QuarkFabricGpioControllerClass g_quarkFabricGpio;
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)


#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/// Class used to interact with the Quark Legacy GPIO hardware.
class QuarkLegacyGpioControllerClass
{
public:
    /// Constructor.
    QuarkLegacyGpioControllerClass()
    {
        m_hController = INVALID_HANDLE_VALUE;
    }

    /// Destructor.
    virtual ~QuarkLegacyGpioControllerClass()
    {
        DmapCloseController(m_hController);
    }

    /// Method to open the Legacy GPIO controller if it is not already open.
    /**
    \return HRESULT error or success code.
    */
    inline HRESULT openIfNeeded()
    {
        if (m_hController == INVALID_HANDLE_VALUE)
        {
            return _openController();
        }
        else
        {
            return S_OK;
        }
    }

    /// Method to set the state of a Legacy Core Well GPIO port bit.
    inline HRESULT setCorePinState(ULONG portBit, ULONG state);

    /// Method to read the state of a Legacy Core Well GPIO bit.
    inline HRESULT getCorePinState(ULONG portBit, ULONG & state);

    /// Method to set the direction (input or output) of a Legacy Core Well GPIO port bit.
    inline HRESULT setCorePinDirection(ULONG portBit, ULONG mode);

    /// Method to get the direction (input or output) of a Legacy Core Well GPIO port bit.
    inline HRESULT getCorePinDirection(ULONG portBit, ULONG & mode);

    /// Method to set the state of a Legacy Resume Well GPIO port bit.
    inline HRESULT setResumePinState(ULONG portBit, ULONG state);

    /// Method to read the state of a Legacy Resume Well GPIO bit.
    inline HRESULT getResumePinState(ULONG portBit, ULONG & state);

    /// Method to set the direction (input or output) of a Legacy Resume Well GPIO port bit.
    inline HRESULT setResumePinDirection(ULONG portBit, ULONG mode);

    /// Method to get the direction (input or output) of a Legacy Resume Well GPIO port bit.
    inline HRESULT getResumePinDirection(ULONG portBit, ULONG & mode);

private:

    //
    // QuarkLegacyGpioControllerClass private data members.
    //

    /// Handle to the controller device.
    /**
    This handle can be used perform I/O operations on the Legacy GPIO Controller.
    */
    HANDLE m_hController;

    //
    // QuarkLegacyGpioControllerClass private methods.
    //

    /// Method to open the Legacy GPIO Controller.
    HRESULT _openController();

    /// Method to set a Legacy Core Well GPIO pin as an input.
    inline HRESULT _setCorePinInput(ULONG portBit)
    {
        QUARKLGPIO_INPUT_BUFFER inp;
        DWORD bytesReturned;

        inp.Bank = QUARKLGPIO_BANK_COREWELL;
        inp.Mask = 0x1 << portBit;

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_SET_DIR_INPUT,
            &inp,
            sizeof(inp),
            nullptr,
            0,
            &bytesReturned,
            nullptr))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }
        else
        {
            return S_OK;
        }
    }

    /// Method to set a Legacy Core Well GPIO pin as an output
    inline HRESULT _setCorePinOutput(ULONG portBit)
    {
        QUARKLGPIO_INPUT_BUFFER inp;
        DWORD bytesReturned;

        inp.Bank = QUARKLGPIO_BANK_COREWELL;
        inp.Mask = 0x1 << portBit;

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_SET_DIR_OUTPUT,
            &inp,
            sizeof(inp),
            nullptr,
            0,
            &bytesReturned,
            nullptr))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }
        else
        {
            return S_OK;
        }
    }

    /// Method to set a Legacy Resume Well GPIO pin as an input.
    inline HRESULT _setResumePinInput(ULONG portBit)
    {
        QUARKLGPIO_INPUT_BUFFER inp;
        DWORD bytesReturned;

        inp.Bank = QUARKLGPIO_BANK_RESUMEWELL;
        inp.Mask = 0x1 << portBit;

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_SET_DIR_INPUT,
            &inp,
            sizeof(inp),
            nullptr,
            0,
            &bytesReturned,
            nullptr))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }
        else
        {
            return S_OK;
        }
    }

    /// Method to set a Legacy Resume Well GPIO pin as an output
    inline HRESULT _setResumePinOutput(ULONG portBit)
    {
        QUARKLGPIO_INPUT_BUFFER inp;
        DWORD bytesReturned;

        inp.Bank = QUARKLGPIO_BANK_RESUMEWELL;
        inp.Mask = 0x1 << portBit;

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_SET_DIR_OUTPUT,
            &inp,
            sizeof(inp),
            nullptr,
            0,
            &bytesReturned,
            nullptr))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }
        else
        {
            return S_OK;
        }
    }
};
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/// The global object used to interact with the Legacy GPIO hardware.
__declspec (selectany) QuarkLegacyGpioControllerClass g_quarkLegacyGpio;
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if defined(_M_IX86) || defined(_M_X64)
/// Class used to interact with the BayTrail Fabric GPIO hardware.
class BtFabricGpioControllerClass
{
public:
    /// Constructor.
    BtFabricGpioControllerClass()
    {
        m_hS0Controller = INVALID_HANDLE_VALUE;
        m_hS5Controller = INVALID_HANDLE_VALUE;
        m_s0Controller = nullptr;
        m_s5Controller = nullptr;
    }

    /// Destructor.
    virtual ~BtFabricGpioControllerClass()
    {
        DmapCloseController(m_hS0Controller);
        m_s0Controller = nullptr;

        DmapCloseController(m_hS5Controller);
        m_s5Controller = nullptr;
    }

    /// Method to map the S0 GPIO controller registers if they are not already mapped.
    /**
    \return HRESULT error or success code.
    */
    inline HRESULT mapS0IfNeeded()
    {
        HRESULT hr = S_OK;

        if (m_hS0Controller == INVALID_HANDLE_VALUE)
        {
            hr = _mapS0Controller();
        }

        return hr;
    }

    /// Method to map the S5 GPIO controller registers if they are not already mapped.
    /**
    \return HRESULT error or success code.
    */
    inline HRESULT mapS5IfNeeded()
    {
        HRESULT hr = S_OK;

        if (m_hS5Controller == INVALID_HANDLE_VALUE)
        {
            hr = _mapS5Controller();
        }

        return hr;
    }

    /// Method to set the state of an S0 GPIO port bit.
    inline HRESULT setS0PinState(ULONG gpioNo, ULONG state);

    /// Method to set the state of an S5 GPIO port bit.
    inline HRESULT setS5PinState(ULONG gpioNo, ULONG state);

    /// Method to read the state of an S0 GPIO bit.
    inline HRESULT getS0PinState(ULONG gpioNo, ULONG & state);

    /// Method to read the state of an S5 GPIO bit.
    inline HRESULT getS5PinState(ULONG gpioNo, ULONG & state);

    /// Method to set the direction (input or output) of an S0 GPIO port bit.
    inline HRESULT setS0PinDirection(ULONG gpioNo, ULONG mode);

    /// Method to set the direction (input or output) of an S5 GPIO port bit.
    inline HRESULT setS5PinDirection(ULONG gpioNo, ULONG mode);

    /// Method to set the function (mux state) of an S0 GPIO port bit.
    inline HRESULT setS0PinFunction(ULONG gpioNo, ULONG function);

    /// Method to set the function (mux state) of an S5 GPIO port bit.
    inline HRESULT setS5PinFunction(ULONG gpioNo, ULONG function);

private:

#pragma warning(push)
#pragma warning(disable : 4201) // Ignore nameless struct/union warnings

    /// Pad Configuration Register.  Active high (1 enables) unless noted.
    typedef union {
        struct {
            ULONG FUNC_PIN_MUX : 3;         ///< Functional Pin Muxing
            ULONG _rsv0 : 1;
            ULONG IDYNWK2KEN : 1;           ///< Reduce weak 2k contention current
            ULONG _rsv1 : 2;
            ULONG PULL_ASSIGN : 2;          ///< Pull assignment: 0 - None, 1 - Up, 2 - Down
            ULONG PULL_STR : 2;             ///< Pull strength: 0 - 2k, 1 - 10k, 2 - 20k, 3 - 40k
            ULONG BYPASS_FLOP : 1;          ///< Bypass pad I/O flops: 0 - Flop enabled if exists
            ULONG _rsv2 : 1;
            ULONG IHYSCTL : 2;              ///< Hysteresis control
            ULONG IHYSENB : 1;              ///< Hysteresis enable, active low
            ULONG FAST_CLKGATE : 1;         ///< 1 enables the glitch filter fast clock
            ULONG SLOW_CLKGATE : 1;         ///< 1 enables the glitch filter slow clock
            ULONG FILTER_SLOW : 1;          ///< Use RTC clock for unglitch filter
            ULONG FILTER_EN : 1;            ///< Enable the glitch filter
            ULONG DEBOUNCE : 1;             ///< Enable debouncer (uses community debounce time)
            ULONG _rsv3 : 2;
            ULONG STRAP_VAL : 1;            ///< Reflect strap pin value even if overriden
            ULONG GD_LEVEL : 1;             ///< 1 - Use level IRQ, 0 - Edge triggered IRQ
            ULONG GD_TPE : 1;               ///< 1 - Enable positive edge/level detection
            ULONG GD_TNE : 1;               ///< 1 - Enable negative edge/level detection
            ULONG DIRECT_IRQ_EN : 1;        ///< Enable direct wire interrupt, not shared.
            ULONG I25COMP : 1;              ///< Enable 25 ohm compensation of hflvt buffers
            ULONG DISABLE_SECOND_MASK : 1;  ///< Disable second mask when PB_CONFIG ALL_FUNC_MASK used
            ULONG _rsv4 : 1;
            ULONG IODEN : 1;                ///< Enable open drain.
        };
        ULONG ALL_BITS;
    } _PCONF0;

    /// Delay Line Multiplexer Register.
    typedef union {
        struct {
            ULONG DLL_STD_MUX : 5;         ///< Delay standard mux
            ULONG DLL_HGH_MUX : 5;         ///< Delay high mux
            ULONG DLL_DDR_MUX : 5;         ///< Delay ddr mux
            ULONG DLL_CF_OD : 1;           ///< Cf values, software override enable
            ULONG _rsv : 16;
        };
        ULONG ALL_BITS;
    } _PCONF1;

    /// Pad Value Register.
    typedef union {
        struct {
            ULONG PAD_VAL : 1;             ///< Value read from or written to the I/O pad
            ULONG IOUTENB : 1;             ///< Output enable, active low
            ULONG IINENB : 1;              ///< Input enable, active low
            ULONG FUNC_C_VAL : 15;         ///< C value for function delay
            ULONG FUNC_F_VAL : 4;          ///< F value for function delay
            ULONG _rsv : 10;
        };
        ULONG ALL_BITS;
    } _PAD_VAL;


#pragma warning( pop )

    /// Layout of the BayTrail GPIO Controller registers in memory for one pad.
    typedef struct _GPIO_PAD {
        _PCONF0   PCONF0;          ///< 0x00 - Pad Configuration
        _PCONF1   PCONF1;          ///< 0x04 - Delay Line Multiplexer
        _PAD_VAL  PAD_VAL;         ///< 0x08 - Pad Value
        ULONG     _reserved;       ///< 0x0C - 4 ULONG address space per register set
    } volatile GPIO_PAD, *PGPIO_PAD;

    //
    // BtFabricGpioControllerClass private data members.
    //

    /// Handle to the controller device for GPIOs active only in S0 state.
    /**
    This handle can be used to map the S0 GPIO Controller registers in to user
    memory address space.
    */
    HANDLE m_hS0Controller;

    /// Handle to the controller device for GPIOs active in S5 state.
    /**
    This handle can be used to map the S5 GPIO Controller registers in to user
    memory address space.
    */
    HANDLE m_hS5Controller;

    /// Pointer to the S0 GPIO controller object in this process' address space.
    /**
    This controller object is used to access the S0 GPIO registers after
    they are mapped into this process' virtual address space.
    */
    PGPIO_PAD m_s0Controller;

    /// Pointer to the S5 GPIO controller object in this process' address space.
    /**
    This controller object is used to access the S0 GPIO registers after
    they are mapped into this process' virtual address space.
    */
    PGPIO_PAD m_s5Controller;

    //
    // QuarkFabricGpioControllerClass private methods.
    //

    /// Method to map the S0 GPIO Controller into this process' virtual address space.
    HRESULT _mapS0Controller();

    /// Method to map the S5 GPIO Controller into this process' virtual address space.
    HRESULT _mapS5Controller();

    /// Method to set an S0 GPIO pin as an input.
    inline void _setS0PinInput(ULONG gpioNo);

    /// Method to set an S5 GPIO pin as an input.
    inline void _setS5PinInput(ULONG gpioNo);

    /// Method to set an S0 GPIO pin as an output
    inline void _setS0PinOutput(ULONG gpioNo);

    /// Method to set an S5 GPIO pin as an output
    inline void _setS5PinOutput(ULONG gpioNo);
};

/// The global object used to interact with the BayTrail Fabric GPIO hardware.
__declspec (selectany) BtFabricGpioControllerClass g_btFabricGpio;

#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_ARM)
/// Class used to interact with the PI2 BCM2836 GPIO hardware.
class BcmGpioControllerClass
{
public:
    /// Constructor.
    BcmGpioControllerClass()
    {
        m_hController = INVALID_HANDLE_VALUE;
        m_registers = nullptr;
    }

    /// Destructor.
    virtual ~BcmGpioControllerClass()
    {
        DmapCloseController(m_hController);
        m_registers = nullptr;
    }

    /// Method to map the BCM2836 GPIO controller registers if they are not already mapped.
    /**
    \return HRESULT error or success code.
    */
    inline HRESULT mapIfNeeded()
    {
        HRESULT hr = S_OK;

        if (m_hController == INVALID_HANDLE_VALUE)
        {
            hr = _mapController();
        }

        return hr;
    }

    /// Method to set the state of a GPIO port bit.
    inline HRESULT setPinState(ULONG gpioNo, ULONG state);

    /// Method to read the state of a GPIO bit.
    inline HRESULT getPinState(ULONG gpioNo, ULONG & state);

    /// Method to set the direction (input or output) of a GPIO port bit.
    inline HRESULT setPinDirection(ULONG gpioNo, ULONG mode);

    /// Method to set the function (mux state) of a GPIO port bit.
    inline HRESULT setPinFunction(ULONG gpioNo, ULONG function);

    /// Method to turn pin pullup on or off.
    inline HRESULT setPinPullup(ULONG gpioNo, BOOL pullup);

private:

    // Value to write to GPPUD to turn pullup/down off for pins.
    const ULONG pullupOff = 0;

    // Value to write to GPPUD to turn pullup on for a pin.
    const ULONG pullupOn = 2;

    /// Layout of the BCM2836 GPIO Controller registers in memory.
    typedef struct _BCM_GPIO {
        ULONG   GPFSELN[6];         ///< 0x00-0x17 - Function select GPIO 00-53
        ULONG   _rsv01;             //   0x18
        ULONG   GPSET0;             ///< 0x1C - Output Set GPIO 00-31
        ULONG   GPSET1;             ///< 0x20 - Output Set GPIO 32-53
        ULONG   _rsv02;             //   0x24
        ULONG   GPCLR0;             ///< 0x28 - Output Clear GPIO 00-31
        ULONG   GPCLR1;             ///< 0x2C - Output Clear GPIO 32-53
        ULONG   _rsv03;             //   0x30
        ULONG   GPLEV0;             ///< 0x34 - Level GPIO 00-31
        ULONG   GPLEV1;             ///< 0x38 - Level GPIO 32-53
        ULONG   _rsv04;             //   0x3C
        ULONG   GPEDS0;             ///< 0x40 - Event Detect Status GPIO 00-31
        ULONG   GPEDS1;             ///< 0x44 - Event Detect Status GPIO 32-53
        ULONG   _rsv05;             //   0x48
        ULONG   GPREN0;             ///< 0x4C - Rising Edge Detect Enable GPIO 00-31
        ULONG   GPREN1;             ///< 0x50 - Rising Edge Detect Enable GPIO 32-53
        ULONG   _rsv06;             //   0x54
        ULONG   GPFEN0;             ///< 0x58 - Falling Edge Detect Enable GPIO 00-31
        ULONG   GPFEN1;             ///< 0x5C - Falling Edge Detect Enable GPIO 32-53
        ULONG   _rsv07;             //   0x60
        ULONG   GPHEN0;             ///< 0x64 - High Detect Enable GPIO 00-31
        ULONG   GPHEN1;             ///< 0x68 - High Detect Enable GPIO 32-53
        ULONG   _rsv08;             //   0x6C
        ULONG   GPLEN0;             ///< 0x70 - Low Detect Enable GPIO 00-31
        ULONG   GPLEN1;             ///< 0x74 - Low Detect Enable GPIO 32-53
        ULONG   _rsv09;             //   0x78
        ULONG   GPAREN0;            ///< 0x7C - Async Rising Edge Detect GPIO 00-31
        ULONG   GPAREN1;            ///< 0x80 - Async Rising Edge Detect GPIO 32-53
        ULONG   _rsv10;             //   0x84
        ULONG   GPAFEN0;            ///< 0x88 - Async Falling Edge Detect GPIO 00-31
        ULONG   GPAFEN1;            ///< 0x8C - Async Falling Edge Detect GPIO 32-53
        ULONG   _rsv11;             //   0x90
        ULONG   GPPUD;              ///< 0x94 - GPIO Pin Pull-up/down Enable
        ULONG   GPPUDCLK0;          ///< 0x98 - Pull-up/down Enable Clock GPIO 00-31
        ULONG   GPPUDCLK1;          ///< 0x9C - Pull-up/down Enable Clock GPIO 32-53
        ULONG   _rsv12[4];          //   0xA0
        ULONG   _Test;              //   0xB0
    } volatile BCM_GPIO, *PBCM_GPIO;

    //
    // BcmGpioControllerClass private data members.
    //

    /// Handle to the controller device for GPIOs.
    /**
    This handle can be used to refer to the opened GPIO Controller to perform such 
    actions as closing or locking the controller.
    */
    HANDLE m_hController;

    /// Pointer to the GPIO controller object in this process' address space.
    /**
    This controller object is used to access the GPIO registers after
    they are mapped into this process' virtual address space.
    */
    PBCM_GPIO m_registers;

    //
    // BcmGpioControllerClass private methods.
    //

    /// Method to map the Controller into this process' virtual address space.
    HRESULT _mapController();

};

/// The global object used to interact with the BayTrail Fabric GPIO hardware.
__declspec (selectany) BcmGpioControllerClass g_bcmGpio;

#endif // defined(_M_ARM)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has verified the input parameters.
\param[in] portBit The number of the bit to set. Range: 0-7.
\param[in] state The state to set on the port bit. 0 - low, 1 - high.
\return HRESULT error or success code.
*/
inline HRESULT QuarkFabricGpioControllerClass::setPinState(ULONG portBit, ULONG state)
{
    HRESULT hr = S_OK;
    
    hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        if (state == 0)
        {
            _bittestandreset((LONG*)&m_registers->GPIO_SWPORTA_DR.ALL_BITS, portBit);
        }
        else
        {
            _bittestandset((LONG*)&m_registers->GPIO_SWPORTA_DR.ALL_BITS, portBit);
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
If the port bit is configured as in input, the state passed back is the state of the external
signal connnected to the bit.  If the port bit is configured as an output, the state passed
back is the state that was last written to the port bit.  This method assumes the caller has
verified the input parameter.
\param[in] portBit The number of the bit to set. Range: 0-7.
\param[out] state The state read from the port bit.
\return HRESULT error or success code.
*/
inline HRESULT QuarkFabricGpioControllerClass::getPinState(ULONG portBit, ULONG & state)
{
    HRESULT hr = S_OK;

    hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        state = (m_registers->GPIO_EXT_PORTA.ALL_BITS >> portBit) & 0x01;
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to modify. Range: 0-7.
\param[in] mode The mode to set for the bit.  Range: DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT QuarkFabricGpioControllerClass::setPinDirection(ULONG portBit, ULONG mode)
{
    HRESULT hr = S_OK;

    hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        switch (mode)
        {
        case DIRECTION_IN:
            _setPinInput(portBit);
            break;
        case DIRECTION_OUT:
            _setPinOutput(portBit);
            break;
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to modify. Range: 0-7.
\param[out] mode The mode of the bit, DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT QuarkFabricGpioControllerClass::getPinDirection(ULONG portBit, ULONG & mode)
{
    HRESULT hr = S_OK;
 
    hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        if ((m_registers->GPIO_SWPORTA_DDR.ALL_BITS >> portBit) == 0)
        {
            mode = DIRECTION_IN;
        }
        else
        {
            mode = DIRECTION_OUT;
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to modify. Range: 0-7.
\param[in] state The state to set on the port bit. 0 - low, 1 - high.
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::setCorePinState(ULONG portBit, ULONG state)
{
    HRESULT hr = S_OK;
    QUARKLGPIO_INPUT_BUFFER inp;
    DWORD ioCtl;
    DWORD bytesReturned;

    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {
        inp.Bank = QUARKLGPIO_BANK_COREWELL;
        inp.Mask = 0x1 << portBit;

        if (state == 0)
        {
            ioCtl = IOCTL_QUARKLGPIO_SET_PINS_LOW;
        }
        else
        {
            ioCtl = IOCTL_QUARKLGPIO_SET_PINS_HIGH;
        }

        if (!DeviceIoControl(
            m_hController,
            ioCtl,
            &inp,
            sizeof(inp),
            nullptr,
            0,
            &bytesReturned,
            nullptr))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to read. Range: 0-7.
\param[out] state Set to the state of the input bit.  0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::getCorePinState(ULONG portBit, ULONG & state)
{
    HRESULT hr = S_OK;
    QUARKLGPIO_INPUT_BUFFER inp;
    DWORD bytesReturned;
    DWORD portContents;

    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {
        inp.Bank = QUARKLGPIO_BANK_COREWELL;
        inp.Mask = 0x1 << portBit;

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_READ_PINS,
            &inp,
            sizeof(inp),
            &portContents,
            sizeof(portContents),
            &bytesReturned,
            nullptr))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }

    if (SUCCEEDED(hr))
    {
        state = (portContents >> portBit) & 0x01;
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to modify. Range: 0-1.
\param[in] mode The mode to set for the bit.  Range: DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::setCorePinDirection(ULONG portBit, ULONG mode)
{
    HRESULT hr = S_OK;
    
    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {
        switch (mode)
        {
        case DIRECTION_IN:
            hr = _setCorePinInput(portBit);
            break;
        case DIRECTION_OUT:
            hr = _setCorePinOutput(portBit);
            break;
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.  This method is largely
intended for testing use--it is more efficient to just set the desired direction rather
than checking first.
\param[in] portBit The number of the bit to modify. Range: 0-1.
\param[out] mode The mode of the bit, DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::getCorePinDirection(ULONG portBit, ULONG & mode)
{
    HRESULT hr = S_OK;
    
    QUARKLGPIO_INPUT_BUFFER inp;
    DWORD readValue;
    DWORD bytesReturned;

    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {

        inp.Bank = QUARKLGPIO_BANK_COREWELL;
        inp.Mask = 0xFF;                // Not used, but if it were, we would want all 8 bits

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_GET_DIR,
            &inp,
            sizeof(inp),
            &readValue,
            sizeof(readValue),
            &bytesReturned,
            nullptr))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }

        if (SUCCEEDED(hr))
        {
            if (((readValue >> portBit) & 0x01) == 1)
            {
                mode = DIRECTION_IN;
            }
            else
            {
                mode = DIRECTION_OUT;
            }
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.  This method is largely
intended for testing use--it is more efficient to just set the desired direction rather
than checking first.
\param[in] portBit The number of the bit to modify. Range: 0-5.
\param[out] mode The mode of the bit, DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::getResumePinDirection(ULONG portBit, ULONG & mode)
{
    HRESULT hr = S_OK;
    
    QUARKLGPIO_INPUT_BUFFER inp;
    DWORD readValue;
    DWORD bytesReturned;

    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {

        inp.Bank = QUARKLGPIO_BANK_RESUMEWELL;
        inp.Mask = 0xFF;                // Not used, but if it were, we would want all 8 bits

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_GET_DIR,
            &inp,
            sizeof(inp),
            &readValue,
            sizeof(readValue),
            &bytesReturned,
            nullptr))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }

        if (SUCCEEDED(hr))
        {
            if (((readValue >> portBit) & 0x01) == 1)
            {
                mode = DIRECTION_IN;
            }
            else
            {
                mode = DIRECTION_OUT;
            }
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to modify. Range: 0-5.
\param[in] mode The mode to set for the bit.  Range: DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::setResumePinDirection(ULONG portBit, ULONG mode)
{
    HRESULT hr = S_OK;
    
    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {
        switch (mode)
        {
        case DIRECTION_IN:
            hr = _setResumePinInput(portBit);
            break;
        case DIRECTION_OUT:
            hr = _setResumePinOutput(portBit);
            break;
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to modify. Range: 0-7.
\param[in] state The state to set on the port bit. 0 - low, 1 - high.
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::setResumePinState(ULONG portBit, ULONG state)
{
    HRESULT hr = S_OK;
    
    QUARKLGPIO_INPUT_BUFFER inp;
    DWORD ioCtl;
    DWORD bytesReturned;

    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {
        inp.Bank = QUARKLGPIO_BANK_RESUMEWELL;
        inp.Mask = 0x1 << portBit;

        if (state == 0)
        {
            ioCtl = IOCTL_QUARKLGPIO_SET_PINS_LOW;
        }
        else
        {
            ioCtl = IOCTL_QUARKLGPIO_SET_PINS_HIGH;
        }

        if (!DeviceIoControl(
            m_hController,
            ioCtl,
            &inp,
            sizeof(inp),
            nullptr,
            0,
            &bytesReturned,
            nullptr))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
/**
This method assumes the caller has checked the input parameters.
\param[in] portBit The number of the bit to read. Range: 0-7.
\param[out] state Set to the state of the input bit.  0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT QuarkLegacyGpioControllerClass::getResumePinState(ULONG portBit, ULONG & state)
{
    HRESULT hr = S_OK;  
    QUARKLGPIO_INPUT_BUFFER inp;
    DWORD bytesReturned;
    DWORD portContents;

    hr = openIfNeeded();

    if (SUCCEEDED(hr))
    {
        inp.Bank = QUARKLGPIO_BANK_RESUMEWELL;
        inp.Mask = 0x1 << portBit;

        if (!DeviceIoControl(
            m_hController,
            IOCTL_QUARKLGPIO_READ_PINS,
            &inp,
            sizeof(inp),
            &portContents,
            sizeof(portContents),
            &bytesReturned,
            nullptr))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }

    if (SUCCEEDED(hr))
    {
        state = (portContents >> portBit) & 0x01;
    }

    return hr;
}
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S0 GPIO number of the pad to set. Range: 0-127.
\param[in] state State to set the pad to. 0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::setS0PinState(ULONG gpioNo, ULONG state)
{
    HRESULT hr = mapS0IfNeeded();

    if (SUCCEEDED(hr))
    {
        _PAD_VAL padVal;
        padVal.ALL_BITS = m_s0Controller[gpioNo].PAD_VAL.ALL_BITS;
        if (state == 0)
        {
            padVal.PAD_VAL = 0;
        }
        else
        {
            padVal.PAD_VAL = 1;
        }
        m_s0Controller[gpioNo].PAD_VAL.ALL_BITS = padVal.ALL_BITS;
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S5 GPIO number of the pad to set. Range: 0-59.
\param[in] state State to set the pad to. 0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::setS5PinState(ULONG gpioNo, ULONG state)
{
    HRESULT hr = mapS5IfNeeded();

    if (SUCCEEDED(hr))
    {
        _PAD_VAL padVal;
        padVal.ALL_BITS = m_s5Controller[gpioNo].PAD_VAL.ALL_BITS;
        if (state == 0)
        {
            padVal.PAD_VAL = 0;
        }
        else
        {
            padVal.PAD_VAL = 1;
        }
        m_s5Controller[gpioNo].PAD_VAL.ALL_BITS = padVal.ALL_BITS;
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S0 GPIO number of the pad to read. Range: 0-127.
\param[out] state Set to the state of the input bit.  0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::getS0PinState(ULONG gpioNo, ULONG & state)
{
    HRESULT hr = mapS0IfNeeded();

    if (SUCCEEDED(hr))
    {
        _PAD_VAL padVal;
        padVal.ALL_BITS = m_s0Controller[gpioNo].PAD_VAL.ALL_BITS;
        state = padVal.PAD_VAL;
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S5 GPIO number of the pad to read. Range: 0-59.
\param[out] state Set to the state of the input bit.  0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::getS5PinState(ULONG gpioNo, ULONG & state)
{
    HRESULT hr = mapS5IfNeeded();

    if (SUCCEEDED(hr))
    {
        _PAD_VAL padVal;
        padVal.ALL_BITS = m_s5Controller[gpioNo].PAD_VAL.ALL_BITS;
        state = padVal.PAD_VAL;
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S0 GPIO number of the pad to configure. Range: 0-127.
\param[in] mode The mode to set for the bit.  Range: DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::setS0PinDirection(ULONG gpioNo, ULONG mode)
{
    HRESULT hr = S_OK;
    
    hr = mapS0IfNeeded();

    if (SUCCEEDED(hr))
    {
        switch (mode)
        {
        case DIRECTION_IN:
            _setS0PinInput(gpioNo);
            break;
        case DIRECTION_OUT:
            _setS0PinOutput(gpioNo);
            break;
        }
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S5 GPIO number of the pad to configure. Range: 0-59.
\param[in] mode The mode to set for the bit.  Range: DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::setS5PinDirection(ULONG gpioNo, ULONG mode)
{
    HRESULT hr = S_OK;
    
    hr = mapS5IfNeeded();

    if (SUCCEEDED(hr))
    {
        switch (mode)
        {
        case DIRECTION_IN:
            _setS5PinInput(gpioNo);
            break;
        case DIRECTION_OUT:
            _setS5PinOutput(gpioNo);
            break;
        }
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S0 GPIO number of the pad to configure.  Range: 0-127.
\param[in] function The function to set for the pin.  Range: 0-7 (only 0-1 used here).
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::setS0PinFunction(ULONG gpioNo, ULONG function)
{
    HRESULT hr = S_OK;
    
    hr = mapS0IfNeeded();

    if (SUCCEEDED(hr))
    {
        _PCONF0 padConfig;
        padConfig.ALL_BITS = m_s0Controller[gpioNo].PCONF0.ALL_BITS;
        padConfig.FUNC_PIN_MUX = function;
        m_s0Controller[gpioNo].PCONF0.ALL_BITS = padConfig.ALL_BITS;
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The S5 GPIO number of the pad to configure.  Range: 0-59.
\param[in] function The function to set for the pin.  Range: 0-7 (only 0-1 used here).
\return HRESULT error or success code.
*/
inline HRESULT BtFabricGpioControllerClass::setS5PinFunction(ULONG gpioNo, ULONG function)
{
    HRESULT hr = S_OK;
    
    hr = mapS5IfNeeded();

    if (SUCCEEDED(hr))
    {
        _PCONF0 padConfig;
        padConfig.ALL_BITS = m_s5Controller[gpioNo].PCONF0.ALL_BITS;
        padConfig.FUNC_PIN_MUX = function;
        m_s5Controller[gpioNo].PCONF0.ALL_BITS = padConfig.ALL_BITS;
    }

    return hr;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This routine disables the output latch for the pad, disables pin output and
enables pin input.
\param[in] gpioNo The S0 GPIO number of the pad to configure. Range: 0-127.
*/
inline void BtFabricGpioControllerClass::_setS0PinInput(ULONG gpioNo)
{
    _PCONF0 padConfig;
    padConfig.ALL_BITS = m_s0Controller[gpioNo].PCONF0.ALL_BITS;
    padConfig.BYPASS_FLOP = 1;         // Disable flop
    padConfig.PULL_ASSIGN = 0;         // Disable pull-up
    m_s0Controller[gpioNo].PCONF0.ALL_BITS = padConfig.ALL_BITS;

    _PAD_VAL padVal;
    padVal.ALL_BITS = m_s0Controller[gpioNo].PAD_VAL.ALL_BITS;
    padVal.IINENB = 0;                 // Enable pad for input
    padVal.IOUTENB = 1;                // Disable pad for output
    m_s0Controller[gpioNo].PAD_VAL.ALL_BITS = padVal.ALL_BITS;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This routine disables the output latch for the pad, disables pin output and
enables pin input.
\param[in] gpioNo The S5 GPIO number of the pad to configure. Range: 0-127.
*/
inline void BtFabricGpioControllerClass::_setS5PinInput(ULONG gpioNo)
{
    _PCONF0 padConfig;
    padConfig.ALL_BITS = m_s5Controller[gpioNo].PCONF0.ALL_BITS;
    padConfig.BYPASS_FLOP = 1;         // Disable flop
    padConfig.PULL_ASSIGN = 0;         // No pull resistor
    m_s5Controller[gpioNo].PCONF0.ALL_BITS = padConfig.ALL_BITS;

    _PAD_VAL padVal;
    padVal.ALL_BITS = m_s5Controller[gpioNo].PAD_VAL.ALL_BITS;
    padVal.IINENB = 0;                 // Enable pad for input
    padVal.IOUTENB = 1;                // Disable pad for output
    m_s5Controller[gpioNo].PAD_VAL.ALL_BITS = padVal.ALL_BITS;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This routine enables the output latch for the pad, disables pull-ups on
the pad, disables pin input and enables pin output.
\param[in] gpioNo The S0 GPIO number of the pad to configure. Range: 0-127.
*/
inline void BtFabricGpioControllerClass::_setS0PinOutput(ULONG gpioNo)
{
    _PCONF0 padConfig;
    padConfig.ALL_BITS = m_s0Controller[gpioNo].PCONF0.ALL_BITS;
    padConfig.BYPASS_FLOP = 0;         // Enable flop
    padConfig.PULL_ASSIGN = 0;         // No pull resistor
    padConfig.FUNC_PIN_MUX = 0;        // Mux function 0 (GPIO)
    m_s0Controller[gpioNo].PCONF0.ALL_BITS = padConfig.ALL_BITS;

    _PAD_VAL padVal;
    padVal.ALL_BITS = m_s0Controller[gpioNo].PAD_VAL.ALL_BITS;
    padVal.IOUTENB = 0;                // Enable pad for output
    padVal.IINENB = 1;                 // Disable pad for input
    m_s0Controller[gpioNo].PAD_VAL.ALL_BITS = padVal.ALL_BITS;
}
#endif // defined(_M_IX86) || defined(_M_X64)

#if defined(_M_IX86) || defined(_M_X64)
/**
This routine enables the output latch for the pad, disables pull-ups on
the pad, disables pin input and enables pin output.
\param[in] gpioNo The S5 GPIO number of the pad to configure. Range: 0-127.
*/
inline void BtFabricGpioControllerClass::_setS5PinOutput(ULONG gpioNo)
{
    _PCONF0 padConfig;
    padConfig.ALL_BITS = m_s5Controller[gpioNo].PCONF0.ALL_BITS;
    padConfig.BYPASS_FLOP = 0;         // Enable flop
    padConfig.PULL_ASSIGN = 0;         // No pull resistor
    m_s5Controller[gpioNo].PCONF0.ALL_BITS = padConfig.ALL_BITS;

    _PAD_VAL padVal;
    padVal.ALL_BITS = m_s5Controller[gpioNo].PAD_VAL.ALL_BITS;
    padVal.IOUTENB = 0;                // Enable pad for output
    padVal.IINENB = 1;                 // Disable pad for input
    m_s5Controller[gpioNo].PAD_VAL.ALL_BITS = padVal.ALL_BITS;
}
#endif // defined(_M_IX86) || defined(_M_X64)




#if defined(_M_ARM)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The GPIO number of the pad to set. Range: 0-31.
\param[in] state State to set the pad to. 0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT BcmGpioControllerClass::setPinState(ULONG gpioNo, ULONG state)
{
    HRESULT hr = mapIfNeeded();
    ULONG bitMask;

    if (SUCCEEDED(hr))
    {
        if (gpioNo < 32)
        {
            bitMask = 1 << gpioNo;
            if (state == 0)
            {
                m_registers->GPCLR0 = bitMask;
            }
            else
            {
                m_registers->GPSET0 = bitMask;
            }
        }
        else
        {
            bitMask = 1 << (gpioNo - 32);
            if (state == 0)
            {
                m_registers->GPCLR1 = bitMask;
            }
            else
            {
                m_registers->GPSET1 = bitMask;
            }
        }
    }

    return hr;
}
#endif // defined(_M_ARM)

#if defined(_M_ARM)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The GPIO number of the pad to read. Range: 0-31.
\param[out] state Set to the state of the input bit.  0 - LOW, 1 - HIGH.
\return HRESULT error or success code.
*/
inline HRESULT BcmGpioControllerClass::getPinState(ULONG gpioNo, ULONG & state)
{
    HRESULT hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        if (gpioNo < 32)
        {
            state = ((m_registers->GPLEV0) >> gpioNo) & 1;
        }
        else
        {
            state = ((m_registers->GPLEV1) >> (gpioNo - 32)) & 1;
        }
    }

    return hr;
}
#endif // defined(_M_ARM)

#if defined(_M_ARM)
/**
This method assumes the caller has checked the input parameters.  This method has 
the side effect of setting the pin to GPIO use (since the BCM GPIO controller uses 
the same bits to control both function and direction).
\param[in] gpioNo The GPIO number of the pad to configure. Range: 0-53.
\param[in] mode The mode to set for the bit.  Range: DIRECTION_IN or DIRECTION_OUT
\return HRESULT error or success code.
*/
inline HRESULT BcmGpioControllerClass::setPinDirection(ULONG gpioNo, ULONG mode)
{
    HRESULT hr = S_OK;
    ULONG funcSelData = 0;

    hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        hr = GetControllerLock(m_hController);
    }

    if (SUCCEEDED(hr))
    {
        // Each GPIO has a 3-bit function field (000b selects input, 001b output, etc.) 
        // and there are 10 such fields in each 32-bit function select register.

        funcSelData = m_registers->GPFSELN[gpioNo / 10];   // Read function register data

        funcSelData &= ~(0x07 << ((gpioNo % 10) * 3));      // Clear bits for GPIO (make input)

        if (mode == DIRECTION_OUT)                          // If GPIO should be output:
        {
            funcSelData |= (0x01 << ((gpioNo % 10) * 3));   // Set one bit for GPIO (make output)
        }

        m_registers->GPFSELN[gpioNo / 10] = funcSelData;   // Write function register data back

        ReleaseControllerLock(m_hController);
    }

    return hr;
}
#endif // defined(_M_ARM)

#if defined(_M_ARM)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The GPIO number of the pad to configure.  Range: 0-53.
\param[in] function The function to set for the pin.  Range: 0-1.  Function 0 is GPIO, and
function 1 is the non-GPIO use of the pin (called "alternate function 0" in the datasheet).
\return HRESULT error or success code.
*/
inline HRESULT BcmGpioControllerClass::setPinFunction(ULONG gpioNo, ULONG function)
{
    HRESULT hr = S_OK;
    ULONG funcSelData = 0;

    hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        hr = GetControllerLock(m_hController);
    }

    if (SUCCEEDED(hr))
    {
        // Each GPIO has a 3-bit function field (000b selects input, 001b output, etc.) 
        // and there are 10 such fields in each 32-bit function select register.

        funcSelData = m_registers->GPFSELN[gpioNo / 10];   // Read function register data

        // If the function is already set to GPIO, and the new function is GPIO, leave it 
        // alone (so we don't change the pin direction trying to set the pin function).
        if (((funcSelData & (0x06 << ((gpioNo % 10) * 3))) != 0) || (function != 0))
        {
            funcSelData &= ~(0x07 << ((gpioNo % 10) * 3));      // Clear bits for (make GPIO input)

            if (function == 1)                                  // If alternate function 0 is wanted:
            {
                funcSelData |= (0x04 << ((gpioNo % 10) * 3));   // Set function code to 100b
            }

            m_registers->GPFSELN[gpioNo / 10] = funcSelData;   // Write function register data back
        }

        ReleaseControllerLock(m_hController);
    }

    return hr;
}
#endif // defined(_M_ARM)

#if defined(_M_ARM)
/**
This method assumes the caller has checked the input parameters.
\param[in] gpioNo The GPIO number of the pad to configure.  Range: 0-53.
\param[in] pullup TRUE to turn pullup on for this pin, FALSE to turn it off.
\return HRESULT error or success code.
*/
inline HRESULT BcmGpioControllerClass::setPinPullup(ULONG gpioNo, BOOL pullup)
{
    HRESULT hr = S_OK;
    ULONG gpioPull = 0;
    HiResTimerClass timer;


    hr = mapIfNeeded();

    if (SUCCEEDED(hr))
    {
        hr = GetControllerLock(m_hController);
    }

    if (SUCCEEDED(hr))
    {
        if (pullup)
        {
            gpioPull = pullupOn;
        }
        else
        {
            gpioPull = pullupOff;
        }

        //
        // The sequence to set pullup/down for a pin is:
        // 1) Write desired state to GPPUD
        // 2) Wait 150 cycles
        // 3) Write clock mask to GPPUDCLK0/1 with bits set that correspond to pins to be configured
        // 4) Wait 150 cycles
        // 5) Write to GPPUD to remove state
        // 6) Write to GPPUDCLK0/1 to remove clock bits
        //
        // 150 cycles is 0.25 microseconds with a cpu clock of 600 Mhz.
        //
        m_registers->GPPUD = gpioPull;         // 1)

        timer.StartTimeout(1);
        while (!timer.TimeIsUp());              // 2)
        
        m_registers->GPPUDCLK0 = 1 << gpioNo;
        m_registers->GPPUDCLK1 = 0;            // 3)
        
        timer.StartTimeout(1);
        while (!timer.TimeIsUp());              // 4)
        
        m_registers->GPPUD = 0;                // 5)
        
        m_registers->GPPUDCLK0 = 0;
        m_registers->GPPUDCLK1 = 0;            // 6)

        ReleaseControllerLock(m_hController);
    }

    return hr;
}
#endif // defined(_M_ARM)

#endif  // _GPIO_CONTROLLER_H_