// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _GALILEO_PINS_H_
#define _GALILEO_PINS_H_

#include <Windows.h>

#include "ArduinoCommon.h"
#include "GpioController.h"
#include "PCAL9535ASupport.h"
#include "PCA9685Support.h"
#include "CY8C9540ASupport.h"
#include "ExpanderDefs.h"

// Pin function type values.
const UCHAR FUNC_NUL = 0x00;   ///< No function has been set
const UCHAR FUNC_DIO = 0x01;   ///< Digital I/O function
const UCHAR FUNC_PWM = 0x02;   ///< Pulse Width Modulation (PWM) function
const UCHAR FUNC_AIN = 0x04;   ///< Analog In function
const UCHAR FUNC_I2C = 0x08;   ///< I2C Bus function
const UCHAR FUNC_SPI = 0x10;   ///< SPI Bus function
const UCHAR FUNC_SER = 0x20;   ///< Hardware Serial function
const UCHAR FUNC_I2S = 0x40;   ///< Hardware I2S function
const UCHAR FUNC_SPK = 0X80;   ///< Hardware 8254 speaker function

/// The class used to configure and use GPIO pins.
class BoardPinsClass
{
public:
    BoardPinsClass();

    virtual ~BoardPinsClass()
    {
    }

    /// Struct for pin-specific attributes.
    /** This struct contains all the attributes needed to configure and use one
        of the I/O pins. */
    typedef struct {
        UCHAR gpioType;        ///< Fabric, Legacy Resume, Legacy Core, Expander, etc.
        UCHAR portBit;         ///< Which bit on the port is attached to this pin
        UCHAR pullupExp : 4;   ///< Number of I/O expander for pull-up control
        UCHAR pullupBit : 4;   ///< Bit of I/O expander for pull-up control
        UCHAR triStExp : 4;    ///< Number of I/O expander for tri-state control
        UCHAR triStBit : 4;    ///< Bit of I/O expander for tri-state control
        UCHAR muxA : 4;        ///< Number of first MUX for pin, if any
        UCHAR muxB : 4;        ///< Number of second MUX for pin, if any
        UCHAR digIoMuxA : 1;   ///< State of 1st MUX for digital I/O use of pin
        UCHAR digIoMuxB : 1;   ///< State of 2nd MUX for digital I/O use of pin
        UCHAR pwmMuxA : 1;     ///< State of 1st MUX for PWM use of pin
        UCHAR pwmMuxB : 1;     ///< State of 2nd MUX for PWM use of pin
        UCHAR anInMuxA : 1;    ///< State of 1st MUX for analog input use of pin
        UCHAR anInMuxB : 1;    ///< State of 2nd MUX for analog input use of pin
        UCHAR i2cMuxA : 1;     ///< State of 1st MUX for I2C use of pin
        UCHAR i2cMuxB : 1;     ///< State of 2nd MUX for I2C use of pin
        UCHAR spiMuxA : 1;     ///< State of 1st MUX for SPI use of pin
        UCHAR spiMuxB : 1;     ///< State of 2nd MUX for SPI use of pin
        UCHAR serMuxA : 1;     ///< State of 1st MUX for serial use of pin
        UCHAR serMuxB : 1;     ///< State of 2nd MUX for serial use of pin
        UCHAR i2SMux : 1;      ///< State of MUX for I2S use of pin
        UCHAR spkMux : 1;      ///< State of MUX for 8254 speaker use of pin
        UCHAR triStIn : 1;     ///< Tri-state control bit state for input pin
        UCHAR _pad : 1;        ///< Pad to byte boundary
        UCHAR funcMask;        ///< Mask of functin types supported on the pin
    } PORT_ATTRIBUTES, *PPORT_ATTRIBUTES;
    
    /// Struct for mux-specific attributes.
    /** This struct contains all the attributes needed to use a mux. */
    typedef struct {
        UCHAR selectExp;        ///< I/O Expander that drives the select signal
        UCHAR selectBit;        ///< Bit of I/O Expander that drives the select signal
    } MUX_ATTRIBUTES, *PMUX_ATTRIBUTES;

