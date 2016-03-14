// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef TWO_WIRE_H
#define TWO_WIRE_H

#include <Windows.h>
#include <stdint.h>
#include <vector>
#include <algorithm>

#include "ArduinoError.h"
#include "I2c.h"

#ifndef TWI_FREQ
#define TWI_FREQ 100000L
#endif

#define BUFFER_LENGTH 32

// Forward declaration(s):
int Log(const char *format, ...);

class TwoWire
{

public:

    /// Enum with status codes returned by endTransmission().
    enum TwiError {
        SUCCESS = 0,
        TWI_BUFFER_OVERRUN = 1,
        ADDR_NACK_RECV = 2,
        DATA_NACK_RECV = 3,
        OTHER_ERROR = 4,
    };

    /// Constructor.
    TwoWire()
    {
        _cleanTransaction();
    }

    /// Destructor.
    virtual ~TwoWire()
    {
    }

    /// Method to begin use of the I2C bus by the code using this library.
    void begin()
    {
        HRESULT hr;

        hr = g_i2c.begin();

        if (FAILED(hr))
        {
            ThrowError(hr, "Error beginning I2C use: %08x", hr);
        }

        m_writeBuffs.clear();
        m_readBuffs.clear();
        m_writeBuff.clear();
    }

    /// Method to end use of the I2C bus by the code using this library.
    void end()
    {
        _cleanTransaction();

        g_i2c.end();
    }

    // slave mode not supported
    // void begin(uint8_t);
    // void begin(int);

    /// Method to start a write tranfer to an I2C slave.
    /**
    This method prepares to queue writes to an I2C slave.  If this method specifies
    a slave address different than previously queued transfers, those transfers are 
    performed first and an I2C STOP is done before the new I2C sequence is created.
    \note Writes done after calling beginTransmission() are captured and processed
    when endTransmission() is called.  If beginTransmission is called again, without
    calling endTranmission() first, the write requests are lost.
    \param[in] address The address of the I2C slave to read from.
    \return None.  Any error is thrown.
    */
    void beginTransmission(int address)
    {
        // Set the address of the I2C slave we are working with.
        _setSlaveAddress(address);

        // Empty the current write buffer.
        m_writeBuff.clear();
    }

    /// Complete a series of I2C writes.
    /**
    This method completes a series of writes that was begun with a beginTransmission()
    call.  It consolidates the series of write requests into a single write transfer, 
    and then queues it to the current I2C sequence.  All queued I2C tranfers are then 
    performed, and the bus is released with a STOP.
    \return SUCCESS - Success, ADDR_NACK_RECV - Slave did not ACK a transfer operation.
    */
    ULONG endTransmission(void)
    {
        return this->endTransmission(TRUE);
    }

    /// Queue a series of writes, and optionally perform them.
    /**
    This method consolidates all write requests done since a beginTransmission() into 
    a single write tranfer, and queues it to the current I2C sequence.  If sendStop is
    TRUE, all queued transfers are performed and the bus is released with a STOP.  If 
    sendStop is false, the write transfer is queued and no other action is taken.
    \param[in] sendStop FALSE - just queue a write transfer, TRUE - also perform all queued transfers.
    \return SUCCESS - Success. ADDR_NACK_RECV, DATA_NACK_RECV, OTHER ERROR - Error.
    */
    ULONG endTransmission(BOOL sendStop)
    {
        HRESULT hr;

        ULONG retVal = SUCCESS;

        // Create a buffer on the write buffer queue for the transfer.
        m_writeBuffs.push_back(m_writeBuff);

        // Empty the write buffer now that it's contents are queued.
        m_writeBuff.clear();

        // If we have data to write, perform the write. If there is no data to write, do nothing.
        if (m_writeBuffs.back().size() > 0)
        {
            // Queue a write from the buffer.
            hr = m_i2cTransaction.queueWrite(m_writeBuffs.back().data(), (ULONG)m_writeBuffs.back().size());

            if (FAILED(hr))
            {
                _cleanTransaction();
                ThrowError(hr, "An error occurred queueing an I2C write of %d bytes.  Error: 0x%08X", m_writeBuffs.back().size(), hr);
            }

            // Perform all queued transfers if a STOP was specified.
            if (sendStop)
            {
                hr = m_i2cTransaction.execute(g_i2c.getController());

                if (FAILED(hr))
                {
                    if (m_i2cTransaction.getError() == I2cTransactionClass::ERROR_CODE::ADR_NACK)
                    {
                        retVal = ADDR_NACK_RECV;
                    }
                    else if (m_i2cTransaction.getError() == I2cTransactionClass::ERROR_CODE::DATA_NACK)
                    {
                        retVal = DATA_NACK_RECV;
                    }
                    else
                    {
                        retVal = OTHER_ERROR;
                    }
                    m_readBuffs.clear();
                }

                // Clean up all queued transfers now that they have been performed (or failed).
                m_writeBuffs.clear();

                // Clean out the transaction so it can be used again in the future.
                m_i2cTransaction.reset();

                // Get the current count of bytes available in the read buffer.  Any read buffers
                // queued should be full of data (or gone, if the transfer failed).
                _calculateReadBytesInBuffer();
            }
        }

        return retVal;
    }

