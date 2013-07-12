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

using ESRI.ArcGIS.OperationsDashboard;
using OpsDashboardAddIns_MoreDemo.MapTool_DrawPolygon;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using client = ESRI.ArcGIS.Client;

namespace OpsDashboardAddIns_MoreDemo.Config
{
   /// <summary>
   /// This configuration dialog allows user to select the search area layer from the list of all polygon layers on map
   /// </summary>
   public partial class DrawPolygonMapToolDialog : Window
   {
      public client.FeatureLayer PolygonLayer { get; set; }

      public DrawPolygonMapToolDialog(MapWidget mapWidget)
      {
         InitializeComponent();
         DataContext = this;

         if (mapWidget == null)
            return;

         //Create a list and store the polygon layers
         List<client.FeatureLayer> polygonLayers = new List<client.FeatureLayer>();

         foreach (client.FeatureLayer layer in MapFeatureLayersFinder.GetMapFeatureLayers(mapWidget.Map))
         {
            if (layer.LayerInfo.GeometryType == client.Tasks.GeometryType.Polygon)
               polygonLayers.Add(layer);
         }

         //Configure the layer comboboxe properties
         cmbLayers.ItemsSource = polygonLayers;
         cmbLayers.SelectedItem = polygonLayers[0];
         cmbLayers.DisplayMemberPath = "DisplayName";
      }

      private void btnOK_Click_1(object sender, RoutedEventArgs e)
      {
         PolygonLayer = cmbLayers.SelectedItem as client.FeatureLayer;

         if (PolygonLayer != null)
            DialogResult = true;
         else
            DialogResult = false;

         this.Close();
      }
   }
}
