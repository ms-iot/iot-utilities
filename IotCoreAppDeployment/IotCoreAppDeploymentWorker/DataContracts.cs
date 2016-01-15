using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IotCoreAppDeploymentCs
{
    [DataContract]
    public class InstalledPackages
    {
        [DataMember(Name = "InstalledPackages")]
        public AppxPackage[] Items { get; set; }
    }


    [DataContract]
    public class AppxPackage
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "PackageFamilyName")]
        public string PackageFamilyName { get; set; }

        [DataMember(Name = "PackageFullName")]
        public string PackageFullName { get; set; }

        [DataMember(Name = "PackageOrigin")]
        public string PackageOrigin { get; set; }

        [DataMember(Name = "PackageRelativeId")]
        public string PackageRelativeId { get; set; }
    }

    [DataContract]
    public class DeploymentState
    {
        [DataMember(Name = "Code")]
        public int HResult { get; set; }

        [DataMember(Name = "CodeText")]
        public string CodeText { get; set; }

        [DataMember(Name = "Reason")]
        public string Reason { get; set; }

        [DataMember(Name = "Success")]
        public bool IsSuccess { get; set; }
    }
}
