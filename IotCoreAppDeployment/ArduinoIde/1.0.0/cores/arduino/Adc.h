// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _ADC_H_
#define _ADC_H_

#include <Windows.h>
#include "ADC108S102Support.h"
#include "AD7298Support.h"
#include "ADS1015Support.h"
#include "MCP3008support.h"

class AdcClass
{
public:
    /// Constructor.
    AdcClass()
    {
        m_boardType = BoardPinsClass::BOARD_TYPE::NOT_SET;
    }

    /// Destructor.
    virtual ~AdcClass()
    {
        if (m_boardType == BoardPinsClass::BOARD_TYPE::MBM_IKA_LURE)
        {
            m_ikaLureAdc.end();
        }
        else if (m_boardType == BoardPinsClass::BOARD_TYPE::MBM_BARE)
        {
            m_addOnAdc.end();
        }
        else if (m_boardType == BoardPinsClass::BOARD_TYPE::PI2_BARE)
        {
            m_addOnAdc.end();
        }
#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
        else if (m_boardType == BoardPinsClass::BOARD_TYPE::GALILEO_GEN2)
        {
            m_gen2Adc.end();
        }
        else if (m_boardType == BoardPinsClass::BOARD_TYPE::GALILEO_GEN1)
        {
            m_gen1Adc.end();
        }
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
    }

    /// Take a reading with the ADC on the board.
    /**
    This method assumes the pin number passed in has been verified to be within 
    the range of analog inputs.
    \param[in] pin Number of GPIO pin to read with the ADC.
    \param[out] value The value read from the ADC.
    \param[out] bits The size of the reading in "value" in bits.
    \return HRESULT success or error code.
    */
    inline HRESULT readValue(ULONG pin, ULONG & value, ULONG & bits)
    {
        HRESULT hr = S_OK;
        
        ULONG channel;


        // Translate the pin number passed in to analog channel 0-5.
        // This calculation is based on the fact that all the ADCs we work with
        // have A0-An mapped to channels 0-n on the ADC.
        channel = pin - A0;

        // Verify we have initialized the correct ADC.
        hr = _verifyAdcInitialized();
 
        if (SUCCEEDED(hr))
        {
            if (m_boardType == BoardPinsClass::BOARD_TYPE::MBM_IKA_LURE)
            {
                hr = m_ikaLureAdc.readValue(channel, value, bits);
            }
            else if (m_boardType == BoardPinsClass::BOARD_TYPE::MBM_BARE)
            {
                hr = m_addOnAdc.readValue(channel, value, bits);
            }
            else if (m_boardType == BoardPinsClass::BOARD_TYPE::PI2_BARE)
            {
                hr = m_addOnAdc.readValue(channel, value, bits);
            }
#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
            else if (m_boardType == BoardPinsClass::BOARD_TYPE::GALILEO_GEN2)
            {
                hr = m_gen2Adc.readValue(channel, value, bits);
            }
            else if (m_boardType == BoardPinsClass::BOARD_TYPE::GALILEO_GEN1)
            {
                hr = m_gen1Adc.readValue(channel, value, bits);
            }
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
        }
        
        return hr;
    }

private:

    /// The board type for which this object has been initialized.
    BoardPinsClass::BOARD_TYPE m_boardType;

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
    /// Gen2 ADC device.
    ADC108S102Device m_gen2Adc;

    /// Gen1 ADC device.
    AD7298Device m_gen1Adc;
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)

    ADS1015Device m_ikaLureAdc;

    MCP3008Device m_addOnAdc;

    /// Initialize this object if it has not already been done.
    inline HRESULT _verifyAdcInitialized()
    {
        HRESULT hr = S_OK;
        

        // If the ADC has not yet been initialized:
        if (m_boardType == BoardPinsClass::BOARD_TYPE::NOT_SET)
        {
            // Get the board generation.
            hr = g_pins.getBoardType(m_boardType);
            
            if (SUCCEEDED(hr))
            {
                if (m_boardType == BoardPinsClass::BOARD_TYPE::MBM_IKA_LURE)
                {
                    hr = m_ikaLureAdc.begin();
                }
                else if (m_boardType == BoardPinsClass::BOARD_TYPE::MBM_BARE)
                {
                    hr = m_addOnAdc.begin();
                }
                else if (m_boardType == BoardPinsClass::BOARD_TYPE::PI2_BARE)
                {
                    hr = m_addOnAdc.begin();
                }
#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)   // If building a Win32 app:
                else if (m_boardType == BoardPinsClass::BOARD_TYPE::GALILEO_GEN2)
                {
                    hr = m_gen2Adc.begin();

                }
                else if (m_boardType == BoardPinsClass::BOARD_TYPE::GALILEO_GEN1)
                {
                    hr = m_gen1Adc.begin();

                }
#endif // WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
                else
                {
                    // If we have an unrecognized board or one that does not support ADC,
                    // indicate ADC is uninitialized.
                    m_boardType = BoardPinsClass::BOARD_TYPE::NOT_SET;
                }
            }
        }

        
        return hr;
    }

};

/// Global object used to access the A-to-D converter.
__declspec (selectany) AdcClass g_adc;

#endif  // _ADC_H_