// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _MCP3008_SUPPORT_H_
#define _MCP3008_SUPPORT_H_

#include <Windows.h>

#include "Spi.h"
#include "GpioController.h"

#define MBM_SPI_CS_PIN 5
#define PI2_SPI_CS_PIN 24

#define MCP3008_SPI_MODE SPI_MODE0
#define MCP3008_MAX_SPI_KHZ 1350
#define MCP3008_SPI_TRANSFER_BITS 24

class MCP3008Device
{
public:
    /// Constructor.
    MCP3008Device() :
        m_csPin(0)
    {
    }

    /// Destructor.
    virtual ~MCP3008Device()
    {
    }

    /// Prepare to use this ADC.
    /**
    \return HRESULT success or error code.
    */
    inline HRESULT begin()
    {
        HRESULT hr;
        BoardPinsClass::BOARD_TYPE board;

        hr = g_pins.getBoardType(board);

        if (FAILED(hr))
        {
            hr = DMAP_E_BOARD_TYPE_NOT_RECOGNIZED;
        }

        // Create and initialize the SPI Controller object if we don't already have one.
        if (SUCCEEDED(hr))
        {
            if (board == BoardPinsClass::BOARD_TYPE::MBM_BARE)
            {
                m_csPin = MBM_SPI_CS_PIN;
                m_spi = new BtSpiControllerClass;
                hr = m_spi->configurePins(MBM_PIN_MISO, MBM_PIN_MOSI, MBM_PIN_SCK);
            }
            else if (board == BoardPinsClass::BOARD_TYPE::PI2_BARE)
            {
                m_csPin = PI2_SPI_CS_PIN;
                m_spi = new BcmSpiControllerClass;
                hr = m_spi->configurePins(PI2_PIN_SPI0_MISO, PI2_PIN_SPI0_MOSI, PI2_PIN_SPI0_SCK);
            }
            else
            {
                hr = DMAP_E_BOARD_TYPE_NOT_RECOGNIZED;
            }

            if (SUCCEEDED(hr))
            {
                hr = g_pins.setPinMode(m_csPin, DIRECTION_OUT, FALSE);

                if (SUCCEEDED(hr))
                {
                    hr = g_pins.setPinState(m_csPin, HIGH);
                }

                if (SUCCEEDED(hr))
                {
                    hr = g_pins.verifyPinFunction(m_csPin, FUNC_DIO, BoardPinsClass::LOCK_FUNCTION);
                }
            }
        }

        if (SUCCEEDED(hr))
        {
            // Set the SPI bit shifting order to MSB First.
            m_spi->setMsbFirstBitOrder();

            // Map the SPI1 controller registers into memory.
            hr = m_spi->begin(EXTERNAL_SPI_BUS,
                              MCP3008_SPI_MODE,
                              MCP3008_MAX_SPI_KHZ,
                              MCP3008_SPI_TRANSFER_BITS);
        }
        
        return hr;
    }

    /// Release the ADC.
    inline void end()
    {
        // Release the ADC SPI bus.
        m_spi->end();

        // Revert the dedicated SPI pins to GPIO.
        m_spi->revertPinsToGpio();

        // Unlock the CS line so it can be used for non-GPIO function.
        g_pins.verifyPinFunction(m_csPin, FUNC_DIO, BoardPinsClass::UNLOCK_FUNCTION);
    }

    /// Take a reading with the ADC used on the Gen2 board.
    /**
    \param[in] channel Number of channel on ADC to read.
    \param[out] value The value read from the ADC.
    \param[out] bits The size of the reading in "value" in bits.
    \return HRESULT success or error code.
    */
    inline HRESULT readValue(ULONG channel, ULONG & value, ULONG & bits)
    {
        HRESULT hr = S_OK;
        
        ULONG dataOut = FIXED_CMD_BITS;
        ULONG dataIn = 0;

        // Make sure the channel number is in range.
        if (channel >= ADC_CHANNELS)
        {
            hr = DMAP_E_ADC_DOES_NOT_HAVE_REQUESTED_CHANNEL;
        }

        if (SUCCEEDED(hr))
        {
            // Prepare to send the channel number to the SPI controller.
            dataOut |= channel << CHAN_SHIFT;

            // Perform a conversion and get the result.
            g_pins.setPinState(m_csPin, LOW);

            hr = m_spi->transfer24(dataOut, dataIn);
                
            g_pins.setPinState(m_csPin, HIGH);
        }

        if (SUCCEEDED(hr))
        {
            // Extract the reading from the data sent back from the ADC.
            value = dataIn & ((1 << ADC_BITS) - 1);
            bits = ADC_BITS;
        }
        
        return hr;
    }

private:
    /// The number of channels on the ADC.
    const ULONG ADC_CHANNELS = 8;

    /// The number of bits in an ADC conversion.
    const ULONG ADC_BITS = 10;

    /// The shift amount to put the channel number into a 24-bit value.
    const ULONG CHAN_SHIFT = 12;

    /// A 24-bit mask with fixed bits of transfer to MCP3008 pre-configured.
    const ULONG FIXED_CMD_BITS = 0x018000;

    /// The pin number of the CS pin.
    ULONG m_csPin;

    /// The SPI Controller object used to talk to the ADC.
    SpiControllerClass* m_spi;

};

#endif  // _MCP3008_SUPPORT_H_