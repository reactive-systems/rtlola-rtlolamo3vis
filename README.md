# RTLolaMo<sup>3</sup>Vis
This repository contains all code of the RTLolaMo<sup>3</sup>Vis project. This includes the code of the RTLolaMo<sup>3</sup>Vis App as well as the code that build the necessary interface of the RTLola monitor.
The app and some example usages are described in our [RV](https://yeni.cmpe.bogazici.edu.tr/rv24/) paper.

## RTLolaMo<sup>3</sup>Vis App
The code of the app can be found in `RTLolaMo3Vis` where we also include a more detailed README specific to the app.
The app is built using [Xamarin Forms](https://dotnet.microsoft.com/en-us/apps/xamarin/xamarin-forms) and is thus mostly written in `C#`.

## Monitor FFI
The app needs an interface of the RTLola monitor to make the Interpreter that was previously built in Rust usable for the C# code of the app.
We include the code necessary for building this FFI in `monitor-c-ffi`.

## Copyright
Copyright (C) CISPA - Helmholtz Center for Information Security 2024. Authors: Jan Baumeister, Jan Kautenburger, and Clara Rubeck. Based on original work at Universität des Saarlandes (C) 2024. Authors: Jan Baumeister, Jan Kautenburger, and Clara Rubeck.