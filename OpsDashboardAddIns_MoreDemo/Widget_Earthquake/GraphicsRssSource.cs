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

using System.Xml;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;

namespace OpsDashboardAddIns_MoreDemo
{
   /// <summary>
   ///  Provides an observable collection of Graphics representing earthquake locations parsed from XmlNodes, 
   ///  that can then be bound to the GraphicsSource of a GraphicsLayer.
   /// </summary>
   public class GraphicsRssSource : ObservableCollection<Graphic>
   {
      // Brushes used repeatedly in the map tips.
      System.Windows.Media.Brush mapTipBackground = System.Windows.Media.Brushes.LightYellow;
      System.Windows.Media.Brush mapTipBorder = System.Windows.Media.Brushes.DarkGray;

      // Xml Namespaces in the feed.
      XmlNamespaceManager _mgr = null;

      // RSS feed contains lat, longs. If RSS feed contains geometries in different coordinate system to the map to which the
      // geometries will be added, 
      private static ESRI.ArcGIS.Client.Geometry.SpatialReference wgs84 = new ESRI.ArcGIS.Client.Geometry.SpatialReference(4326);

      // Store the symbol used to draw earthquakes.
      Symbol _sym = null;


      public GraphicsRssSource(System.Collections.IEnumerable sourceItems, Symbol sym)
      {
         // Link class to the RSS feed.
         BuildNamespaceManager();

         // Save the symbol for creating graphics with.
         _sym = sym;

         // Pass the RSS feed items to a method to populate this collection of graphics.
         Fill(sourceItems);
      }

      // For each Rss element passed in, create a Graphic from the coordinates and title in the XML and
      // the symbol specified in the constructor, and add to this observavble collection.
      public void Fill(System.Collections.IEnumerable sourceItems)
      {
         // Clear out existing items.
         ClearItems();

         // Iterate items in the collection passed in.
         foreach (var itm in sourceItems)
         {
            // Expecting items that can be case to XmlNode in the collection.
            System.Xml.XmlElement el = itm as System.Xml.XmlElement;
            System.Xml.XmlNode nd = itm as System.Xml.XmlNode;
            if (el != null)
            {
               // Extract the earthquake title information and location from the Xml.

               string earthquakeTitle = el.SelectSingleNode(@"d:title", _mgr).InnerText;
               if (string.IsNullOrEmpty(earthquakeTitle)) continue;

               string latlong = el.SelectSingleNode(@"georss:point", _mgr).InnerText;
               if (string.IsNullOrEmpty(latlong)) continue;

               double lat = 0.0; double lon = 0.0;
               string[] coords = latlong.Split(" ".ToCharArray());
               if (!double.TryParse(coords[0], out lat)) continue;
               if (!double.TryParse(coords[1], out lon)) continue;

               // Turn the quake information into a graphic
               Graphic g = new Graphic()
               {
                  Geometry = new MapPoint(lon, lat, wgs84),
                  Symbol = _sym,
                  MapTip = CreateMapTip(earthquakeTitle)
               };
               // Add the graphic to the collection.
               Add(g);
            }
         }
      }

      /// <summary>
      /// Create a TextBlock to use as a map tip for a graphic, based on the specified string.
      /// </summary>
      /// <param name="mapTipContents">String to use as the main map tip contents.</param>
      /// <returns>A new map tip.</returns>
      private System.Windows.FrameworkElement CreateMapTip(string mapTipContents)
      {
         TextBlock tipText = new TextBlock()
         {
            Text = mapTipContents,
            FontSize = 20,
            FontWeight = System.Windows.FontWeights.ExtraBlack
         };

         StackPanel sp = new StackPanel()
         {
            Orientation = Orientation.Horizontal,
            Margin = new System.Windows.Thickness(5)
         };

         Border tipBorder = new Border()
         {
            BorderBrush = mapTipBorder,
            BorderThickness = new System.Windows.Thickness(2)
         };

         Grid mapTip = new Grid()
         {
            Background = mapTipBackground
         };

         sp.Children.Add(tipText);
         mapTip.Children.Add(sp);
         mapTip.Children.Add(tipBorder);

         return mapTip;
      }

      private void BuildNamespaceManager()
      {
         if (_mgr != null) return;

         _mgr = new XmlNamespaceManager(new NameTable());
         _mgr.AddNamespace("d", "http://www.w3.org/2005/Atom");
         _mgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
         _mgr.AddNamespace("georss", "http://www.georss.org/georss");

      }
   }
}
