using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Hudl.Mjolnir")]
[assembly: AssemblyDescription("Fault tolerance library for protecting against cascading failure. Uses bulkheads and circuit breakers to isolate problems and fail fast.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Hudl")]
[assembly: AssemblyProduct("Hudl.Mjolnir")]
[assembly: AssemblyCopyright("Copyright © 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("Hudl.Mjolnir.Tests")]
[assembly: InternalsVisibleTo("Hudl.Mjolnir.SystemTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("97b23684-6c4a-4749-b307-5867cbce2dff")]

// Used for NuGet packaging, uses semantic versioning: major.minor.patch-prerelease.
[assembly: AssemblyInformationalVersion("2.6.0")]

// Keep this the same as AssemblyInformationalVersion.
[assembly: AssemblyFileVersion("2.6.0")]

// ONLY change this when the major version changes; never with minor/patch/build versions.
// It'll almost always be the major version followed by three zeroes (e.g. 1.0.0.0).
[assembly: AssemblyVersion("2.0.0.0")]
