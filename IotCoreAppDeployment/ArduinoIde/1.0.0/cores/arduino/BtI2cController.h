// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _BT_I2C_CONTROLLER_H_
#define _BT_I2C_CONTROLLER_H_

#include <Windows.h>
#include <functional>

#include "I2cController.h"
#include "BoardPins.h"


//
// Class that is used to interact with the BayTrail/Quark I2C Controller hardware.
//
class BtI2cControllerClass : public I2cControllerClass
{
public:
    BtI2cControllerClass() :
        m_registers(nullptr),
        m_controllerInitialized(FALSE)
    {
    }

    virtual ~BtI2cControllerClass()
    {
        this->end();
    }
    
    // Initialize the specified I2C bus for use.
    HRESULT begin(ULONG busNumber) override;

    // This method returns the external I2C bus pins to their default configurations.
    inline void end() override
    {
        revertPinsToGpio();

        if (m_hController != INVALID_HANDLE_VALUE)
        {
            // Unmap the SPI controller.
            DmapCloseController(m_hController);
        }
    }

    // This method configures the pins to be used for this I2C bus.
    HRESULT configurePins(ULONG sdaPin, ULONG sclPin) override;

    // Method to initialize the I2C Controller at the start of a transaction.
    HRESULT _initializeForTransaction(ULONG slaveAddress, BOOL useHighSpeed) override;

    // This method records that the controller has been initialized.
    inline void setInitialized()
    {
        m_controllerInitialized = TRUE;
    }

    // This method returns TRUE if the I2C Controller has been initialized, FALSE otherwise.
    inline BOOL isInitialized()
    {
        return m_controllerInitialized;
    }

    //
    // I2C Controller accessor methods.  These methods assume the I2C Controller
    // has already been mapped using mapIfNeeded().
    //

    inline BOOL txFifoFull() const override
    {
        return (m_registers->IC_STATUS.TFNF == 0);
    }

    inline BOOL txFifoEmpty() const override
    {
        return (m_registers->IC_STATUS.TFE == 1);
    }

    inline BOOL rxFifoNotEmtpy() const override
    {
        return (m_registers->IC_STATUS.RFNE == 1);
    }

    inline BOOL rxFifoEmpty() const override
    {
        return (m_registers->IC_STATUS.RFNE == 0);
    }

    HRESULT _performContiguousTransfers(I2cTransferClass* & pXfr) override;
    
    inline UCHAR readByte() override
    {
        return (m_registers->IC_DATA_CMD.DAT);
    }

    inline BOOL isActive() const override
    {
        return (m_registers->IC_STATUS.MST_ACTIVITY == 1);
    }

    /// Determine whether a TX Error has occurred or not.
    /**
    All I2C bus errors we are interested in are TX errors: failure to ACK an
    an address or write data.
    \return TRUE, an error occured.  FALSE, no error has occured.
    */
    inline BOOL errorOccurred() override
    {
        return (m_registers->IC_RAW_INTR_STAT.TX_ABRT == 1);
    }

    /// Determine if an I2C address was sent but not acknowledged by any slave.
    /**
    This method tests for the error that is expected to occur if an attempt
    is made talk to an I2C slave that does not exist.
    \return TRUE, an I2C address was not acknowdged.  FALSE, all addresses sent
    have been acknowledged by at least one slave.
    */
    inline BOOL addressWasNacked() override
    {
        return (m_registers->IC_TX_ABRT_SOURCE.ABRT_7B_ADDR_NOACK == 1);
    }

    /// Determine if I2C data was sent but not acknowledged by a slave.
    /**
    This method tests for the error that occurs if a slave has been found, 
    but then fails to acknowledge a data byte sent by the master.
    \return TRUE, I2C data was not acknowdged.  FALSE, all data sent has 
    been acknowledged by a slave.
    */
    inline BOOL dataWasNacked() override
    {
        return (m_registers->IC_TX_ABRT_SOURCE.ABRT_TXDATA_NOACK == 1);
    }

