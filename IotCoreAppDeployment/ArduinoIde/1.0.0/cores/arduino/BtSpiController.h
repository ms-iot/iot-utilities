// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _BT_SPI_CONTROLLER_H_
#define _BT_SPI_CONTROLLER_H_

#include <Windows.h>
#include "SpiController.h"
#include "DmapSupport.h"
#include "BoardPins.h"


/// BayTrail SPI Controller Class for use with MinnowBoard Max.
class BtSpiControllerClass : public SpiControllerClass
{
public:
    /// Constructor.
    BtSpiControllerClass();

    /// Destructor.
    virtual ~BtSpiControllerClass()
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
    HRESULT _transfer(ULONG dataOut, ULONG & dataIn, ULONG bits) override;

    /// Transfer a buffer of data on the SPI bus.
    inline HRESULT transferBuffer(PBYTE dataOut, PBYTE dataIn, size_t bufferBytes) override
    {
        return DMAP_E_SPI_BUFFER_TRANSFER_NOT_IMPLEMENTED;
    }

private:

#pragma warning(push)
#pragma warning(disable : 4201) // Ignore nameless struct/union warnings

    /// SSP Control Register 0
    typedef union {
        struct {
            ULONG DSS : 4;         ///< Data Size Select (data size is (EDSS*16)+DSS+1 )
            ULONG FRF : 2;         ///< Frame Format (00: SPI, 01: SSP, 10: Microwire, 11: PSP)
            ULONG ECS : 1;         ///< External Clock Select (0: on chip, 1: external)
            ULONG SSE : 1;         ///< Synchronous Serial Port Enable (0: disable, 1: enable)
            ULONG SCR : 12;        ///< Serial Clock Rate
            ULONG EDSS : 1;        ///< Extended Data Size (0: 4-16bits, 1: 17-32 bits)
            ULONG NCS : 1;         ///< Network Clock Select (0: honor ECS bit, 1: use network clock)
            ULONG RIM : 1;         ///< Receive FIFO Overrunn Int Mask (0: allow int, 1: mask int)
            ULONG TIM : 1;         ///< Transmit FIFO Underrun Int Mask (0: allow int, 1: mask int)
            ULONG FRDC : 3;        ///< Frame Rate Divider Control (network mode frame is FRDC+1 time slots)
            ULONG _rsv1 : 2;       // Reserved
            ULONG _rsv2 : 1;       // Reserved
            ULONG ACS : 1;         ///< Audio Clock Select (0: honor NCS & ECS, 1: use Audio Clock)
            ULONG MOD : 1;         ///< Mode (0: normlal SSP Mode, 1: Network Mode)
        };
        ULONG ALL_BITS;
    } _SSCR0;

