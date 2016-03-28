// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef PGM_SPACE_H
#define PGM_SPACE_H

#include <cstdint>
#include <cstring>

/*
 * The avr/pgmspace.h file providies a set of macros
 * necessary for accessing program memory space in the
 * Arduino (in AVR chips program memory is located in
 * a different physical location than the data memory).
 * Here, we have a flat memory space and we are
 * stripping out these macros to allow for an easy
 * conversion of Arduino specific code.
 */

typedef const char* PGM_P;
typedef const void* PGM_VOID_P;

inline uint8_t pgm_read_byte(PGM_P address)
{
	return *reinterpret_cast<uint8_t *>(const_cast<char*>(address));
}

inline uint16_t pgm_read_word(PGM_P address)
{
	return *reinterpret_cast<uint16_t *>(const_cast<char*>(address));
}
inline uint32_t pgm_read_dword(PGM_P address)
{
	return *reinterpret_cast<uint32_t *>(const_cast<char*>(address));	
} 

inline float pgm_read_float(PGM_P address)
{
	return *reinterpret_cast<float *>(const_cast<char*>(address));
}

inline uint8_t pgm_read_byte(PGM_VOID_P address)
{
	return *const_cast<uint8_t*> (reinterpret_cast<const uint8_t *>(address));
}

inline uint16_t pgm_read_word(PGM_VOID_P address)
{
	return *const_cast<uint16_t*> (reinterpret_cast<const uint16_t *>(address));
}
inline uint32_t pgm_read_dword(PGM_VOID_P address)
{
	return *const_cast<uint32_t*> (reinterpret_cast<const uint32_t *>(address));	
} 

inline float pgm_read_float(PGM_VOID_P address)
{
	return *const_cast<float*> (reinterpret_cast<const float *>(address));
}

#define pgm_read_byte_near(address) pgm_read_byte(address)
#define pgm_read_word_near(address) pgm_read_word(address)
#define pgm_read_dword_near(address) pgm_read_dword(address)
#define pgm_read_float_near(address) pgm_read_float(address)

#define pgm_read_byte_far(address) pgm_read_byte(address)
#define pgm_read_word_far(address) pgm_read_word(address)
#define pgm_read_dword_far(address) pgm_read_dword(address)
#define pgm_read_float_far(address) pgm_read_float(address)


// PGM versions of string functions can use regular string functions on Windows
inline char* strcpy_P(char* buffer, PGM_P from)
{
#pragma warning(disable : 4996)
	return strcpy(buffer, from);
} 

inline size_t strlen_P(PGM_P str) 
{
#pragma warning(disable : 4996)
	return strlen(str);
}

// Other APIs for conversions
#define utoa _ultoa
char* dtostrf(double value, char width, uint8_t precision, char* buffer);

#endif
