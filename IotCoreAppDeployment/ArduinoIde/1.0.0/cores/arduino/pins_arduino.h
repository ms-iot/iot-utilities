// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef _PINS_ARDUINO_H_
#define _PINS_ARDUINO_H_


#if defined(_M_IX86) || defined(_M_X64)

// Pin numbers mappings for MinnowBoard Max

const static uint8_t GPIO0 = 21;
const static uint8_t GPIO1 = 23;
const static uint8_t GPIO2 = 25;
const static uint8_t GPIO3 = 14;
const static uint8_t GPIO4 = 16;
const static uint8_t GPIO5 = 18;
const static uint8_t GPIO6 = 20;
const static uint8_t GPIO7 = 22;
const static uint8_t GPIO8 = 24;
const static uint8_t GPIO9 = 26;
const static uint8_t SCL = 13;
const static uint8_t SDA = 15;
const static uint8_t CS0 = 5;
const static uint8_t MISO = 7;
const static uint8_t MOSI = 9;
const static uint8_t SCLK = 11;
const static uint8_t CTS1 = 10;
const static uint8_t RTS1 = 12;
const static uint8_t RX1 = 8;
const static uint8_t TX1 = 6;
const static uint8_t RX2 = 19;
const static uint8_t TX2 = 17;

#elif defined (_M_ARM)

// Pin numbers mappings for Raspberry Pi2

const static uint8_t GPIO2 = 3;
const static uint8_t GPIO3 = 5;
const static uint8_t GPIO4 = 7;
const static uint8_t GPIO5 = 29;
const static uint8_t GPIO6 = 31;
const static uint8_t GPIO7 = 26;
const static uint8_t GPIO8 = 24;
const static uint8_t GPIO9 = 21;
const static uint8_t GPIO10 = 19;
const static uint8_t GPIO11 = 23;
const static uint8_t GPIO12 = 32;
const static uint8_t GPIO13 = 33;
const static uint8_t GPIO14 = 8;
const static uint8_t GPIO15 = 10;
const static uint8_t GPIO16 = 36;
const static uint8_t GPIO17 = 11;
const static uint8_t GPIO18 = 12;
const static uint8_t GPIO19 = 35;
const static uint8_t GPIO20 = 38;
const static uint8_t GPIO21 = 40;
const static uint8_t GPIO22 = 15;
const static uint8_t GPIO23 = 16;
const static uint8_t GPIO24 = 18;
const static uint8_t GPIO25 = 22;
const static uint8_t GPIO26 = 37;
const static uint8_t GPIO27 = 13;
const static uint8_t GPIO47 = 41;
const static uint8_t GCLK = 7;
const static uint8_t GEN0 = 11;
const static uint8_t GEN1 = 12;
const static uint8_t GEN2 = 13;
const static uint8_t GEN3 = 15;
const static uint8_t GEN4 = 16;
const static uint8_t GEN5 = 18;
const static uint8_t SCL1 = 5;
const static uint8_t SDA1 = 3;
const static uint8_t CS0 = 24;
const static uint8_t CS1 = 26;
const static uint8_t SCLK = 23;
const static uint8_t MISO = 21;
const static uint8_t MOSI = 19;
const static uint8_t RXD = 10;
const static uint8_t TXD = 8;

static const uint8_t LED_BUILTIN = 41;

#endif // defined (_M_ARM)

#endif // _PINS_ARDUINO_H_
