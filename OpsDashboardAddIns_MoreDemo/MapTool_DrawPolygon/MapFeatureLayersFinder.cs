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
using System.Text;
using System.Threading.Tasks;
using client = ESRI.ArcGIS.Client;

namespace OpsDashboardAddIns_MoreDemo.MapTool_DrawPolygon
{
   /// <summary>
   /// Helper class. This class is used to find all the feature layers displayed on a map
   /// </summary>
   internal class MapFeatureLayersFinder
   {
      //This method returns all the feature layers displayed on a map
      public static List<client.FeatureLayer> GetMapFeatureLayers(client.Map Map)
      {
         List<client.FeatureLayer> FeatureLayers = new List<client.FeatureLayer>();

         //To know more about acceleratedDisplayLayers, read
         //http://resources.arcgis.com/en/help/runtime-wpf/concepts/index.html#//0170000000n4000000
         client.AcceleratedDisplayLayers adLayers = Map.Layers.FirstOrDefault(l => l is client.AcceleratedDisplayLayers) as client.AcceleratedDisplayLayers;

         foreach (client.FeatureLayer layer in adLayers.ChildLayers.OfType<client.FeatureLayer>())
            FeatureLayers.Add(layer);

         return FeatureLayers;
      }
   }
}
