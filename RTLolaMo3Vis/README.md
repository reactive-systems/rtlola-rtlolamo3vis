# RTLolaMo<sup>3</sup>Vis
The RTLolaMo<sup>3</sup>Vis app is built using [Xamarin Forms](https://dotnet.microsoft.com/en-us/apps/xamarin/xamarin-forms) and thus C#. 
Xamarin Forms follows the [MVVM principle](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm). Thus, the code is divided into folders according to its functionality. 
**Views** are responsible for the actual UI and defines pages using [XAML](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/xaml/?view=netdesktop-8.0) and C#.
**View Models** update views whenever new data arrives and thus binds data to its visualization.
Lastly, **Models** encapsulate the data the app uses. 

Following the principles of cross-platform development the app supports Android and iOS. However, the here published version is mostly tested on iOS.
Most of the included code is not platform specific and thus in the `RTLolaMo3Vis` folder, platform-specific code is included in the folder `RTLolaMo3Vis.Android` and `RTLolaMo3Vis.iOS` respectively. 
Platform-specific code is only necessary for very few functionalities, e.g., notifications.

## Views
The RTLolaMo<sup>3</sup>Vis App includes a **view** for each page that can be chosen from the Tabbar. These views are found in the `Views` folder.
The visual and clickable elements of each view are defined in the `.xaml` file. The logic behind each button and changing of text and so on is defined in the corresponding `.cs` file.

### ConnectionPage
The `ConnectionPage` files contains the definition for the first, initial page that the app opens. Here, users can define over which type of connection packets should be received. This page also includes the functionality for choosing the event source configuration.


### SpecPage
Next, the `SpecPage.xaml` file includes the button to choose the specification as well as build the monitor when all configurations are chosen.
Note that when a specification is chosen, its text is shown on this page.
Additionally, the `.cs` file of this page include the function accessing the FFI that built the monitor called `new_monitor`. Functions from the FFI are imported using `DllImport`.

### StatsPage
The third page defined in `StatsPage.xaml` and `StatsPage.cs` is the page that shows all statistics. It initially includes a button to choose a verdict sink configuration. After a configuration is chosen and the page is updated, for example, by visiting another page and the clicking back on it, the page shows the empty and later filled plots.

### TabbedHistoryPage
Lastly, the fourth page shows all received triggers sorted by their importance. 
This page is defined in the `TabbedHistoryPage` files. It also allows users to swipe between different pages, either showing all triggers or only triggers of a specific importance.


## ViewModels
Since Xamarin Forms works on the [MVVM principle](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm), i.e., Model-View-ViewModel, the views are updated using ViewModels. 

In our case, a view model updates the trigger page. The `TriggerHistoryViewModel` updates the `TabbedTriggerPage` whenever a new trigger arrives. Similarly, the `TriggerDetailViewModel` gathers the data for a specific trigger to show all its details when it is clicked.

## Services
In our app, services store data for view models and views. This includes the data for all plots as well as the data for the trigger history page.

### TriggerDataStore
The `TriggerDataStore` houses all received triggers, sorted according to their importance and time they were received at. Whenever a new trigger arrives, the view model connected to the data store, i.e., the `TriggerPageViewModel`, updates the `TriggerPage`.

### MultipleChartsDataStore
The `MultipleChartsDataStore` has all data points that need to be plotted.
These points are sorted by the plot and series they belong to. A plot can contain multiple series, e.g., lines of data points.
It is directly connected to the `StatsPage`, i.e., the page that shows all plots.
Since it uses `ObservableCollection` objects, the view is updated whenever these collections are updated without necessitating a view model.


## Models
The models include all custom data structures we use. As mentioned, these encapsulate the data the app needs and thus prepares it for visualization.

### Monitor
The monitor is a wrapper for the raw pointer to the monitor object. It is defined as a singleton and ensures that there is only ever one pointer.

### SpecTrigger
This data structure is used in the `TriggerDataStore` to save all data related to a trigger, such as the message and importance.

### ConnectionReceiver
The connection receives data sent to the app for monitoring through the specified channel. It then passes the received message directly to the previously built monitor via the `AcceptMessage` function.
If no monitor is built, it saves the data in an accumulation array for interpretation later on.

Note that the app makes no attempt whatsoever of interpreting the data, this is all left to the monitor.

### DataPointWrapper
A `DataPointWrapper` encodes all data a single data point in the visualization carries., i.e., its y- and x-axis values, which plot and series it belongs to, and when it arrived in the app.

## Including an FFI
An FFI must be included in either the `.Android` or the `.iOS` folder, depending on which architecture the app is used for. Since we experimented mostly with iOS, we include a short explanation of how to include an FFI suitable for iOS here.

### Building the FFI
As detailed in the `README` file in the `monitor-c-ffi` folder, iOS needs a fat library as an FFI, which can be built from our monitor interface using `cargo lipo`

### Including the file in the app
The `.a` file can then be imported into the `RTLolaMo3Vis.iOS` folder. Note that, by convention, the file name should start with `lib`.

### Building the app
To build an iOS app that includes an FFI, we must include an additional build argument 
```
build argument needs to go here
```
Note that `libmonitor_interface` should be replaced by the currently used file name.

### :warning: Warning
Whenever a new `.a` file is included, and you want to rebuild the app, clean the solution first!
Without cleaning the solution, the built simply uses the old version of the updated `.a` file.


### Common issues
Some common issues we faced include:
- using a `.a` file built for the wrong architecture
- misspelling function names from the FFI when using `DllImport`
- not updating the additional built arguments when updating the `.a` file
- not paying close attention to which actor owns, i.e., allocates and garbage-cleans objects. Either the app or the FFI code owns an object, double freeing an object for example leads to segmentation faults.

## Note 
If you want to learn more about how to include FFIs in C# or Xamarin Forms, we recommend looking into *managed* and *unmanaged* code, as the actual C# code running is managed code, while the code the FFI executes is unmanaged.
Thus, interacting with unmanaged code also necessitates handling data structures and data types accordingly.


## Copyright
Copyright (C) CISPA - Helmholtz Center for Information Security 2024. Authors: Jan Baumeister, Jan Kautenburger, and Clara Rubeck.