    /// Handle any errors that have occurred during an I2C transaction.
    /**
    Determine whether an error occurred, and if so (and it is the first error on this
    transaction) record the error information.
    \param[out] error The wiring error or success code for the transaction result.
    \return HRESULT success or error code.
    */
    inline HRESULT _handleErrors() override
    {
        HRESULT hr = S_OK;

        // If an error occurred during this transaction:
        if (errorOccurred())
        {
            // If error information has not yet been captured for this transaction:
            if (m_error == I2cTransactionClass::SUCCESS)
            {
                // Record the type of error that occured.
                if (addressWasNacked())
                {
                    m_error = I2cTransactionClass::ADR_NACK;
                }
                else if (dataWasNacked())
                {
                    m_error = I2cTransactionClass::DATA_NACK;
                }
                else
                {
                    m_error = I2cTransactionClass::OTHER;
                }
            }
            // Clear the error.
            clearErrors();

            hr = E_FAIL;
        }
        return hr;
    }

    /// Reset the controller after a bus error has occurred.
    /**
    The actul error condition is cleard by reading the IC_CLR_TX_ABRT register.
    */
    inline void clearErrors() override
    {
        ULONG dummy = m_registers->IC_CLR_TX_ABRT.ALL_BITS;
    }

private:

    //
    // Structures used to map and access the I2C Controller registers.
    // These must agree with the actual hardware!
    //

    #pragma warning(push)
    #pragma warning(disable : 4201) // Ignore nameless struct/union warnings

