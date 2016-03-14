// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#ifndef EEPROM_H
#define EEPROM_H

#include <inttypes.h> 

/// \brief A pseudo static class to support EEPROM usage
/// \details EEPROM is a type of non-volatile memory used in computers
/// and other electronic devices to store small amounts of data that
/// must be saved when power is removed (e.g. calibration tables or
/// device configuration). This class allows you to read and write from
/// this memory.
class EEPROMClass
{
  public:
    /// \brief Reads a byte from the EEPROM.
    /// \param [in] address The location to read from, starting from 0 (int)
    /// \returns the value stored in that location (byte)
    /// \note Locations that have never been written to have the value of 255.
    uint8_t
    read (
        const int address
    ) const;

    /// \brief Write a byte to the EEPROM.
    /// \param [in] address The location to write to, starting from 0 (int)
    /// \param [in] value The value to write, from 0 to 255 (byte)
    /// \note An EEPROM write takes 3.3 ms to complete. The EEPROM memory has
    /// a specified life of 100,000 write/erase cycles, so you may need to be
    /// careful about how often you write to it.
    void
    write (
        const int address,
        const uint8_t value
    ) const;
};

extern EEPROMClass EEPROM;  ///< This variable will provide global access to
                            /// this pseudo-static class, and will be instantiated
                            /// in the .cpp file.

#endif
