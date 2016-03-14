// Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
// Licensed under the BSD 2-Clause License.  
// See License.txt in the project root for license information.

#define MIN_PULSE_WIDTH 544
#define MAX_PULSE_WIDTH 2400
#define DEFAULT_PULSE_WIDTH 1500
#define REFRESH_INTERVAL 20000 // 20 ms

// Use a pulse rate of 50 pulses per second to drive servos
#define SERVO_FREQUENCY_HZ 50

class Servo
{
private:
    int _attachedPin;
    int _min;
    int _max;
    ULONG _currentPulseMicroseconds;
    ULONG _actualPeriodMicroseconds;

public:
    Servo();
    void attach(int pin);
    void attach(int pin, int min, int max);
    void detach();
    void write(int angle);
    void writeMicroseconds(int value);
    int read();
    inline ULONG readMicroseconds();
    inline bool attached();

};

/// Determine if attach() has been called successfully for this servo object.
/**
\return True if this object is attached, False otherwise.
*/
inline bool Servo::attached()
{
    return (_attachedPin != -1);
}

/// Get the currently set pulse width in microseconds.
/// \return the last pulse width that was set, in microseconds.
inline ULONG Servo::readMicroseconds()
{
    return _currentPulseMicroseconds;
}

