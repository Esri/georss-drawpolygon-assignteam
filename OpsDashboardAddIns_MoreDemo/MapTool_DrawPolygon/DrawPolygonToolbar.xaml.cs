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

using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client;
using System.Collections.Generic;
using ESRI.ArcGIS.Client.Geometry;
using System;

namespace OpsDashboardAddIns_MoreDemo.MapTool_DrawPolygon
{
   /// <summary>
   /// This toolbar allows users to draw a polygon to represent a search area, and to fill out attributes of the search area
   /// </summary>
   public partial class DrawPolygonToolbar : UserControl, IMapToolbar
   {
      #region Class members
      private MapWidget MapWidget = null;
      private client.FeatureLayer SearchAreaLayer = null;
      private client.Editor Editor = null;
      private Graphic NewSearchArea = null;
      private IDictionary<string, object> SearchAreaAttributes = new Dictionary<string, object>();
      #endregion

      public DrawPolygonToolbar(MapWidget mapWidget, client.FeatureLayer searchAreaFL)
      {
         InitializeComponent();
         DataContext = this;
         
         #region Initialize the map widget and the search area feature layer
         MapWidget = mapWidget;
         if (!mapWidget.Map.IsInitialized)
            return;
         SearchAreaLayer = searchAreaFL;
         SearchAreaLayer.DisableClientCaching = true;
         #endregion

         #region MapWidget is not null, initialize the editor tool
         Editor = new client.Editor()
         {
            LayerIDs = new string[] { SearchAreaLayer.ID },
            Map = mapWidget.Map,
            GeometryServiceUrl = @"http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer"
         };
         Editor.EditCompleted += Editor_EditCompleted;
         Editor.EditorActivated += Editor_EditorActivated;
         #endregion
      }

      #region IMapToolbar members
      /// <summary>
      /// OnActivated is called when the toolbar is installed into the map widget.
      /// </summary>
      public void OnActivated()
      {
      }

      /// <summary>
      ///  OnDeactivated is called before the toolbar is uninstalled from the map widget. 
      /// </summary>
      public void OnDeactivated()
      {
         Editor = null;
         SearchAreaAttributes = null;
         NewSearchArea = null;
      }
      #endregion

      #region Events related to the Draw Search Area button
      //Set the Add command when the Draw button is clicked
      private void btnDraw_Click_1(object sender, RoutedEventArgs e)
      {
         btnDraw.Command = Editor.Add;     
      }

      //When editor is activated (user starts drawing), Any previously created graphics should be cleared
      void Editor_EditorActivated(object sender, Editor.CommandEventArgs e)
      {
         if (NewSearchArea != null)
            SearchAreaLayer.UndoEdits(NewSearchArea);
      }

      //Save the edit result to NewSearchArea when the shape of the search area has been created
      void Editor_EditCompleted(object sender, Editor.EditEventArgs e)
      {
         try
         {
            //Get the changes from the event arg
            IEnumerator<Editor.Change> changes = e.Edits.GetEnumerator();

            //Return if there are problems with the edits 
            if (changes.MoveNext() == false)
               return;

            //Since we are adding a new polygon the number of edits will always be one.
            //Save the new grphic to NewSearchArea; Attributes will be filled out later
            NewSearchArea = changes.Current.Graphic;

            if (IsValidGeometry(NewSearchArea.Geometry) == false)
               MessageBox.Show("invalid search area's geometry");
         }
         catch (Exception)
         {
            MessageBox.Show("Error adding new polygon. Please retry");
         }
      }
      #endregion

      #region Event related to the Edit Attributes button
      //Show attribute page when clicked
      private void btnAttribute_Click_1(object sender, RoutedEventArgs e)
      {
         //Show attribute page
         AttributePage atbs = new AttributePage(SearchAreaLayer);
         if (atbs.ShowDialog() == false)
            return;

         //Copy user's input attributes
         SearchAreaAttributes = atbs.Attributes;
      }
      #endregion

      #region Event related to the Done button
      //Save edits when clicked
      private void DoneButton_Click(object sender, RoutedEventArgs e)
      {
         //Populate attributes and save only if the graphic was drawn correctly
         if (NewSearchArea != null)
         {
            //Fill out the attributes of the new search area with user's input
            object attribute;
            List<string> attributeNames = new List<string>(NewSearchArea.Attributes.Keys);
            foreach (string attributeName in attributeNames)
               if (SearchAreaAttributes.TryGetValue(attributeName, out attribute))
                  NewSearchArea.Attributes[attributeName] = attribute;

            //Save the new search area
            SearchAreaLayer.SaveEdits();
         }

         // When the user is finished with the toolbar, revert to the configured toolbar.
         if (MapWidget != null)
            MapWidget.SetToolbar(null);
      }
      #endregion

      #region Helper methods
      //Validate a given geometry
      private bool IsValidGeometry(Geometry geometry)
      {
         if (geometry == null || geometry.Extent == null || geometry.Extent.Width == 0 || geometry.Extent.Height == 0)
            return false;
         else
            return true;
      }
      #endregion

   }
}