    /// SSP Control Register 1
    typedef union {
        struct {
            ULONG RIE : 1;         ///< Receive FIFO Interrupt Enable (0: disable, 1: enable)
            ULONG TIE : 1;         ///< Transmit FIFO Interrupt Enable (0: disable, 1: enable)
            ULONG LBM : 1;         ///< Loop Back Mode (0: normal operation, 1: test mode)
            ULONG SPO : 1;         ///< Serial Clock Polarity (0: idle clock low, 1: idle high)
            ULONG SPH : 1;         ///< Serial Clock Phase
            ULONG MWDS : 1;        ///< Microwire Transmit Data Size (0: 8bit cmds, 1: 16bit cmds)
            ULONG TFT : 4;         ///< Transmit FIFO Interrupt Threshold (threshold is TFT+1)
            ULONG RFT : 4;         ///< Receive FIFO Interrupt Threshold (threshold is RFT+1)
            ULONG EFWR : 1;        ///< Enable FIFO Write/Read Function (0: normal operation, 1: test)
            ULONG STRF : 1;        ///< Select FIFO for Enable FIFO Write/Read (test use only)
            ULONG IFS : 1;         ///< Invert Frame Signal (0: don't invert, 1: invert from normal)
            ULONG _rsv : 1;        // Reserved
            ULONG PINTE : 1;       ///< Peripheral Trailing Byte Interupt Enable (0: disable, 1: enable)
            ULONG TINTE : 1;       ///< Receiver Time-out Interrupt Enable (0: disable, 1: enable)
            ULONG RSRE : 1;        ///< Receive Service Request Enable (0: DMA req disabled, 1: enabled)
            ULONG TSRE : 1;        ///< Transmit Servcie Request Enable (0: DMA req disabled, 1: enabled)
            ULONG TRAIL : 1;       ///< Trailing Byte (0: processor handles trailing bytes, 1: DMA handles)
            ULONG RWOT : 1;        ///< Receive WithOut Transmit (0: TX/RX mode, 1: RX without TX mode)
            ULONG SFRMDIR : 1;     ///< SSP Frame Direction (0: Master mode, 1: Slave mode)
            ULONG SCLKDIR : 1;     ///< SSP Serial Bit Rate Clock Direction (0: Master mode, 1: Slave mode)
            ULONG ECRB : 1;        ///< Enable Clock Request B (0: clock req from other SSP disabled, 1: enabled)
            ULONG ECRA : 1;        ///< Enable Clock Request A (0: disabled, 1: enabled)
            ULONG SCFR : 1;        ///< Slave Clock Free Running (0: clock input continuous, 1: only during transfers)
            ULONG EBCEI : 1;       ///< Enable Bit Count Error Interrupt (0: disabled, 1: enabled)
            ULONG TTE : 1;         ///< TXD Tristate Enable (0: TXD not tristated, 1: tristate when inactive)
            ULONG TTELP : 1;       ///< TXD Tristate Enable on Last Phase (0: tristate on TXD latch, 1: on next clock)
        };
        ULONG ALL_BITS;
    } _SSCR1;

    /// SSP Status Register
    typedef union {
        struct {
            ULONG _rsv3 : 2;       // Reserved
            ULONG TNF : 1;         ///< Transmit FIFO Not Full (0: TX FIFO full, 1: TX FIFO not full)
            ULONG RNE : 1;         ///< Receive FIFO Not Empty (0: rx FIFO empty, 1: RX FIFO not empty)
            ULONG BSY : 1;         ///< SPI Busy (0: SSP idle or disabled, 1: TX or RX in-progress)
            ULONG TFS : 1;         ///< Transmit FIFO Service Request (1: TX FIFO at/below threshold)
            ULONG RFS : 1;         ///< Receive FIFO Service Request (1: RX FIFO exceeds threshold)
            ULONG ROR : 1;         ///< Receiver Overrun Status (1: RX attempted with full RX FIFO)
            ULONG TFL : 4;         ///< Transmit FIFO Level (count of entries in TX FIFO, 0 = empty or full)
            ULONG RFL : 4;         ///< Receive FIFO Level (entries in RX FIFO =  RFL-1, 0xF = empty or full)
            ULONG _rsv2 : 2;       // Reserved
            ULONG PINT : 1;        ///< Peripheral Trailing Byte Interrupt (0: no int pending, 1: int pending)
            ULONG TINT : 1;        ///< Receiver Time-out Interrupt (0: no int pending, 1: int pending)
            ULONG EOC : 1;         ///< End of Chain (0: DMA hasn't signaled end of chain, 1: has signaled)
            ULONG TUR : 1;         ///< Transmit FIFO Under Run (0: no underrun, 1: pop empty FIFO - interrupt)
            ULONG CSS : 1;         ///< Clock Synchronization Status (0: SSP ready, 1: clock synchronizing slave)
            ULONG BCE : 1;         ///< Bit Count Error (0: ok, 1: SSPFRM signal asserted with bit count != 0)
            ULONG _rsv1 : 8;       // Reserved
        };
        ULONG ALL_BITS;
    } _SSSR;

