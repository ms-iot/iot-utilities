// ProcessLauncherWhitelist.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <string>
#include <vector>
#include <algorithm>
#include <stdio.h>
#include <wchar.h>
#include <conio.h>

using namespace std;

void Banner();
void Usage();
void GetWhitelist();
void ShowMenu();
int GetMenuSelection();
int BuildFolderList(wstring InitialFolder);
void DeleteItem();

// number of items in the menu
#define MAX_MENU_ITEMS 7

wchar_t * wcKey = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\EmbeddedMode\\ProcessLauncher";
wchar_t * wcValue = L"AllowedExecutableFilesList";

// Menu stuff.
void DisplayWhitelist();
void SearchFolderList();
bool FileExists(wstring wsFilename);
bool AddToExeListQuestion(wstring wsFilename);
void BuildFilesInFolder(wstring wsPath, wstring wsFilename);
bool IsExeName(wstring fullString);
void WriteRegFile();
void AddExeToList();
void WriteWhiteListToRegistry();

vector<wstring> Whitelist;
vector<wstring> FolderList;
wstring wsRoot(L"\\");

// FoundFiles is temporarily used to build a list of found files from a search criteria.
vector<wstring> FoundFiles;

int _tmain(int argc, _TCHAR* argv[])
{
	Banner();

	if (1 != argc)
	{
		Usage();
		return -1;
	}

	wprintf(L"Walking the device folder list, standby...");
	BuildFolderList(wsRoot);
	wprintf(L"\n");

	// read the existing whitelist (if it exists) from the registry
	GetWhitelist();

	bool bInMenu = true;

	while (bInMenu)
	{
		ShowMenu();
		int iMenuSelection = GetMenuSelection();	// returns 'false' if "9" exit has been selected.
		if (-1 == iMenuSelection)
		{
			bInMenu = false;
			break;
		}

		switch (iMenuSelection)
		{
		case 1:
			DisplayWhitelist();
			break;
		case 2:
			AddExeToList();
			break;
		case 3:
			SearchFolderList();
			break;
		case 4:
			DeleteItem();
			break;
		case 5:
			Whitelist.clear();
			break;
		case 6:
			WriteRegFile();
			break;
		case 7:
			WriteWhiteListToRegistry();
			break;
		default:
			break;
		}

	}
	
	return 0;
}

void ShowMenu()
{
	wprintf(L"\n");
	wprintf(L"1) Display List (%ld items in the list)\n", Whitelist.size());
	wprintf(L"2) Manually add to the list\n");
	wprintf(L"3) Search the o/s to add an item to the list\n");
	wprintf(L"4) Delete an item from the list\n");
	wprintf(L"5) Clear the list\n");
	wprintf(L"6) Create .REG file for Whitelist\n");
	wprintf(L"7) Save Whitelist to registry\n");
	wprintf(L"9) Exit\n");
}

void SearchFolderList()
{
	wchar_t wcFilename[FILENAME_MAX] = { 0 };
	bool bFound = false;
	int iCount = 0;

	FoundFiles.clear();

	wprintf(L"Search for (filename or wildcard) >");
	if (NULL != _getws_s(wcFilename, FILENAME_MAX))
	{
		wstring wsSearchName(wcFilename);
		for (unsigned int x = 0;x < FolderList.size();x++)
		{
			wstring wsFile(wcFilename);
			BuildFilesInFolder(FolderList[x], wsFile);
		}

		if (FoundFiles.size() == 0)
		{
			wprintf(L"No files found\n");
			return;
		}

		if (FoundFiles.size() == 1)
		{
			bool bAdd=AddToExeListQuestion(FoundFiles[0]);
			if (bAdd)
			{
				Whitelist.push_back(FoundFiles[0]);
			}
		}
		else
		{
			if (FoundFiles.size() > 10)
			{
				wprintf(L"More than 10 items match your search criteria, can you refine the search?\n");
			}
			else
			{
				wprintf(L"Choose one of the following:\n");
				for (unsigned int x = 0;x < FoundFiles.size();x++)
				{
					wprintf(L"%02d) %s\n", x + 1, FoundFiles[x].c_str());
				}
				wprintf(L" A) Add ALL of the list above to the whitelist\n");
				wprintf(L"\n");

				// get the option from the user.
				wchar_t wcOption[20] = { 0 };
				while (true)
				{
					wprintf(L"Choose an item or 'a'll>");
					if (NULL != _getws_s(wcOption, 20))
					{
						if (wcslen(wcOption) == 0)
							break;

						if (wcslen(wcOption) == 1 && (wcOption[0] == 'a' || wcOption[0] == 'A'))
						{
							for (unsigned int x = 0;x < FoundFiles.size();x++)
							{
								Whitelist.push_back(FoundFiles[x]);
							}
							break;
						}

						unsigned int iOption = _wtoi(wcOption);
						if (iOption > 0 && iOption <= FoundFiles.size())
						{
							Whitelist.push_back(FoundFiles[iOption - 1]);
							break;
						}
						else
						{
							wprintf(L"You need to select a number from 1 to %d, or 'a' for all\n", FoundFiles.size());
						}
					}
				}
			}
		}
	}
}

