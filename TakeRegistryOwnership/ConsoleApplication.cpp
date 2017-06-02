// ConsoleApplication1.cpp : Defines the entry point for the console application.
//

#include "pch.h"
#pragma comment(lib, "advapi32.lib")

__forceinline void PrintLastError(char* errorMsg)
{
    std::cout << "Error " << errorMsg << ": " << GetLastError() << std::endl;
}

__forceinline void PrintErrorWithResult(char* errorMsg, DWORD result)
{
    std::cout << "Error " << errorMsg << ": " << result << std::endl;
}

// Set or remove a permission from a given handle
// Based on code from: http://msdn.microsoft.com/en-us/library/aa446619(v=VS.85).aspx
bool SetPrivilege(HANDLE token, LPCWSTR privilegeName, bool enablePrivilege)
{
    LUID luid;

    if (!LookupPrivilegeValue(nullptr, privilegeName, &luid))
    {
        PrintLastError("LookupPrivilegeValue");
        return false;
    }

    TOKEN_PRIVILEGES tokenPrivileges = { 0 };
    tokenPrivileges.PrivilegeCount = 1;
    tokenPrivileges.Privileges[0].Luid = luid;
    if (enablePrivilege)
        tokenPrivileges.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
    else
        tokenPrivileges.Privileges[0].Attributes = 0;

    // Enable the privilege or disable all privileges.

    if (!AdjustTokenPrivileges(
        token,
        false,
        &tokenPrivileges,
        sizeof(TOKEN_PRIVILEGES),
        nullptr,
        nullptr))
    {
        PrintLastError("AdjustTokenPrivileges");
        return false;
    }

    if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)
    {
        std::cout << "Error: The token does not have the specified privilege." << std::endl;
        return false;
    }

    return true;
}

// Grant an administrator ownership of a given registry key
// Code based on these MSDN articles: http://msdn.microsoft.com/en-us/library/aa379620(v=VS.85).aspx
//                                and http://msdn.microsoft.com/en-us/library/aa379283(v=VS.85).aspx
bool GrantAdminOwnership(LPWSTR registryKey)
{
    bool succeeded = true;
    PSID everyoneGroupSID = nullptr;
    SID_IDENTIFIER_AUTHORITY worldAuthoritySID = SECURITY_WORLD_SID_AUTHORITY;

    // Specify the DACL to use.
    // Create a SID for the Everyone group.
    if (!AllocateAndInitializeSid(
            &worldAuthoritySID, 1,
            SECURITY_WORLD_RID,
            0, 0, 0, 0, 0, 0, 0,
            &everyoneGroupSID))
    {
        succeeded = false;
        PrintLastError("AllocateAndInitializeSid (Everyone)");
    }

    PSID adminSID = nullptr;
    SID_IDENTIFIER_AUTHORITY NTAuthoritySID = SECURITY_NT_AUTHORITY;
    if (succeeded)
    {
        // Create a SID for the BUILTIN\Administrators group.
        if (!AllocateAndInitializeSid(&NTAuthoritySID, 2,
            SECURITY_BUILTIN_DOMAIN_RID,
            DOMAIN_ALIAS_RID_ADMINS,
            0, 0, 0, 0, 0, 0,
            &adminSID))
        {
            succeeded = false;
            PrintLastError("AllocateAndInitializeSid (Admin)");
        }
    }

    HANDLE token = nullptr;
    if (succeeded)
    {
        // Open a handle to the access token for the calling process.
        if (!OpenProcessToken(GetCurrentProcess(),
            TOKEN_ADJUST_PRIVILEGES,
            &token))
        {
            succeeded = false;
            PrintLastError("OpenProcessToken");
        }
    }

    if (succeeded)
    {
        // Enable the SE_TAKE_OWNERSHIP_NAME privilege.
        if (!SetPrivilege(token, SE_TAKE_OWNERSHIP_NAME, true))
        {
            succeeded = false;
            std::cout << "Error: You must be logged on as Administrator." << std::endl;
        }
    }

    DWORD result;
    if (succeeded)
    {
        // Set the owner in the object's security descriptor.
        result = SetNamedSecurityInfo(
            registryKey, SE_REGISTRY_KEY,
            OWNER_SECURITY_INFORMATION, adminSID,
            nullptr, nullptr, nullptr);

        if (result != ERROR_SUCCESS)
        {
            succeeded = false;
            PrintErrorWithResult("Could not set owner", result);
        }
    }

    if (succeeded)
    {
        // Redisable the SE_TAKE_OWNERSHIP_NAME privilege.
        if (!SetPrivilege(token, SE_TAKE_OWNERSHIP_NAME, false))
        {
            std::cout << "Error: Failed SetPrivilege call unexpectedly." << std::endl;
            succeeded = false;
        }
    }

    PACL oldACL = nullptr;
    if (succeeded)
    {
        result = GetNamedSecurityInfo(
            registryKey, SE_REGISTRY_KEY,
            DACL_SECURITY_INFORMATION,
            nullptr, nullptr, &oldACL, nullptr, nullptr);

        if (ERROR_SUCCESS != result)
        {
            succeeded = false;
            PrintErrorWithResult("Could not get security info", result);
        }
    }

    PACL newACL = nullptr;
    if (succeeded)
    {
        EXPLICIT_ACCESS explicitAccess = { 0 };
        explicitAccess.grfAccessPermissions = GENERIC_ALL;
        explicitAccess.grfAccessMode = SET_ACCESS;
        explicitAccess.grfInheritance = CONTAINER_INHERIT_ACE;
        explicitAccess.Trustee.TrusteeForm = TRUSTEE_IS_SID;
        explicitAccess.Trustee.TrusteeType = TRUSTEE_IS_GROUP;
        explicitAccess.Trustee.ptstrName = static_cast<LPTSTR>(adminSID);

        // Create a new ACL that merges the new ACE
        // into the existing DACL.
        result = SetEntriesInAcl(1, &explicitAccess, oldACL, &newACL);
        if (ERROR_SUCCESS != result)
        {
            PrintErrorWithResult("SetEntriesInAcl", result);
            succeeded = false;
        }
    }

    if (succeeded)
    {
        // Attach the new ACL as the object's DACL.
        result = SetNamedSecurityInfo(
            registryKey, SE_REGISTRY_KEY,
            DACL_SECURITY_INFORMATION,
            nullptr, nullptr, newACL, nullptr);

        if (ERROR_SUCCESS != result)
        {
            succeeded = false;
            PrintErrorWithResult("SetNamedSecurityInfo", result);
        }
    }

    if (adminSID)
        FreeSid(adminSID);

    if (everyoneGroupSID)
        FreeSid(everyoneGroupSID);

    if (newACL)
        LocalFree(newACL);

    if (token)
        CloseHandle(token);

    return succeeded;
}

int wmain(int argc, wchar_t **argv)
{
    if (argc < 2)
    {
        std::cout << "Usage: TakeObjectOwnership <registry key>" << std::endl;
        return 1;
    }

    return (GrantAdminOwnership(argv[1]) == 0 ? 0 : 1);
}
