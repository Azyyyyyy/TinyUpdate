# TinyUpdate
![](../assets/logo-60px.png)

## Table of contents
### Getting Started Guide
The Getting Started Guide will show everything required for integrating TinyUpdate into your application

1. [Integrating](getting-started/integrating.md) - How to integrate TinyUpdate into your application
2. [Packaging](getting-started/packaging.md) - How to package your application files and prepare them for release
3. [Distributing](getting-started/distributing.md) - How to provide updates files to your users
4. [Installing](getting-started/installing.md) - This hasn't been implemented yet, come back later when that's the case!
5. [Updating](getting-started/updating.md) - The process of updating your application to a newer version

### Using TinyUpdate
* Installing - This hasn't been implemented yet, come back later when that's the case!
* [Versioning](using/versioning.md) - How we grab the version that your application is currently running
* [Delta Packages](using/delta-packages.md) - How Delta packages are created and why they are important for both the developer and the end-user
* Application Signing - This hasn't been implemented yet, come back later when that's the case!
* [Package Creation](using/package-creation.md) - This will go through how the packages are created
* [Distributing](using/distributing/README.md)
  * [GitHub](using/distributing/github.md) - overview of using GitHub to provide updates to users
  * [Microsoft IIS](using/distributing/ms-iis.md) - overview of using Microsoft IIS to provide updates to users
  * [Amazon S3](using/distributing/amazon-S3.md) - overview of using Amazon S3 to provide updates to users
  * [Server](using/distributing/general-server.md) - overview of using a server to provide updates
  * [Local](using/distributing/local.md) - overview of using a local disk to provide updates to users
* [Updating](using/updating/README.md)
  * [Update Process](using/updating/update-process.md) - How we go about processing new updates
  * [UpdateClient](using/updating/update-client.md) - The base class which provides generalized updating logic and how you can go about making your own UpdateClient class!
  * [Debugging Updates](using/updating/debugging-updates.md) - What you can do to find out what is happening when an update is failing to apply
  * [Staged Rollouts](using/updating/staged-rollouts.md) - How you can go about staging an update to users