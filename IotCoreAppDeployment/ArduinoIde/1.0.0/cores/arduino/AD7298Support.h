// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _AD7298_SUPPORT_H_
#define _AD7298_SUPPORT_H_

#include <Windows.h>

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:

#include "SpiController.h"
#include "GpioController.h"

class AD7298Device
{
public:
    /// Constructor.
    AD7298Device()
    {
    }

    /// Destructor.
    virtual ~AD7298Device()
    {
    }

    /// Prepare to use this ADC.
    /**
    \return HRESULT success or error code.
    */
    inline HRESULT begin()
    {
        HRESULT hr = S_OK;
        
        // Prepare to use the controller for the ADC's SPI controller.
        hr = m_spi.begin(ADC_SPI_BUS, 2, 20000, 16);

        if (SUCCEEDED(hr))
        {
            // Make the fabric GPIO bit that drives the ADC chip select signal an output.
            hr = g_quarkFabricGpio.setPinDirection(m_csFabricBit, DIRECTION_OUT);
        }

        return hr;
    }

    /// Release the ADC.
    inline void end()
    {
        // Release the ADC SPI bus.
        m_spi.end();
    }

    /// Take a reading with the ADC used on the Gen1 board.
    /**
    \param[in] channel Number of channel on ADC to read.
    \param[out] value The value read from the ADC.
    \param[out] bits The size of the reading in "value" in bits.
    \return HRESULT success or error code.
    */
    inline HRESULT readValue(ULONG channel, ULONG & value, ULONG & bits)
    {
        HRESULT hr = S_OK;
        
        ULONG dataOut;
        ULONG dataIn;
        ULONG chanIn;
        USHORT chanMask;
        CMD_REG cmdReg;


        // Make sure the channel number is in range.
        if (channel >= ADC_CHANNELS)
        {
            hr = DMAP_E_ADC_DOES_NOT_HAVE_REQUESTED_CHANNEL;
        }

        //
        // Jog the ADC twice to bring it out of any unresponsive state (such as Partial
        // Power-Down) that the shutdown of a previous program may have left it in.
        //
        if (SUCCEEDED(hr))
        {
            // Build ADC command register contents with Partial Power-Down bit clear.
            cmdReg.ALL_BITS = 0;
            cmdReg.WRITE = 1;
            dataOut = (ULONG)cmdReg.ALL_BITS;

            hr = g_quarkFabricGpio.setPinState(m_csFabricBit, LOW);  // Make ADC chip select active
            
            if (SUCCEEDED(hr))
            {
                // Write the ADC command register.
                hr = m_spi.transfer16(dataOut, dataIn);
                
                g_quarkFabricGpio.setPinState(m_csFabricBit, HIGH);      // Make ADC chip select inactive
            }
        }

        if (SUCCEEDED(hr))
        {
            // Build ADC command register contents with Partial Power-Down bit clear.
            cmdReg.ALL_BITS = 0;
            cmdReg.WRITE = 1;
            dataOut = (ULONG)cmdReg.ALL_BITS;

            hr = g_quarkFabricGpio.setPinState(m_csFabricBit, LOW);  // Make ADC chip select active
            
            if (SUCCEEDED(hr))
            {
                // Write the ADC command register.
                hr = m_spi.transfer16(dataOut, dataIn);
                
                g_quarkFabricGpio.setPinState(m_csFabricBit, HIGH);      // Make ADC chip select inactive
            }
        }

        //
        // Tell the ADC which channel we want to read.
        //

        if (SUCCEEDED(hr))
        {
            // Build ADC command register contents with bit set for the channel we want to read.
            chanMask = 0x0080 >> channel;
            cmdReg.ALL_BITS = 0;
            cmdReg.CHAN = chanMask;
            cmdReg.WRITE = 1;
            dataOut = (ULONG)cmdReg.ALL_BITS;

            hr = g_quarkFabricGpio.setPinState(m_csFabricBit, LOW);  // Make ADC chip select active
            
            if (SUCCEEDED(hr))
            {
                // Send the channel information to the ADC.
                hr = m_spi.transfer16(dataOut, dataIn);
                
                g_quarkFabricGpio.setPinState(m_csFabricBit, HIGH);      // Make ADC chip select inactive
            }
        }

        //
        // Perform the ADC conversion for the specified channel.
        //

        if (SUCCEEDED(hr))
        {
            // Shift out 16 bits to perform the conversion.
            dataOut = 0;
            hr = g_quarkFabricGpio.setPinState(m_csFabricBit, LOW);  // Make ADC chip select active
            
            if (SUCCEEDED(hr))
            {
                hr = m_spi.transfer16(dataOut, dataIn);
                
                g_quarkFabricGpio.setPinState(m_csFabricBit, HIGH);      // Make ADC chip select inactive
            }
        }

        //
        // Get the conversion result from the ADC.
        //

        if (SUCCEEDED(hr))
        {
            // Build ADC command register contents with Partial Power-Down bit set.
            cmdReg.ALL_BITS = 0;
            cmdReg.WRITE = 1;
            cmdReg.PPD = 1;
            dataOut = (ULONG)cmdReg.ALL_BITS;

            hr = g_quarkFabricGpio.setPinState(m_csFabricBit, LOW);  // Make ADC chip select active
            
            if (SUCCEEDED(hr))
            {
                // Write the ADC command register and shift out the conversion result.
                hr = m_spi.transfer16(dataOut, dataIn);
                
                g_quarkFabricGpio.setPinState(m_csFabricBit, HIGH);      // Make ADC chip select inactive
            }
        }

        //
        // Verify we got data for the right channel and pass it back to the caller.
        //

        if (SUCCEEDED(hr))
        {
            chanIn = (dataIn >> ADC_BITS) & ((1 << ADC_CHAN_BITS) - 1);

            if (chanIn != channel)
            {
                hr = DMAP_E_ADC_DATA_FROM_WRONG_CHANNEL;
            }
        }

        if (SUCCEEDED(hr))
        {
            value = dataIn & ((1 << ADC_BITS) - 1);
            bits = ADC_BITS;
        }
        
        return hr;
    }

private:

    /// Struct for ADC Control Register contents.
    typedef union {
        struct {
            USHORT PPD : 1;
            USHORT TsenceAvg : 1;
            USHORT EXT_REF : 1;
            USHORT dontcare : 2;
            USHORT Tsense : 1;
            USHORT CH7 : 1;
            USHORT CH6 : 1;
            USHORT CH5 : 1;
            USHORT CH4 : 1;
            USHORT CH3 : 1;
            USHORT CH2 : 1;
            USHORT CH1 : 1;
            USHORT CH0 : 1;
            USHORT REPEAT : 1;
            USHORT WRITE : 1;
        };
        struct {
            USHORT pad1 : 6;
            USHORT CHAN : 8;
            USHORT pad2 : 2;
        };
        USHORT ALL_BITS;
    } CMD_REG, *PCMD_REG;

    /// The number of channels on the ADC.
    const ULONG ADC_CHANNELS = 8;

    /// The number of bits in an ADC conversion.
    const ULONG ADC_BITS = 12;

    /// The number of channel number bits returned with an ADC conversion.
    const ULONG ADC_CHAN_BITS = 4;

    /// The SPI Controller object used to talk to the ADC.
    SpiControllerClass m_spi;

    /// The Fabric GPIO bit that controls the chip select signal.
    const ULONG m_csFabricBit = 0;
};
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#endif  // _AD7298_SUPPORT_H_