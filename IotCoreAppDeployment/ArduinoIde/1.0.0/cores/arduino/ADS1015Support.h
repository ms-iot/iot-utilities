// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _ADS1015_SUPPORT_H_
#define _ADS1015_SUPPORT_H_

#include <Windows.h>

#include "I2c.h"
#include "I2cTransaction.h"
#include "I2cController.h"

class ADS1015Device
{
public:
    /// Constructor.
    ADS1015Device()
    {
    }

    /// Destructor.
    virtual ~ADS1015Device()
    {
    }

    /// Prepare to use this ADC.
    /**
    \return HRESULT success or error code.
    */
    inline HRESULT begin()
    {
        HRESULT hr;

        // Prepare to use the I2C bus to talk to the ADC on the Ika Lure.
        
        hr = g_i2c.begin();

        return hr;
    }

    /// Release the ADC.
    inline void end()
    {
        // Release the I2C controller.
        g_i2c.end();
    }

    /// Take a reading with the ADC used on the Ika Lure board.
    /**
    \param[in] channel Number of channel on ADC to read.
    \param[out] value The value read from the ADC.
    \param[out] bits The size of the reading in "value" in bits.
    \return HRESULT success or error code.
    \note This routine is not multi-thread safe.  If two or more threads access the 
    ADC at the same time, each thread could be changing the other thread's configuration 
    (such as channel number).  A global mutex could be added to fix this.
    */
    inline HRESULT readValue(ULONG channel, ULONG & value, ULONG & bits)
    {
        HRESULT hr = S_OK;
        
        BOOL conversionDone = FALSE;
        CONFIG_REG_H configH;
        CONFIG_REG_L configL;
        I2cTransactionClass transaction;
        BYTE configRegAdr[1] = { 1 };
        BYTE configData[2] = { 0 };
        BYTE conversionRegAdr[1] = { 0 };
        BYTE conversionData[2] = { 0 };

        //
        // Build the Configuration Register contents.
        //

        configH.ALL_BITS = CONFIG_REG_INIT_H;
        configL.ALL_BITS = CONFIG_REG_INIT_L;
        switch (channel)
        {
        case 0:
            configH.MUX = ANI0;
            break;
        case 1:
            configH.MUX = ANI1;
            break;
        case 2:
            configH.MUX = ANI2;
            break;
        case 3:
            configH.MUX = ANI3;
            break;
        default:
            hr = DMAP_E_ADC_DOES_NOT_HAVE_REQUESTED_CHANNEL;
        }

        if (SUCCEEDED(hr))
        {
            configH.OS = 1;     // Signal to start a conversion
        }

        //
        // Send the configuration information to the ADC to start the conversion.
        //

        if (SUCCEEDED(hr))
        {
            hr = transaction.setAddress(ADC_I2C_ADR);
        }

        if (SUCCEEDED(hr))
        {
            // Send the address of the register we want to write.
            hr = transaction.queueWrite(configRegAdr, 1);
        }

        if (SUCCEEDED(hr))
        {
            configData[0] = configH.ALL_BITS;
            configData[1] = configL.ALL_BITS;

            hr = transaction.queueWrite(configData, 2);
        }
        
        if (SUCCEEDED(hr))
        {
            hr = transaction.execute(g_i2c.getController());
        }

        //
        // Wait for the conversion to complete.
        //

        if (SUCCEEDED(hr))
        {
            transaction.reset();
            // Send the address of the register we want to read.
            hr = transaction.queueWrite(configRegAdr, 1);
            
        }

        if (SUCCEEDED(hr))
        {
            hr = transaction.queueRead(configData, 2);
            
        }

        while (SUCCEEDED(hr) && !conversionDone)
        {
            hr = transaction.execute(g_i2c.getController());
            

            if (SUCCEEDED(hr))
            {
                configH.ALL_BITS = configData[0];
                if (configH.OS == 1)
                {
                    conversionDone = TRUE;
                }
            }
        }

        //
        // Read the conversion result.
        //

        if (SUCCEEDED(hr))
        {
            transaction.reset();
            // Send the address of the register we want to read.
            hr = transaction.queueWrite(conversionRegAdr, 1);
            
        }

        if (SUCCEEDED(hr))
        {
            hr = transaction.queueRead(conversionData, 2);
            
        }

        if (SUCCEEDED(hr))
        {
            hr = transaction.execute(g_i2c.getController());
            
        }
        
        if (SUCCEEDED(hr))
        {
            value = conversionData[0] << 8;
            value = value | conversionData[1];
            // Extract the reading from the data sent back from the ADC.
            value = value >> DATA_SHIFT;
            // This is a signed ADC, so make sure the result is not negative.
            if ((value & (1 << ADC_BITS)) != 0)
            {
                value = 0;
            }
            // Scale the ADC for its full-scale value not being 5.000 volts.
            value = ((value * FULL_SCALE) + 2500) / 5000;

            bits = ADC_BITS;
        }

        
        return hr;
    }

private:

    /// Struct for ADC Config Register (MSByte) contents.
    typedef union {
        struct {
            BYTE MODE : 1;
            BYTE PGA : 3;
            BYTE MUX : 3;
            BYTE OS : 1;
        };
        BYTE ALL_BITS;
    } CONFIG_REG_H, *PCONFIG_REG_H;

    /// Struct for ADC Config Register (LSBytes) contents;
    typedef union {
        struct {
            BYTE COMP_QUE : 2;
            BYTE COMP_LAT : 1;
            BYTE COMP_POL : 1;
            BYTE COMP_MODE : 1;
            BYTE DR : 3;
        };
        BYTE ALL_BITS;
    } CONFIG_REG_L, *PCONFIG_REG_L;

    /// The number of channels on the ADC.
    const ULONG ADC_CHANNELS = 4;

    /// The number of bits in an ADC conversion.
    const ULONG ADC_BITS = 11;

    /// The shift amount to right justify the data read from the ADC.
    const ULONG DATA_SHIFT = 4;

    /// The full-scale ADC value, compared to 5000.
    const ULONG FULL_SCALE = 6144;

    /// The I2C address of the Ika Lure ADC.
    const BYTE ADC_I2C_ADR = 0x48;

    /// The Configuration Register MSByte initialization values.
    const BYTE CONFIG_REG_INIT_H = 0x01;    // Single-shot mode, 6.144V full scale

    /// The Configuration Register LSByte initialization values.
    const BYTE CONFIG_REG_INIT_L = 0xE3;    // 3.3k Samples/sec, Disable comparator

    /// The mux value for single-ended input on AIN0.
    const BYTE ANI0 = 4;

    /// The mux value for single-ended input on AIN1.
    const BYTE ANI1 = 5;

    /// The mux value for single-ended input on AIN2.
    const BYTE ANI2 = 6;

    /// The mux value for single-ended input on AIN3.
    const BYTE ANI3 = 7;

};

#endif  // _ADS1015_SUPPORT_H_