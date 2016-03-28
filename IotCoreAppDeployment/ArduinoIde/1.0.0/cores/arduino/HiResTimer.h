// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _HI_RES_TIMER_H_
#define _HI_RES_TIMER_H_

#include <Windows.h>

/// Class that is used to work with the system high resolution timer.
class HiResTimerClass
{
public:
    /// Constructor.
    HiResTimerClass()
    {
        QueryPerformanceFrequency(&m_frequency);
        m_startReading.QuadPart = 0;
        m_targetReading.QuadPart = 0;
    }

    /// Destructor.
    virtual ~HiResTimerClass()
    {
        m_frequency.QuadPart = 0;
    }

    /// Method to start a timeout.
    /**
    \param[in] microseconds The desired timeout time.
    */
    inline void StartTimeout(ULONG microseconds)
    {
        LARGE_INTEGER nowTime = { 0 };
        
        QueryPerformanceCounter(&nowTime);
        m_targetReading.QuadPart = (((LONGLONG)microseconds) * m_frequency.QuadPart) + 500000LL;
        m_targetReading.QuadPart = m_targetReading.QuadPart / 1000000LL;
        m_targetReading.QuadPart = m_targetReading.QuadPart + nowTime.QuadPart;
    }

    /// Method to determine whether the timeout has expired or not.
    /**
    This method returns TRUE if a previously started timeout interval is up.
    */
    inline BOOL TimeIsUp()
    {
        LARGE_INTEGER nowTime;
        QueryPerformanceCounter(&nowTime);
        return (nowTime.QuadPart >= m_targetReading.QuadPart);
    }

private:

    /// The high resolution timer frequencey on this system.
    LARGE_INTEGER m_frequency;

    /// The timer reading when the timer was started.
    LARGE_INTEGER m_startReading;

    /// The target timer reading.
    LARGE_INTEGER m_targetReading;
};

#endif  // _HI_RES_TIMER_H_