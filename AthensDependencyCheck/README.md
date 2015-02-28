###AthensDependencyCheck

This tool is there to assist you in migrating your current Win32 applications and dlls to Windows "Athens".

##Usage

The tool can be run by running `AthensDependencyCheck.exe <path> [-os]`. The `path` is the path to the directory of where your exe and dll files are located. By default, the app validates them against the Windows UAP platform, but if you are not looking to use UAP include the `-os` argument.

##Output

The results will be outputted to a file named `<input-filename>.csv` and a summary will be on the command line.