    /// Method to perform a complete read from an I2C slave.
    /**
    This method queues a read transfer and causes it (and any other transfers
    previously queued to the same slave address) to be performed on the I2C bus.
    If this method specifies a slave address different than previously queued
    transfers, those transfers are performed first and an I2C STOP is done before
    the new read transfer is queued.
    \param[in] address The address of the I2C slave to read from.
    \param[in] quantity The number of bytes to read.
    \return Zero.  Any error is thrown.
    */
    ULONG requestFrom(ULONG address, ULONG quantity)
    {
        return this->requestFrom(address, quantity, 1);
    }

    /// Method to queue or perform a read from an I2C slave.
    /**
    This method queues a read transfer and optionally causes it (and any other
    transfers previously queued to the same slave address) to be performed on the 
    I2C bus.  If this method specifies a slave address different than previously queued
    transfers, those transfers are performed first and an I2C STOP is done before
    the new read transfer is queued.
    \param[in] address The address of the I2C slave to read from.
    \param[in] quantity The number of bytes to read.
    \param[in] sendStop TRUE - end the tranfer with an I2C stop, FALSE - don't end with STOP.
    \return Zero.  Any error is thrown.
    */
    ULONG requestFrom(ULONG address, ULONG quantity, BOOL sendStop)
    {
        HRESULT hr;

        if (quantity == 0)
        {
            _cleanTransaction();
            ThrowError(E_INVALIDARG, "Zero byte I2C reads are not allowed.");
        }

        // Set the address of the I2C slave we are working with.
        _setSlaveAddress(address);

        // Create a buffer on the read buffer queue for the transfer.
        buff_t newBuf(quantity, 0);
        m_readBuffs.push_back(newBuf);

        // Queue a read into the buffer.
        hr = m_i2cTransaction.queueRead(m_readBuffs.back().data(), quantity);

        if (FAILED(hr))
        {
            _cleanTransaction();
            ThrowError(hr, "An error occurred queueing an I2C read of %d bytes to address: 0x%02X.  Error: 0x%08X", quantity, address, hr);
        }

        // Perform all queued transfers if a STOP was specified.
        if (sendStop)
        {
            hr = m_i2cTransaction.execute(g_i2c.getController());

            if (FAILED(hr))
            {
                _cleanTransaction();
                ThrowError(hr, "Error encountered performing queued I2C transfers to address: 0x%02X, Error: 0x%08X", address, hr);
            }

            // Clear out queued transfers now that we are done with them.
            m_writeBuffs.clear();

            // Clean out the transaction so it can be used again in the future.
            m_i2cTransaction.reset();

            // Get the current count of bytes available in the read buffer.
            _calculateReadBytesInBuffer();
        }

        return quantity;
    }

    /// Set the address of the I2C slave we are talking to.
    /**
    This method determines if the slave address is changing.  If the address 
    is changing and the I2C transaction has not yet been processed, an error 
    is thrown.
    \param[in] address The slave address to set.
    */
    void _setSlaveAddress(ULONG address)
    {
        HRESULT hr;

        if (address != m_i2cTransaction.getAddress())
        {
            if ((m_i2cTransaction.getAddress() != 0) && m_i2cTransaction.isIncomplete())
            {
                _cleanTransaction();
                ThrowError(HRESULT_FROM_WIN32(ERROR_INVALID_STATE), "Previous I2C operation to address: 0x%02X must be completed before starting new operation to address: 0x%02X", m_i2cTransaction.getAddress(), address);
            }
            m_i2cTransaction.reset();
            m_writeBuffs.clear();
            hr = m_i2cTransaction.setAddress(address);

            if (FAILED(hr))
            {
                _cleanTransaction();
                ThrowError(hr, "Error encountered setting I2C address: 0x%02X, Error: 0x%08X", address, hr);
            }
        }
    }

    /// Queue a single byte write transfer on the I2C bus.
    /**
    \param[in] data The byte to send over the I2C bus.
    \return The number of bytes "sent" (1 in this case).
    */
    virtual size_t write(const uint8_t data)
    {
        this->m_writeBuff.push_back(data);
        return 1;
    }

