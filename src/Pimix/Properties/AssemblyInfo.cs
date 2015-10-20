using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Pimix")]
[assembly: AssemblyDescription("Common classes and helpers.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Pimix")]
[assembly: AssemblyProduct("Pimix")]
[assembly: AssemblyCopyright("Copyright © 2015 Kimi Arthur")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("2abe34d1-ef0a-4ac6-bb95-05dcefc5bb91")]

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
[assembly: AssemblyVersion(Pimix.AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(Pimix.AssemblyInfo.Version)]
[assembly: AssemblyInformationalVersion(Pimix.AssemblyInfo.Version)]

namespace Pimix
{
    static class AssemblyInfo
    {
        public const string Version = "1.0.3";
    }
}
