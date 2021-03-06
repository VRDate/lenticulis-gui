﻿using lenticulis_gui.src.App;
using lenticulis_gui.src.Containers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace lenticulis_gui
{
    /// <summary>
    /// Timeline item
    /// </summary>
    public class TimelineItem : Grid, IHistoryStorable<TimelineItemHistory>
    {
        /// <summary>
        /// Text printed on timeline item
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Resize panel on the right
        /// </summary>
        public WrapPanel RightResizePanel;

        /// <summary>
        /// Resize panel on the left
        /// </summary>
        public WrapPanel LeftResizePanel;

        /// <summary>
        /// Context menu items
        /// </summary>
        public MenuItem DeleteMenuItem, SpreadMenuItem, TransformMenuItem, LayerUp, LayerDown;

        /// <summary>
        /// Size of resize panel
        /// </summary>
        private const int sizeChangePanelWidth = 5;

        /// <summary>
        /// Data storage class
        /// </summary>
        private LayerObject dataObject;

        /// <summary>
        /// The only one constructor, just retains item settings and position
        /// </summary>
        /// <param name="layer">ID of layer, where does this item belong</param>
        /// <param name="column">keyframe (column) where this item starts</param>
        /// <param name="length">how many keyframes (columns) does this item occupy?</param>
        /// <param name="text">text present on this control</param>
        public TimelineItem(int layer, int column, int length, string text)
            : base()
        {
            // create new layerobject assigned to this timeline item
            dataObject = new LayerObject();

            // sets position in grid
            SetPosition(layer, column, length);
            this.Text = text;
            this.dataObject.Visible = true;

            // assigns color
            this.Background = new SolidColorBrush(Colors.LightSkyBlue);
            // and some margin
            this.Margin = new Thickness(0, 0, 0, 1);

            // add label on it, to tell the user, what's the image name
            string labelContent = this.Text;
            if (this.Text.Length > 15)
            {
                labelContent = this.Text.Substring(0, 14) + "...";
            }

            System.Windows.Controls.Label label = new System.Windows.Controls.Label() { Content = labelContent };
            label.Margin = new Thickness(8, 2, 0, 0);
            this.Children.Add(label);

            // Alignment of this item
            this.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            this.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

            // adds control helpers, like resize panels, visibility button and context menu
            AddResizePanel();
            AddVisibilityButton();
            AddContextMenu();
        }

        /// <summary>
        /// Set grid settings - position and size (span)
        /// </summary>
        private void SetGridSettings()
        {
            Grid.SetColumn(this, this.dataObject.Column);
            Grid.SetRow(this, this.dataObject.Layer);
            Grid.SetColumnSpan(this, this.dataObject.Length);
        }

        /// <summary>
        /// Add visibility button
        /// </summary>
        private void AddVisibilityButton()
        {
            // button used for toggling visibility
            ToggleButton btn = new ToggleButton()
            {
                Width = 20,
                Height = 20,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 2 * sizeChangePanelWidth, 0),
                Content = new Image()
                {
                    // default is visible with "eye" icon
                    Source = Utils.iconResourceToImageSource("Eye")
                }
            };

            // hook click event - toggle visibility state
            btn.Click += new RoutedEventHandler(delegate(object o, RoutedEventArgs args)
            {
                ToggleButton source = (ToggleButton)o;
                Image content = (Image)source.Content;
                LayerObject lobj = ((TimelineItem)source.Parent).GetLayerObject();

                // if visible, make it invisible
                if (lobj.Visible)
                {
                    lobj.Visible = false;
                    // change icon to strikeout eye
                    content.Source = Utils.iconResourceToImageSource("EyeStrikeOut");
                }
                else
                {
                    lobj.Visible = true;
                    // change icon back to normal eye
                    content.Source = Utils.iconResourceToImageSource("Eye");
                }

                // repaint stuff on canvases
                RepaintChange();
            });

            this.Children.Add(btn);
        }

        /// <summary>
        /// Add resize panels to timelien item
        /// </summary>
        private void AddResizePanel()
        {
            RightResizePanel = new WrapPanel();
            LeftResizePanel = new WrapPanel();

            RightResizePanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            RightResizePanel.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            RightResizePanel.Width = sizeChangePanelWidth;
            RightResizePanel.Background = Brushes.SteelBlue;
            RightResizePanel.Cursor = System.Windows.Input.Cursors.SizeWE;

            LeftResizePanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            LeftResizePanel.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            LeftResizePanel.Width = sizeChangePanelWidth;
            LeftResizePanel.Background = Brushes.SteelBlue;
            LeftResizePanel.Cursor = System.Windows.Input.Cursors.SizeWE;

            this.Children.Add(RightResizePanel);
            this.Children.Add(LeftResizePanel);
        }

        /// <summary>
        /// Add context menu to item
        /// </summary>
        private void AddContextMenu()
        {
            ContextMenu cMenu = new ContextMenu();

            // delete item - for removing timeline item from project
            DeleteMenuItem = new MenuItem()
            {
                Header = LangProvider.getString("REMOVE_TIMELINE_ITEM"),
                Icon = new Image()
                {
                    Source = Utils.iconResourceToImageSource("Erase")
                }
            };

            // spread to whole layer - for spreading from image 0 to last one
            SpreadMenuItem = new MenuItem()
            {
                Header = LangProvider.getString("SPREAD_TIMELINE_ITEM")
            };

            // option for opening transformations window
            TransformMenuItem = new MenuItem()
            {
                Header = LangProvider.getString("TRANSFORMATIONS_TIMELINE_ITEM")
            };

            // option for moving parent layer up
            LayerUp = new MenuItem()
            {
                Header = LangProvider.getString("LAYER_UP")
            };

            // option for moving parent layer down
            LayerDown = new MenuItem()
            {
                Header = LangProvider.getString("LAYER_DOWN")
            };

            cMenu.Items.Add(DeleteMenuItem);
            cMenu.Items.Add(SpreadMenuItem);
            cMenu.Items.Add(TransformMenuItem);
            cMenu.Items.Add(new Separator());
            cMenu.Items.Add(LayerUp);
            cMenu.Items.Add(LayerDown);

            this.ContextMenu = cMenu;
        }

        /// <summary>
        /// Set position of timeline item in grid
        /// </summary>
        /// <param name="layer">layer number (row)</param>
        /// <param name="column">column number</param>
        /// <param name="length">length (column span)</param>
        public void SetPosition(int layer, int column, int length)
        {
            // all those parameters needs to be zero or greater
            if (column >= 0 && layer >= 0 && length > 0)
            {
                this.dataObject.Layer = layer;
                this.dataObject.Column = column;
                this.dataObject.Length = length;

                SetGridSettings();

                RepaintChange();
            }
        }

        /// <summary>
        /// Repaint canvases after change
        /// </summary>
        private void RepaintChange()
        {
            // main window object
            MainWindow mw = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mw == null)
                return;

            // repaint canvas
            if (mw.Panel3D.IsEnabled)
                mw.Generate3D();
            else
                mw.RepaintCanvas();
        }

        /// <summary>
        /// True if timeline item is in position [row, column]
        /// </summary>
        /// <param name="row">row number</param>
        /// <param name="column"> column number</param>
        /// <returns>True if item is in position [row, column]</returns>
        public bool IsInPosition(int row, int column)
        {
            return (this.dataObject.Layer == row && column >= this.dataObject.Column && column < (this.dataObject.Column + this.dataObject.Length));
        }

        /// <summary>
        /// True if timelien item is in column
        /// </summary>
        /// <param name="column">Column number</param>
        /// <returns>true if item is in column</returns>
        public bool IsInColumn(int column)
        {
            return (column >= this.dataObject.Column && column < (this.dataObject.Column + this.dataObject.Length));
        }

        /// <summary>
        /// Retrieves data holder object
        /// </summary>
        /// <returns>data holder object</returns>
        public LayerObject GetLayerObject()
        {
            return dataObject;
        }

        /// <summary>
        /// Returns new history object
        /// </summary>
        /// <returns></returns>
        public TimelineItemHistory GetHistoryItem()
        {
            return new TimelineItemHistory()
            {
                UndoColumn = dataObject.Column,
                UndoRow = dataObject.Layer,
                UndoLength = dataObject.Length,
                AddAction = false,
                RemoveAction = false,
                Instance = this
            };
        }

        /// <summary>
        /// Overrides default method; returns just text from control label
        /// </summary>
        /// <returns>text identifier</returns>
        public override string ToString()
        {
            return Text;
        }
    }
}