    // I2C Control Register.
    typedef union {
        struct {
            ULONG MASTER_MODE : 1;          // 0: master disabled, 1: master enabled
            ULONG SPEED : 2;                // 01: standard (100 kbit/s), 10: fast (400 kbit/s)
            ULONG _rsv1 : 1;                // Reserved
            ULONG IC_10BITADDR_MASTER : 1;  // 0: start transfers with 7-bit addressing, 1: use 10-bit
            ULONG IC_RESTART_EN : 1;        // 0: disable restars, 1: allow restarts
            ULONG _rsv2 : 26;               // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CON;

    // I2C Master Target Address Register.
    typedef union {
        struct {
            ULONG IC_TAR : 10;                 // Target slave address for an I2C transaction
            ULONG GC_OR_START : 1;             // Reserved - see Quark datasheet 9.5.1.27
            ULONG SPECIAL : 1;                 // Reserved - see Quark datasheet 9.5.1.27
            ULONG IC_10BITADDR_MASTER : 1;     // Clear for 7-bit addressing
            ULONG _rsv : 19;                   // Reserved
        };
        ULONG ALL_BITS;
    } _IC_TAR;

    // I2C Data Buffer and Command Register.
    typedef union {
        struct {
            ULONG DAT : 8;                  // Data to be transmitted or received on the I2C bus
            ULONG CMD : 1;                  // 0: transmit the data, 1: receive a byte of data
            ULONG STOP : 1;                 // 0: do not issue STOP, 1: issue STOP after byte transfer
            ULONG RESTART : 1;              // 0: RESTART only if direction change, 1: RESTART before xfr
            ULONG _rsv : 21;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_DATA_CMD;

    // I2C Standard Speed SCL High Count Register.
    typedef union {
        struct {
            ULONG IC_SS_SCL_HCNT : 16;      // 6-65525, I2C clock high period count
            ULONG _rsv : 16;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_SS_SCL_HCNT;

    // I2C Standard Speed SCL Low Count Register.
    typedef union {
        struct {
            ULONG IC_SS_SCL_LCNT : 16;      // 8-65535, I2C clock low period count
            ULONG _rsv : 16;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_SS_SCL_LCNT;

    // I2C Fast Speed SCL High Count Register.
    typedef union {
        struct {
            ULONG IC_FS_SCL_HCNT : 16;      // 6-65535, I2C clock high period count
            ULONG _rsv : 16;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_FS_SCL_HCNT;

    // I2C Fast Speed SCL Low Count Register.
    typedef union {
        struct {
            ULONG IC_FS_SCL_LCNT : 16;      // 8-65535, I2C clock low period count.
            ULONG _rsv : 16;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_FS_SCL_LCNT;

    // I2C Interrupt Status Register.
    typedef union {
        struct {
            ULONG R_RX_UNDER : 1;           // Set if read buffer is read when it is empty
            ULONG R_RX_OVER : 1;            // Set if byte received when RX FIFO is full
            ULONG R_RX_FULL : 1;            // Set if RX FIFO reaches IC_RX_TL threshold
            ULONG R_TX_OVER : 1;            // Set if byte written when TX FIFO is full
            ULONG R_TX_EMPTY : 1;           // Set with TX FIFO is at or below IC_TX_TL threshold
            ULONG R_RD_REQ : 1;             // Slave function, unused here
            ULONG R_TX_ABRT : 1;            // Set if TX fails and can't complete
            ULONG _rsv1 : 1;                // Reserved
            ULONG R_ACTIVITY : 1;           // Set after there has been I2C activity
            ULONG R_STOP_DET : 1;           // Set when a STOP condition has occurred
            ULONG R_START_DET : 1;          // Set when a START or RESTART condition has occurred
            ULONG _rsv2 : 21;               // Reserved
        };
        ULONG ALL_BITS;
    } _IC_INTR_STAT;

    // I2C Interrupt Mask Register.
    typedef union {
        struct {
            ULONG M_RX_UNDER : 1;           // 0: Disable RX Underflow status and interrupt
            ULONG M_RX_OVER : 1;            // 0: Disable RX Overflow status and interrupt
            ULONG M_RX_FULL : 1;            // 0: Disable RX Full status and interrupt
            ULONG M_TX_OVER : 1;            // 0: Disable TX Overflow status and interrupt
            ULONG M_TX_EMPTY : 1;           // 0: Disable TX Empty status and interrupt
            ULONG _rsv1 : 1;                // Reserved
            ULONG M_TX_ABRT : 1;            // 0: Disable TX Abort status and interrupt
            ULONG _rsv2 : 1;                // Reserved
            ULONG M_ACTIVITY : 1;           // 0: Disable I2C Activity status and interrupt
            ULONG M_STOP_DET : 1;           // 0: Disable STOP Condition status and interrupt
            ULONG M_START_DET : 1;          // 0: Disable START/RESTART Conditon status and interrupt
            ULONG _rsv3 : 21;               // Reserved
        };
        ULONG ALL_BITS;
    } _IC_INTR_MASK;

    // I2C Raw Interrupt Status Register.
    typedef union {
        struct {
            ULONG RX_UNDER : 1;             // 0: Unmasked RX Underflow status
            ULONG RX_OVER : 1;              // 0: Unmasked RX Overflow status
            ULONG RX_FULL : 1;              // 0: Unmasked RX Full status
            ULONG TX_OVER : 1;              // 0: Unmasked TX Overflow status
            ULONG TX_EMPTY : 1;             // 0: Unmasked TX Empty status
            ULONG _rsv1 : 1;                // Reserved
            ULONG TX_ABRT : 1;              // 0: Unmasked TX Abort status
            ULONG _rsv2 : 1;                // Reserved
            ULONG ACTIVITY : 1;             // 0: Unmasked I2C Activity status
            ULONG STOP_DET : 1;             // 0: Unmasked STOP Condition status
            ULONG START_DET : 1;            // 0: Unmasked START/RESTART Conditon status
            ULONG _rsv3 : 21;               // Reserved
        };
        ULONG ALL_BITS;
    } _IC_RAW_INTR_STAT;

    // I2C Receive FIFO Threshold Level Register.
    typedef union {
        struct {
            ULONG IC_RX_TL : 8;             // 0-FIFO_LEN, threshold is IC_RX_TL + 1
            ULONG _rsv : 24;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_RX_TL;

    // I2C Transmit FIFO Threshold Level Register.
    typedef union {
        struct {
            ULONG IC_TX_TL : 8;             // 0-FIFO_LEN, threshold is IC_TX_TL value
            ULONG _rsv : 24;                // Rserved
        };
        ULONG ALL_BITS;
    } _IC_TX_TL;

    // I2C Clear Combined and Individual Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_INTR : 1;             // Read to clear all software clearable interrupts
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_INTR;

    // I2C Clear RX_UNDER Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_RX_UNDER : 1;         // Read to clear RX_UNDER interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_RX_UNDER;

    // I2C Clear RX_OVER Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_RX_OVER : 1;          // Read to clear RX_OVER interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_RX_OVER;

    // I2C Clear TX_OVER Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_TX_OVER : 1;          // Read to clear TX_OVER interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_TX_OVER;

    // I2C Clear RD_REQ Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_RD_REQ : 1;           // Read to clear RD_REQ interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_RD_REQ;

    // I2C Clear TX_ABRT Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_TX_ABRT : 1;          // Read to clear TX_ABRT interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_TX_ABRT;

    // I2C Clear ACTIVITY Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_ACTIVITY : 1;         // Read to clear ACTIVITY interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_ACTIVITY;

    // I2C Clear STOP_DET Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_STOP_DET : 1;         // Read to clear STOP_DET interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_STOP_DET;

    // I2C Clear START_DET Interrupt Register.
    typedef union {
        struct {
            ULONG CLR_START_DET : 1;        // Read to clear START_DET interrupt status
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_CLR_START_DET;

    // I2C Controller Enable Register.
    typedef union {
        struct {
            ULONG ENABLE : 1;               // 0: I2C Controller Disabled, 1: Enabled
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_ENABLE;

    // I2C Controller Status Register.
    typedef union {
        struct {
            ULONG ACTIVITY : 1;             // 0: Not active, 1: Active
            ULONG TFNF : 1;                 // 0: TX FIFO is full, 1: TX FIFO is not full
            ULONG TFE : 1;                  // 0: TX FIFO is not empty, 1: TX FIFO is empty
            ULONG RFNE : 1;                 // 0: RX FIFO is empty, 1: RX FIFO is not empty
            ULONG RFF : 1;                  // 0: RX FIFO is not full, 1: RX FIFO is full
            ULONG MST_ACTIVITY : 1;         // 0: Master state machine is idle, 1: Active
            ULONG _rsv : 26;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_STATUS;

    // I2C Transmit FIFO Level Register.
    typedef union {
        struct {
            ULONG TXFLR : 5;                // Count of valid data entries in TX FIFO
            ULONG _rsv : 27;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_TXFLR;

    // I2C Receive FIFO Level Register.
    typedef union {
        struct {
            ULONG RXFLR : 5;                // Count of valid data entries in RX FIFO
            ULONG _rsv : 27;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_RXFLR;

    // I2C SDA Hold Time Register.
    typedef union {
        struct {
            ULONG IC_SDA_HOLD : 16;         // 1-65535, I2C clock periods to hold data after SCL drops
            ULONG _rsv : 16;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_SDA_HOLD;

    // I2C Transmit Abort Source Register.
    typedef union {
        struct {
            ULONG ABRT_7B_ADDR_NOACK : 1;   // Set if in 7-bit addr mode and addr not acked
            ULONG ABRT_10ADDR1_NOACK : 1;   // Set if 10-bit addr mode, and 1st addr byte not acked
            ULONG ABRT_10ADDR2_NOACK : 1;   // Set if 10-bit addr mode, and 2nd addr byte not acked
            ULONG ABRT_TXDATA_NOACK : 1;    // Set if addr was acked, but data was not acked
            ULONG _rsv1 : 3;                // Reserved
            ULONG ABRT_SBYTE_ACKDET : 1;    // Set if a start byte was sent, and a slave acked it
            ULONG _rsv2 : 1;                // Reserved
            ULONG ABRT_SBYTE_NORSTRT : 1;   // Set if start byte send with restart disabled
            ULONG ABRT_10B_RD_NORSTRT : 1;  // Set if 10-bit address read sent with restart disabled
            ULONG ABRT_MASTER_DIS : 1;      // Set if Master operation attempted with Master mode disabled
            ULONG ARB_LOST : 1;             // Set if master has lost arbitration
            ULONG _rsv3 : 19;               // Reserved
        };
        ULONG ALL_BITS;
    } _IC_TX_ABRT_SOURCE;

    // I2C Enable Status Register.
    typedef union {
        struct {
            ULONG IC_EN : 1;                // 0: Controller is inactive, 1: Controller active
            ULONG _rsv : 31;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_ENABLE_STATUS;

    // Standerd Speed and Full Speed Spike Suppression Limit Register.
    typedef union {
        struct {
            ULONG IC_FS_SPKLENRX_TL : 8;    // 2-255, max I2C clock periods to reject spike
            ULONG _rsv : 24;                // Reserved
        };
        ULONG ALL_BITS;
    } _IC_FS_SPKLEN;

    #pragma warning( pop )

    // Layout of the Quark I2C Controller registers in memory.
    typedef struct _I2C_CONTROLLER {
        volatile _IC_CON            IC_CON;             // 0x00 - Control Register
        volatile _IC_TAR            IC_TAR;             // 0x04 - Master Target Address
        ULONG                       _reserved1[2];      // 0x08 - 0x0F
        volatile _IC_DATA_CMD       IC_DATA_CMD;        // 0x10 - Data Buffer and Command
        volatile _IC_SS_SCL_HCNT    IC_SS_SCL_HCNT;     // 0x14 - Standard Speed Clock SCL High Count
        volatile _IC_SS_SCL_LCNT    IC_SS_SCL_LCNT;     // 0x18 - Standard Speed Clock SCL Low Count
        volatile _IC_FS_SCL_HCNT    IC_FS_SCL_HCNT;     // 0x1C - Fast Speed Clock Scl High Count
        volatile _IC_FS_SCL_LCNT    IC_FS_SCL_LCNT;     // 0x20 - Fast Speed Clock Scl Low Count
        ULONG                       _reserved2[2];      // 0x24 - 0x2B
        volatile _IC_INTR_STAT      IC_INTR_STAT;       // 0x2C - Interrupt Status
        volatile _IC_INTR_MASK      IC_INTR_MASK;       // 0x30 - Interrupt Mask
        volatile _IC_RAW_INTR_STAT  IC_RAW_INTR_STAT;   // 0x34 - Raw Interrupt Status
        volatile _IC_RX_TL          IC_RX_TL;           // 0x38 - Receive FIFO Threshold Level
        volatile _IC_TX_TL          IC_TX_TL;           // 0x3C - Transmit FIFO Threshold Level
        volatile _IC_CLR_INTR       IC_CLR_INTR;        // 0x40 - Clear Combined and Individual Interrupt
        volatile _IC_CLR_RX_UNDER   IC_CLR_RX_UNDER;    // 0x44 - Clear RX_UNDER Interrupt
        volatile _IC_CLR_RX_OVER    IC_CLR_RX_OVER;     // 0x48 - Clear RX_OVER Interrupt
        volatile _IC_CLR_TX_OVER    IC_CLR_TX_OVER;     // 0x4C - Clear TX_OVER Interrupt
        volatile _IC_CLR_RD_REQ     IC_CLR_RD_REQ;      // 0x50 - Clear RD_REQ Interrupt
        volatile _IC_CLR_TX_ABRT    IC_CLR_TX_ABRT;     // 0x54 - Clear TX_ABRT Interrupt
        ULONG                       _reserved3;         // 0x58 - 0x5B
        volatile _IC_CLR_ACTIVITY   IC_CLR_ACTIVITY;    // 0x5C - Clear ACTIVITY Interrupt
        volatile _IC_CLR_STOP_DET   IC_CLR_STOP_DET;    // 0x60 - Clear STOP_DET Interrupt
        volatile _IC_CLR_START_DET  IC_CLR_START_DET;   // 0x64 - Clear START_DET Interrupt
        ULONG                       _reserved4;         // 0x68 - 0x6B
        volatile _IC_ENABLE         IC_ENABLE;          // 0x6C - Enable
        volatile _IC_STATUS         IC_STATUS;          // 0x70 - Status
        volatile _IC_TXFLR          IC_TXFLR;           // 0x74 - Transmit FIFO Level
        volatile _IC_RXFLR          IC_RXFLR;           // 0x78 - Receive FIFO Level
        volatile _IC_SDA_HOLD       IC_SDA_HOLD;        // 0x7C - SDA Hold
        volatile _IC_TX_ABRT_SOURCE IC_TX_ABRT_SOURCE;  // 0x80 - Transmit Abort Source
        ULONG                       _reserved5[6];      // 0x84 - 0x9B
        volatile _IC_ENABLE_STATUS  IC_ENABLE_STATUS;   // 0x9C - Enable Status
        volatile _IC_FS_SPKLEN      IC_FS_SPKLEN;       // 0xA0 - SS and FS Spike Suppression Limit
    } I2C_CONTROLLER, *PI2C_CONTROLLER;

    //
    // I2cControllerClass private data members.
    //

    // Pointer to the object used to address the I2C Controller registers after
    // they are mapped into this process' address space.
    PI2C_CONTROLLER m_registers;

    // Method to map the I2C controller into this process' virtual address space.
    HRESULT _mapController() override;

    // TRUE if the controller has been initialized.
    BOOL m_controllerInitialized;
};

#endif // _BT_I2C_CONTROLLER_H_