// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _I2C_CONTROLLER_H_
#define _I2C_CONTROLLER_H_

#include <Windows.h>
#include <functional>

#include "I2cTransfer.h"
#include "I2cTransaction.h"
#include "DmapSupport.h"

#define EXTERNAL_I2C_BUS 0
#define SECOND_EXTERNAL_I2C_BUS 1

#define INVALID_PIN_NUMBER 0xFFFFFFFF

//
// Base class for classes used to interact with I2C Controller hardware.
//
class I2cControllerClass
{
public:
    I2cControllerClass() :
        m_hController(INVALID_HANDLE_VALUE),
        m_sdaPin(INVALID_PIN_NUMBER),
        m_sclPin(INVALID_PIN_NUMBER),
        m_busNumber(EXTERNAL_I2C_BUS),
        m_error(I2cTransactionClass::ERROR_CODE::SUCCESS),
        m_maxWaitTicks(0)
    {
    }

    virtual ~I2cControllerClass()
    {
    }

    /// Initialize the pin assignments for this I2C controller.
    virtual HRESULT configurePins(ULONG sdaPin, ULONG sclPin) = 0;

    /// Revert the pins used by this I2C controller to GPIO use.
    HRESULT revertPinsToGpio();

    /// Initialize the specified I2C bus.
    /**
    \param[in] busNumber The number of the I2C bus to open.
    \return HRESULT success or error code.
    */
    virtual HRESULT begin(ULONG busNumber) = 0;

    /// Finish using the I2C controller associated with this object.
    virtual inline void end() = 0;

    // This method maps the I2C controller if needed.
    inline HRESULT mapIfNeeded()
    {
        if (m_hController == INVALID_HANDLE_VALUE)
        {
            return _mapController();
        }
        else
        {
            return S_OK;
        }
    }

    // Method to initialize the I2C Controller at the start of a transaction.
    virtual HRESULT _initializeForTransaction(ULONG slaveAddress, BOOL useHighSpeed) = 0;

    //
    // I2C Controller accessor methods.  These methods assume the I2C Controller
    // has already been mapped using mapIfNeeded().
    //

    virtual inline BOOL txFifoFull() const = 0;

    virtual inline BOOL txFifoEmpty() const = 0;

    virtual inline BOOL rxFifoNotEmtpy() const = 0;

    virtual inline BOOL rxFifoEmpty() const = 0;

    virtual HRESULT _performContiguousTransfers(I2cTransferClass* & pXfr) = 0;

    inline HRESULT calculateCurrentCounts(I2cTransferClass* pXfr, LONG& byteCount, LONG& readCount);

    inline I2cTransactionClass::ERROR_CODE getTransfersError()
    {
        return m_error;
    }

    virtual inline UCHAR readByte() = 0;

    virtual inline BOOL isActive() const = 0;

    /// Determine whether a TX Error has occurred or not.
    /**
    All I2C bus errors we are interested in are TX errors: failure to ACK an
    an address or write data.
    \return TRUE, an error occured.  FALSE, no error has occured.
    */
    virtual inline BOOL errorOccurred() = 0;

    /// Determine if an I2C address was sent but not acknowledged by any slave.
    /**
    This method tests for the error that is expected to occur if an attempt
    is made talk to an I2C slave that does not exist.
    \return TRUE, an I2C address was not acknowdged.  FALSE, all addresses sent
    have been acknowledged by at least one slave.
    */
    virtual inline BOOL addressWasNacked() = 0;

    /// Determine if I2C data was sent but not acknowledged by a slave.
    /**
    This method tests for the error that occurs if a slave has been found, 
    but then fails to acknowledge a data byte sent by the master.
    \return TRUE, I2C data was not acknowdged.  FALSE, all data sent has 
    been acknowledged by a slave.
    */
    virtual inline BOOL dataWasNacked() = 0;

    /// Handle any errors that have occurred during an I2C transaction.
    /**
    Determine whether an error occurred, and if so (and it is the first error on this
    transaction) record the error information.
    \param[out] error The wiring error or success code for the transaction result.
    \return HRESULT success or error code.
    */
    virtual inline HRESULT _handleErrors() = 0;

    /// Reset the controller after a bus error has occured.
    /**
    The actul error condition is cleard by reading the IC_CLR_TX_ABRT register.
    */
    virtual inline void clearErrors() = 0;

    /// Method to get the handle to the I2C Controller this object has open.
    inline HANDLE getControllerHandle() { return m_hController; }

protected:
    /// Handle to the open device.
    /**
    This handle can be used to map the I2C Controller registers into user-mode 
    address space, or to lock the device for exclusive access.
    */
    HANDLE m_hController;

    /// I2C Serial Data pin number.
    ULONG m_sdaPin;

    /// I2C Serial Clock pin number.
    ULONG m_sclPin;

    /// The bus number for the I2C controller associated with this object.
    ULONG m_busNumber;

    // Error codr from a sequence of contiguous transfers.
    I2cTransactionClass::ERROR_CODE m_error;

    // Maximum number of wait ticks we have waited for outstanding reads to complete.
    ULONGLONG m_maxWaitTicks;

    /// Method to map the I2C controller into this process' virtual address space.
    virtual HRESULT _mapController() = 0;

private:

    //
    // I2cControllerClass private data members.
    //

};

// Method to calculate the count of bytes to transfer for the current group of transfers
// up to the end of the transaction or the next callback (whichever occurs first).
inline HRESULT I2cControllerClass::calculateCurrentCounts(
    I2cTransferClass* pXfr,
    LONG& byteCount,
    LONG& readCount)
{
    I2cTransferClass* currentXfr = pXfr;

    // Clear out any counts currently in place.
    byteCount = 0;
    readCount = 0;

    // For each transfer in the queue or until a callback "transfer" is found:
    while ((currentXfr != nullptr) && !currentXfr->hasCallback())
    {
        // Include the size of the transfer's buffer in the command count.
        byteCount += currentXfr->getBufferSize();

        // If this is a read transfer, include buffer size in read count.
        if (currentXfr->transferIsRead())
        {
            readCount += currentXfr->getBufferSize();
        }

        // Get the next transfer.
        currentXfr = currentXfr->getNextTransfer();
    }

    return S_OK;
}

#endif // _I2C_CONTROLLER_H_