bool FileExists(wstring wsFilename)
{
	WIN32_FIND_DATA fd = { 0 };

	HANDLE hFile = FindFirstFile(wsFilename.c_str(), &fd);
	if (INVALID_HANDLE_VALUE == hFile)
		return false;

	CloseHandle(hFile);
	return true;
}

void Banner()
{
	wprintf(L"Process Launcher Whitelist Configuration\n");
	wprintf(L"\n");
}

void Usage()
{
	wprintf(L"Usage: <app>\n");
	wprintf(L"No command line options here\n");
	wprintf(L"All configuration happens in the app!\n\n");
}

void GetWhitelist()
{
	HKEY hKey = NULL;

	if (ERROR_SUCCESS != RegOpenKeyEx(HKEY_LOCAL_MACHINE,
		wcKey,
		0,
		KEY_QUERY_VALUE,
		&hKey))
	{
		return;	// Key doesn't exist, so we don't have a list.
	}

	// get the size.
	DWORD dwSize = 0;
	if (ERROR_SUCCESS != RegQueryValueEx(hKey, wcValue, NULL, NULL, NULL,&dwSize))
	{
		RegCloseKey(hKey);
		return;
	}

	// allocate the buffer
	wchar_t *lpValues = (wchar_t*)malloc(dwSize);
	ZeroMemory(lpValues, dwSize);
	// store the pointer to the allocated memory.

	if (NULL == lpValues)
	{
		RegCloseKey(hKey);
		return;
	}

	// get the values.
	if (ERROR_SUCCESS != RegQueryValueEx(hKey, wcValue, NULL, NULL, (LPBYTE)lpValues, &dwSize))
	{
		RegCloseKey(hKey);
		free(lpValues);
		return;
	}

	// Done with the Registry.
	RegCloseKey(hKey);

	wchar_t *lpBase = lpValues;
	// Split the strings and add to Whitelist.
	for (;'\0' != *lpBase;lpBase += wcslen(lpBase) + 1)
	{
		wstring wsItem(lpBase);
		Whitelist.push_back(wsItem);
	}

	free(lpValues);
	return;
}

int GetMenuSelection()
{
	int iMenuItem = 0;
	wchar_t wcInput[20] = { 0 };
	while (true)
	{
		wprintf(L"Option >");
		if (NULL != _getws_s(wcInput, 20))
		{
			iMenuItem = _wtoi(wcInput);
			if (9 == iMenuItem)
				return -1;

			if (iMenuItem >= 1 && iMenuItem <= MAX_MENU_ITEMS)
			{
				break;
			}
		}
		else
		{
			wprintf(L"Please choose one of the following items\n");
			ShowMenu();
		}
	}

	return iMenuItem;
}

void DisplayWhitelist()
{
	if (Whitelist.size() == 0)
	{
		wprintf(L"You don't have any executables in your whitelist\n\n");
		return;
	}

	wprintf(L"Here's what's currently in the whitelist\n");

	for (unsigned int x = 0;x < Whitelist.size();x++)
	{
		wprintf(L"%d - %s\n", x + 1, Whitelist[x].c_str());
	}

	wprintf(L"\n");
}

int BuildFolderList(wstring InitialFolder)
{
	WIN32_FIND_DATA fd = { 0 };
//	InitialFolder.append(L"\\");
	int iCount = 0;
	HANDLE hFind = NULL;

	wstring wSearch = InitialFolder;
	wSearch.append(L"*.*");
	hFind = FindFirstFile(wSearch.c_str(), &fd);
	if (INVALID_HANDLE_VALUE == hFind)
	{
		return iCount;
	}

	while(true)
	{
		if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			if (fd.cFileName[0] != L'.')
			{
				wstring wsPath = InitialFolder;
				wsPath.append(fd.cFileName);
				wsPath.append(L"\\");
				FolderList.push_back(wsPath);
				BuildFolderList(wsPath);
			}
		}
		if (!FindNextFile(hFind, &fd))
			break;
	} 

	FindClose(hFind);

	return iCount;
}

