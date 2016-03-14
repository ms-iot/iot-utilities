// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _ARDUINO_COMMON_H_
#define _ARDUINO_COMMON_H_

#include <cstdint>

const UCHAR LOW = 0x00;
const UCHAR HIGH = 0x01;

const UCHAR DIRECTION_IN = 0x00;
const UCHAR DIRECTION_OUT = 0x01;
const UCHAR INPUT_PULLUP = 0x02;

// INPUT pin mode
#define INPUT DIRECTION_IN

// OUTPUT pin mode
#define OUTPUT DIRECTION_OUT

const UCHAR LSBFIRST = 0x00;
const UCHAR MSBFIRST = 0x01;

const UCHAR NUM_ARDUINO_PINS = 20;

const UCHAR CHANGE = 0x01;
const UCHAR FALLING = 0x02;
const UCHAR RISING = 0x03;

// Pin name to number mapping.
const UCHAR D0 = 0;
const UCHAR D1 = 1;
const UCHAR D2 = 2;
const UCHAR D3 = 3;
const UCHAR D4 = 4;
const UCHAR D5 = 5;
const UCHAR D6 = 6;
const UCHAR D7 = 7;
const UCHAR D8 = 8;
const UCHAR D9 = 9;
const UCHAR D10 = 10;
const UCHAR D11 = 11;
const UCHAR D12 = 12;
const UCHAR D13 = 13;

const UCHAR A0 = 14;
const UCHAR A1 = 15;
const UCHAR A2 = 16;
const UCHAR A3 = 17;
const UCHAR A4 = 18;
const UCHAR A5 = 19;
const UCHAR A6 = 20;
const UCHAR A7 = 21;

// Pseudo pin numbers used PWM "pins" on off-board PWM chips.
const UCHAR PWM0 = 50;
const UCHAR PWM1 = 51;
const UCHAR PWM2 = 52;
const UCHAR PWM3 = 53;
const UCHAR PWM4 = 54;
const UCHAR PWM5 = 55;
const UCHAR PWM6 = 56;
const UCHAR PWM7 = 57;
const UCHAR PWM8 = 58;
const UCHAR PWM9 = 59;
const UCHAR PWM10 = 60;
const UCHAR PWM11 = 61;
const UCHAR PWM12 = 62;
const UCHAR PWM13 = 63;
const UCHAR PWM14 = 64;
const UCHAR PWM15 = 65;

// SPI signal to pin mapping for Arduino compatible pin configuration.
const UCHAR ARDUINO_PIN_MOSI = D11;     ///< Pin used for Galileo SPI MOSI signal
const UCHAR ARDUINO_PIN_MISO = D12;     ///< Pin used for Galileo SPI MISO signal
const UCHAR ARDUINO_PIN_SCK = D13;      ///< Pin used for Galileo SPI Clock signal

// SPI signal to pin mapping for bare MBM.
const UCHAR MBM_PIN_MISO = 7;           ///< Pin used for MBM SPI MISO signal
const UCHAR MBM_PIN_MOSI = 9;           ///< Pin used for MBM SPI MOSI signal
const UCHAR MBM_PIN_SCK = 11;           ///< Pin used for MBM SPI Clock signal
const UCHAR MBM_PIN_CS0 = 5;            ///< Pin used for MBM SPI Chip select

// SPI signal to pin mapping for bare PI2 SPI0.
const UCHAR PI2_PIN_SPI0_MISO = 21;     ///< Pin used for PI2 SPI0 MISO signal
const UCHAR PI2_PIN_SPI0_MOSI = 19;     ///< Pin used for PI2 SPI0 MOSI signal
const UCHAR PI2_PIN_SPI0_SCK = 23;      ///< Pin used for PI2 SPI0 Clock signal
const UCHAR PI2_PIN_SPI0_CS0 = 24;      ///< Pin used for PI2 SPI0 Chip select 0
const UCHAR PI2_PIN_SPI0_CS1 = 26;      ///< Pin used for PI2 SPI0 Chip select 1

// SPI signal to pin mapping for bare PI2 SPI1.
const UCHAR PI2_PIN_SPI1_MISO = 35;     ///< Pin used for PI2 SPI1 MISO signal
const UCHAR PI2_PIN_SPI1_MOSI = 38;     ///< Pin used for PI2 SPI1 MOSI signal
const UCHAR PI2_PIN_SPI1_SCK = 40;      ///< Pin used for PI2 SPI1 Clock signal

// I2C signal to pin mapping for Arduino pin configuration.
const UCHAR ARDUINO_PIN_I2C_DAT = A4;   ///< Pin used for Arduino I2C Data signal
const UCHAR ARDUINO_PIN_I2C_CLK = A5;   ///< Pin used for Arduino I2C Clock signal

// I2C signal to pin mapping for MBM 26-pin connector.
const UCHAR BARE_MBM_PIN_I2C_DAT = 15;  ///< Pin used for MBM I2C Data signal
const UCHAR BARE_MBM_PIN_I2C_CLK = 13;  ///< Pin used for MBM I2C Clock signal

// I2C signal to pin mapping for PI2 I2C1 40-pin connector.
const UCHAR BARE_PI2_PIN_I2C1_DAT = 3;  ///< Pin used for PI2 I2C1 Data signal
const UCHAR BARE_PI2_PIN_I2C1_CLK = 5;  ///< Pin used for PI2 I2C1 Clock signal

// I2C signal to pin mapping for PI2 I2C0 40-pin connector.
const UCHAR BARE_PI2_PIN_I2C0_DAT = 27; ///< Pin used for PI2 I2C0 Data signal
const UCHAR BARE_PI2_PIN_I2C0_CLK = 28; ///< Pin used for PI2 I2C0 Clock signal

#endif  // _ARDUINO_COMMON_H_