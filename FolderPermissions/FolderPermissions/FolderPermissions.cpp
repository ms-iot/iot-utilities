// FolderPermissions.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#include <Aclapi.h>
#include <Sddl.h>

void Banner();
void Usage();
bool FolderExists(wchar_t *strFolderName);
bool FolderHasAppxPermissions(wchar_t *strFolderName);
bool SetFolderPermissions(wchar_t *strFolderName);
bool RevokeFolderPermissions(wchar_t *strFolderName);

int _tmain(int argc, _TCHAR* argv[])
{
	Banner();

	if (argc < 2 || argc > 3)
	{
		Usage();
		return -1;
	}

	bool bFolderExists = FolderExists(argv[1]);
	if (!bFolderExists)
	{
		wprintf(L"Folder %s does not exist\n", argv[1]);
		return -1;
	}

	if (argc == 2)
	{
		if (FolderHasAppxPermissions(argv[1]))
		{
			wprintf(L"Folder %s has APPX R/W permissions\n",argv[1]);
		}
		else
		{
			wprintf(L"Folder %s does not have APPX R/W Permissions\n", argv[1]);
		}
	}

	if (argc == 3)
	{
		bool bParamsOK = false;
		// check argv[2] for '-e' or 'r' (enable/revoke).
		if (_wcsicmp(argv[2], L"-e") == 0)
		{
			bParamsOK = true;
			if (!FolderHasAppxPermissions(argv[1]))
			{
				wprintf(L"Setting APPX R/W Permissions on folder %s\n", argv[1]);
				if (SetFolderPermissions(argv[1]))
				{
					wprintf(L"Success - APPX R/W Permissions now set on folder %s\n", argv[1]);
				}
				else
				{
					wprintf(L"Something went wrong, APPX R/W permissions not set on folder %s\n", argv[1]);
				}
			}
		}

		if (_wcsicmp(argv[2], L"-r") == 0)
		{
			bParamsOK = true;
			
			if (FolderHasAppxPermissions(argv[1]))
			{
				if (RevokeFolderPermissions(argv[1]))
				{
					wprintf(L"Folder %s APPX R/W permissions have been revoked\n", argv[1]);
				}
				else
					wprintf(L"Something went wrong, APPX R/W permissions not revoked on folder %s\n", argv[1]);
			}
			else
			{
				wprintf(L"Folder %s does not have APPX R/W permissions\n", argv[1]);
			}
			
			return 0;
		}

		if (!bParamsOK)
		{
			wprintf(L"Unknown paramater - Usage is displayed below...\n");
			Usage();
		}
	}

    return 0;
}

void Usage()
{
	wprintf(L"Usage: App <Folder> [-e | -r]\n");
	wprintf(L"Where [-e] will enable APPX access to a folder\n");
	wprintf(L"and [-r] will remove APPX access to a folder\n");
	wprintf(L"App <Folder> will display current APPX access permissions\n");
	wprintf(L"\n");
}

void Banner()
{
	wprintf(L"APPX Folder Permissions\n");
}

bool FolderExists(wchar_t *strFolderName)
{
	bool bRet = false;
	WIN32_FIND_DATA fd = { 0 };
	HANDLE hFind = NULL;

	hFind = FindFirstFile(strFolderName, &fd);
	if (INVALID_HANDLE_VALUE != hFind)
	{
		if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			bRet = true;
		}
		FindClose(hFind);
	}

	return bRet;
}

bool FolderHasAppxPermissions(wchar_t *strFolderName)
{
	bool bRet = false;
	PACL pOldDACL = NULL;
	PSECURITY_DESCRIPTOR pSD = NULL;
	SECURITY_INFORMATION si = DACL_SECURITY_INFORMATION;

	if (ERROR_SUCCESS == GetNamedSecurityInfo(strFolderName, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, &pOldDACL, NULL, &pSD))
	{
		if (NULL != pSD)
			LocalFree(pSD);

		ACE_HEADER* aceHeader = nullptr;
		DWORD dwCount = pOldDACL->AceCount;

		for (DWORD x = 0;x < dwCount;x++)
		{
			GetAce(pOldDACL, x, (LPVOID*)&aceHeader);
			if (aceHeader->AceType == ACCESS_ALLOWED_ACE_TYPE)
			{
				ACCESS_ALLOWED_ACE* accessAllowedAce = (ACCESS_ALLOWED_ACE*)aceHeader;
				SID *sid = (SID *)&accessAllowedAce->SidStart;

				if (IsValidSid(sid))
				{
					LPWSTR pString = NULL;
					ConvertSidToStringSid(sid, &pString);
					if (0 == wcsncmp(pString, L"S-1-15-2-1", wcslen(pString)))
					{
						if ((GENERIC_READ & accessAllowedAce->Mask) && (GENERIC_WRITE & accessAllowedAce->Mask))
						{
							bRet = true;
							break;
						}
					}
					LocalFree(pString);
				}
			}
		}
	}

	return bRet;
}

