// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _ERROR_CODES_H_
#define _ERROR_CODES_H_

#include <Windows.h>
#include <map>

//
// A map of DMap error codes and related messages.
//
extern std::map<HRESULT, LPCWSTR> DmapErrors;

//
// Locally defined error HRESULT codes.
// All these codes use FACILITY_ITF and an error Code value >0x200.  This guarentees
// they won't conflict with Windows HRESULTS for other facilities, or with COM HRESULTS.
//

//
// General error codes specific to the DMap support code.
//

//
// Board hardware related error codes.
// 

/// HexValue: 0x80049201
/// A pin is already locked for use for a function that conflicts with the use requested.
#define DMAP_E_PIN_FUNCTION_LOCKED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9201)

/// HexValue: 0x80049202
/// A pin number was specified that is beyond the range of pins supported by the board.
#define DMAP_E_PIN_NUMBER_TOO_LARGE_FOR_BOARD MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9202)

/// HexValue: 0x80049203
/// A function has been requested on a pin that does not support that function.
#define DMAP_E_FUNCTION_NOT_SUPPORTED_ON_PIN MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9203)

/// HexValue: 0x80049204
/// A pin direction was specified that was neither INPUT nor OUPUT.
#define DMAP_E_INVALID_PIN_DIRECTION MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9204)

/// HexValue: 0x80049205
/// An internal inconsistency in the DMap code has been found.
#define DMAP_E_DMAP_INTERNAL_ERROR MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9205)

/// HexValue: 0x80049206
/// A desited state for a pin was specified that was neither HIGH nor LOW.
#define DMAP_E_INVALID_PIN_STATE_SPECIFIED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9206)

/// HexValue: 0x80049207
/// The board type could not be determined.
#define DMAP_E_BOARD_TYPE_NOT_RECOGNIZED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9207)

/// HexValue: 0x80049208
/// An invalid board type was specified.
#define DMAP_E_INVALID_BOARD_TYPE_SPECIFIED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9208)

/// HexValue: 0x80049209
/// The port/bit specified does not exist on the device.
#define DMAP_E_INVALID_PORT_BIT_FOR_DEVICE MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9209)

//
// DMap driver related error codes.
//

/// HexValue: 0x80049210
/// An invalid handle was specified attempting to get a controller lock.
#define DMAP_E_INVALID_LOCK_HANDLE_SPECIFIED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9210)

/// HexValue: 0x80049211
/// An attempt was made to map more than the maximum number of devices.
#define DMAP_E_TOO_MANY_DEVICES_MAPPED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9211)

/// HexValue: 0x80049212
/// The specified device was not found on the systems.
#define DMAP_E_DEVICE_NOT_FOUND_ON_SYSTEM MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9212)

//
// I2C related error codes.
//

/// HexValue: 0x80049220
/// The specified I2C address is outside the legal range for 7-bit I2C addresses.
#define DMAP_E_I2C_ADDRESS_OUT_OF_RANGE MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9220)

/// HexValue: 0x80049221
/// No, or empty, write buffer was specified.
#define DMAP_E_I2C_NO_OR_EMPTY_WRITE_BUFFER MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9221)

/// HexValue: 0x80049222
/// No, or zero length, read buffer was specified.
#define DMAP_E_I2C_NO_OR_ZERO_LENGTH_READ_BUFFER MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9222)

/// HexValue: 0x80049223
/// No callback routine was specified to be queued.
#define DMAP_E_I2C_NO_CALLBACK_ROUTINE_SPECIFIED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9223)

/// HexValue: 0x80049224
/// More than 5 seconds elapsed waiting to acquire the I2C bus lock.
#define DMAP_E_I2C_BUS_LOCK_TIMEOUT MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9224)

/// HexValue: 0x80049225
/// Fewer than the expected number of bytes were received on the I2C bus.
#define DMAP_E_I2C_READ_INCOMPLETE MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9225)

/// HexValue: 0x80049226
/// More than the expected number of bytes were received on the I2C bus.
#define DMAP_E_I2C_EXTRA_DATA_RECEIVED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9226)

/// HexValue: 0x80049227
/// One or more transfers remained undone at the end of the I2C operation.
#define DMAP_E_I2C_OPERATION_INCOMPLETE MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9227)

/// HexValue: 0x80049228
/// The I2C bus specified does not exist.
#define DMAP_E_I2C_INVALID_BUS_NUMBER_SPECIFIED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9228)

/// HexValue: 0x80049229
/// The specified I2C transfer length is longer than the controller supports.
#define DMAP_E_I2C_TRANSFER_LENGTH_OVER_MAX MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9229)

//
// ADC related error codes.
//

/// HexValue: 0x80049230
/// ADC data for a different channel than requested was received.
/**
This error is likely to indicate that two or more threads or proccesses are
attempting to use the ADC at the same time.
*/
#define DMAP_E_ADC_DATA_FROM_WRONG_CHANNEL MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9230)

/// HexValue: 0x80049231
/// The ADC does not have the channel that has been requested.
#define DMAP_E_ADC_DOES_NOT_HAVE_REQUESTED_CHANNEL MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9231)

//
// SPI related error codes.
//

/// HexValue: 0x80049240
/// The width of data sent does not match the data width set on the SPI controller.
/**
The SPI data width must be set before begin() is called since the controller
data width can't be changed while the controller is running.  Call SPI.end(),
set the desired width, then call SPI.begin() to start the contrller again.
*/
#define DMAP_E_SPI_DATA_WIDTH_MISMATCH MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9240)

/// HexValue: 0x80049241
/// The specified BUS number does not exist on this board.
#define DMAP_E_SPI_BUS_REQUESTED_DOES_NOT_EXIST MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9241)

/// HexValue: 0x80049242
/// The SPI mode specified is not a legal SPI mode value (0-3).
#define DMAP_E_SPI_MODE_SPECIFIED_IS_INVALID MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9242)

/// HexValue: 0x80049243
/// The SPI speed specified is not in the supported range.
#define DMAP_E_SPI_SPEED_SPECIFIED_IS_INVALID MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9243)

/// HexValue: 0x80049244
/// This SPI implementation does not support buffer transfers.
#define DMAP_E_SPI_BUFFER_TRANSFER_NOT_IMPLEMENTED MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9244)

/// HexValue: 0x80049245
/// The specified number of bits per transfer is not supported by the SPI controller.
#define DMAP_E_SPI_DATA_WIDTH_SPECIFIED_IS_INVALID MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9245)

//
// PWM related error codes.
//

/// HexValue: 0x80049250
/// A GPIO operation was performed on a pin configured as a PWM output.
#define DMAP_E_GPIO_PIN_IS_SET_TO_PWM MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 0x9250)



#endif  // _ERROR_CODES_H_
