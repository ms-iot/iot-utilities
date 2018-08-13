#include "pch.h"
#include <fstream>
#include <string>
#include <regex>
#include "Objbase.h"

const std::string PROFILE_FILENAME = "myprofile.xml";

const std::regex SUBSCRIBER_ID("Subscriber Id\\s+:\\s");
const std::regex INTERFACE_NAME("Name\\s+:\\s");
const std::regex HOME_PROVIDER_NAME("Home provider name\\s+:\\s");
const std::regex SIM_ICC_ID("SIM ICC Id\\s+:\\s");

const std::string NAME_TAG = "<Name>";
const std::string SUBSCRIBER_TAG = "<SubscriberID>";
const std::string SIM_ICC_TAG = "<SimIccID>";
const std::string HOME_PROVIDER_TAG = "<HomeProviderName>";
const std::string ACCESS_STRING_TAG = "<AccessString>";

const std::string PROFILE = std::string("<?xml version=\"1.0\"?>\n") +
std::string("<MBNProfileExt xmlns=\"http://www.microsoft.com/networking/WWAN/profile/v4\">\n") +
std::string("	<Name></Name>\n") +
std::string("	<IsDefault>true</IsDefault>\n") +
std::string("	<ProfileCreationType>UserProvisioned</ProfileCreationType>\n") +
std::string("	<SubscriberID></SubscriberID>\n") +
std::string("	<SimIccID></SimIccID>\n") +
std::string("	<HomeProviderName></HomeProviderName>\n") +
std::string("	<ConnectionMode>auto</ConnectionMode>\n") +
std::string("	<Context>\n") +
std::string("		<AccessString></AccessString>\n") +
std::string("		<Compression>DISABLE</Compression>\n") +
std::string("		<AuthProtocol>NONE</AuthProtocol>\n") +
std::string("	</Context>\n") +
std::string("	<PurposeGroups>\n") +
std::string("		<PurposeGroupGuid>{3E5545D2-1137-4DC8-A198-33F1C657515F}</PurposeGroupGuid>\n") +
std::string("	</PurposeGroups>\n") +
std::string("	<AdminEnable>true</AdminEnable>\n") +
std::string("	<AdminRoamControl>AllRoamAllowed</AdminRoamControl>\n") +
std::string("	<IsExclusiveToOther>true</IsExclusiveToOther>\n") +
std::string("</MBNProfileExt>");

std::string Execute(const char* cmd)
{
    char buffer[128];
    std::string result = "";
    FILE* pipe = _popen(cmd, "r");

    if (!pipe) throw std::runtime_error("popen() failed!");

    try
    {
        while (!feof(pipe)) {
            if (fgets(buffer, 128, pipe) != NULL)
                result += buffer;
        }
    }
    catch (...)
    {
        _pclose(pipe);
        throw;
    }

    _pclose(pipe);

    return result;
}

std::string Execute(std::string cmd)
{
    return Execute(cmd.c_str());
}

std::string RemoveInvalidChar(std::string s)
{
    for (int i = 0; i < s.length(); i++)
    {
        if (s[i] == '&' || s[i] == '<' || s[i] == '>' || s[i] == '"' || s[i] == '\'')
        {
            s[i] = ' ';
        }
    }

    return s;
}

std::string CreateGuidString()
{
    GUID guid;
    CoCreateGuid(&guid);

    char c[37];

    sprintf_s(c, "%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x",
        guid.Data1, guid.Data2, guid.Data3,
        guid.Data4[0], guid.Data4[1], guid.Data4[2], guid.Data4[3],
        guid.Data4[4], guid.Data4[5], guid.Data4[6], guid.Data4[7]);

    std::string s(c);

    return s;
}

