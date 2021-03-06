﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using lenticulis_gui.src.App;

namespace lenticulis_gui.src.Dialogs
{
    /// <summary>
    /// Interaction logic for LayerSelectWindow.xaml
    /// </summary>
    public partial class LayerSelectWindow : MetroWindow
    {
        public int selectedLayer = -1;

        public LayerSelectWindow(List<String> layers)
        {
            InitializeComponent();

            Title = LangProvider.getString("CHOOSE_LAYER_WINDOW_TITLE");

            // prepare listbox items
            ListBoxItem lbi;
            LayerListBox.Items.Clear();

            // at first, include "general" item, which means "all merged layers"
            lbi = new ListBoxItem();
            lbi.Content = LangProvider.getString("ALL_LAYERS_MERGED");
            LayerListBox.Items.Add(lbi);

            // if there are some layers
            if (layers != null)
            {
                // go through each of them
                foreach (String layer in layers)
                {
                    // and add it as listbox item
                    lbi = new ListBoxItem();
                    lbi.Content = layer;
                    LayerListBox.Items.Add(lbi);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // store the selection and close dialog - the caller will take the selection and use it
            selectedLayer = LayerListBox.SelectedIndex;
            Close();
        }
    }
}
