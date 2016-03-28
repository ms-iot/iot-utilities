/** \file ArduinoError.h
 * Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
 * Licensed under the BSD 2-Clause License.
 * See License.txt in the project root for license information.
 */

#ifndef ARDUINO_ERROR_H
#define ARDUINO_ERROR_H

#include <stdexcept>
#include <strsafe.h>
#include <ErrorCodes.h>

/// \brief The error class for the Arduino
/// \details The Arduino platform does not handle exceptions. This class is
/// necessary to bubble out exceptions generated from the C++ implementation
class _arduino_fatal_error : public std::runtime_error
{
public:
    typedef std::runtime_error _Mybase;

    /// \brief The error constructor
    /// \param [in] message A contextual message used to help diagnose the error
    explicit _arduino_fatal_error(const char *_Message)
        : _Mybase(_Message)
    {    // construct from message string
    }
};

/// \brief An exception used to exit the Arduino loop
/// \details Typical Arduino sketches loop infinitely, even when errors
/// occur. Such behavior no longer makes sense on an operating system
/// and therefore the ability to exit is provided.
class _arduino_quit_exception : public std::exception { };

/// \brief A function provided to allow the user to exit the Arduino sketch
/// \note This is the preferred method to exit a sketch, because it allows
/// for stack unwinding and deconstructors to to be called.
inline void _exit_arduino_loop()
{
    throw _arduino_quit_exception();
}

/// \brief A wrapper function for _arduino_fatal_error
/// \details Allows a formatted string to be provided, then built and passed
/// along to the _arduino_fatal_error class.
/// \param [in] hr HRESULT of the error
/// \param [in] pszFormat If format includes format specifiers (subsequences
/// beginning with %), the additional arguments following format are formatted
/// and inserted in the resulting string replacing their respective specifiers.
inline void ThrowError(_In_ HRESULT hr, _In_ _Printf_format_string_ STRSAFE_LPCSTR pszFormat, ...)
{

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
    HRESULT hr;
    char buf[BUFSIZ];

    va_list argList;

    va_start(argList, pszFormat);

    hr = StringCbVPrintfA(buf,
        ARRAYSIZE(buf),
        pszFormat,
        argList);

    va_end(argList);

    if (SUCCEEDED(hr))
    {
        throw _arduino_fatal_error(buf);
    }
    else
    {
        throw _arduino_fatal_error("StringCbVPrintfA() failed while attempting to print exception message");
    }
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#if !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a UWP app:
    int result;
    char buf[BUFSIZ];
    wchar_t wbuf[BUFSIZ];
    va_list argList;
    va_start(argList, pszFormat);
    result = vsprintf_s(buf, BUFSIZ, pszFormat, argList);

    va_end(argList);

    if (result > 0)
    {
        MultiByteToWideChar(CP_ACP, 0, buf, _countof(buf), wbuf, _countof(wbuf));
        auto it = DmapErrors.find(hr);
        Platform::String^ exceptionString;
        if (it != DmapErrors.end())
        {
            std::wstring exceptionSz(wbuf);
            exceptionSz.append(L": ");
            exceptionSz.append(it->second);
            exceptionString = ref new Platform::String(exceptionSz.c_str());
        }
        else
        {
            exceptionString = ref new Platform::String(wbuf);
        }
        throw ref new Platform::Exception(hr, exceptionString);
    }
    else
    {
        throw ref new Platform::Exception(hr);
    }
#endif // !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

}

#if !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a UWP app:
inline void ThrowError(_In_ HRESULT hr, _In_ _Printf_format_string_ STRSAFE_LPCWSTR pszFormat, ...)
{
    int result;
    wchar_t wbuf[BUFSIZ];
    va_list argList;
    va_start(argList, pszFormat);
    result = vswprintf_s(wbuf, BUFSIZ, pszFormat, argList);

    va_end(argList);

    if (result > 0)
    {
        auto it = DmapErrors.find(hr);
        Platform::String^ exceptionString;
        if (it != DmapErrors.end())
        {
            std::wstring exceptionSz(wbuf);
            exceptionSz.append(L"\n");
            exceptionSz.append(it->second);
            exceptionString = ref new Platform::String(exceptionSz.c_str());
        }
        else
        {
            exceptionString = ref new Platform::String(wbuf);
        }
        throw ref new Platform::Exception(hr, exceptionString);
    }
    else
    {
        throw ref new Platform::Exception(hr);
    }
}
#endif // !WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

#endif