    /// Struct for I/O Expander-specific attributes.
    /** This struce stores all the attributes needed to access an I/O Expander. */
    typedef struct {
        UCHAR Exp_Type;         ///< I/O Expander chip type
        UCHAR I2c_Address;      ///< I2C address of the I/O expander
        UCHAR HighSpeed;        ///< 0 - use standard speed, 1 - allow high speed
        UCHAR padding;
    } EXP_ATTRIBUTES, *PEXP_ATTRIBUTES;

    /// Struct used to store function state for each pin.
    /**
    Struct used to track function configuration on a pin.  It holds the currently configured
    function and whether the function is allowed to be changed implicitely.
    */
    typedef struct {
        UCHAR currentFunction;  ///< Function the pin is currently configure for
        BOOLEAN locked;         ///< True locks the pin to current function, false allows change
    } PIN_FUNCTION, *PPIN_FUNCTION;

    /// Struct used to store PWM Channel information.
    /**
    This struct stores PWM Channel attributes for a GPIO pin.
    */
    typedef struct {
        UCHAR expander;         ///< I/O Expander that drives PWM for this pin
        UCHAR channel;          ///< PWM channel on the expander for PWM for this pin
        UCHAR portBit;          ///< Port/bit of I/O expander associated with this PWM channel
        UCHAR padding;
    } PWM_CHANNEL, *PPWM_CHANNEL;

    /// Enum of function lock actions.
    const enum FUNC_LOCK_ACTION {
        NO_LOCK_CHANGE,         ///< Don't take any lock action
        LOCK_FUNCTION,          ///< Lock the pin to this function
        UNLOCK_FUNCTION         ///< Unlock the pin function
    };

    /// Enum of board types.
    const enum BOARD_TYPE {
        NOT_SET,               ///< Indicates board type not yet set
        GALILEO_GEN1,          ///< Original Galileo board
        GALILEO_GEN2,          ///< Galileo Gen2 board
        MBM_BARE,              ///< MBM without Lure attached
        MBM_IKA_LURE,          ///< MBM with Ika Lure attached
        PI2_BARE               ///< PI2 without expansion attached
    };

    /// Method to set an I/O pin to a state (HIGH or LOW).
    HRESULT setPinState(ULONG pin, ULONG state);

    /// Method to read the state of an I/O pin.
    HRESULT getPinState(ULONG pin, ULONG & state);

    /// Method to set the direction of a pin (DIRECTION_IN or DIRECTION_OUT).
    HRESULT setPinMode(ULONG pin, ULONG mode, BOOL pullUp);

    /// Method to verify that a pin is configured for the desired function.
    HRESULT verifyPinFunction(ULONG pin, ULONG function, FUNC_LOCK_ACTION lockAction);

    /// Method to set the PWM duty cycle for a pin.
    HRESULT setPwmDutyCycle(ULONG pin, ULONG dutyCycle);

    /// Method to set the PWM pulse repetition frequency.
    HRESULT setPwmFrequency(ULONG pin, ULONG frequency);

    // Method to get the actual PWM pulse repetition frequncy that is set.
    ULONG getActualPwmFrequency(ULONG pin);

    /// Method to override auto-detection of board type.
    HRESULT setBoardType(BOARD_TYPE board);

    /// Method to get the board type.
    inline HRESULT getBoardType(BOARD_TYPE & board);

    /// Method to test whether a pin number is safe to use as an array index.
    inline BOOL pinNumberIsSafe(ULONG pin);

    /// Method to get the number of GPIO pins present on the current board
    inline HRESULT getGpioPinCount(ULONG & pinCount);

private:

    /// Pointer to the array of pin attributes.
    const PORT_ATTRIBUTES* m_PinAttributes;

    /// Pointer to the array of MUX attributes.
    const MUX_ATTRIBUTES* m_MuxAttributes;

    /// Pointer to the array of I/O Expander attributes.
    const EXP_ATTRIBUTES* m_ExpAttributes;

    /// Pointer to array of pin function tracking structures.
    const PPIN_FUNCTION m_PinFunctions;

