﻿using lenticulis_gui.src.App;
using lenticulis_gui.src.Dialogs;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace lenticulis_gui
{
    public partial class MainWindow
    {
        /// <summary>
        /// Centimeters to one inch
        /// </summary>
        private const float cmToInch = 2.54f;

        /// <summary>
        /// 5% of width
        /// </summary>
        private const float depthPercent = 0.05f;

        /// <summary>
        /// Unit convert multiplier
        /// </summary>
        private float unitConvert = 1;

        /// <summary>
        /// Real width of image
        /// </summary>
        private float realWidth;

        /// <summary>
        /// Frame disparity spacing
        /// </summary>
        private int frameSpacing = 0;

        #region 3D methods
        /// <summary>
        /// Gets layer dpeths from timeline and return them as array
        /// </summary>
        /// <param name="foreground">foreground depth</param>
        /// <param name="background">background depth</param>
        /// <returns>depths array</returns>
        private double[] GetDepthArray(double foreground, double background)
        {
            double[] depthArray = new double[ProjectHolder.LayerCount];
            double tmpDepth = Double.NegativeInfinity;

            for (int i = 0; i < ProjectHolder.LayerCount; i++)
            {
                TextBox tb = LayerDepth.Children[i] as TextBox;

                if (Double.TryParse(tb.Text, out tmpDepth))
                {
                    //if is set in %
                    if (UnitsDepth.SelectedItem.Equals("%"))
                    {
                        if (tmpDepth > 0)
                            tmpDepth = (tmpDepth / 100.0) * foreground;
                        else if (tmpDepth < 0)
                            tmpDepth = (tmpDepth / 100.0) * background * -1;
                    }

                    //must be between foreground and background
                    if (tmpDepth <= foreground && tmpDepth >= background)
                        depthArray[i] = tmpDepth / unitConvert;
                    else
                        return null;
                }
                else
                    return null;
            }

            return depthArray;
        }

        /// <summary>
        /// Sets width text in 3D panel
        /// </summary>
        private void SetWidthText()
        {
            if (ProjectHolder.Width == 0 || ProjectHolder.Dpi == 0)
            {
                Width3D.Content = "";
                return;
            }

            float width = (ProjectHolder.Width / (float)ProjectHolder.Dpi) * unitConvert;
            realWidth = (int)(width * 100) / 100.0f;

            Width3D.Content = realWidth + " " + Units3D.SelectedValue;
        }

        /// <summary>
        /// Set recommended foreground and background values: +- 5% of image width
        /// </summary>
        private void SetDepthBounds()
        {
            if (realWidth != 0.0f)
            {
                float depthBound = realWidth * depthPercent;
                depthBound = (int)(depthBound * 100) / 100.0f;

                Foreground3D.Text = depthBound.ToString();
                Background3D.Text = (-1 * depthBound).ToString();
            }
        }

        /// <summary>
        /// Set frame spacing text
        /// </summary>
        private void SetSpacingText()
        {
            //initial
            double distance = Double.NegativeInfinity;
            double angle = Double.NegativeInfinity;

            if (Double.TryParse(ViewAngle3D.Text, out angle) && Double.TryParse(ViewDist3D.Text, out distance))
            {
                //must be higher than 0
                if (distance > 0.0 && angle > 0.0)
                {
                    //show frame spacing
                    frameSpacing = Generator3D.CalculateZoneDistance(distance / unitConvert, angle, ProjectHolder.ImageCount);
                    FrameSpacing3D.Content = frameSpacing.ToString();
                }

                //enable / disable generate button
                if (frameSpacing >= 1)
                    Generate3D.IsEnabled = true;
                else
                    Generate3D.IsEnabled = false;
            }
        }

        /// <summary>
        /// Reset values in 3D panel
        /// </summary>
        public void PropertyChanged()
        {
            SetWidthText();
            SetDepthBounds();
            SetSpacingText();
            DepthBox_PropertyChanged(null, null);
        }

        #endregion 3D methods

        #region 3D listeners

        /// <summary>
        /// Action to generate shifts for 3D print
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Generate3D_Click(object sender, RoutedEventArgs e)
        {
            if (!ProjectHolder.ValidProject || timelineList.Count == 0)
                return;

            //Input values
            double viewDist, viewAngle, foreground, background;

            if (Double.TryParse(ViewDist3D.Text, out viewDist) && Double.TryParse(ViewAngle3D.Text, out viewAngle) && Double.TryParse(Foreground3D.Text, out foreground) && Double.TryParse(Background3D.Text, out background))
            {
                LoadingWindow lw = new LoadingWindow("3D");
                lw.Show();

                //get depths of layers
                double[] depthArray = GetDepthArray(foreground, background);

                if (depthArray == null)
                {
                    MessageBox.Show(LangProvider.getString("INVALID_DEPTH"), LangProvider.getString("INVALID_DEPTH_TITLE"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //set new positions
                Generator3D.Generate3D(viewDist / unitConvert, viewAngle, ProjectHolder.ImageCount, ProjectHolder.Width, ProjectHolder.Dpi, timelineList, depthArray);

                //repaint result
                RepaintCanvas();

                System.Threading.Thread.Sleep(1000); //show window and wait to inform user about successfull generate
                lw.Close();
            }
            else
                MessageBox.Show(LangProvider.getString("INVALID_3D_PARAMETERS"), LangProvider.getString("INVALID_3D_PARAMETERS_TITLE"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Calculates frame spacing and enable 3D generate button if higher than 0.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewDist3D_Changed(object sender, TextChangedEventArgs e)
        {
            if (!ProjectHolder.ValidProject)
                return;

            if (ViewAngle3D.Text.Trim().Equals("") || ViewDist3D.Text.Trim().Equals(""))
            {
                FrameSpacing3D.Content = "";
                Generate3D.IsEnabled = false;

                return;
            }

            SetSpacingText();
        }

        /// <summary>
        /// Fill combobox with length units enum values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Units3D_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            //values of length units enumerator
            var values = LengthUnits.GetValues(typeof(LengthUnits));

            cb.ItemsSource = values;
            cb.SelectedItem = LengthUnits.@in;
        }

        /// <summary>
        /// Change unit conversion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Units3D_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //convert
            if (Units3D.SelectedItem.Equals(LengthUnits.cm))
                unitConvert = cmToInch;
            else if (Units3D.SelectedItem.Equals(LengthUnits.@in))
                unitConvert = 1;

            //update UnitsDepth ComboBox
            if (UnitsDepth.Items.Count > 0)
            {
                string selectedItem = UnitsDepth.SelectedItem.ToString();

                UnitsDepth.Items.RemoveAt(0);
                UnitsDepth.Items.Insert(0, Units3D.SelectedItem);

                //if selected != % change selected item to new units
                if (!selectedItem.Equals("%"))
                    UnitsDepth.SelectedItem = Units3D.SelectedItem;
            }

            PropertyChanged();
        }

        /// <summary>
        /// ComboBox Loaded method for UnitsDepth in Timeline
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnitsDepth_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            cb.Items.Add(Units3D.SelectedItem);
            cb.Items.Add("%");
            cb.SelectedItem = Units3D.SelectedItem;
        }

        /// <summary>
        /// Listener that controls layer depths if they're between foreground and background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double foreground;
            double background;

            if (UnitsDepth.SelectedItem == null)
                return;

            //if units are in % bound are 100, ale parse input
            string selectedItem = UnitsDepth.SelectedItem.ToString();
            if(selectedItem == "%") 
            {
                foreground = 100.0;
                background = -100.0;
            }
            else if (!(Double.TryParse(Foreground3D.Text, out foreground) && Double.TryParse(Background3D.Text, out background)))
                return;

            TextBox tb = sender as TextBox;

            //parse textbox and change background if is out of bounds
            double value;
            if (Double.TryParse(tb.Text, out value))
            {
                if (value <= foreground && value >= background)
                    tb.Background = Brushes.White;
                else
                    tb.Background = Brushes.Red;
            }
        }

        /// <summary>
        /// Check every depth layer text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepthBox_PropertyChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < ProjectHolder.LayerCount; i++)
            {
                DepthBox_TextChanged((object)LayerDepth.Children[i], null);
            }
        }

        /// <summary>
        /// Disable / enable 3D tool panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TD_Checked(object sender, RoutedEventArgs e)
        {
            if (Panel3D.IsEnabled)
            {
                Panel3D.IsEnabled = false;
                LayerDepth.IsEnabled = false;
                UnitsDepth.IsEnabled = false;
            }
            else
            {
                Panel3D.IsEnabled = true;
                LayerDepth.IsEnabled = true;
                UnitsDepth.IsEnabled = true;

                //if empty inputs generate buttons is disabled
                if (ViewDist3D.Text.Trim().Equals("") || ViewAngle3D.Text.Trim().Equals("") || Foreground3D.Text.Trim().Equals("") || Background3D.Text.Trim().Equals(""))
                    Generate3D.IsEnabled = false;

                PropertyChanged();
            }
        }

        /// <summary>
        /// Creates and show window with anaglyph preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Anaglyph_Click(object sender, RoutedEventArgs e)
        {
            if (canvasList.Count >= frameSpacing && frameSpacing > 0) 
            {
                new AnaglyphPreview(canvasList[0].GetCanvas(), canvasList[frameSpacing - 1].GetCanvas());
            }
            else
            {
                MessageBox.Show(LangProvider.getString("ANAGLYPH_ERROR"), LangProvider.getString("ANAGLYPH_ERROR_TITLE"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion 3D listeners
    }
}
