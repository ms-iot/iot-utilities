// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _BCM_SPI_CONTROLLER_H_
#define _BCM_SPI_CONTROLLER_H_

#include <Windows.h>
#include "SpiController.h"
#include "DmapSupport.h"
#include "BoardPins.h"


/// BCM2836 SPI Controller Class for use with Raspberry Pi 2.
class BcmSpiControllerClass : public SpiControllerClass
{
public:
    /// Constructor.
    BcmSpiControllerClass();

    /// Destructor.
    virtual ~BcmSpiControllerClass()
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

    /// SPI Master Control and Status Register
    typedef union {
        struct {
            ULONG CS : 2;           ///< Chip Select (00:CS0, 01:CS1, 10:CS2, 11:Reserved)
            ULONG CPHA : 1;         ///< Clock Phase (0:clock trans mid bit, 1:trans at beginning)
            ULONG CPOL : 1;         ///< Clock Polarity (0:clock low at rest, 1:clock high at rest)
            ULONG CLEAR : 2;        ///< Clear FIFO (00:NOP, x1:Clear TX FIFO, 1x:Clear RX FIFO)
            ULONG CSPOL : 1;        ///< Chip Select Polarity (0:CS active low, 1:CS active high)
            ULONG TA : 1;           ///< Transfer Active (0:no transfer, 1:transfer active)
            ULONG DMAEN : 1;        ///< DMA Enable (0:disable, 1:enable)
            ULONG INTD : 1;         ///< Int on Done (0:disable int on done, 1:enable)
            ULONG INTR : 1;         ///< Int on RX (0:disable RX FIFO ints, 1:enable)
            ULONG ADCS : 1;         ///< Auto Deassert Chip Select (0:disable, 1:enable auto CS)
            ULONG REN : 1;          ///< Read Enable - only used for bidirectional mode
            ULONG LEN : 1;          ///< LOSSI Enable (0:SPI master mode, 1:LoSSI master mode)
            ULONG LMONO : 1;        ///< Unused
            ULONG TE_EN : 1;        ///< Unused
            ULONG DONE : 1;         ///< Transfer Done (0:transfer in progress, 1:transfer complete)
            ULONG RXD : 1;          ///< RX FIFO has data (0:RX FIFO empty, 1: RX FIFO has data)
            ULONG TXD : 1;          ///< TX FIFO not full (0:TX FIFO full, 1: RX FIFO can accept data)
            ULONG RXR : 1;          ///< RX FIFO needs Reading (0:RX FIFO LT 3/4, 1:RX FIFO GE 3/4)
            ULONG RXF : 1;          ///< RX FIFO Full (0:RX FIFO not full, 1:RX FIFO full)
            ULONG CSPOL0 : 1;       ///< Chip Select 0 Polarity (0:CS0 active low, 1:CS0 active high)
            ULONG CSPOL1 : 1;       ///< Chip Select 1 Polarity (0:CS1 active low, 1:CS1 active high)
            ULONG CSPOL2 : 1;       ///< Chip Select 2 Polarity (0:CS2 active low, 1:CS2 active high)
            ULONG DMA_LEN : 1;      ///< Enable DMA mode in LOSSI mode
            ULONG LEN_LONG : 1;     ///< Enable Long data in LOSSI mode if DMA_LEN set (0:disable, 1:enable)
            ULONG _rsvd : 6;        // Reserved
        };
        ULONG ALL_BITS;
    } _CS;

    /// FIFO Register
    typedef union {
        struct {
            BYTE DATA_BYTE0;
            BYTE DATA_BYTE1;
            BYTE DATA_BYTE2;
            BYTE DATA_BYTE3;
        };
        ULONG ALL_BITS;                 ///< Data
    } _FIFO;

    /// SPI Master Clock Divider Register
    typedef union {
        struct {
            ULONG CDIV : 16;        ///< Clock Divider - power of 2 (SCLK = Core Clock / CDIV)
            ULONG _rsvd: 16;        // Reserved
        };
        ULONG ALL_BITS;
    } _CLK;

    /// SPI Master Data Length Register
    typedef union {
        struct {
            ULONG LEN : 16;         ///< Data Length (Only used for DMA mode?)
            ULONG _rsvd : 16;       // Reserved
        };
        ULONG ALL_BITS;
    } _DLEN;

        /// SPI LOSSI Mode Output Hold Delay Register
    typedef union {
        struct {
            ULONG TOH : 4;          ///< Output Hold Delay in APB clocks
            ULONG _rsvd : 28;       // Reserved
        };
        ULONG ALL_BITS;
    } _LTOH;

