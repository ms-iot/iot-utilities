// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _I2C_H_
#define _I2C_H_

#include <Windows.h>

#include "I2cController.h"


//
// Base class for classes used to interact with I2C Controller hardware.
//
class I2cClass
{
public:
    I2cClass(ULONG busNumber) :
        m_sdaPin(0xFFFFFFFF),
        m_sclPin(0xFFFFFFFF),
        m_busNumber(busNumber),
        m_controller(nullptr),
        m_refCount(0)
    {
        InitializeCriticalSection(&m_lock);
    }

    virtual ~I2cClass()
    {
        DeleteCriticalSection(&m_lock);
    }
    
    /// Prepare to use the I2C controller associated with this object.
    HRESULT begin();

    /// Finish using the I2C controller associated with this object.
    void end();

    /// Method to get the address of the I2C Controller object.
    inline I2cControllerClass* getController()
    {
        if (m_controller == nullptr)
        {
            begin();
        }
        return m_controller;
    }

protected:

    /// I2C Serial Data pin number.
    ULONG m_sdaPin;

    /// I2C Serial Clock pin number.
    ULONG m_sclPin;

private:

    //
    // I2cControllerClass private data members.
    //

    /// The bus number for the I2C Controller associated with this object.
    ULONG m_busNumber;

    /// The object we use to talk to the I2C Controller.
    I2cControllerClass* m_controller;

    /// Count of how many times this object is currently open by this process.
    LONG m_refCount;

    /// Lock used to serialize opening and closing the controller.
    RTL_CRITICAL_SECTION m_lock;
};

/// The global object for the main I2C bus.
__declspec(selectany) I2cClass g_i2c(EXTERNAL_I2C_BUS);

/// The global object for the secondary I2C bus.
__declspec(selectany) I2cClass g_i2c2nd(SECOND_EXTERNAL_I2C_BUS);

#endif // _I2C_H_