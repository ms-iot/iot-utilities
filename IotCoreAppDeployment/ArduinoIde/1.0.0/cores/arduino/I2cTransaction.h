// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _I2C_TRANSACTION_H_
#define _I2C_TRANSACTION_H_

#include <Windows.h>
#include <functional>

#include "I2cTransfer.h"

class I2cControllerClass;

//
// Here, "transaction" is used to mean a set of I2C transfers that occurs 
// to/from a single I2C slave address.
// A transaction begins with a START and ends with a STOP.  The I2C bus is
// claimed for exclusive use by a transaction during the execution phase.
//
class I2cTransactionClass
{
public:
    I2cTransactionClass() :
        m_controller(nullptr),
        m_slaveAddress(0),
        m_pFirstXfr(nullptr),
        m_pXfrQueueTail(nullptr),
        m_hI2cLock(INVALID_HANDLE_VALUE),
        m_abort(FALSE),
        m_error(SUCCESS),
        m_isIncomplete(FALSE),
        m_useHighSpeed(FALSE)
    {
    }

    virtual inline ~I2cTransactionClass()
    {
        reset();
#if !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a UWP app:
        m_hI2cLock = INVALID_HANDLE_VALUE;
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
        if (m_hI2cLock != INVALID_HANDLE_VALUE)
        {
            CloseHandle(m_hI2cLock);
            m_hI2cLock = INVALID_HANDLE_VALUE;
        }
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
    }

    // Prepare this transaction for re-use.
    // Any previously set slave address is not affected by this method.
    void reset();

    // Sets the 7-bit address of the slave for this transaction.
    HRESULT setAddress(ULONG slaveAdr);

    // Gets the 7-bit address of the slave for this transaction.
    inline ULONG getAddress()
    {
        return m_slaveAddress;
    }

    // Add a write transfer to the transaction.
    inline HRESULT queueWrite(PUCHAR buffer, const ULONG bufferBytes)
    {
        return queueWrite(buffer, bufferBytes, FALSE);
    }

    HRESULT queueWrite(PUCHAR buffer, const ULONG bufferBytes, const BOOL preRestart);

    // Add a read transfer to the transaction.
    inline HRESULT queueRead(PUCHAR buffer, const ULONG bufferBytes)
    {
        return queueRead(buffer, bufferBytes, FALSE);
    }

    HRESULT queueRead(PUCHAR buffer, const ULONG bufferBytes, const BOOL preRestart);

    // Method to queue a callback routine at the current point in the transaction.
    HRESULT queueCallback(const std::function<HRESULT()> callBack);

    // Method to perform the transfers associated with this transaction.
    HRESULT execute(I2cControllerClass* controller);

    // Method to get the number of 1 mSec ticks that occurred while waiting for outstanding reads.
    inline void getReadWaitTicks(ULONG & waits) const
    {
        waits = m_maxWaitTicks;
    }

    // Method to determine if a transfer is the last transfer in the transaction.
    inline BOOL isLastTransfer(I2cTransferClass* pXfr) const
    {
        return (pXfr == m_pXfrQueueTail);
    }

    /// Method to abort any remaining transfers.
    inline void abort()
    {
        m_abort = TRUE;
    }

    /// Enum for transaction error codes;
    const enum ERROR_CODE {
        SUCCESS,                ///< No Error has occured
        ADR_NACK,               ///< Slave address was not acknowledged
        DATA_NACK,              ///< Slave did not acknowledge data
        OTHER                   ///< Some other error occured
    };

    /// Get the current error code for this transaction.
    inline ERROR_CODE getError()
    {
        return m_error;
    }

    /// Method to determine if an error occured during this transaction.
    inline BOOL errorOccured()
    {
        return (m_error != SUCCESS);
    }

    /// Method to determine if this transaction has been completed or not.
    inline BOOL isIncomplete()
    {
        return m_isIncomplete;
    }

    /// Method to signal high speed can be used for this transaction.
    inline void useHighSpeed()
    {
        m_useHighSpeed = TRUE;
    }

private:

    //
    // I2cTransactionClass data members.
    //

    // The I2c Controller object used to process this transaction.
    I2cControllerClass* m_controller;

    // The address of the I2C slave for this transaction.
    // Currently, only 7-bit addresses are supported.
    ULONG m_slaveAddress;

    // Queue of transfers for this transaction.
    I2cTransferClass* m_pFirstXfr;

    // Address of transfer queue tail.
    I2cTransferClass* m_pXfrQueueTail;

    // The max wait time (in mSec) for outstanding reads.
    ULONG m_maxWaitTicks;

    // The I2C Lock handle.
    HANDLE m_hI2cLock;

    // Set to TRUE to abort the remainder of the transaction.
    BOOL m_abort;

    /// Error code 
    ERROR_CODE m_error;

    /// TRUE if one or more incompleted transfers exist on this transaction.
    BOOL m_isIncomplete;

    /// TRUE to allow use of high speed for this transaction.
    BOOL m_useHighSpeed;

    //
    // I2cTransactionClass private member functions.
    //

    // Method to queue a transfer as part of this transaction.
    void _queueTransfer(I2cTransferClass* pXfr);

    // Method to process each transfer in this transaction.
    HRESULT _processTransfers();

    // Method to shut down the I2C Controller after a transaction is done with it.
    HRESULT _shutDownI2cAfterTransaction();

    /// Method to acquire the I2C Controller lock for this transaction.
    HRESULT _acquireI2cLock();

    /// Method to release this transaction's lock on the I2C Controller.
    HRESULT _releaseI2cLock();
};

#endif // _I2C_TRANSACTION_H_