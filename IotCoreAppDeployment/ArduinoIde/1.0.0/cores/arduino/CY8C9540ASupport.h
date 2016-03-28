// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _CY8C9540A_SUPPORT_H_
#define _CY8C9540A_SUPPORT_H_

#include <Windows.h>

class CY8C9540ADevice
{
public:

    /// Set an I/O Expander output to be high or low.
    static HRESULT SetBitState(ULONG i2cAdr, ULONG bit, ULONG state);

    /// Get the current state of an I/O Expander chip output.
    static HRESULT GetBitState(ULONG i2cAdr, ULONG bit, ULONG & state);

    /// Set the direction of a pin on the I/O Expander.
    static HRESULT SetBitDirection(ULONG i2cAdr, ULONG portBit, ULONG direction, BOOL pullup);

    /// Get the direction of a pin on the I/O Expander.
    static HRESULT GetBitDirection(ULONG i2cAdr, ULONG portBit, ULONG & direction);

    /// Configure a port bit as a PWM output.
    static HRESULT SetPortbitPwm(ULONG i2cAdr, ULONG portBit, ULONG pwmChan);

    /// Configure a port bit for Digital I/O use.
    static HRESULT SetPortbitDio(ULONG i2cAdr, ULONG portBit);

    /// Set the PWM pulse width.
    static HRESULT SetPwmDutyCycle(ULONG i2cAdr, ULONG chan, ULONG pulseWidth);

    /// Method to get the resolution of this PWM chip.
    static  ULONG GetResolution()
    {
        return PWM_BITS;
    }

private:
    static const ULONG PWM_BITS;            ///< Number of bits of resolution this PWM chip has.
    static const ULONG PWM_CHAN_COUNT;      ///< Number of PWM channels on this chip.
    static const ULONG PORT_COUNT;          ///< Number of I/O ports supported by this chip.

    static const ULONG IN_BASE_ADR;         ///< Base address of Input Port registers.
    static const ULONG OUT_BASE_ADR;        ///< Base address of Output Port registers.
    static const ULONG INT_STAT_BASE_ADR;   ///< Base address of Inerrupt Status registers.
    static const ULONG PORT_SELECT_ADR;     ///< Address of Port Select register.
    static const ULONG INT_MASK_ADR;        ///< Address of Interrupt Mask register.
    static const ULONG SEL_PWM_ADR;         ///< Address of Select PWM for Port Output register.
    static const ULONG INVERSION_ADR;       ///< Address of Inversion register.
    static const ULONG PIN_DIR_ADR;         ///< Address of Pin Direction register (0: Out, 1: In)
    static const ULONG PULL_UP_ADR;         ///< Address of Drive Mode - Pull Up register.
    static const ULONG PULL_DOWN_ADR;       ///< Address of Drive Mode - Pull Down register.
    static const ULONG OPEN_HIGH_ADR;       ///< Address of Drive Mode - Open Drain High register.
    static const ULONG OPEN_LOW_ADR;        ///< Address of Drive Mode - Open Drain Low register.
    static const ULONG DRIVE_STRONG_ADR;    ///< Address of Drive Mode - Strong register.
    static const ULONG SLOW_STRONG_ADR;     ///< Address of Drive Mode - Slow Strong register.
    static const ULONG HIGH_Z_ADR;          ///< Address of Drive Mode - High-Z register.
    static const ULONG PWM_SELECT_ADR;      ///< Address of PWM Select register.
    static const ULONG CONFIG_PWM_ADR;      ///< Address of Config PWM register.
    static const ULONG PERIOD_PWM_ADR;      ///< Address of Period PWM register.
    static const ULONG PULSE_WIDTH_ADR;     ///< Address of Pulse Width PWM register.
    static const ULONG DIVIDER_ADR;         ///< Address of Programmable Diver register.
    static const ULONG ENABLE_EEE_ADR;      ///< Address of Enable WDE, EEE, EERO register.
    static const ULONG ID_STATUS_ADR;       ///< Address of Device ID/Status register.
    static const ULONG WATCHDOG_ADR;        ///< Address of Watchdog register.
    static const ULONG COMMAND_ADR;         ///< Address of Command register.

    static const ULONG PWM_CLK_32K;         ///< Config PWM register value for 32 kHz clock.
    static const ULONG PWM_CLK_24M;         ///< Config PWM register value for 24 MHz clock.
    static const ULONG PWM_CLK_1M5;         ///< Config PWM register value for 1.5 MHz clock.
    static const ULONG PWM_CLK_94K;         ///< Config PWM register value for 93.75 kHz clock.
    static const ULONG PWM_CLK_368;         ///< Config PWM register value for 367.6 Hz clock.
    static const ULONG PWM_CLK_PREV;        ///< Config PWM register value for Previous PWM clock.

    static const ULONG CMD_STORE_CONFIG;    ///< Command to store device config in EEPROM.
    static const ULONG CMD_FACTORY_DFLTS;   ///< Command to restore factory defaults.
    static const ULONG CMD_WR_EE_DFLTS;     ///< Command to write EEPROM POR defaults.
    static const ULONG CMD_RD_EE_DFLTS;     ///< Command to read EEPROM POR defaults.
    static const ULONG CMD_WR_CONFIG;       ///< Command to write device configuration.
    static const ULONG CMD_RD_CONFIG;       ///< Command to read device configuation.
    static const ULONG CMD_LD_DEFAULTS;     ///< Command to reconfigure with stored POR defaults.

    /// Struct with the layout of the Enable WDE, EEE, EERO register.
    typedef struct {
        UCHAR   WDE : 1;        ///< 0: Write Disable pin is GPIO, 1: Pin is Write Disable.
        UCHAR   EEE : 1;        ///< 0: EEPROM disabled, 1: EEPROM enabled.
        UCHAR   EER0 : 1;       ///< 0: EEPROM is read-write, 1: EEPROM is read-only.
        UCHAR   rsvd : 5;       // Reserved
    } ENABLE_EEE, *PENABLE_EEE;

    /// Struct with the layout of the Device ID/Status Register.
    typedef struct {
        UCHAR   FD_UD : 1;      ///< 0: User Defaults loaded, 1: Factory Defaults loaded.
        UCHAR   rsvd : 3;       // Reserved
        UCHAR   FAMILY : 4;     ///< Device Familey (2, 4 or 6).
    } ID_STATUS, *PID_STATUS;

    /// Constructor.
    CY8C9540ADevice()
    {
    }

    /// Copy constructor.
    CY8C9540ADevice(CY8C9540ADevice&)
    {
    }

    /// Destructor.
    virtual ~CY8C9540ADevice()
    {
    }

    /// Configure the PWM frequency on a channel.
    static HRESULT _configurePwmChannelFrequency(ULONG i2cAdr, ULONG chan);

};

#endif  // _CY8C9540A_SUPPORT_H_