using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("jobutil")]
[assembly: AssemblyDescription("Utility to manage pimix jobs.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Pimix")]
[assembly: AssemblyProduct("Pimix.Apps.JobUtil")]
[assembly: AssemblyCopyright("Copyright © 2015 Kimi Arthur")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1cb70f64-85d8-49da-abca-c265429f79d4")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(Pimix.Apps.JobUtil.AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(Pimix.Apps.JobUtil.AssemblyInfo.Version)]
[assembly: AssemblyInformationalVersion(Pimix.Apps.JobUtil.AssemblyInfo.Version)]

namespace Pimix.Apps.JobUtil
{
    static class AssemblyInfo
    {
        public const string Version = "1.0.3";
    }
}
