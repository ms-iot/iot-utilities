/** \file windowstime.h
 * Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.
 * Licensed under the BSD 2-Clause License.
 * See License.txt in the project root for license information.
 */

#ifndef WINDOWS_TIME_H
#define WINDOWS_TIME_H

#include <Windows.h>

/// \brief Helper class to implement the Arduino time functions on Windows
class WindowsTime {

	const   DWORD SLEEP_THRESHOLD_MS = 17;
	const   DWORD SLEEP_MARGIN_MS = 16;
	const   DWORD SLEEP_MAX_MS = 0x3fffffff;

	LARGE_INTEGER qpFrequency;
	LARGE_INTEGER qpStartCount;

	DWORD qpcDiffToMS(LARGE_INTEGER& qpc, LARGE_INTEGER& qpcStop)
	{
		LARGE_INTEGER qpcDiff;
		qpcDiff.QuadPart = qpcStop.QuadPart - qpc.QuadPart;
		if (qpcDiff.QuadPart > 0)
		{
			qpcDiff.QuadPart = qpcDiff.QuadPart * 1000 / qpFrequency.QuadPart;
			if (qpcDiff.QuadPart > SLEEP_MAX_MS)
			{
				return SLEEP_MAX_MS;
			}
			else
			{
				return qpcDiff.LowPart;
			}
		}
		return 0;
	}

    void operator= (WindowsTime &wt_) {
        UNREFERENCED_PARAMETER(wt_);
    }

public:

	WindowsTime()
	{
		QueryPerformanceFrequency(&qpFrequency);
		QueryPerformanceCounter(&qpStartCount);
	}

    /// \brief Pauses the program for the amount of time (in miliseconds) 
    /// specified as parameter.
    /// \details There are 1000 milliseconds in a second.
    /// \param [in] ms The number of milliseconds to pause
    /// \note The windows timer ticks every 16ms, for shorter precise
    /// delays the thread must spin
    /// \see <a href="http://arduino.cc/en/Reference/Delay" target="_blank">origin: Arduino::delay</a>
    void delay(unsigned long ms)
	{
		LARGE_INTEGER us;
		us.QuadPart = ms;
		us.QuadPart *= 1000;
		delayMicroseconds(us);
	}

    /// \brief Pauses the program for the amount of time (in microseconds) 
    /// specified as parameter.
    /// \details There are a thousand microseconds in a millisecond, and
    /// \param [in] us The number of microseconds to pause
    /// a million microseconds in a second.
    /// \see <a href="http://arduino.cc/en/Reference/DelayMicroseconds" target="_blank">origin: Arduino::delayMicroseconds</a>
    void delayMicroseconds(LARGE_INTEGER& us)
	{
		LARGE_INTEGER qpc, qpcStop;

		QueryPerformanceCounter(&qpcStop);
		qpc = qpcStop;
		qpcStop.QuadPart += us.QuadPart * qpFrequency.QuadPart / 1000000;

		// use windows Sleep for lengthy wait
		DWORD delayMS = qpcDiffToMS(qpc, qpcStop);
		while (delayMS >= SLEEP_THRESHOLD_MS)
		{
			Sleep(delayMS - SLEEP_MARGIN_MS);

			QueryPerformanceCounter(&qpc);

			delayMS = qpcDiffToMS(qpc, qpcStop);
		}

		// spin remaining time, for µs accurate timing
		do {
			QueryPerformanceCounter(&qpc);
		} while (qpcStop.QuadPart > qpc.QuadPart);

	}

    /// \brief Retrieves the number of milliseconds since the currently running program started.
    /// \returns Number of milliseconds since the program started.
    /// \warning This number will overflow (go back to zero), after approximately 50 days.
    /// \see <a href="http://arduino.cc/en/Reference/Millis" target="_blank">origin: Arduino::millis</a>
    unsigned long millis(void)
	{
		LARGE_INTEGER qpc;
		QueryPerformanceCounter(&qpc);
		qpc.QuadPart -= qpStartCount.QuadPart;
		return (unsigned long)(qpc.QuadPart * 1000 / qpFrequency.QuadPart);
	}

    /// \brief Retrieves the number of microseconds since the currently running program started. 
    /// \returns Number of microseconds since the program started.
    /// \warning This number will overflow (go back to zero), after approximately 70 minutes.
    /// \see <a href="http://arduino.cc/en/Reference/Micros" target="_blank">origin: Arduino::micros</a>
    unsigned long micros(void)
	{
		LARGE_INTEGER qpc;
		QueryPerformanceCounter(&qpc);
		qpc.QuadPart -= qpStartCount.QuadPart;
		return (unsigned long)(qpc.QuadPart * 1000000 / qpFrequency.QuadPart);
	}
};

__declspec(selectany) WindowsTime _WindowsTime;

#endif
