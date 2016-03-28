// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _PCA9685_SUPPORT_H_
#define _PCA9685_SUPPORT_H_

#include <Windows.h>

class PCA9685Device
{
public:

    /// Set a PWM chip output to constant on or constant off.
    static HRESULT SetBitState(ULONG i2cAdr, ULONG bit, ULONG state);

    /// Get the current state of a PWM chip output.
    static HRESULT GetBitState(ULONG i2cAdr, ULONG bit, ULONG & state);

    /// Set the PWM pulse width.
    static HRESULT SetPwmDutyCycle(ULONG i2cAdr, ULONG bit, ULONG pulseWidth);

    /// Set the PWM pulse repetition rate.
    static HRESULT SetPwmFrequency(ULONG i2cAdr, ULONG frequencyHz);

    /// Get the actual PWM pulse repetition rate.
    static ULONG GetActualPwmFrequency(ULONG i2cAdr);

    /// Method to get the resolution of this PWM chip.
    static ULONG GetResolution()
    {
        return PWM_BITS;
    }

private:
    static const ULONG PWM_BITS;        ///< Number of bits of resolution this PWM chip has
    static const ULONG MODE1_ADR;       ///< Address of MODE1 register
    static const ULONG MODE2_ADR;       ///< Address of MODE2 register
    static const ULONG SUBADR1_ADR;     ///< Address of SUBADR1 register
    static const ULONG SUBADR2_ADR;     //< Address of SUBADR2 register
    static const ULONG SUBADR3_ADR;     ///< Address of SUBADR3 register
    static const ULONG ALLCALLADR_ADR;  ///< Address of ALLCALLADR register
    static const ULONG LEDS_BASE_ADR;   ///< Base address of LED output registers
    static const ULONG REGS_PER_LED;    ///< Number of registers for each LED
    static const ULONG PRE_SCALE_ADR;   ///< Address of frequency prescale register
    static const ULONG TestMode_ADR;    ///< Address of TestMode register
    static const ULONG LED_COUNT;       ///< Number of LEDs supported by PWM chip

    /// Struct with the layout of the PWM chip MODE1 register.
    typedef struct {
        UCHAR   ALLCALL : 1;    ///< 0: don't respond to LED all call address, 1: respond
        UCHAR   SUB3    : 1;    ///< 0: don't respond to sub-address 3, 1: respond
        UCHAR   SUB2    : 1;    ///< 0: don't respond to sub-address 2, 1: respond
        UCHAR   SUB1    : 1;    ///< 0: don't respond to sub-address 1, 1: respond
        UCHAR   SLEEP   : 1;    ///< 0: normal operation, 1: sleep (oscillator off)
        UCHAR   AI      : 1;    ///< 0: auto-increment address disabled, 1: enabled
        UCHAR   EXTCLK  : 1;    ///< 0: use internal 25mhz clock, 1: use external clock
        UCHAR   RESTART : 1;    ///< 0: restart disabled, 1: restart enabled
    } MODE1, *PMODE1;

    /// Struct with the layout of the PWM chip MODE2 register.
    typedef struct {
        UCHAR   OUTNE   : 2;    ///< Effect of _OE_ high (NA here, we wire _OE_ low)
        UCHAR   OUTDRV  : 1;    ///< 0: outputs pull low, float high, 1: drive high & low
        UCHAR   OCH     : 1;    ///< 0: outputs change on I2C STOP, 1: change on I2C ACK
        UCHAR   INVRT   : 1;    ///< 0: outputs as written, 1: invert outputs
        UCHAR   _rsv    : 3;    ///< Reserved
    } MODE2, *PMODE2;

    /// Constructor.
    PCA9685Device()
    {
    }

    /// Copy constructor.
    PCA9685Device(PCA9685Device&)
    {
    }

    /// Destructor.
    virtual ~PCA9685Device()
    {
    }

    // If/when multiple PCA9685 chips are supported, the data unique to the chip
    //    should be indexed by I2C address.

    /// Set to TRUE when the chip is known to have been initialized.
    static BOOL m_chipIsInitialized;

    /// The current PWM pulse rate pre-scale value for all channels.
    static UCHAR m_freqPreScale;

    /// Method to take any necessary actions to initialize the PWM chip.
    static HRESULT _InitializeChip(ULONG i2cAdr);
};

#endif  // _PCA9685_SUPPORT_H_