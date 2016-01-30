using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("fileutil")]
[assembly: AssemblyDescription("Utility to manage pimix files.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Pimix")]
[assembly: AssemblyProduct("Pimix.Apps.FileUtil")]
[assembly: AssemblyCopyright("Copyright © 2015 Kimi Arthur")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("531cb29e-4a81-4379-ab90-04481e829ade")]

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
[assembly: AssemblyVersion(Pimix.Apps.FileUtil.AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(Pimix.Apps.FileUtil.AssemblyInfo.Version)]
[assembly: AssemblyInformationalVersion(Pimix.Apps.FileUtil.AssemblyInfo.Version)]

namespace Pimix.Apps.FileUtil
{
    static class AssemblyInfo
    {
        public const string Version = "1.1.3";
    }
}
