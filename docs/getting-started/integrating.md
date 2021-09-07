# Step 1 - Integrating
The first step for integrating TinyUpdate is to get the needed packages, for this project they will be `TinyUpdate.Binary` (What applies/creates the updates) and `TinyUpdate.Local` (What grabs the updates which need to be applied). To get these, go to your NuGet window in your IDE or type in `dotnet add package <package name>` if you are using CLI .

After that has been done all we need to do is to make the updater client and call ``UpdateApp()``
```cs
var updateClient = new LocalUpdateClient(@"C:\Users\Aaron\source\Releases", new BinaryApplier());
await updateClient.UpdateApp();
```

**Note**: In a real application you wouldn't hard code the path in but use a relative path or use the working path (``Environment.CurrentDirectory``)

your also need to add these usings
```cs
using TinyUpdate.Extensions;
using TinyUpdate.Local;
using TinyUpdate.Binary;
```

## Overview
1. **[Integrating](integrating.md)** - How to integrate TinyUpdate into your application
2. [Packaging](packaging.md) - How to package your application files and prepare them for release
3. [Distributing](distributing.md) - How to provide updates files to your users
4. [Installing](installing.md) - This hasn't been implemented yet, come back later when that's the case!
5. [Updating](updating.md) - How we go about the update process