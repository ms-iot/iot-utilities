// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _I2C_TRANSFER_H_
#define _I2C_TRANSFER_H_

#include <Windows.h>
#include <functional>

//
// Here, "transfer" is used to mean reading or writing one or more bytes within a
// transaction, in a single direction.
// Transfers are of type WRITE unless they are explicitely set to be a read transfer.
//
class I2cTransferClass
{
public:
    I2cTransferClass()
    {
        clear();
    }

    virtual ~I2cTransferClass()
    {
    }

    // Prepare to use or re-use this object.
    inline void clear()
    {
        m_pNextXfr = nullptr;
        m_pBuffer = nullptr;
        m_bufBytes = 0;
        m_isRead = FALSE;
        m_preRestart = FALSE;
        m_callBack = nullptr;
        resetCmd();
        resetRead();
    }
    
    // Prepare to use this object to step through buffer for command processing.
    inline void resetCmd()
    {
        m_nextCmd = 0;
        m_lastCmdFetched = FALSE;
    }

    // Prepare to use this object to step through buffer for read processing.
    inline void resetRead()
    {
        m_nextRead = 0;
    }

    inline void markReadTransfer()
    {
        m_isRead = TRUE;
    }

    inline void markPreRestart()
    {
        m_preRestart = TRUE;
    }

    inline void setBuffer(PUCHAR buffer, const ULONG bufBytes)
    {
        m_pBuffer = buffer;
        m_bufBytes = bufBytes;
    }

    inline PUCHAR getBuffer() const
    {
        return m_pBuffer;
    }

    inline ULONG getBufferSize() const
    {
        return m_bufBytes;
    }

    inline BOOL transferIsRead() const
    {
        return m_isRead;
    }

    inline BOOL preResart() const
    {
        return m_preRestart;
    }

    inline void chainNextTransfer(I2cTransferClass* pNext)
    {
        m_pNextXfr = pNext;
    }

    // Returns nullptr when there is no "next transfer".
    inline I2cTransferClass* getNextTransfer() const
    {
        return m_pNextXfr;
    }

    // Gets the next command/write byte.  Returns FALSE if there is none.
    inline BOOL getNextCmd(UCHAR & next)
    {
        if (m_nextCmd >= m_bufBytes)
        {
            return FALSE;
        }
        else
        {
            next = m_pBuffer[m_nextCmd];
            m_nextCmd++;
            if (m_nextCmd == m_bufBytes)
            {
                m_lastCmdFetched = TRUE;
            }
            return TRUE;
        }
    }

    // Returns TRUE is the last command byte has been fetched from buffer.
    inline BOOL lastCmdFetched() const
    {
        return m_lastCmdFetched;
    }

    // Return the next available location in the read buffer, or nullptr
    // if the read buffer is full.
    inline PUCHAR getNextReadLocation()
    {
        if (!m_isRead || (m_nextRead >= m_bufBytes))
        {
            return nullptr;
        }
        else
        {
            PUCHAR nextRead = &(m_pBuffer[m_nextRead]);
            m_nextRead++;
            return nextRead;
        }
    }

    // Method to associate a callback routine with this transfer.
    inline HRESULT setCallback(std::function<HRESULT()> callBack)
    {
        m_callBack = callBack;
        return S_OK;
    }

    // Method to invoke any callback routine associated with this transfer.
    inline HRESULT invokeCallback()
    {
        if (hasCallback())
        {
            return m_callBack();
        }
        return S_OK;
    }

    // Return TRUE if this transfer specifies a callback function.
    inline BOOL hasCallback() const
    {
        return (m_callBack != nullptr);
    }

private:

    //
    // I2cTransferClass data members.
    //

    // Pointer to the next transfer in the queue (if any)
    I2cTransferClass* m_pNextXfr;

    // Pointer to buffer associated with this transfer.
    PUCHAR m_pBuffer;

    // Size of the buffer in bytes.
    ULONG m_bufBytes;

    // TRUE if this is a read transfer.
    BOOL m_isRead;

    // Index of next command/write location in buffer.
    ULONG m_nextCmd;

    // Index of next read location in buffer (unused for writes).
    ULONG m_nextRead;

    // TRUE when the last command byte has been fetched.
    BOOL m_lastCmdFetched;

    // TRUE to start transfer with a RESTART.
    BOOL m_preRestart;

    // Pointer to any callback function associated with this transfer.
    std::function<HRESULT()> m_callBack;
};


#endif // _I2C_TRANSFER_H_