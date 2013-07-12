// Copyright 2013 Esri
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.​

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using OpsDashboardAddIns_MoreDemo.Config;
using System.Collections.Generic;

namespace OpsDashboardAddIns_MoreDemo.MapTool_DrawPolygon
{
   /// <summary>
   /// This map tool allows user to draw a polygon on the map and to input attributes for the polygon.
   /// When finished, the edits will be committed and uploaded 
   /// </summary>
   [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
   [ExportMetadata("DisplayName", "Draw Search Area")]
   [ExportMetadata("Description", "Use this tool to draw a search area")]
   [ExportMetadata("ImagePath", "/OpsDashboardAddIns_MoreDemo;component/Images/feature_class_polygon.png")]
   [DataContract]
   public partial class DrawPolygon : UserControl, IMapTool
   {
      #region Properties
      /// <summary>
      /// The SearchAreaLayerID property is to be persisted when the view gets saved
      /// so that the view will pick the same feature layer as the search area layer when 
      /// the view opens again
      /// </summary>
      [DataMember(Name = "searchAreaFLID")]
      public string SearchAreaLayerID { get; private set; }

      client.FeatureLayer searchAreaLayer = null;
      #endregion

      public DrawPolygon()
      {
         InitializeComponent();
      }

      #region IMapTool
      /// <summary>
      /// The MapWidget property is set by the MapWidget that hosts the map tools. The application ensures that this property is set when the
      /// map widget containing this map tool is initialized.
      /// </summary>
      public MapWidget MapWidget { get; set; }

      /// <summary>
      /// OnActivated is called when the map tool is added to the toolbar of the map widget in which it is configured to appear. 
      /// Called when the operational view is opened, and also after a custom toolbar is reverted to the configured toolbar,
      /// and during toolbar configuration.
      /// </summary>
      public void OnActivated()
      {
         //Retrieve the previously selected search area layer (i.e. when SearchAreaLayerID is not null or empty)
         //If the search area layer has never been set then pick the first polygon layer
         if (!string.IsNullOrEmpty(SearchAreaLayerID))
            searchAreaLayer = MapFeatureLayersFinder.GetMapFeatureLayers(MapWidget.Map).FirstOrDefault(layer => layer.ID == SearchAreaLayerID);
         else
            searchAreaLayer = MapFeatureLayersFinder.GetMapFeatureLayers(MapWidget.Map).FirstOrDefault(layer => layer.LayerInfo.GeometryType == client.Tasks.GeometryType.Polygon);
      }

      /// <summary>
      ///  OnDeactivated is called before the map tool is removed from the toolbar. Called when the operational view is closed,
      ///  and also before a custom toolbar is installed, and during toolbar configuration.
      /// </summary>
      public void OnDeactivated()
      {
         searchAreaLayer = null;
      }

      /// <summary>
      ///  Determines if a Configure button is shown for the map tool.
      ///  User can specify a layer as the search area layer
      /// </summary>
      public bool CanConfigure
      {
         get { return true; }
      }

      /// <summary>
      /// Allow users to specify the search area layer, or use the first polygon layer as default
      /// </summary>
      public bool Configure(System.Windows.Window owner)
      {
         DrawPolygonMapToolDialog configDialog = new DrawPolygonMapToolDialog(MapWidget);   
         if (configDialog.ShowDialog() == false)
         {
            MessageBox.Show("Error selecting search area layer");
            return false;
         }

         searchAreaLayer = configDialog.PolygonLayer;
         SearchAreaLayerID = searchAreaLayer.ID;
         return true;
      }
      #endregion

      /// <summary>
      /// Show the draw polygon toolbar when the tool is clicked
      /// </summary>
      private void Button_Click(object sender, RoutedEventArgs e)
      {
         //Show the draw polygon toolbar if the search area layer is not null
         if (searchAreaLayer == null)
            MessageBox.Show("Search area layer is invalid. Make sure the map has a polygon layer");
         else
            MapWidget.SetToolbar(new DrawPolygonToolbar(MapWidget, searchAreaLayer));

         // Set the Checked property of the ToggleButton to false after work is complete.
         ToggleButton.IsChecked = false;
      }
   }
}
