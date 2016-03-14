// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _BCM_I2C_CONTROLLER_H_
#define _BCM_I2C_CONTROLLER_H_

#include <Windows.h>
#include <functional>

#include "I2cTransfer.h"
#include "I2cController.h"

//
// Class that is used to interact with the BCM2836 I2C Controller hardware.
//
class BcmI2cControllerClass : public I2cControllerClass
{
public:
    BcmI2cControllerClass() :
        m_registers(nullptr)
    {
    }

    virtual ~BcmI2cControllerClass()
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

    //
    // I2C Controller accessor methods.  These methods assume the I2C Controller
    // has already been mapped using mapIfNeeded().
    //

    inline BOOL txFifoFull() const override
    {
        return (m_registers->S.TXD == 0);
    }

    inline BOOL txFifoEmpty() const override
    {
        return (m_registers->S.TXE == 1);
    }

    inline BOOL rxFifoNotEmtpy() const override
    {
        return (m_registers->S.RXD == 1);
    }

    inline BOOL rxFifoEmpty() const override
    {
        return (m_registers->S.RXD == 0);
    }

    HRESULT _performContiguousTransfers(I2cTransferClass* & pXfr) override;

    inline UCHAR readByte() override
    {
        return (m_registers->FIFO.DATA);
    }

    inline BOOL isActive() const override
    {
        return (m_registers->S.TA == 1);
    }

