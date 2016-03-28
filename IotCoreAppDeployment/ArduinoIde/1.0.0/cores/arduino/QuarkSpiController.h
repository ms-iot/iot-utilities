// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _QUARK_SPI_CONTROLLER_H_
#define _QUARK_SPI_CONTROLLER_H_

#include <Windows.h>
#include "SpiController.h"
#include "DmapSupport.h"
#include "BoardPins.h"

/// Quark SPI Controller Class for use with Galileo Gen1 and Gen2.
class QuarkSpiControllerClass : public SpiControllerClass
{
public:
    /// Constructor.
    QuarkSpiControllerClass();

    /// Destructor.
    virtual ~QuarkSpiControllerClass()
    {
        this->end();
    }

    /// Initialize the pin assignments for this SPI controller.
    HRESULT configurePins(ULONG misoPin, ULONG mosiPin, ULONG sckPin) override;

    /// Initialize the specified SPI bus, using the default mode and clock rate.
    /**
    \param[in] busNumber The number of the SPI bus to open (0 or 1)
    \return HRESULT success or error code.
    */
    HRESULT begin(ULONG busNumber) override
    {
        return begin(busNumber, DEFAULT_SPI_MODE, DEFAULT_SPI_CLOCK_KHZ, DEFAULT_SPI_BITS);
    }

    /// Initialize the specified SPI bus for use.
    HRESULT begin(ULONG busNumber, ULONG mode, ULONG clockKhz, ULONG dataBits) override;

    /// Finish using an SPI controller.
    void end() override;

    /// Set the SPI clock rate to one of the values we support on this SPI controller.
    HRESULT setClock(ULONG clockKhz) override;

    /// Set the SPI mode (clock polarity and phase).
    HRESULT setMode(ULONG mode) override;

    /// Set the number of bits in an SPI transfer.
    HRESULT setDataWidth(ULONG bits) override
    {
        if ((bits < m_minTransferBits) || (bits > m_maxTransferBits))
        {
            return DMAP_E_SPI_DATA_WIDTH_SPECIFIED_IS_INVALID;
        }
        m_dataBits = bits;
        return S_OK;
    }

    /// Perform a transfer on the SPI bus.
    /**
    \param[in] dataOut The data to send on the SPI bus
    \param[out] datIn The data received on the SPI bus
    \return HRESULT success or error code.
    */
    inline HRESULT _transfer(ULONG dataOut, ULONG & dataIn, ULONG bits) override;
    
    /// Transfer a buffer of data on the SPI bus.
    inline HRESULT transferBuffer(PBYTE dataOut, PBYTE dataIn, size_t bufferBytes) override
    {
        return DMAP_E_SPI_BUFFER_TRANSFER_NOT_IMPLEMENTED;
    }

private:

    #pragma warning(push)
    #pragma warning(disable : 4201) // Ignore nameless struct/union warnings

    /// SPI Control Register 0
    typedef union {
        struct {
            ULONG DSS : 5;      ///< Data Size Select
            ULONG FRF : 2;      ///< Frame Format (Must be 0x00, other values reserved)
            ULONG SSE : 1;      ///< Synchronous Serial Port Enable
            ULONG SCR : 8;      ///< Serial Clock Rate
            ULONG _rsv : 16;    // Reserved
        };
        ULONG ALL_BITS;
    } _SSCR0;

    /// SPI Control Register 1
    typedef union {
        struct {
            ULONG RIE : 1;      ///< Receive FIFO Interrupt Enable
            ULONG TIE : 1;      ///< Transmit FIFO Interrupt Enable
            ULONG LBM : 1;      ///< Loop Back Mode
            ULONG SPO : 1;      ///< Serial Clock Polarity
            ULONG SPH : 1;      ///< Serial Clock Phase
            ULONG _rsv1 : 1;    // Reserved
            ULONG TFT : 5;      ///< Transmit FIFO Interrupt Threshold
            ULONG RFT : 5;      ///< Receive FIFO Interrupt Threshold
            ULONG EFWR : 1;     ///< Enable FIFO Write/Read Function
            ULONG STRF : 1;     ///< Select FIFO for Enable FIFO Write/Read
            ULONG _rsv2 : 14;   // Reserved
        };
        ULONG ALL_BITS;
    } _SSCR1;

    /// SPI Status Register
    typedef union {
        struct {
            ULONG ALT_FRM : 2;  ///< Alternative Frame (not supported)
            ULONG TNF : 1;      ///< Transmit FIFO Not Full Flag
            ULONG RNE : 1;      ///< Receive FIFO Not Empty Flag
            ULONG BSY : 1;      ///< SPI Busy Flag
            ULONG TFS : 1;      ///< Transmit FIFO Service Request Flag
            ULONG RFS : 1;      ///< Receive FIFO Service Request Flag
            ULONG ROR : 1;      ///< Receiver Overrun Status
            ULONG TFL : 5;      ///< Transmit FIFO Level
            ULONG RFL : 5;      ///< Receive FIFO Level
            ULONG _rsv : 14;    // Reserved
        };
        ULONG ALL_BITS;
    } _SSSR;

