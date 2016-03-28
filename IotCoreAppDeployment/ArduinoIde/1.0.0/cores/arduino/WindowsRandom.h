/** \file windowsrandom.h
 * Copyright (c) Microsoft Open Technologies, Inc.  All rights reserved.  
 * Licensed under the BSD 2-Clause License.
 * See License.txt in the project root for license information.
 */

#ifndef WINDOWS_RANDOM_H
#define WINDOWS_RANDOM_H

#include <random>

/// \brief Helper class to implement the Arduino random functions on Windows
class WindowsRandom
{
private:
	std::mt19937 Engine;
	std::uniform_int_distribution<int> Distribution;

public:
	WindowsRandom()
	{
	}

    /// \brief Set seed value for random engine
    /// \param [in] seed Engine seed value
	void Seed(int seed)
	{
		Engine.seed(seed);
	}

    /// \brief Returns the next random number from the engine
	long Next()
	{
		return Distribution(Engine);
	}
};

__declspec(selectany) WindowsRandom _WindowsRandom;

#endif