bool AddToExeListQuestion(wstring wsFilename)
{
	bool bRet = false;
	while (true)
	{
		wprintf(L"Add %s to whitelist (Y/N)?", wsFilename.c_str());
		wchar_t c = _getwch();
		printf("%c\n", c);
		if (L'y' == c || L'Y' == c)
		{
			bRet = true;
			break;
		}

		if (L'n' == c || L'N' == c)
		{
			break;
		}
	}
	return bRet;
}

void BuildFilesInFolder(wstring wsPath, wstring wsFilename)
{
	// FoundFiles
	WIN32_FIND_DATA fd = { 0 };
	// need to do this the long way since we might get a partial filename "foo*.exe"
	// fd.cFilename will be the actual filename.
	wstring wsFullPath = wsPath;
	wsFullPath.append(wsFilename);

	HANDLE hFile = FindFirstFile(wsFullPath.c_str(),&fd);
	if (INVALID_HANDLE_VALUE == hFile)
		return;

	while (true)
	{
		wstring wsAdd = wsPath;
		wsAdd.append(fd.cFileName);
		FoundFiles.push_back(wsAdd);

		if (!FindNextFile(hFile, &fd))
		{
			break;
		}
	}

}

bool IsExeName(wstring fullString) 
{
	// need to get fullstring to lowercase before we do the compare.
	transform(fullString.begin(), fullString.end(), fullString.begin(), tolower);

	wstring wExe(L".exe");

	if (fullString.length() >= wExe.length()) {
		return (0 == fullString.compare(fullString.length() - wExe.length(), wExe.length(), wExe));
	}
	else {
		return false;
	}
}

void DeleteItem()
{
	if (Whitelist.size() == 0)
	{
		wprintf(L"The whitelist is empty, nothing to do here\n");
		return;
	}

	DisplayWhitelist();

	wchar_t wcOption[20] = { 0 };
	while (true)
	{
		wprintf(L"Which item do you want to remove (enter to skip)?");
		if (NULL != _getws_s(wcOption, 20))
		{
			if (wcslen(wcOption) == 0)
			{
				// just hit enter, go back.
				break;
			}

			unsigned int iOption = _wtoi(wcOption);
			if (iOption > 0 && iOption <= Whitelist.size())
			{
				Whitelist.erase(Whitelist.begin() + iOption - 1);
				break;
			}
		}
		else
		{
			wprintf(L"NULL == wcOption\n");
		}
	}
}

void WriteWhiteListToRegistry()
{
	if (Whitelist.size() == 0)
	{
		HKEY hKey = NULL;
		// Delete the value.
		if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_LOCAL_MACHINE, wcKey, 0, KEY_WRITE, &hKey))
		{
			// if we opened the parent Key then try to delete the value.
			// if we didn't open the key then we are done.
			if (ERROR_SUCCESS == RegDeleteValue(hKey, L"AllowedExecutableFilesList"))
			{
				wprintf(L"Whitelist is now cleared.\n");
			}
			RegCloseKey(hKey);
		}
		return;
	}

	// build the size of buffer we need.
	unsigned int bufferSize = 2;	// for the trailing nulls...
	for (unsigned int x = 0;x < Whitelist.size();x++)
	{
		bufferSize += wcslen(Whitelist[x].c_str())*sizeof(wchar_t) + 2;	// need the null per string.
	}

	BYTE* lpBuffer = (BYTE*)malloc(bufferSize);
	if (NULL == lpBuffer)
	{
		wprintf(L"Cannot allocate buffer for Whitelist Registry Entries\n");
		return;
	}
	ZeroMemory(lpBuffer, bufferSize);

	if (NULL == lpBuffer)
	{
		wprintf(L"Whoops, something went wrong, bailing\n");
		return;
	}

	// fill the buffer...
	unsigned int Offset = 0;
	for (unsigned int x = 0;x < Whitelist.size();x++)
	{
		const wchar_t* pString = Whitelist[x].c_str();
		unsigned int iLen = wcslen(pString)*sizeof(wchar_t);
		memcpy(lpBuffer + Offset, pString, iLen);
		Offset += iLen + 2;	// length of string + terminating null.
	}

	// now that we have the buffer, write to the registry.

	HKEY hKey = NULL;

	// try to open the final key.
	if (ERROR_SUCCESS != RegOpenKeyEx(HKEY_LOCAL_MACHINE, wcKey, 0, KEY_WRITE, &hKey))
	{
		wprintf(L"Registry Key for Whitelist doesn't exist - creating\n");
		// wKey doesn't exist.
		DWORD dwDisposition = 0;
		if (ERROR_SUCCESS != RegCreateKeyEx(HKEY_LOCAL_MACHINE, wcKey, 0, NULL, REG_OPTION_NON_VOLATILE, NULL, NULL, &hKey, &dwDisposition))
		{
			free(lpBuffer);
			wprintf(L"Error creating registry key\n");
			return;
		}
	}

	if (NULL != hKey)
	{
		wprintf(L"Setting Key Values\n");
		if (ERROR_SUCCESS == RegSetValueEx(hKey, wcValue, 0, REG_MULTI_SZ, (LPBYTE)lpBuffer, bufferSize))
		{
			wprintf(L"Success - Whitelist updated\n");
		}
		else
		{
			DWORD dwError=GetLastError();
			wprintf(L"Error Setting Registry Value %ld\n", dwError);
		}
		RegCloseKey(hKey);
	}
	free(lpBuffer);
}

