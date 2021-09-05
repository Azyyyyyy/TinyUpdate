# TinyUpdate
![](../../../assets/logo-60px.png)

## UpdateClient
### Methods
`CheckForUpdate` - Checks for new updates and returns them as a `UpdateInfo`

`DownloadUpdate` - Downloads the update(s) onto the users device

`ApplyUpdate` - Applies the update(s) with the `IUpdateApplier` that was given when the `UpdateClient` was constructed

`GetChangelog` - Gets the changelog for a certain update (or the newest version if given a `UpdateInfo`)