    /// SSP Interrupt Test Register
    typedef union {
        struct {
            ULONG _rsv2 : 5;       // Reserved
            ULONG TTFS : 1;        ///< Test Transmit FIFO Service Request
            ULONG TRFS : 1;        ///< Test Receive FIFO Service Request
            ULONG TROR : 1;        ///< Test Receive FIFO Overrun
            ULONG _rsv1 : 24;      // Reserved
        };
        ULONG ALL_BITS;
    } _SSITR;

    /// SSP Data Register
    typedef struct {
        ULONG ALL_BITS;            ///< Data (right justified data of specified width)
    } _SSDR;

    /// SSP Time Out register
    typedef union {
        struct {
            ULONG TIMEOUT : 24;    ///< Timeout interval in Peripheral Clock ticks
            ULONG _rsv : 8;        // Reserved
        };
        ULONG ALL_BITS;
    } _SSTO;

    /// SSP Programmable Serial Protocol register
    typedef union {
        struct {
            ULONG SCMODE : 2;      ///< Serial Bit-rate Clock Mode (0-3)
            ULONG SFRMP : 1;       ///< Serial Frame Polarity
            ULONG ETDS : 1;        ///< End of Transfer Data State
            ULONG STRTDLY : 3;     ///< Start Delay
            ULONG DMYSTRT : 2;     ///< Dummy Start
            ULONG SFRMDLY : 7;     ///< Serial Frame Delay
            ULONG SFRMWDTH : 6;    ///< Serial Frame Width
            ULONG _rsv2 : 1;       // Reserved
            ULONG DMYSTOP : 2;     ///< Dummy Stop
            ULONG FSRT : 1;        ///< Frame Sync Relative Timing Bit
            ULONG _rsv1 : 6;       // Reserved
        };
        ULONG ALL_BITS;
    } _SSPSP;

    /// SSP TX Time Slot Active register
    typedef union {
        struct {
            ULONG TTSA : 8;        ///< TX Time Slot Active
            ULONG _rsv : 24;       // Reserved
        };
        ULONG ALL_BITS;
    } _SSTSA;

    /// SSP RX Time Slot Acive register
    typedef union {
        struct {
            ULONG RTSA : 8;        ///< RX Time Slot Active
            ULONG _rsv : 24;       // Reserved
        };
        ULONG ALL_BITS;
    } _SSRSA;

    /// SSP Time Slot Status register
    typedef union {
        struct {
            ULONG TSS : 3;        ///< Time Slot Status
            ULONG _rsv : 28;      // Reserved
            ULONG NMBSY : 1;      ///< Network Mode Busy
        };
        ULONG ALL_BITS;
    } _SSTSS;

    /// SSP Audio Clock Divider register
    typedef union {
        struct {
            ULONG ACDS : 3;        ///< Audio Clock Divider Select
            ULONG SCDB : 1;        ///< SYSCLK Divider Bypass
            ULONG ACPS : 3;        ///< Audio Clock PLL Select (0-5)
            ULONG _rsv : 25;       // Reserved
        };
        ULONG ALL_BITS;
    } _SSACD;

    /// I2S Transmit FIFO register
    typedef union {
        struct {
            ULONG HWMTF : 10;      ///< High Water Mark Transmit FIFO
            ULONG LWMTF : 10;      ///< Low Water Mark Transmit FIFO
            ULONG ITFL : 11;       ///< I2S Transmit FIFO Level
            ULONG _rsv : 1;        // Reserved
        };
        ULONG ALL_BITS;
    } _ITF;

    /// SPI Tranmit FIFO register
    typedef union {
        struct {
            ULONG HWMTF : 8;       ///< High Water Mark Transmit FIFO
            ULONG LWMTF : 8;       ///< Low Water Mark Transmit FIFO
            ULONG SITFL : 9;       ///< SPI Transmit FIFO Level
            ULONG _rsv : 7;        // Reserved
        };
        ULONG ALL_BITS;
    } _SITF;

    /// SPI Receive FIFO register
    typedef union {
        struct {
            ULONG WMRF : 8;        ///< Water Mark Receive FIFO
            ULONG SIRFL : 9;       ///< SPI Receive FIFO Level
            ULONG _rsv : 15;       // Reserved
        };
        ULONG ALL_BITS;
    } _SIRF;

#pragma warning( pop )