void WriteRegFile()
{
	if (Whitelist.size() == 0)
	{
		wprintf(L"No whitelist items to write to the registry file\n");
		return;
	}

	// start with writing "IoTWhitelist.reg", we can prompt later...
	HANDLE hFile = CreateFile(L"IoTWhiteList.reg", GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	wchar_t *pReg = L"Windows Registry Editor Version 5.00\r\n\r\n";
	DWORD dwWritten = 0;
	WriteFile(hFile, (LPVOID)pReg, sizeof(wchar_t)*wcslen(pReg), &dwWritten, NULL);

	wstring wsReg(L"[HKEY_LOCAL_MACHINE\\");
	wsReg.append(wcKey);
	wsReg.append(L"]\r\n");
	WriteFile(hFile, (LPVOID)wsReg.c_str(), sizeof(wchar_t)*wcslen(wsReg.c_str()), &dwWritten, NULL);

	// "Value J"=hex(7):<Multi-string value data (as comma-delimited list of hexadecimal values representing UTF-16LE NUL-terminated strings)>
	wsReg.clear();
	wsReg.append(L"\"");
	wsReg.append(wcValue);
	wsReg.append(L"\"");
	wsReg.append(L"=hex(7):");
	WriteFile(hFile, (LPVOID)wsReg.c_str(), sizeof(wchar_t)*wcslen(wsReg.c_str()), &dwWritten, NULL);

	wchar_t wcOut[20] = { 0 };
	for (unsigned int x = 0;x < Whitelist.size();x++)
	{
		const wchar_t *pString = Whitelist[x].c_str();
		for (unsigned int t = 0;t < wcslen(pString);t++)
		{
			wsprintf(wcOut, L"%02x,%02x,", pString[t] & 0xff, (pString[t] & 0xff00) >> 8);
			WriteFile(hFile, (LPVOID)wcOut, sizeof(wchar_t)*wcslen(wcOut), &dwWritten, NULL);
		}
		wsprintf(wcOut, L"00,00,");
		WriteFile(hFile, (LPVOID)wcOut, sizeof(wchar_t)*wcslen(wcOut), &dwWritten, NULL);
	}
	wsprintf(wcOut, L"00,00");
	WriteFile(hFile, (LPVOID)wcOut, sizeof(wchar_t)*wcslen(wcOut), &dwWritten, NULL);

	wsprintf(wcOut, L"\r\n");
	WriteFile(hFile, (LPVOID)wcOut, sizeof(wchar_t)*wcslen(wcOut), &dwWritten, NULL);

	CloseHandle(hFile);

	wprintf(L"Registry file has been written\n");
}

void AddExeToList()
{
	wprintf(L"Provide the name of an executable,\n");
	wprintf(L"If the app is APPX local then just provide the .EXE name, no path needed\n");
	wprintf(L"You can also provide a full path to a .EXE\n\n");

	wprintf(L"Application to add>");
	wchar_t wcFilename[FILENAME_MAX];
	if (NULL != _getws_s(wcFilename, FILENAME_MAX))
	{
		if (wcslen(wcFilename) == 0)
		{
			wprintf(L"You need to enter a .EXE name\n");
			return;
		}

		wstring wsFilename(wcFilename);
		bool bAdd = true;
		if (!IsExeName(wsFilename))
		{
			wprintf(L"The name you provided doesn't end in \".exe\", continue?");
			wchar_t c = _getwch();
			printf("%c\n", c);
			if (L'y' == c || L'Y' == c)
			{
				bAdd = true;
			}
			else
			{
				bAdd = false;
			}
		}

		if (true == bAdd)
			Whitelist.push_back(wcFilename);
	}
}