    void onReceive(void(*)(int))
    {
        Log("FEATURE UNAVAILABLE: Galileo cannot act as I2C slave device!");
    }
    
    void onRequest(void(*)(void))
    {
        Log("FEATURE UNAVAILABLE: Galileo cannot act as I2C slave device!");
    }

    /// Queue an array of bytes write on the I2C bus.
    /**
    \param[in] data Pointer to the first byte to send over the I2C bus.
    \param[in] cbData The length of the data in bytes.
    \return The number of bytes "sent" (the value cbData in this case).
    */
    size_t write(const uint8_t *data, size_t cbData)
    {
        size_t length = this->m_writeBuff.size();
        this->m_writeBuff.resize(this->m_writeBuff.size() + cbData);
        auto it = this->m_writeBuff.begin();
        advance(it, length);
        std::copy_n(data, cbData, it);
        return cbData;
    }

    /// Queue a null terminated string write on the I2C bus.
    /**
    \param[in] string Pointer to the start of the null terminated byte string.
    \return The number of bytes "sent" (the number of characters in the string).
    */
    size_t write(PCHAR string)
    {
        size_t length = this->m_writeBuff.size();
        this->m_writeBuff.resize(this->m_writeBuff.size() + strlen(string));
        auto it = this->m_writeBuff.begin();
        advance(it, length);
        std::copy_n(string, strlen(string), it);
        return strlen(string);
    }

    /// Method to return the number of bytes of available to be read from the buffer.
    /**
    \return The total number of bytes currently in the queued read buffers.
    */
    inline ULONG available(void)
    {
        return m_readBytesAvailable;
    }

    /// Method to get the next byte from the read buffers.
    /**
    \note All bytes requested to be read should be present in the read buffers.  This is 
    also true for an read transfers requested that failed--their bytes are zeros.
    \return The next byte from the queued read buffers.
    */
    ULONG read(void)
    {
        ULONG retVal = 0;

        // If we have at least one byte in our read buffer:
        if (m_readBytesAvailable > 0)
        {
            // Get a pointer to the current buffer.
            buff_t* buffer = &m_readBuffs[m_readBuffIndex];

            // If all the bytes in this buffer have already been handled:
            if (m_readByteIndex >= buffer->size())
            {
                // Clear the buffer to free storage, we are done with it.
                buffer->clear();

                // Move to the next buffer in the queue.  There should be one, 
                // since we have at least one byte in a buffer waiting to be 
                // retreived.  We don't allow zero length reads, so there has 
                // to be at least one byte in this new buffer.
                m_readBuffIndex++;
                buffer = &m_readBuffs[m_readBuffIndex];
                m_readByteIndex = 0;
            }

            // Get the byte from the buffer and count it as handled.
            retVal = buffer->data()[m_readByteIndex];
            m_readByteIndex++;
            m_readBytesAvailable--;

            // If we have no more bytes remaining:
            if (m_readBytesAvailable == 0)
            {
                // Get rid of the whole read buffer queue.
                m_readBuffs.clear();
                m_readBuffIndex = 0;
                m_readByteIndex = 0;
            }
        }

        return retVal;
    }

private:

    /// The I2C transaction object used to drive transfers.
    I2cTransactionClass m_i2cTransaction;

    /// Typdef for a transmit or receive buffer.
    typedef std::vector<uint8_t> buff_t;

    /// Typedef for a queue of buffers.
    typedef std::vector<buff_t> buff_queue_t;

    /// Queue of write buffers.
    buff_queue_t m_writeBuffs;

    /// Current write buffer.
    buff_t m_writeBuff;

    /// Queue of read buffers.
    buff_queue_t m_readBuffs;

    /// Index into the read buffer queue to the current read buffer.
    ULONG m_readBuffIndex;

    /// Index into the current read buffer to the next byte to fetch from the buffer.
    ULONG m_readByteIndex;

    /// Count of bytes available to be read.
    ULONG m_readBytesAvailable;

    /// Method to count the total number of read bytes in read buffers.
    void _calculateReadBytesInBuffer()
    {
        m_readBytesAvailable = 0;
        for (buff_queue_t::iterator i = m_readBuffs.begin(); i != m_readBuffs.end(); i++)
        {
            m_readBytesAvailable += (ULONG) i->size();
        }
    }

    /// Method to clean up any existing transaction.
    void _cleanTransaction()
    {
        m_i2cTransaction.reset();
        m_writeBuff.clear();
        m_writeBuff.clear();
        m_readBuffs.clear();
        m_readBuffIndex = 0;
        m_readByteIndex = 0;
        m_readBytesAvailable = 0;
    }
};

__declspec(selectany) TwoWire Wire;

#endif
