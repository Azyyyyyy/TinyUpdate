# Tiny Update
![](assets/logo-60px.png)

[![Build Status](https://github.com/Azyyyyyy/TinyUpdate/actions/workflows/build_and_test.yml/badge.svg)](https://github.com/Azyyyyyy/TinyUpdate/actions/workflows/build_and_test.yml)
## What is this?
Tiny Update is an Updater that focuses on being easy to use while being feature rich and fast!

## What does this contain?
The main parts of the updater are:
* IUpdateCreator: This is responsible for creating both delta and full updates
* IUpdateApplier: This is responsible for applying any update that gets thrown to your application!
* UpdateChecker: This is responsible for Checking for new updates, downloading updates and getting any release notes (it also contains an ``IUpdateApplier`` and exposes the ``IUpdateApplier`` functions)
* Hard Link's: This allows us to have no need for copying files that haven't changed from the last update
* TinyUpdate.Create: This project allows you to easy create updates for your application! **Note that you need .Net 5+ installed on your system to use this tool** (just type in ``dotnet tool install --global TinyUpdate.Create --version 0.0.0.6-alpha`` to install it and then any time you need to create an update you just have to type in ``tinyupdate``)


## What is currently implemented?
### Hard Link
This is implemented for Windows and Linux

## IUpdateCreator's
### BinaryCreator
This is fully implemented

## IUpdateApplier's
### BinaryApplier
This is fully implemented with Hard Link support and has more modern MsDelta flags, allowing MsDelta to create the smallest update files possible

## UpdateClient's
### GithubClient
This is fully implemented with support for REST and for GraphQL (Note that the GraphQL api requires a personal token with public_repo)

### LocalClient

### WebClient