    /// Pointer to array of PWM channels.
    const PWM_CHANNEL* m_PwmChannels;

    /// The number of GPIO pins present on the current board.
    ULONG m_GpioPinCount;

    /// The type of the board we are running on.
    BOARD_TYPE m_boardType;

    /// Method to configure an I/O Pin for one of the functions it suppports.
    HRESULT _setPinFunction(ULONG pin, ULONG function);

    /// Method to configure an I/O Pin for Digital I/O use.
    HRESULT _setPinDigitalIo(ULONG pin);

    /// Method to configure an I/O Pin for PWM use.
    HRESULT _setPinPwm(ULONG pin);

    /// Method to configure an I/O Pin for Analog Input use.
    HRESULT _setPinAnalogInput(ULONG pin);

    /// Method to configure an I/O Pin for I2C Bus use.
    HRESULT _setPinI2c(ULONG pin);

    /// Method to configure an I/O Pin for SPI Bus use.
    HRESULT _setPinSpi(ULONG pin);

    /// Method to configure an I/O Pin for Hardware Serial use.
    HRESULT _setPinHwSerial(ULONG pin);

    /// Method to choose between the input driver and output driver for an I/O pin.
    HRESULT _configurePinDrivers(ULONG pin, ULONG mode);

    /// Method to turn the pullup resistor on or off for an I/O pin.
    HRESULT _configurePinPullup(ULONG pin, BOOL pullUp);

    /// Method to set a mux to a desired state.
    HRESULT _setMux(ULONG pin, ULONG mux, ULONG selection);

    /// Method to set the direction on an I/O Expander port pin.
    HRESULT _setExpBitDirection(ULONG expNo, ULONG bitNo, ULONG directin, BOOL pullup);

    /// Method to set the state of an I/O Expander port pin.
    HRESULT _setExpBitToState(ULONG pin, ULONG expNo, ULONG bitNo, ULONG state);

    /// Method to verify the board type has been configured.
    HRESULT _verifyBoardType();

    /// Method to determine what type of board we are running on.
    HRESULT _determineBoardType();

    /// Method to determine the generation of a Galileo board.
    HRESULT _determineGalileoGen();

    /// Method to determine the configuration of an MBM board.
    HRESULT _determineMbmConfig();

    /// Method to determine the configuration of a PI2 board.
    HRESULT _determinePi2Config();

    /// Test an I2C address to see if a slave is present on it.
    HRESULT _testI2cAddress(ULONG i2cAdr);
};

/// Global object used to configure and use the I/O pins.
__declspec (selectany) BoardPinsClass g_pins;

/**
Method to get the number of GPIO pins present on the current board.
\param[out] pinCount the number of GPIO pins present.
\return HRESULT error or success code.
*/
inline HRESULT BoardPinsClass::getGpioPinCount(ULONG & pinCount)
{
    HRESULT hr = S_OK;

    if (m_boardType == NOT_SET)
    {
        hr = _determineBoardType();
    }

    pinCount = m_GpioPinCount;

    return hr;
}

/**
This method determines the board type if it is not yet known.
\param[out] gen The type of the board we are currently running on.
\return HRESULT error or success code.
*/
inline HRESULT BoardPinsClass::getBoardType(BOARD_TYPE & board)
{
    HRESULT hr = S_OK;

    hr = _verifyBoardType();

    if (SUCCEEDED(hr))
    {
        board = m_boardType;
    }

    return hr;
}

/**
Method to determine if a pin number is in the legal range or not.
\param[in] pin the pin number to check for range
\return TRUE if pin number is in range, FALSE otherwise
*/
inline BOOL BoardPinsClass::pinNumberIsSafe(ULONG pin)
{
    return (pin < m_GpioPinCount);
}

/**
Determine if the board generation has been determined yet, and if not, determine the
board generation and configure the code for that generation.
\return HRESULT error or success code.
*/
inline HRESULT BoardPinsClass::_verifyBoardType()
{
    if (m_boardType != NOT_SET)
    {
        return S_OK;
    }
    else
    {
        return _determineBoardType();
    }
}

#endif // _GALILEO_PINS_H_