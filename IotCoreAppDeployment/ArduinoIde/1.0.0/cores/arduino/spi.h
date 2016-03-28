// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _SPI_H_
#define _SPI_H_

#include <Windows.h>

#include "ArduinoCommon.h"
#include "ArduinoError.h"
#include "SpiController.h"
#include "QuarkSpiController.h"
#include "BtSpiController.h"
#include "BcmSpiController.h"
#include "BoardPins.h"

// SPI clock values in KHz.
#define SPI_CLOCK_DIV2 8000
#define SPI_CLOCK_DIV4 4000
#define SPI_CLOCK_DIV8 2000
#define SPI_CLOCK_DIV16 1000
#define SPI_CLOCK_DIV32 500
#define SPI_CLOCK_DIV64 250
#define SPI_CLOCK_DIV128 125

// SPI mode values
#define SPI_MODE0 0
#define SPI_MODE1 1
#define SPI_MODE2 2
#define SPI_MODE3 3

// Bit order values.
#define LSBFIRST        0x00
#define MSBFIRST        0x01

class SPIClass
{
public:
    /// Constructor.
    SPIClass()
    {
        m_controller = nullptr;
        m_bitOrder = MSBFIRST;             // Default bit order is MSB First
        m_clockKHz = 4000;                 // Default clock rate is 4 MHz
        m_mode = SPI_MODE0;                // Default to Mode 0
        m_dataWidth = DEFAULT_SPI_BITS;    // Default to one byte per SPI transfer
    }

    /// Destructor.
    virtual ~SPIClass()
    {
        this->end();
    }

    /// Initialize the externally accessible SPI bus for use.
    /**
    \return None.
    */
    void begin()
    {
        HRESULT hr;
        BoardPinsClass::BOARD_TYPE board;

        hr = g_pins.getBoardType(board);

        if (FAILED(hr))
        {
            ThrowError(hr, "An error occurred determining board type: %08x", hr);
        }

        // Create and initialize the SPI Controller object if we don't already have one.
        if (m_controller == nullptr)
        {
            if (board == BoardPinsClass::BOARD_TYPE::MBM_BARE)
            {
                m_controller = new BtSpiControllerClass;
                hr = m_controller->configurePins(MBM_PIN_MISO, MBM_PIN_MOSI, MBM_PIN_SCK);
            }
            else if (board == BoardPinsClass::BOARD_TYPE::PI2_BARE)
            {
                m_controller = new BcmSpiControllerClass;
                hr = m_controller->configurePins(PI2_PIN_SPI0_MISO, PI2_PIN_SPI0_MOSI, PI2_PIN_SPI0_SCK);
            }
            else
            {
                m_controller = new QuarkSpiControllerClass;
                hr = m_controller->configurePins(ARDUINO_PIN_MISO, ARDUINO_PIN_MOSI, ARDUINO_PIN_SCK);
            }

            if (FAILED(hr))
            {
                ThrowError(hr, "An error occurred configuring pins for SPI use: %08x", hr);
            }
        }

        // Set the desired SPI bit shifting order.
        if (m_bitOrder == MSBFIRST)
        {
            m_controller->setMsbFirstBitOrder();
        }
        else
        {
            m_controller->setLsbFirstBitOrder();
        }

        // Map the SPI1 controller registers into memory.
        hr = m_controller->begin(EXTERNAL_SPI_BUS, m_mode, m_clockKHz, m_dataWidth);

        if (FAILED(hr))
        {
            ThrowError(hr, "An error occurred initializing the SPI controller: %08x", hr);
        }
    }

    /// Free up the external SPI bus so its pins can be used for other functions.
    /**
    \return None.
    */
    void end()
    {
        HRESULT hr;

        if (m_controller != nullptr)
        {
            // Set all SPI pins as digital I/O.
            hr = m_controller->revertPinsToGpio();

            if (FAILED(hr))
            {
                ThrowError(hr, "An error occurred reverting SPI pins to GPIO: %08x", hr);
            }

            // Get rid of the underlying SPI Controller object.  This closes the handle
            // we have open to the SPI controller.
            delete m_controller;
            m_controller = nullptr;
        }
    }

    /// Set the SPI bit shift order.
    /**
    \param[in] bitOrder The order to shift bits on-to/off-of the SPI bus. (MSBFIRST or LSBFIRST)
    \return None.
    \note The default shift order is MSBFIRST.  Usually this does not need to be changed.
    */
    void setBitOrder(int bitOrder)
    {
        if ((bitOrder != MSBFIRST) && (bitOrder != LSBFIRST))
        {
            ThrowError(E_INVALIDARG, "SPI bit order must be MSBFIRST or LSBFIRST.");
        }
        m_bitOrder = bitOrder;

        if (m_controller != nullptr)
        {
            if (bitOrder == MSBFIRST)
            {
                m_controller->setMsbFirstBitOrder();
            }
            else
            {
                m_controller->setLsbFirstBitOrder();
            }
        }
    }

 
    /// Set the SPI clock speed.
    /**
    \param[in] clockKhz The SPI bit clock rate in Khz
    \return None.
    \note For Arduino UNO compatibiltiy, this method takes one of the following defined values:
      SPI_CLOCK_DIV2 - Sets 8 MHz clock
      SPI_CLOCK_DIV4 - Sets 4 MHz clock
      SPI_CLOCK_DIV8 - Sets 2 MHz clock
      SPI_CLOCK_DIV16 - Sets 1 MHz clock
      SPI_CLOCK_DIV32 - Sets 500 KHz clock
      SPI_CLOCK_DIV64 - Sets 250 KHz clock
      SPI_CLOCK_DIV128 - Sets 125 KHz clock
    
     Primarily for testing, this method also accepts the following values:
      50, 25, 10, 5, 1 - Sets clock speed in KHz (50 KHz to 1 KHz).
    
     The actual value passed to this routine is the desired speed in KHz, with
     an acceptable range of 1 or higher.  Underlying software layers may set a
     clock value that is different than the one requested to map onto supported
     clock generator settigns, however the clock speed set will not be higher
     than the speed requested.
    */
    void setClockDivider(ULONG clockKHz)
    {
        HRESULT hr;

        m_clockKHz = clockKHz;

        if (m_controller != nullptr)
        {
            hr = m_controller->setClock(clockKHz);

            if (FAILED(hr))
            {
                ThrowError(hr, "An error occurred setting the SPI clock rate: %d", hr);
            }
        }
    }