    /// SPI DMA DREQ Controls Register
    typedef union {
        struct {
            ULONG TDREQ : 8;        ///< DMA Write Request Threshold
            ULONG TPANIC : 8;       ///< DMA Write Panic Threshold
            ULONG RDREQ : 8;        ///< DMA Read Request Threshold
            ULONG RPANIC : 8;       ///< DMA Read Panic Threshold
        };
        ULONG ALL_BITS;
    } _DC;


#pragma warning( pop )

    /// Layout of the BCM2836 SPI Controller registers in memory.
    typedef struct _SPI_CONTROLLER {
        volatile _CS        CS;     ///< 0x00 - SPI Master Control and Status Register
        volatile _FIFO      FIFO;   ///< 0x04 - SPI Master TX and RX FIFOs
        volatile _CLK       CLK;    ///< 0x08 - SPI Master Clock Divider Register
        volatile _DLEN      DLEN;   ///< 0x0C - SPI Master Data Length Register
        volatile _LTOH      LTOH;   ///< 0x10 - SPI LOSSI Mode TOH Register
        volatile _DC        DC;     ///< 0x14 - SPI DMA DREQ Controls Register
    } SPI_CONTROLLER, *PSPI_CONTROLLER;

#pragma warning(push)
#pragma warning(disable : 4201) // Ignore nameless struct/union warnings

    // Spi bus speed values.
    ULONG spiSpeed10mhz;               ///< Parameters for 10 mhz SPI bit clock
    ULONG spiSpeed8mhz;                ///< Parameters for 8 mhz SPI bit clock
    ULONG spiSpeed4mhz;                ///< Parameters for 4 mhz SPI bit clock
    ULONG spiSpeed2mhz;                ///< Parameters for 2 mhz SPI bit clock
    ULONG spiSpeed1mhz;                ///< Parameters for 1 mhz SPI bit clock
    ULONG spiSpeed500khz;              ///< Parameters for 500 khz SPI bit clock
    ULONG spiSpeed250khz;              ///< Parameters for 250 khz SPI bit clock
    ULONG spiSpeed125khz;              ///< Parameters for 125 khz SPI bit clock
    ULONG spiSpeed50khz;               ///< Parameters for 50 khz SPI bit clock
    ULONG spiSpeed31k25hz;             ///< Parameters for 31.25 khz SPI bit clock
    ULONG spiSpeed25khz;               ///< Parameters for 25 khz SPI bit clock
    ULONG spiSpeed10khz;               ///< Parameters for 10 khz SPI bit clock
    ULONG spiSpeed5khz;                ///< Parameters for 5 khz SPI bit clock
    ULONG spiSpeed4khz;                ///< Parameters for 4 khz SPI bit clock

    /// Device handle used to map SPI controller registers into user-mode address space.
    HANDLE m_hController;

    /// Pointer to SPI controller registers mapped into this process' address space.
    PSPI_CONTROLLER m_registers;

    /// SPI clock phase.
    ULONG m_clockPhase;

    /// SPI clock polarity.
    ULONG m_clockPolarity;

    /// The minimum width of a transfer on this controller.
    const UINT m_minTransferBits = 8;

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
\note The BCM2836 only supports byte transfers in polled mode.  If an attempt is made to transfer 
a number of bits that is not divisible by eight, this method returns an error.
*/
inline HRESULT BcmSpiControllerClass::_transfer(ULONG dataOut, ULONG & dataIn, ULONG bits)
{
    HRESULT hr = S_OK;
    _CS cs;
    BYTE* oneByte;
    int bytesRemaining;


    if (m_registers == nullptr)
    {
        hr = DMAP_E_DMAP_INTERNAL_ERROR;
    }

    // Make sure the transfer is composed of 1-4 whole bytes.
    if ( SUCCEEDED(hr) && (((bits & 0x07) != 0) || (bits == 0) || (bits > 32)) )
    {
        hr = DMAP_E_SPI_DATA_WIDTH_SPECIFIED_IS_INVALID;
    }

    if (SUCCEEDED(hr))
    {
        bytesRemaining = bits / 8;
        oneByte = (BYTE*)&dataOut;
        dataIn = 0;

        while (bytesRemaining > 0)
        {
            // Wait for an available space in the TX FIFO.
            do { cs.ALL_BITS = m_registers->CS.ALL_BITS; } while (cs.TXD == 0);

            // Send a byte of the data.
            m_registers->FIFO.DATA_BYTE0 = oneByte[bytesRemaining - 1];

            // Wait for the RX FIFO to have data.
            do { cs.ALL_BITS = m_registers->CS.ALL_BITS; } while (cs.RXD == 0);

            // Read the received data.
            dataIn = (dataIn << 8) | (m_registers->FIFO.ALL_BITS & 0x000000FF);

            bytesRemaining--;
        }
    }

    return hr;
}

#endif  // _BCM_SPI_CONTROLLER_H_