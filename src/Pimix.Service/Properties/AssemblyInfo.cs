using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Pimix.Service")]
[assembly: AssemblyDescription("Pimix Server related components")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Pimix")]
[assembly: AssemblyProduct("Pimix.Service")]
[assembly: AssemblyCopyright("Copyright © 2015 Kimi Arthur")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("61cbf740-d894-4f39-9071-bbb96cfd13a4")]

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
[assembly: AssemblyVersion(Pimix.Service.AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(Pimix.Service.AssemblyInfo.Version)]
[assembly: AssemblyInformationalVersion(Pimix.Service.AssemblyInfo.Version)]

namespace Pimix.Service
{
    static class AssemblyInfo
    {
        public const string Version = "1.2.2";
    }
}