    /// Set the SPI mode (clock polarity and phase).
    /**
    \param[in] mode The mode to set (SPI_MODE0, SPI_MODE1, SPI_MODE2 or SPI_MODE3).
    \return None.
    \note Often the default (Mode 0) will work, so SPI mode does not need to be set.
    */
    void setDataMode(UINT mode)
    {
        HRESULT hr;

        if ((mode != SPI_MODE0) && (mode != SPI_MODE1) && (mode != SPI_MODE2) && (mode != SPI_MODE3))
        {
            ThrowError(E_INVALIDARG, "Spi Mode must be SPI_MODE0, SPI_MODE1, SPI_MODE2 or SPI_MODE3.");
        }
        m_mode = mode;

        if (m_controller != nullptr)
        {
            hr = m_controller->setMode(mode);

            if (FAILED(hr))
            {
                ThrowError(hr, "An error occurred setting the SPI mode: %d", hr);
            }
        }
    }

    /// Set the SPI data width.
    /**
    \param[in] bits The width of each transfer in bits (4-32).
    \note The data width must be set before begin() is called.  If an SPI controller is in use the following
    sequence can be used to change the data width: end(), setDataWidth(newWidth), begin().
    */
    void setDataWidth(UINT bits)
    {
        m_dataWidth = bits;
    }

    /// Transfer one byte in each direction on the SPI bus.
    /**
    \param[in] val The data byte to send out on the SPI bus.
    \return The data byte received from the SPI bus.
    \note Single byte transfers are the default on the SPI bus.
    */
    inline ULONG transfer(ULONG val)
    {
        HRESULT hr;
        ULONG dataReturn = 0;

        if (m_controller == nullptr)
        {
            ThrowError(HRESULT_FROM_WIN32(ERROR_INVALID_STATE), "Can't transfer on SPI bus until an SPI.begin() has been done.");
        }

        // Transfer the data.
        hr = m_controller->transfer8(val, dataReturn);

        if (FAILED(hr))
        {
            ThrowError(hr, "An error occurred atempting to transfer SPI data: %d", hr);
        }

        return dataReturn;
    }

    /// Transfer two bytes in each direction on the SPI bus.
    /**
    \param[in] val The least significant 16 bits are the two data bytes to be sent.
    \return The two data bytes read from the SPI bus.
    */
    inline ULONG transfer16(ULONG val)
    {
        HRESULT hr;
        ULONG dataReturn = 0;

        if (m_controller == nullptr)
        {
            ThrowError(HRESULT_FROM_WIN32(ERROR_INVALID_STATE), "Can't transfer on SPI bus until an SPI.begin() has been done.");
        }

        // Transfer the data.
        hr = m_controller->transfer16(val, dataReturn);

        if (FAILED(hr))
        {
            ThrowError(hr, "An error occurred atempting to transfer SPI data: %d", hr);
        }

        return dataReturn;
    }

    /// Transfer three bytes in each direction on the SPI bus.
    /**
    \param[in] val The least significant 24 bits are the three data bytes to be sent.
    \return The three data bytes read from the SPI bus.
    */
    inline ULONG transfer24(ULONG val)
    {
        HRESULT hr;
        ULONG dataReturn = 0;

        if (m_controller == nullptr)
        {
            ThrowError(HRESULT_FROM_WIN32(ERROR_INVALID_STATE), "Can't transfer on SPI bus until an SPI.begin() has been done.");
        }

        // Transfer the data.
        hr = m_controller->transfer24(val, dataReturn);

        if (FAILED(hr))
        {
            ThrowError(hr, "An error occurred atempting to transfer SPI data: %d", hr);
        }

        return dataReturn;
    }

    /// Transfer four bytes in each direction on the SPI bus.
    /**
    \param[in] val The least significant 24 bits are the three data bytes to be sent.
    \return The three data bytes read from the SPI bus.
    */
    inline ULONG transfer32(ULONG val)
    {
        HRESULT hr;
        ULONG dataReturn = 0;

        if (m_controller == nullptr)
        {
            ThrowError(HRESULT_FROM_WIN32(ERROR_INVALID_STATE), "Can't transfer on SPI bus until an SPI.begin() has been done.");
        }

        // Transfer the data.
        hr = m_controller->transfer32(val, dataReturn);

        if (FAILED(hr))
        {
            ThrowError(hr, "An error occurred atempting to transfer SPI data: %d", hr);
        }

        return dataReturn;
    }

private:

    /// Underlying SPI Controller object that really does the work.
    SpiControllerClass *m_controller;

    /// Bit order (LSBFIRST or MSBFIRST)
    ULONG m_bitOrder;

    /// SPI clock rate, based on the "Clock Divider" value.
    ULONG m_clockKHz;

    /// SPI data width.
    ULONG m_dataWidth;

    /// SPI mode to use.
    ULONG m_mode;
};

/// The global SPI bus object.
__declspec(selectany) SPIClass SPI;

#endif  // _SPI_H_