bool SetFolderPermissions(wchar_t *strFolderName)
{
	PACL pOldDACL = NULL, pNewDACL = NULL;
	PSECURITY_DESCRIPTOR pSD = NULL;
	EXPLICIT_ACCESS ea;
	SECURITY_INFORMATION si = DACL_SECURITY_INFORMATION;
	PSID pSID = NULL;
	DWORD dwRet = NULL;

	bool bRet = false;

	if (ERROR_SUCCESS == GetNamedSecurityInfo(strFolderName, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, &pOldDACL, NULL, &pSD))
	{
		if (NULL != pSD)
			LocalFree(pSD);

		ZeroMemory(&ea, sizeof(EXPLICIT_ACCESS));
		DWORD SidSize = SECURITY_MAX_SID_SIZE;
		if ((pSID = LocalAlloc(LMEM_FIXED, SidSize)))
		{
			if (CreateWellKnownSid(WinBuiltinAnyPackageSid, NULL, pSID, &SidSize))
			{
				ea.grfAccessMode = SET_ACCESS;
				ea.grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
				ea.Trustee.TrusteeForm = TRUSTEE_IS_SID;
				ea.Trustee.TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP;
				ea.Trustee.ptstrName = (LPWSTR)pSID;
				ea.grfAccessPermissions = GENERIC_READ | GENERIC_WRITE;
				if (ERROR_SUCCESS == SetEntriesInAcl(1, &ea, pOldDACL, &pNewDACL))
				{
					if (ERROR_SUCCESS == SetNamedSecurityInfo(strFolderName, SE_FILE_OBJECT, si, NULL, NULL, pNewDACL, NULL))
					{
						bRet = true;
					}
				}
			}
			if (NULL != pNewDACL)
				LocalFree((HLOCAL)pNewDACL);
			LocalFree((HLOCAL)pSID);
		}
	}

	return bRet;
}

bool RevokeFolderPermissions(wchar_t *strFolderName)
{
	PACL pOldDACL = NULL, pNewDACL = NULL;
	PSECURITY_DESCRIPTOR pSD = NULL;
	EXPLICIT_ACCESS ea;
	SECURITY_INFORMATION si = DACL_SECURITY_INFORMATION;
	PSID pSID = NULL;
	DWORD dwRet = NULL;

	bool bRet = false;

	if (ERROR_SUCCESS == GetNamedSecurityInfo(strFolderName, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, &pOldDACL, NULL, &pSD))
	{
		if (NULL != pSD)
			LocalFree(pSD);

		ZeroMemory(&ea, sizeof(EXPLICIT_ACCESS));
		DWORD SidSize = SECURITY_MAX_SID_SIZE;
		if ((pSID = LocalAlloc(LMEM_FIXED, SidSize)))
		{
			if (CreateWellKnownSid(WinBuiltinAnyPackageSid, NULL, pSID, &SidSize))
			{
				ea.grfAccessMode = REVOKE_ACCESS;
				ea.grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
				ea.Trustee.TrusteeForm = TRUSTEE_IS_SID;
				ea.Trustee.TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP;
				ea.Trustee.ptstrName = (LPWSTR)pSID;
				ea.grfAccessPermissions = GENERIC_READ | GENERIC_WRITE;
				if (ERROR_SUCCESS == SetEntriesInAcl(1, &ea, pOldDACL, &pNewDACL))
				{
					if (ERROR_SUCCESS == SetNamedSecurityInfo(strFolderName, SE_FILE_OBJECT, si, NULL, NULL, pNewDACL, NULL))
					{
						bRet = true;
					}
				}
			}
			if (NULL != pNewDACL)
				LocalFree((HLOCAL)pNewDACL);
			LocalFree((HLOCAL)pSID);
		}
	}

	return bRet;
}