int main(int argc, char **argv)
{
    std::string apn = "";

    if (argc != 2 || argv[1] == "?" || argv[1] == "help")
    {
        std::cout << argv[0] << " <APN>" << std::endl;
        return 1;
    }
    else
    {
        apn = argv[1];
    }

    std::string nameGuid = CreateGuidString();

    std::string subscriberId = Execute("netsh mbn show ready *");
    std::string simIccId = subscriberId;

    std::smatch match;

    if (!std::regex_search(subscriberId, match, SUBSCRIBER_ID))
    {
        std::cout << "Cannot find Subscriber Id" << std::endl;
        return 1;
    }
    else
    {
        std::size_t begin = match.position() + match.length();
        std::size_t end = subscriberId.find("\n", begin);
        subscriberId = subscriberId.substr(begin, end - begin);

        std::cout << "Subscriber Id: " << subscriberId << std::endl;
    }

    if (!std::regex_search(simIccId, match, SIM_ICC_ID))
    {
        std::cout << "Cannot find SIM ICC Id" << std::endl;
        return 1;
    }
    else
    {
        std::size_t begin = match.position() + match.length();
        std::size_t end = simIccId.find("\n", begin);
        simIccId = simIccId.substr(begin, end - begin);

        std::cout << "SIM ICC Id: " << simIccId << std::endl;
    }

    std::string interfaceName = Execute("netsh mbn show interfaces *");
    std::cout << interfaceName << std::endl;

    if (!std::regex_search(interfaceName, match, INTERFACE_NAME))
    {
        std::cout << "Cannot find Interface Name" << std::endl;
        return 1;
    }
    else
    {
        std::size_t begin = match.position() + match.length();
        std::size_t end = interfaceName.find("\n", begin);
        interfaceName = interfaceName.substr(begin, end - begin);

        std::cout << "Interface Name: " << interfaceName << std::endl;
    }

    std::string homeProviderName = Execute("netsh mbn show homeprovider *");

    if (!std::regex_search(homeProviderName, match, HOME_PROVIDER_NAME))
    {
        std::cout << "Cannot find Home Provider Name" << std::endl;
        return 1;
    }
    else
    {
        std::size_t begin = match.position() + match.length();
        std::size_t end = homeProviderName.find("\n", begin);
        homeProviderName = homeProviderName.substr(begin, end - begin);

        std::cout << "Home Provider Name: " << homeProviderName << std::endl;
    }

    std::string profile = PROFILE;
    profile.insert(profile.find(NAME_TAG) + NAME_TAG.length(), nameGuid);
    profile.insert(profile.find(SUBSCRIBER_TAG) + SUBSCRIBER_TAG.length(), RemoveInvalidChar(subscriberId));
    profile.insert(profile.find(SIM_ICC_TAG) + SIM_ICC_TAG.length(), RemoveInvalidChar(simIccId));
    profile.insert(profile.find(HOME_PROVIDER_TAG) + HOME_PROVIDER_TAG.length(), RemoveInvalidChar(homeProviderName));
    profile.insert(profile.find(ACCESS_STRING_TAG) + ACCESS_STRING_TAG.length(), RemoveInvalidChar(apn));

    std::ofstream out(PROFILE_FILENAME);
    out << profile;
    out.close();

    std::cout << "create profile: " << PROFILE_FILENAME << std::endl;

    std::string addProfile = Execute((std::string("netsh mbn add profile interface=\"") + interfaceName + std::string("\" name=\"") + PROFILE_FILENAME + std::string("\"")).c_str());

    std::cout << "netsh mbn add profile: ";

    if (addProfile != "\n")
    {
        std::cout << "Fail!" << std::endl;
        std::cout << addProfile << std::endl;
        return 1;
    }

    std::cout << "Done!" << std::endl;

    std::string connect = Execute((std::string("netsh mbn connect interface=\"") + interfaceName + std::string("\" connmode=name name=\"") + nameGuid + std::string("\"")));

    std::cout << "netsh mbn connect: ";

    if (connect != "\n")
    {
        std::cout << "Fail!" << std::endl;
        std::cout << connect << std::endl;
        return 1;
    }
    else {
        std::cout << "Done!" << std::endl;
        std::cout << connect << std::endl;
    }
    // Wait for Connection building.
    Sleep(1000);

    interfaceName = Execute("netsh mbn show interfaces *");
    std::cout << interfaceName << std::endl;
    return 0;
}