    /// Layout of the BayTrail SPI Controller registers in memory.
    typedef struct _SPI_CONTROLLER {
        volatile _SSCR0     SSCR0;             ///< 0x00 - SSP Control Register 0
        volatile _SSCR1     SSCR1;             ///< 0x04 - SSP Control Register 1
        volatile _SSSR      SSSR;              ///< 0x08 - SSP Status Register
        volatile _SSITR     SSITR;             ///< 0x0C - SSP Interrupt Test Register
        volatile _SSDR      SSDR;              ///< 0x10 - SSP Data Register
        ULONG               _reserved[5];      ///< 0x14 - 0x27
        volatile _SSTO      SSTO;              ///< 0x28 - SSP Time Out
        volatile _SSPSP     SSPSP;             ///< 0x2C - SSP Programmable Serial Protocol
        volatile _SSTSA     SSTSA;             ///< 0x30 - SSP TX Time Slot Active
        volatile _SSRSA     SSRSA;             ///< 0x34 - SSP RX Time Slot Active
        volatile _SSTSS     SSTSS;             ///< 0x38 - SSP Time Slot Status
        volatile _SSACD     SSACD;             ///< 0x3C - SSP Audio Clock Divider
        volatile _ITF       ITF;               ///< 0x40 - I2S Transmit FIFO
        volatile _SITF      SITF;              ///< 0x44 - SPI Transmit FIFO
        volatile _SIRF      SIRF;              ///< 0x48 - SPI Receive FIFO
    } SPI_CONTROLLER, *PSPI_CONTROLLER;

#pragma warning(push)
#pragma warning(disable : 4201) // Ignore nameless struct/union warnings

    /// Private Clock Params register
    typedef union {
        struct {
            ULONG CLK_EN : 1;          ///< Enable of the M over N divider
            ULONG M_VAL : 15;          ///< M value for the M over N divider
            ULONG N_VAL : 15;          ///< N value for the M over N divider
            ULONG CLK_UPDATE : 1;      ///< Update clock after setting new M and N values
        };
        ULONG ALL_BITS;
    } _PRV_CLOCK_PARAMS;

    /// Software Reset register
    typedef union {
        struct {
            ULONG RESET_APB : 1;        ///< Reset the APB domain
            ULONG RESET_FUNC : 1;       ///< Reset the function clock domain
            ULONG _rsv : 30;            // Reserved
        };
        ULONG ALL_BITS;
    } _RESETS;

    /// General Purpose Register
    typedef union {
        struct {
            ULONG POWER_ENABLE : 1;                            ///< Not applicable
            ULONG SLEEP_ENABLE : 1;                            ///< Not applicable
            ULONG RESET_E : 1;                                 ///< Not applicable
            ULONG GPO1 : 1;                                    ///< Not applicable
            ULONG GPO2 : 1;                                    ///< Not applicable
            ULONG GPO3 : 1;                                    ///< Not applicable
            ULONG _rsv1 : 18;                                  // Reserved
            ULONG SPI1_DMA_RXTO_HOLDOFF_DISABLE : 1;           ///< Disable DMA hold off
            ULONG SPI_TERMINATE_TX_ON_RX_FULL_DISABLE : 1;     ///< Disable terminate TX when RX full
            ULONG _rsv0 : 6;                                   // Reserved
        };
        ULONG ALL_BITS;
    } _GENERAL;

    /// SSP Register
    typedef union {
        struct {
            ULONG DISABLE_SSP_DMA_FINISH : 1;      ///< Disable SSP DMA finish
            ULONG ILB_CKBIT : 1;                   ///< Legacy chicken bit bug fix
            ULONG BRG_PIO_R : 1;                   ///< Not applicable
            ULONG GPIO_SSPSCLKEN : 1;              ///< Not applicable
            ULONG _rsv : 28;                       // Reserved
        };
        ULONG ALL_BITS;
    } _SSP_REG;

