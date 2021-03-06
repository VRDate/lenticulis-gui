﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using MahApps.Metro.Controls;
using lenticulis_gui.src.App;
using lenticulis_gui.src.Containers;
using lenticulis_gui.src.SupportLib;
using lenticulis_gui.src.Dialogs;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.ComponentModel;

namespace lenticulis_gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        /// <summary>
        /// timeline column dimensions
        /// </summary>
        private const int rowHeight = 30;
        private const int columnMinWidth = 90;

        /// <summary>
        /// timeline item list
        /// </summary>
        public List<TimelineItem> timelineList;

        /// <summary>
        /// canvas list
        /// </summary>
        private List<WorkCanvas> canvasList;

        /// <summary>
        /// layer number for layer move up/down
        /// </summary>
        private int layerContext;

        /// <summary>
        /// Selected tranfromation tool
        /// </summary>
        public static TransformType SelectedTool = TransformType.Translation;

        public MainWindow()
        {
            InitializeComponent();

            GetDrives();

            // Get available languages
            Dictionary<String, String> availableLangs = LangProvider.GetAvailableLangs();
            // and fetch them to menu items as available options
            LangChooserItem.Items.Clear();
            foreach (KeyValuePair<String, String> kvp in availableLangs)
            {
                // create new menu item
                MenuItem mi = new MenuItem();
                mi.Header = kvp.Value;
                // assign click event to change all we want
                mi.Click += new RoutedEventHandler(delegate(object o, RoutedEventArgs args)
                {
                    // change language
                    LangProvider.UseLanguage(kvp.Key);
                    // and update all bindings
                    LangDataSource.UpdateDataSources();
                });
                LangChooserItem.Items.Add(mi);
            }

            this.Closing += MainWindow_Closing;
        }

        /// <summary>
        /// Set image and layer count
        /// </summary>
        /// <param name="imageCount">image count</param>
        /// <param name="layerCount">layer count</param>
        public void SetProjectProperties(int imageCount, int layerCount)
        {
            Timeline.Children.Clear();
            SetImageCount(imageCount);
            AddTimelineHeader();
            AddTimelineLayer(layerCount, false, true, 0.0);

            timelineList = new List<TimelineItem>();
            ProjectHolder.HistoryList = new HistoryList();

            if (ProjectHolder.ViewDistance != 0.0)
                ViewDist3D.Text = ProjectHolder.ViewDistance.ToString();
            if (ProjectHolder.ViewAngle != 0.0)
                ViewAngle3D.Text = ProjectHolder.ViewAngle.ToString();
            if (ProjectHolder.Foreground != 0.0)
                Foreground3D.Text = ProjectHolder.Foreground.ToString();
            if (ProjectHolder.Background != 0.0)
                Background3D.Text = ProjectHolder.Background.ToString();

            RefreshCanvasList();
        }

        /// <summary>
        /// Save routine - asks for file, if needed
        /// </summary>
        /// <returns></returns>
        private bool SaveRoutine(bool saveAs = false)
        {
            if (!ProjectHolder.ValidProject)
                return false;

            if (saveAs || ProjectHolder.ProjectFileName == null || ProjectHolder.ProjectFileName == "")
            {
                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FileName = "projekt";
                dialog.DefaultExt = ".lcp";
                dialog.Filter = LangProvider.getString("LENTICULIS_PROJECT_FILE") + " (.lcp)|*.lcp";

                Nullable<bool> res = dialog.ShowDialog();
                if (res == true)
                {
                    // save project to newly selected file target
                    ProjectSaver.saveProject(dialog.FileName);
                    ProjectHolder.ProjectFileName = dialog.FileName;
                    return true;
                }
                return false;
            }
            else
            {
                // use previously stored filename
                ProjectSaver.saveProject();
                return true;
            }
        }

        #region Tools & buttons listeners

        /// <summary>
        /// Saving button click event hook - invoke save dialog and proceed saving if confirmed and filled correctly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveRoutine();
        }

        /// <summary>
        /// Clicked on project loading
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectHolder.ValidProject)
            {
                MessageBoxResult res = MessageBox.Show(LangProvider.getString("UNSAVED_WORK_CONFIRM_SAVE"), LangProvider.getString("UNSAVED_WORK"), MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                switch (res)
                {
                    // When clicked "Yes", offer saving, and if saving succeeds, proceed to load; otherwise do nothing
                    case MessageBoxResult.Yes:
                        if (!SaveRoutine())
                            return;
                        break;
                    // When clicked "No", discard any changes
                    case MessageBoxResult.No:
                        break;
                    // When clicked "Cancel", just do nothing
                    case MessageBoxResult.Cancel:
                        return;
                }
            }

            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = LangProvider.getString("LENTICULIS_PROJECT_FILE") + " (.lcp)|*.lcp";

            Nullable<bool> dres = dialog.ShowDialog();
            if (dres == true)
            {
                // load project from selected file
                // Create new loading window
                LoadingWindow lw = new LoadingWindow("project");
                // show it
                lw.Show();
                // and disable this window to disallow all operations
                this.IsEnabled = false;

                ProjectLoader.loadProject(dialog.FileName);

                // after image was loaded, enable main window
                this.IsEnabled = true;
                // and close loading window
                lw.Close();

                PropertyChanged3D();
                RepaintCanvas();
            }
        }

        /// <summary>
        /// Creates new project
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ProjectCloseSaveDialog())
                return;

            ProjectHolder.CleanUp();
            Storage.Instance.cleanUp();
            ProjectPropertiesWindow ppw = new ProjectPropertiesWindow();
            ppw.ShowDialog();
        }

        /// <summary>
        /// Show save dialog. Returns false if cancel was selected
        /// </summary>
        /// <returns>False if cancel selected else true</returns>
        private bool ProjectCloseSaveDialog()
        {
            if (ProjectHolder.ValidProject)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show(LangProvider.getString("NEW_PROJECT_CONFIRM_TEXT"), LangProvider.getString("NEW_PROJECT_CONFIRM_TITLE"), MessageBoxButton.YesNoCancel);

                if (messageBoxResult == MessageBoxResult.Cancel)
                    return false;

                if (messageBoxResult == MessageBoxResult.Yes)
                    SaveRoutine();
            }

            return true;
        }

        /// <summary>
        /// Opens dialog with project properties
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProjectPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectHolder.ValidProject)
            {
                ProjectPropertiesWindow ppw = new ProjectPropertiesWindow();
                ppw.ShowDialog();
            }
        }

        /// <summary>
        /// Opens memory usage dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemoryUsageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectHolder.ValidProject)
            {
                HistoryMemoryWindow hmw = new HistoryMemoryWindow();
                hmw.ShowDialog();
            }
        }

        /// <summary>
        /// Change canvas view event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoubleCanvas_Checked(object sender, RoutedEventArgs e)
        {
            // no project loaded / created
            if (!ProjectHolder.ValidProject)
                return;

            SetRangeSlider(0, ProjectHolder.ImageCount - 1);
            ShowDoubleCanvas(0, ProjectHolder.ImageCount - 1);
        }

        /// <summary>
        /// Change canvas view event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SingleCanvas_Checked(object sender, RoutedEventArgs e)
        {
            // no project loaded / created
            if (!ProjectHolder.ValidProject)
                return;

            SetSingleSlider(0);
            ShowSingleCanvas(0);
        }

        /// <summary>
        /// Change tool to translation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Translation_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.SelectedTool = TransformType.Translation;
        }

        /// <summary>
        /// Change tool to scale
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scale_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.SelectedTool = TransformType.Scale;
        }

        /// <summary>
        /// Change tool to rotate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Rotate_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.SelectedTool = TransformType.Rotate;
        }

        /// <summary>
        /// Zoom in action
        /// </summary>
        private void ZoomIn()
        {
            // no project loaded / created
            if (!ProjectHolder.ValidProject)
                return;

            foreach (WorkCanvas wc in canvasList)
            {
                if (wc.CanvasScale < 50.0)
                    wc.CanvasScale *= 1.2;
            }
        }

        /// <summary>
        /// Zoom out action
        /// </summary>
        private void ZoomOut()
        {
            // no project loaded / created
            if (!ProjectHolder.ValidProject)
                return;

            foreach (WorkCanvas wc in canvasList)
            {
                if (wc.CanvasScale > 0.1)
                    wc.CanvasScale /= 1.2;
            }
        }

        /// <summary>
        /// Clicked on zoom in button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomInButton_Clicked(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        /// <summary>
        /// Clicked on zoom out button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomOutButton_Clicked(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        /// <summary>
        /// On resize event - window itself
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // adjust slider size to match the timeline
            SliderPanel.Margin = new Thickness() { Left = 43 + (Timeline.ActualWidth / Timeline.ColumnDefinitions.Count) / 2, Right = (Timeline.ActualWidth / Timeline.ColumnDefinitions.Count) / 2 };
        }

        /// <summary>
        /// Clicked on export button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportButton_Clicked(object sender, RoutedEventArgs e)
        {
            // no project loaded / created
            if (!ProjectHolder.ValidProject)
                return;

            ExportWindow ew = new ExportWindow();
            ew.ShowDialog();
        }

        /// <summary>
        /// Clicked on "About" menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAboutWindow_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }

        /// <summary>
        /// Clicked on GitHub menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenGithub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/LenticularSoftworks");
        }

        /// <summary>
        /// Clicked on licencing menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenLicence_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.gnu.org/copyleft/gpl.html");
        }

        /// <summary>
        /// Clicked on close menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseProgram_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Custom close method. Shows save dialog first
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!ProjectCloseSaveDialog())
                e.Cancel = true;
        }

        /// <summary>
        /// Undo action
        /// </summary>
        private void Undo()
        {
            ProjectHolder.HistoryList.Undo();
            RepaintCanvas();

            SetUndoRedoButtons();
        }

        /// <summary>
        /// Redo action
        /// </summary>
        private void Redo()
        {
            ProjectHolder.HistoryList.Redo();
            RepaintCanvas();

            SetUndoRedoButtons();
        }

        /// <summary>
        /// Add new item to history list
        /// </summary>
        /// <param name="item">History list item</param>
        public void AddToHistoryList(HistoryItem item) 
        {
            ProjectHolder.HistoryList.AddHistoryItem(item);

            SetUndoRedoButtons();
        }

        /// <summary>
        /// Disable / enable undo, redo buttons
        /// </summary>
        private void SetUndoRedoButtons() 
        {
            HistoryList list = ProjectHolder.HistoryList;

            if (list.HistoryListPointer >= 0)
                UndoButton.IsEnabled = true;
            else
                UndoButton.IsEnabled = false;

            if (list.HistoryListPointer < list.HistoryListSize - 1)
                RedoButton.IsEnabled = true;
            else
                RedoButton.IsEnabled = false;
        }

        /// <summary>
        /// Clicked "Undo" button (menu item or toolbar)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        /// <summary>
        /// Clicked "Redo" button (menu item or toolbar)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        /// <summary>
        /// Window keyDown listener
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            //Change resize icon when holding shift
            if (Keyboard.IsKeyDown(Key.LeftShift) && SelectedTool == TransformType.Scale)
            {
                ScaleButton.Content = new Image
                {
                    Source = Utils.iconResourceToImageSource("Resize_lock")
                };
            }

            if (e.Key == Key.Z && Keyboard.IsKeyDown(Key.LeftCtrl))
                Undo();

            if (e.Key == Key.Y && Keyboard.IsKeyDown(Key.LeftCtrl))
                Redo();
        }

        /// <summary>
        /// Window key up listener
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            //Change back resize icon when holding shift
            if ((e.Key == Key.LeftShift || e.SystemKey == Key.LeftShift) && SelectedTool == TransformType.Scale)
            {
                ScaleButton.Content = new Image
                {
                    Source = Utils.iconResourceToImageSource("Resize")
                };
            }

            //hide bounding box on escape button event
            if (e.Key == Key.Escape)
            {
                foreach (var canvas in canvasList)
                    canvas.HideBox();
            }
        }

        /// <summary>
        /// Mouse wheel listener for zomm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //only with ctrl key down
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                return;

            if (e.Delta > 0)
                ZoomIn();
            else if (e.Delta < 0)
                ZoomOut();
        }

        #endregion Tools & buttons listeners
    }
}
