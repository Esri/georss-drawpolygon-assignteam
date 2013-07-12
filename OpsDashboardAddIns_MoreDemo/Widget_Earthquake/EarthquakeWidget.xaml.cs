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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OpsDashboardAddIns_MoreDemo
{
   [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
   [ExportMetadata("DataSourceRequired", false)]
   [ExportMetadata("DisplayName", "Earthquake Widget")]
   [ExportMetadata("Description", "An add-in that displays recent earthquakes on a map.")]
   [ExportMetadata("ImagePath", "/OpsDashboardAddIns_MoreDemo;component/Images/WarningRed.png")]
   [DataContract]
   public partial class EarthquakeWidget : UserControl, IWidget, IMapWidgetConsumer
   {
      // Reference to the map to which earthquake graphics should be added.
      client.Map _map = null;

      // The separate graphics layer to contain earthquake graphics, and symbol to draw them.
      client.GraphicsLayer _allQuakes = null;
      client.GraphicsLayer _graphics = null;
      client.Symbols.SimpleMarkerSymbol _sym = null;

      //XmlNamespaceManager _xmlNamespaces = null;
      XmlDataProvider _xmlData = null;

      GraphicsRssSource _rssGraphics = null;

      public EarthquakeWidget()
      {
         InitializeComponent();

         Caption = "New Earthquake Widget";

         // Retrieve the symbols used to show earthquake epicenters, defined in the Widget.xaml resources.
         _sym = (client.Symbols.SimpleMarkerSymbol)this.TryFindResource("EpicenterSymbol");

         // Set up XmlDataProvider here, so we can later change the source property if required.
         _xmlData = this.TryFindResource("atomProvider") as XmlDataProvider;

         // Hook to the data change event on this.
         _xmlData.DataChanged += _xmlData_DataChanged;

      }

      #region IWidget members

      [DataMember(Name = "id")]
      public string Id { get; set; }

      [DataMember(Name = "caption")]
      public string Caption { get; set; }

      public void OnActivated()
      {
         // Update the widget from retrieved settings.
         this.Caption = Caption;
         this.MapWidgetId = MapWidgetId;

         // Find the MapWidget and Map that earthquakes should be drawn onto.
         GetMapAndAddEarthquakes();
      }

      public void OnDeactivated()
      {
         // There is no need to remove graphics from the map, as the map
         // will be thrown away after the widget is deactivated.
      }

      public bool CanConfigure
      {
         // Configuration is required to choose the map to show earthquakes on.
         get { return true; }
      }

      public bool Configure(Window owner, IList<ESRI.ArcGIS.OperationsDashboard.DataSource> dataSources)
      {
         // Allow author to configure the map widget that the earthquakes are shown in.

         // Get a list of all map widgets.
         IEnumerable<MapWidget> mapWidgets = ESRI.ArcGIS.OperationsDashboard.OperationsDashboard.Instance.Widgets.OfType<MapWidget>();

         // Show the configuration dialog, passing in reference to find current MapWidgetId, if set.
         Config.EarthquakeWidgetDialog dialog = new Config.EarthquakeWidgetDialog(mapWidgets, this) { Owner = owner };

         // If the user cancels the configuation dialog, indicate that configuration failed and the widget should not
         // be added to the current confirguration.
         if (dialog.ShowDialog() != true)
            return false;

         // Retrieve the selected values for the properties from the configuration dialog.
         Caption = dialog.Caption;
         MapWidgetId = dialog.MapWidget.Id;

         // If re-configuring, then ensure any graphics previously added to either map are removed.
         if (_map != null)
         {
            if ((_graphics != null) && (_map.Layers.Contains(_graphics)))
            {
               _graphics.ClearGraphics();
               _map.Layers.Remove(_graphics);
               _graphics = null;
            }
         }

         // Get the map from the selected widget.
         GetMapAndAddEarthquakes();

         // Indicate that configuration was successful and the widget can be added to the configuration.
         return true;
      }

      #endregion

      #region IMapWidgetConsumer Members

      /// <summary>
      /// Serialize the ID of the map widget the author chose to use.
      /// </summary>
      [DataMember(Name = "mapWidgetId")]
      public string MapWidgetId { get; set; }

      #endregion

      #region Helper functions

      /// <summary>
      ///  Called when the feed has been read.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void _xmlData_DataChanged(object sender, EventArgs e)
      {
         // When feed is finished updating, then we can refresh the graphic layer.
         AddEarthquakesToMap();
      }

      /// <summary>
      /// Gets the Map from a specific MapWidget that is identified by ID, accounting for widgets that may not yet be initialized
      /// by hooking an event handler and waiting for initialization of uninitialized maps. Once retrieved, add earthquakes to the map
      /// based on the currently selected feed.
      /// </summary>
      private void GetMapAndAddEarthquakes()
      {
         MapWidget mapWidget = OperationsDashboard.Instance.Widgets.Where(w => w.Id == MapWidgetId).FirstOrDefault() as MapWidget;
         if (mapWidget != null)
         {
            if (mapWidget.IsInitialized)
            {
               // Assume here that Map is not null, as the widget is initialized.
               _map = mapWidget.Map;
               // Add earthquakes.
               AddEarthquakesToMap();
            }
            else
            {
               // Wait for initialization of the map widget.
               mapWidget.Initialized += (sender, e) =>
               {
                  _map = mapWidget.Map;
                  // Add earthquakes.
                  AddEarthquakesToMap();
               };
            }
         }
      }

      /// <summary>
      /// Extracts earthquake information from the feed and turns this into a graphics layer in the map. 
      /// </summary>
      private void AddEarthquakesToMap()
      {
         if ((_map == null) || (_xmlData == null) || (_xmlData.Document == null)) return;

         //XmlDocument doc = _xmlData.Document as XmlDocument;
         if (_xmlData.Document.ChildNodes == null) return;

         // extract earthquake nodes.
         XmlNodeList entries = _xmlData.Document.SelectNodes("//d:feed//d:entry", _xmlData.XmlNamespaceManager);

         // Create new graphics container object, or fill existing one, with the entries from the feed.
         var itmsSource = _xmlData.Document.ChildNodes;
         if (_rssGraphics == null)
         {
            _rssGraphics = new GraphicsRssSource(entries, _sym);
         }
         else
         {
            _rssGraphics.Fill(entries);
         }

         // If the graphics layer is not already created, then create it and bind to the collection of earthquake graphics,
         // and add the layer to the map.
         if (_allQuakes == null)
         {
            _allQuakes = new client.GraphicsLayer();
            _allQuakes.ID = "EarthquakeWidget.AllQuakesGraphicsLayer";
            _allQuakes.GraphicsSource = _rssGraphics;
            _map.Layers.Add(_allQuakes);
         }
      }

      /// <summary>
      /// Handler for when the feed list box selection changes.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void FeedListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // Update the data provider to use the new Source selected by the user.
         if (_xmlData != null)
         {
            if (e.AddedItems.Count == 1)
            {
               Feed selectedFeed = e.AddedItems[0] as Feed;

               // Update the feed, data will be refreshed asynchronously.
               _xmlData.Source = selectedFeed.FeedUri;
               //AddEarthquakesToMap();
            }
         }
      }
      #endregion
   }
}
