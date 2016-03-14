/** \file pulsein.h
 * Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
 * Licensed under the BSD 2-Clause License.  
 * See License.txt in the project root for license information.
 */

#pragma once

/// \brief Time the duration of the pulse in microseconds
/// \details Read a pulse on a pin(HIGH or LOW), and returns the duration of
/// the pulse in microseconds. Returns 0 if the timeout is exceeded while
/// waiting for a pulse.
/// \param [in] iPin Pin number to read the pulse
/// \param [in] iValue Pulse to read: HIGH or LOW
/// \param [in] [ulTimeout] Wait time, in microseconds, for pulse start; 1 second default
/// \return The duration of the pulse in microseconds
/// \see <a href="http://arduino.cc/en/Reference/PulseIn" target="_blank">origin: Arduino::pulseIn</a>
unsigned long pulseIn(int iPin, int iValue, unsigned long ulTimeout = 1000000UL);

/// \brief Calculates the duration between start and end time in microseconds
/// \param [in] ulStartTime Start time in microseconds
/// \param [in] ulEndTime End time in microsseconds
/// \return The duration in microseconds
/// \note Compensates for timer overflow.
unsigned long _Duration(unsigned long ulStartTime, unsigned long ulEndTime);
