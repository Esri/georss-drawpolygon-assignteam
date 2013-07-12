//Copyright 2013 Esri
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.​

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ESRI.ArcGIS.OperationsDashboard;

namespace OpsDashboardAddIns_MoreDemo.Config
{
   public partial class EarthquakeWidgetDialog : Window
   {

      public string Caption { get; private set; }
      public MapWidget MapWidget { get; private set; }

      public EarthquakeWidgetDialog(IEnumerable<MapWidget> mapWidgets, EarthquakeWidget initialWidget)
      {
         InitializeComponent();

         CaptionTextBox.Text = initialWidget.Caption;

         // Retrieve a list of all map widgets from the application and bind this to a combo box 
         // for the user to select a map from.
         IEnumerable<ESRI.ArcGIS.OperationsDashboard.IWidget> mapws = OperationsDashboard.Instance.Widgets.Where(w => w is MapWidget);
         MapWidgetCombo.ItemsSource = mapws;

         // Disable the combo box if no map widgets found.
         if (mapws.Count() < 1)
         {
            MapWidgetCombo.IsEnabled = false;
         }
         else
         {
            MapWidgetCombo.ItemsSource = mapWidgets;
            foreach (MapWidget mw in mapWidgets)
            {

               // If the initial settings match one of the found widgets, select that map widget in the combo box.
               if (mw.Id == initialWidget.MapWidgetId)
               {
                  MapWidgetCombo.SelectedItem = mw;
               }
            }
            if (MapWidgetCombo.SelectedItem == null)
               MapWidgetCombo.SelectedItem = MapWidgetCombo.Items[0];
         }
      }


      private void OKButton_Click(object sender, RoutedEventArgs e)
      {
         Caption = CaptionTextBox.Text;

         // If there is a map widget selected, get the ID.
         MapWidget = MapWidgetCombo.SelectedItem as MapWidget; ;

         // Widget should only be added to the view if a map widget was chosen.
         DialogResult = (MapWidget != null);
      }


   }
}
