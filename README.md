# Introduction
This is a demo application on how to use [Azure Spatial Anchors](https://azure.microsoft.com/en-us/services/spatial-anchors/#overview) with [StereoKit](https://stereokit.net/) to persist world anchors between sessions and devices.

# Pre-requisites

## StereoKit
[StereoKit](https://stereokit.net/) is "an easy-to-use open source mixed reality library for building HoloLens and VR applications with C# and OpenXR!"

Want to build a quick VR or MR app? Stereokit will most likely be the tool to use.

It provides a template to Visual Studio so that you have the initial boilerplate done and also a cool simulator. Check out their site for more details.

![StereoKit example](https://stereokit.net/img/screenshots/StereoKitInk.jpg)

*copyright Stereokit*

## Azure Spatial Anchors

Azure Spatial Anchors (ASA) allow apps to map, persist, and restore 3D content or points of interest at real-world scale (aka spatial anchors).

The interesting part of using ASA is that it works on multiple devices and allows an easy way to share the anchors accross them. So one user with, for instance, and Hololens can place an anchors on the real world, and another user with an Android phone can see also see the anchor, this of course as long as both users are on the same physical space.

To run this sample it's required to have an Azure account and create Spatial Anchor resouce. For more on this you can follow [this tutorial](https://docs.microsoft.com/en-us/azure/spatial-anchors/how-tos/create-asa-account).

![Spatial Anchors shared across devices](https://docs.microsoft.com/pt-pt/azure/spatial-anchors/media/cross-platform.png)


*copyright Microsoft*

# Sample Details

This demo was tested on a Hololens 2. Azure Spatial Anchors to discover anchors needs some sort of anchors location criteria. Currently ASA supports:
- providing a list of know anchors identifiers;
- looking for anchors near the device using device sensors
- looking for anchors near other anchors

The challenge with Hololens is that if we try to use the device sensors to detect near anchors, well, it (usually) never works.

So what one can do is persist the ASA unique identifiers to a database and, for instance, use a QR Code for each space/room and correlate the QR Code with the ASA Ids.

For this sample what I did was simple ask for a local file to store the generated ASA ID. When the session is started the app asks for a file. If none is provided that it assumes no anchors exist. 
