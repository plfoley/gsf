﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;

// Assembly identity attributes.
[assembly: AssemblyVersion("2.0.25.0")]

// Informational attributes.
[assembly: AssemblyCompany("Grid Protection Alliance")]
[assembly: AssemblyCopyright("Copyright © 2012.  All Rights Reserved.")]
[assembly: AssemblyProduct("openPDC Historian")]

// Assembly manifest attributes.
#if DEBUG
[assembly: AssemblyConfiguration("Debug Build")]
#else
[assembly: AssemblyConfiguration("Release Build")]
#endif
[assembly: AssemblyDefaultAlias("Hadoop.Replication")]
[assembly: AssemblyDescription("Historian replication component to Hadoop.")]
[assembly: AssemblyTitle("Hadoop.Replication")]

// Other configuration attributes.
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: Guid("fbb49f9b-954b-40a7-986f-460d12cb31e9")]