    /// Determine whether a TX Error has occurred or not.
    /**
    All I2C bus errors we are interested in are TX errors: failure to ACK an
    an address or write data.
    \return TRUE, an error occured.  FALSE, no error has occured.
    */
    inline BOOL errorOccurred() override
    {
        _S sReg;
        sReg.ALL_BITS = m_registers->S.ALL_BITS;
        return (sReg.ERR == 1);
//        return (m_registers->S.ERR == 1);
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
        return (m_registers->S.ERR == 1);
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
        return FALSE;
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

    /// Reset the controller after a bus error has occured.
    /**
    The actul error condition is cleard by reading the IC_CLR_TX_ABRT register.
    */
    inline void clearErrors() override
    {
        _S statusReg;
        statusReg.ALL_BITS = 0;
        statusReg.CLKT = 1;
        statusReg.ERR = 1;
        m_registers->S.ALL_BITS = statusReg.ALL_BITS;
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
        ULONG ALL_BITS;
        struct {
            ULONG READ : 1;             // 0: write transfer, 1: read transfer
            ULONG _rsv1 : 3;            // Reserved
            ULONG CLEAR : 2;            // 00: no action, x1 or 1x: Clear FIFO
            ULONG _rsv2 : 1;            // Reserved
            ULONG ST : 1;               // Start Transfer - 0: no action, 1:one shot start
            ULONG INTD : 1;             // Int on DONE - 0: no int, 1: int on DONE
            ULONG INTT : 1;             // Int on TX - 0: no int, 1: int while TXW == 1
            ULONG INTR : 1;             // Int on RX - 0: no int, 1: int while RXR == 1
            ULONG _rsv3 : 4;            // Reserved
            ULONG I2CEN : 1;            // I2C Enable - 0: controller disabled, 1: enabled
            ULONG _rsv4 : 16;           // Reserved
        };
    } _C;
    const ULONG _C_USED_MASK = 0x000087B1;    // Mask of non-reserved bits in the Control Register

    // I2C Status Register.
    typedef union {
        ULONG ALL_BITS;
        struct {
            ULONG TA : 1;               // Transfer Active - 0: no active transfer, 1: active
            ULONG DONE : 1;             // Done - 0: transfer no complete, 1: complete
            ULONG TXW : 1;              // FIFO needs writing - 0: FIFO above threshold, 1: below
            ULONG RXR : 1;              // FIFO needs reading - 0: FIFO below threshold, 1: above
            ULONG TXD : 1;              // TX FIFO has space - 0: FIFO full, 1: at least 1 byte free
            ULONG RXD : 1;              // RX FIRO has data - 0: FIFO empty, 1: at least 1 data in FIFO
            ULONG TXE : 1;              // TX FIFO empty - 0: data in FIFO, 1: FIFO is empty
            ULONG RXF : 1;              // RX FIFO full - 0: FIFO not full, 1: FIFO is full
            ULONG ERR : 1;              // ACK error - 0: no error, 1: slave NACKed address
            ULONG CLKT : 1;             // Clock stretch timeout - 0: ok, 1: timeout occurred
            ULONG _rsv : 18;            // Reserved
            ULONG STATE : 4;            // I2C controller current state
        };
    } _S;
    const ULONG _S_USED_MASK = 0x000003FF;    // Mask of non-reserved bits in the Status Register

    // I2C Data Length Register.
    typedef union {
        ULONG ALL_BITS;
        struct {
            ULONG DLEN : 16;            // Data Lenght - number of bytes in transfer
            ULONG _rsv : 16;            // Reserved
        };
    } _DLEN;
    const ULONG _DLEN_USED_MASK = 0x0000FFFF; // Mask of non-reserved bits in the Data Length Register

    // I2C Slave Address Register.
    typedef union {
        ULONG ALL_BITS;
        struct {
            ULONG ADDR : 7;             // Slave address for transfer
            ULONG _rsv : 25;            // Reserved
        };
    } _A;
    const ULONG _A_USED_MASK = 0x0000007F;    // Mask of non-reserved bits in the Slave Address Register

    // I2C Data FIFOs.
    typedef union {
        ULONG ALL_BITS;
        struct {
            ULONG DATA : 8;             // Write byte to TX FIFO, read byte from RX FIFO
            ULONG _rsv : 24;            // Reserved
        };
    } _FIFO;
    const ULONG _FIFO_USED_MASK = 0x000000FF;  // Mask of non-reserved bits in the FIFOs Register

    // I2C Clock Divider Register.
    typedef union {
        ULONG ALL_BITS;
        struct {
            ULONG CDIV : 16;            // Clock divider: SCL = 150mhz / CDIV
            ULONG _rsv : 16;            // Reserved
        };
    } _DIV;
    const ULONG _DIV_USED_MASK = 0x0000FFFF;  // Mask of non-reserved bits in the Clock Divider Register

    // Standard (100khz) and high (400khz) divider values.
    const ULONG CDIV_100KHZ = 1500;
    const ULONG CDIV_400KHZ = 376;      // Actually 398.9khz, divider value must be even

    // I2C Data Delay Register.
    typedef union {
        ULONG ALL_BITS;
        struct {
            ULONG REDL : 16;            // Rising edge delay: clock cycles from clock to sample
            ULONG FEDL : 16;            // Falling edge delay: cycles from clock to bit output
        };
    } _DEL;
    ULONG _DEL_USED_MASK = 0xFFFFFFFF;  // Mask of non-reserved bits in the Data Delay Register

    // I2C Clock Stretch Timeout Register.
    typedef union {
        ULONG ALL_BITS;
        struct {
            ULONG TOUT : 16;            // Clock stretch timeout: 0: no timeout, >0: SCL clock cycles
            ULONG _rsv : 16;            // Reserved
        };
    } _CLKT;
    ULONG _CLKT_USED_MASK = 0x0000FFFF; // Mask of non-reserved bits in the Clock Stretch Timeout Register

#pragma warning( pop )

    // Layout of the Quark I2C Controller registers in memory.
    typedef struct _I2C_CONTROLLER {
        volatile _C     C;      // 0x00 - Control Register
        volatile _S     S;      // 0x04 - Status Register
        volatile _DLEN  DLEN;   // 0x08 - Data Length Register
        volatile _A     A;      // 0x0C - Slave Address Register
        volatile _FIFO  FIFO;   // 0x10 - Data FIFOs (RX and TX)
        volatile _DIV   DIV;    // 0x14 - Clock Divider Register
        volatile _DEL   DEL;    // 0x18 - Data Delay Register
        volatile _CLKT  CLKT;   // 0x1C - Clock Stretch Timeout Register
    } I2C_CONTROLLER, *PI2C_CONTROLLER;

    //
    // I2cControllerClass private data members.
    //

    // Pointer to the object used to address the I2C Controller registers after
    // they are mapped into this process' address space.
    PI2C_CONTROLLER m_registers;

    // Method to map the I2C controller into this process' virtual address space.
    HRESULT _mapController() override;

    // Perform one or more contiguous write transfers.
    HRESULT _performWrites(I2cTransferClass* &pXfr);

    // Perform one or more contiguous read transfers.
    HRESULT _performReads(I2cTransferClass* &pXfr);

    // Perform a Write-Restart-Read sequence of transfers.
    HRESULT _performWriteRead(I2cTransferClass* &pXfr);

    // The maximum length of a transfer.
    const LONG m_maxTransferBytes = 0xFFFF;
};

#endif // _BCM_I2C_CONTROLLER_H_