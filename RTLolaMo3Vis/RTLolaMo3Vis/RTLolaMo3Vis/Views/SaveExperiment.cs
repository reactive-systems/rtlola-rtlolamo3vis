using RTLolaMo3Vis.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace RTLolaMo3Vis.Views
{
    /// <summary>
    /// This page includes input boxes and buttons that allow the text to be
    /// saved-to and loaded-from a file.
    /// </summary>
    public class SaveAndLoadText : ContentPage
    {
        string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp.json");
        Button loadButton, saveButton, pickButton;

        public SaveAndLoadText()
        {
            var input = new Entry { Text = "" };
            var output = new Label { Text = "" };
            var editor = new Editor { HeightRequest = 500, WidthRequest = 200, Placeholder = "Placeholder Text" };
            pickButton = new Button { Text = "Pick File" };

            pickButton.Clicked += async (sender, e) =>
            {
                try
                {
                    var result = await FilePicker.PickAsync();
                    if (result != null)
                    {
                        var Text = $"File Name: {result.FileName}";
                        fileName = result.FullPath;
                        Debug.WriteLine("Picked Path: " + fileName);
                    }

                }
                catch (Exception ex)
                {
                }
            };

            saveButton = new Button { Text = "Save" };

            saveButton.Clicked += (sender, e) =>
            {
                loadButton.IsEnabled = saveButton.IsEnabled = false;
                Debug.WriteLine("saved Path: " + fileName);
                File.WriteAllText(fileName, editor.Text);
                loadButton.IsEnabled = saveButton.IsEnabled = true;
            };

            loadButton = new Button { Text = "Load" };
            loadButton.Clicked += (sender, e) =>
            {
                loadButton.IsEnabled = saveButton.IsEnabled = false;
                Debug.WriteLine("Loaded Path: " + fileName);
                editor.Text = File.ReadAllText(fileName);
                loadButton.IsEnabled = saveButton.IsEnabled = true;
            };
            loadButton.IsEnabled = File.Exists(fileName);

            var buttonLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Children = { saveButton, loadButton, pickButton }
            };

            Content = new StackLayout
            {
                BackgroundColor = Color.Gray,
                Margin = new Thickness(20),
                Children =
                {
                    new Label
                    {
                        Text = "Save and Load Text",
                        FontSize = Device.GetNamedSize (NamedSize.Medium, typeof(Label)),
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label { Text = "Type below and press Save, then Load" },
                    //input,
                    buttonLayout,
                    editor
                }
            };
        }
    }
}
