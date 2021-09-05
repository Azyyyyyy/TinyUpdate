# Step 3 - Distributing
Now that you got the files needed for updating we need to distribute the update. The two files that you need to distribute are:
* ``RELEASE`` file: This is the file that tells any ``UpdateClient`` what updates are currently available for the application
* Update file: This will be the file that contains the actual update, the extension of this file depends on what created the update.

## Local File Distribution
To make this project simple and one that you can remake, we will be using the local filesystem for updates as done in [Step 1 - Integrating](integrating.md). It's not recommended to use this in a real project unless all your users will have access to a similar network path.

## Overview
1. [Integrating](integrating.md) - How to integrate TinyUpdate into your application
2. [Packaging](packaging.md) - How to package your application files and prepare them for release
3. **[Distributing](distributing.md)** - How to provide updates files to your users
4. [Installing](installing.md) - This hasn't been implemented yet, come back later when that's the case!
5. [Updating](updating.md) - How we go about the update process