    /// SPI CS Control register
    typedef union {
        struct {
            ULONG SPI_CS_MODE : 1;         ///< Selects HW mode or SW mode for SPI Chip Select
            ULONG SPI_CS_STATE : 1;        ///< Software override of CS line in SW SPI CS mode
            ULONG _rsv : 1;                // Reserved
        };
        ULONG ALL_BITS;
    } _SPI_CS_CTRL;

#pragma warning( pop )

    /// Offset from SPI Controller register base to the upper registers.
    const ULONG SPI_CONTROLLER_UPPER_OFFSET = 0x400;

    /// Layout of the BayTrail SPI Controller upper address registers in memory.
    typedef struct _SPI_CONTROLLER_UPPER {
        volatile _PRV_CLOCK_PARAMS    PRV_CLOCK_PARAMS;    ///< 0x400 - Private Clock Params
        volatile _RESETS            RESETS;                ///< 0x404 - Software Reset
        volatile _GENERAL           GENERAL;               ///< 0x408 - General Purpose Register
        volatile _SSP_REG           SSP_REG;               ///< 0x40C - Misc. SSP control bits
        ULONG                        _reserved[2];         ///< 0x410 - 0x17
        volatile _SPI_CS_CTRL       SPI_CS_CTRL;           ///< 0x418 - SPI CS Control
    } SPI_CONTROLLER_UPPER, *PSPI_CONTROLLER_UPPER;

    // Struct used to specify an SPI bus speed.
    typedef struct _SPI_BUS_SPEED {
        ULONG M_VALUE;                             // M value for M over N clock divider
        ULONG N_VALUE;                             // N value for M over N clock divider
        ULONG SCR;                                 // Serial clock rate divider (divider = SCR + 1).
    } SPI_BUS_SPEED, *PSPI_BUS_SPEED;

    // Spi bus speed values.
    SPI_BUS_SPEED spiSpeed15mhz;               ///< Parameters for 15 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed12p5mhz;             ///< Parameters for 12.5 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed8mhz;                ///< Parameters for 8 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed4mhz;                ///< Parameters for 4 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed2mhz;                ///< Parameters for 2 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed1mhz;                ///< Parameters for 1 mhz SPI bit clock
    SPI_BUS_SPEED spiSpeed500khz;              ///< Parameters for 500 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed250khz;              ///< Parameters for 250 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed125khz;              ///< Parameters for 125 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed50khz;               ///< Parameters for 50 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed31k25hz;             ///< Parameters for 31.25 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed25khz;               ///< Parameters for 25 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed10khz;               ///< Parameters for 10 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed5khz;                ///< Parameters for 5 khz SPI bit clock
    SPI_BUS_SPEED spiSpeed1khz;                ///< Parameters for 1 khz SPI bit clock


    /// Device handle used to map SPI controller registers into user-mode address space.
    HANDLE m_hController;

    /// Pointer to SPI controller registers mapped into this process' address space.
    PSPI_CONTROLLER m_registers;

    /// Pointer to upper SPI controller registers mapped into this process' address space.
    PSPI_CONTROLLER_UPPER m_registersUpper;

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
inline HRESULT BtSpiControllerClass::_transfer(ULONG dataOut, ULONG & dataIn, ULONG bits)
{
    HRESULT hr = S_OK;
    
    ULONG txData;
    ULONG rxData;
    _SSCR0 sscr0;


    if (m_registers == nullptr)
    {
        hr = DMAP_E_DMAP_INTERNAL_ERROR;
    }

    if (SUCCEEDED(hr))
    {
        txData = dataOut;
        txData = txData & (0xFFFFFFFF >> (32 - bits));

        // Make sure the SPI bus is enabled.
        sscr0.ALL_BITS = m_registers->SSCR0.ALL_BITS;
        sscr0.SSE = 1;
        m_registers->SSCR0.ALL_BITS = sscr0.ALL_BITS;

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

#endif  // _BT_SPI_CONTROLLER_H_