    /// SPI Data Register
    typedef union {
        struct {
            ULONG DATA8 : 8;    ///< Space for 8-bit data
            ULONG _unused : 24; // Unused bits with 8-bit data
        };
        ULONG ALL_BITS;
    } _SSDR;

    /// DDS Clock Rate Register
    typedef union {
        struct {
            ULONG DDS_CLK_RATE : 24;    ///< Clock rate generator multiplier value
            ULONG _rsv : 8;
        };
        ULONG ALL_BITS;
    } _DDS_RATE;

    #pragma warning( pop )

    /// Layout of the Quark SPI Controller registers in memory.
    typedef struct _SPI_CONTROLLER {
        volatile _SSCR0     SSCR0;          ///< 0x00 - SPI Control Register 0
        volatile _SSCR1     SSCR1;          ///< 0x04 - SPI Control Register 1
        volatile _SSSR      SSSR;           ///< 0x08 - SPI Status Register
        ULONG               _reserved1;     // 0x0C - 0x0F
        volatile _SSDR      SSDR;           ///< 0x10 - SPI Data Register
        ULONG               _reserved2[5];  // 0x14 - 0x27
        volatile _DDS_RATE  DDS_RATE;       ///< 0x28 - DDS Clock Rate Register
    } SPI_CONTROLLER, *PSPI_CONTROLLER;

    /// Struct used to specify an SPI bus speed.
    typedef struct _SPI_BUS_SPEED {
        ULONG dds_clk_rate;                 ///< Value for DDS_CLK_RATE register
        ULONG scr;                          ///< Value for SSCR0.SCR bit field
    } SPI_BUS_SPEED, *PSPI_BUS_SPEED;

    // Spi bus speed values.
    SPI_BUS_SPEED spiSpeed20mhz;            ///< Parameters for 20 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed12p5mhz;          ///< Parameters for 12.5 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed8mhz;             ///< Parameters for 8 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed4mhz;             ///< Parameters for 4 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed2mhz;             ///< Parameters for 2 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed1mhz;             ///< Parameters for 1 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed500khz;           ///< Parameters for 500 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed250khz;           ///< Parameters for 250 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed125khz;           ///< Parameters for 125 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed50khz;            ///< Parameters for 50 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed31k25hz;          ///< Parameters for 31.25 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed25khz;            ///< Parameters for 25 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed10khz;            ///< Parameters for 10 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed5khz;             ///< Parameters for 5 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed1khz;             ///< Parameters for 1 khz SPI bit clock

    /// Device handle used to map SPI controller registers into user-mode address space.
    HANDLE m_hController;

    /// Pointer SPI controller registers mapped into this process' address space.
    PSPI_CONTROLLER m_registers;

    /// The minimum width of a transfer on this controller.
    const UINT m_minTransferBits = 4;

    /// The maximum width of a transfer on this controller.
    const UINT m_maxTransferBits = 32;
};

/**
Transfer a number of bits on the SPI bus.
\param[in] dataOut Data to send on the SPI bus
\param[out] datIn The data reaceived on the SPI bus
\param[in] bits The number of bits to transfer in each direction on the bus.  This must agree with
the data width set previously.
\return HRESULT success or error code.
*/
inline HRESULT QuarkSpiControllerClass::_transfer(ULONG dataOut, ULONG & dataIn, ULONG bits)
{
    HRESULT hr = S_OK;
    ULONG txData;
    ULONG rxData;

    if (m_registers == nullptr)
    {
        hr = DMAP_E_DMAP_INTERNAL_ERROR;
    }

    if (SUCCEEDED(hr))
    {
        txData = dataOut;
        txData = txData & (0xFFFFFFFF >> (32 - bits));

        // Make sure the SPI bus is enabled.
        m_registers->SSCR0.SSE = 1;

        // Wait for an empty space in the FIFO.
        while (m_registers->SSSR.TNF == 0);

        // Send the data.
        m_registers->SSDR.ALL_BITS = txData;

        // Wait for data to be received.
        while (m_registers->SSSR.RNE == 0);

        // Get the received data.
        rxData = m_registers->SSDR.ALL_BITS;

        dataIn = rxData & (0xFFFFFFFF >> (32 - bits));
    }

    return hr;
}

#endif  // _QUARK_SPI_CONTROLLER_H_