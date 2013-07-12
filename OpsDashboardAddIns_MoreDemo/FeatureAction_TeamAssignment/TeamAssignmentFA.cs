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
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel.Composition;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OpsDashboardAddIns_MoreDemo.FeatureAction_SMS
{
   /// <summary>
   /// A FeatureAction is an extension to Operations Dashboard for ArcGIS which can be shown when a user right-clicks on
   /// a feature in a widget. 
   /// 
   /// In this sample, user executes the feature action on a search area (a polygon feature), which generates a buffer area around the polygon.
   /// The buffer area will be used to search for the team features (a point layer) that intersect with it.
   /// 
   /// The team features will then be highlighted on map and a team assigmment page will be displayed. User can change the selected teams 
   /// by unselecting the teams on map or on the assignment page. 
   /// 
   /// When team assignment is confirmed team's search area attribute will be updated immediately. 
   /// User can also choose to send SMS to notify the teams about the assigned area.
   /// 
   /// Note:
   /// This tool only assign teams to search areas, it does not clear the unassigned teams from their search areas
   /// 
   /// Data Schema Requirements:
   /// To use this tool, the map must contain at least a polygon layer (the search area layer) and a point layer (the rescue team layer).
   /// The polygon layer must have a display name field (usually automatically set when there's a string field) that stores the search area name;
   /// The point layer must also have a display name field and a string field "SearchAreaName" for the name of the assigned search area. 
   /// The sending SMS function will be enabled if this layer also has the fields "PhoneNumber" and "Carrier".
   /// 
   /// </summary>
   [Export("ESRI.ArcGIS.OperationsDashboard.FeatureAction")]
   [ExportMetadata("DisplayName", "Assign Teams")]
   [ExportMetadata("Description", "Assign Teams to the Search Area")]
   [ExportMetadata("ImagePath", "/OpsDashboardAddIns_MoreDemo;component/Images/TeamAssignment.png")]
   public class TeamAssignmentFA : IFeatureAction
   {
      #region class members
      private client.Tasks.GeometryService _geometryTask;
      private MapWidget MapWidget;
      private DataSource TeamDataSrc = null;
      private List<client.Graphic> TeamsNearby = new List<client.Graphic>();
      private client.FeatureLayer TeamFeatureLayer;
      private string SearchAreaName;
      #endregion

      public TeamAssignmentFA()
      {
         _geometryTask = new client.Tasks.GeometryService(
           "http://tasks.arcgisonline.com/ArcGIS/rest/services/Geometry/GeometryServer");
         _geometryTask.BufferCompleted += GeometryTask_BufferCompleted;
         _geometryTask.Failed += GeometryTaskFailed;
      }

      #region IFeatureAction
      /// <summary>
      ///  Determines if a Configure button is shown for the feature action.
      ///  Provides an opportunity to gather user-defined settings.
      /// </summary>
      /// <value>True if the Configure button should be shown, otherwise false.</value>
      public bool CanConfigure
      {
         get { return false; }
      }

      /// <summary>
      ///  Provides functionality for the feature action to be configured by the end user through a dialog.
      ///  Called when the user clicks the Configure button next to the feature action.
      /// </summary>
      /// <param name="owner">The application window which should be the owner of the dialog.</param>
      /// <returns>True if the user clicks ok, otherwise false.</returns>
      public bool Configure(System.Windows.Window owner)
      {
         return true;
      }

      /// <summary>
      /// Determines if the feature action can be executed based on the specified data source and feature, before the option to execute
      /// the feature action is displayed to the user.
      /// </summary>
      /// <param name="searchAreaDS">The data source which will be subsequently passed to the Execute method if CanExecute returns true.</param>
      /// <param name="searchArea">The data which will be subsequently passed to the Execute method if CanExecute returns true.</param>
      /// <returns>True if the feature action should be enabled, otherwise false.</returns>
      public bool CanExecute(DataSource searchAreaDS, client.Graphic searchArea)
      {
         //Map widget that contains the searchArea data Source
         MapWidget areaMW;
         //Map widget that contains the rescueTeam data Source
         MapWidget teamMW;
         
         try
         {
            #region Check if the polygon data source is valid
            //Check if the searchAreaDS has a display name field
            if (searchAreaDS.DisplayFieldName == null) return false;

            //Check if the map widget associated with the search area dataSource is valid
            if ((areaMW = MapWidget.FindMapWidget(searchAreaDS)) == null) return false;

            //Check if the search area layer is polygon 
            if (areaMW.FindFeatureLayer(searchAreaDS).LayerInfo.GeometryType != client.Tasks.GeometryType.Polygon) return false;
            #endregion

            #region Find the rescue team data source and check if it is consumed by the same mapWidget as the polygon data source
            //Find all the data sources that can possibly be a taem data sources
            //(i.e. are associated with point layers and hase the field "SearchAreaName")
            var teamDataSrcs = OperationsDashboard.Instance.DataSources.Where(ds => IsValidTeamDataSource(ds));
            if (teamDataSrcs == null || teamDataSrcs.Count() == 0)
               return false;

            //For each of the data source retrieved, get its associated mapWidget
            //Check if the polygon feature source is also consumed by the same map widget (i.e. teamMW)
            foreach (DataSource teamDataSrc in teamDataSrcs)
            {
               if ((teamMW = MapWidget.FindMapWidget(teamDataSrc)) == null) return false;
               
               if (teamMW.Id == areaMW.Id)
               {
                  MapWidget = areaMW;
                  TeamDataSrc = teamDataSrc;
                  TeamFeatureLayer = MapWidget.FindFeatureLayer(TeamDataSrc);
                  return true;
               }
            }
            #endregion

            return false;
         }
         catch (Exception)
         {
            return false;
         }
      }

      /// <summary>
      /// Execute is called when user chooses the feature action from the feature actions context menu. Only called if
      /// CanExecute returned true.
      /// </summary>
      public void Execute(DataSource searchAreaDS, client.Graphic searchArea)
      {
         try
         {
            //Clear any running task
            _geometryTask.CancelAsync();

            //Get the map widget and the map that contains the polygon used to generate the buffer
            client.Map mwMap = MapWidget.Map;

            //Define the params to pass to the buffer operation
            client.Tasks.BufferParameters bufferParameters = CreateBufferParameters(searchArea, mwMap);

            //Copy the display field of the search area for later use.
            //Polygon.Attributes[polygonDataSrc.DisplayFieldName] might be null if for example, 
            //user creates a new polygon without specifying its attributes
            if (searchArea.Attributes[searchAreaDS.DisplayFieldName] == null)
               SearchAreaName = "";
            else
               SearchAreaName = searchArea.Attributes[searchAreaDS.DisplayFieldName].ToString();

            //Execute the GP tool
            _geometryTask.BufferAsync(bufferParameters);
         }
         catch (Exception)
         {
            MessageBox.Show("Error searching for team. Please retry");
         }
      }
      #endregion

      #region Create buffer polygon
      /// <summary>
      /// Get the spatial query result (the buffer polygon) then use the polygon to search for the 
      /// team features that intersect with it. 
      /// Finally, show the team assignment page and select the teams nearby
      /// </summary>
      async void GeometryTask_BufferCompleted(object sender, client.Tasks.GraphicsEventArgs e)
      {
         if (e.Results != null)
         {
            //There will be only one result 
            client.Graphic buffer = e.Results[0];

            //Then query for the team features that intersect with the buffer polygon
            await QueryForFeatures(buffer.Geometry);

            #region Confirm team assignment and send SMS to the teams.
            //If the team data source doesn't have the phone number or the carrier field, we won't allowing sending SMS
            bool smsEnabled = (TeamDataSrc.Fields.Count(f => f.FieldName == "PhoneNumber" || f.FieldName == "Carrier") == 2) ? true : false;
            TeamAssignmentPage smsPage = new TeamAssignmentPage(SearchAreaName, TeamFeatureLayer, TeamsNearby, smsEnabled);     
            smsPage.Show();
            #endregion
         }
      }
      #endregion

      #region Query for the team features within the specified distance that intersect with the buffer
      public async Task QueryForFeatures(client.Geometry.Geometry buffer)
      {
         //Reset the list of TeamsNearby before re-populating them later in the method
         TeamsNearby.Clear();

         //Set up the query and query result
         Query query = new Query("", buffer, true);
         if (TeamDataSrc != null)
         {
            //Run the query and check the result
            QueryResult result = await TeamDataSrc.ExecuteQueryAsync(query);
            if ((result == null) || (result.Canceled) || (result.Features == null))
               return;

            // Get the array of Oids from the query results.
            var resultOids = from resultFeature in result.Features select System.Convert.ToInt32(resultFeature.Attributes[TeamDataSrc.ObjectIdFieldName]);

            // For each graphic feature in featureLayer, use its OID to find the graphic feature from the result set.
            // Note that though the featureLayer's graphic feature and the result set's feature graphic feature share the same properties,
            // they are indeed different objects
            foreach (client.Graphic team in TeamFeatureLayer.Graphics)
            {
               int featureOid;
               int.TryParse(team.Attributes[TeamDataSrc.ObjectIdFieldName].ToString(), out featureOid);

               //If feature has been selected in previous session, unselect it
               if (team.Selected)
                  team.UnSelect();

               //If the feature is in the query result set, select it
               //Also add them to the TeamsNearby list so we can send SMS later
               if ((resultOids.Contains(featureOid)))
               {
                  team.Select();
                  TeamsNearby.Add(team);
               }
            }
         }
      }
      #endregion

      #region Helper methods
      /// <summary>
      /// Define the radius where teams will be searched. We hardcoded it to 1 mile for this sample
      /// </summary>
      private client.Tasks.BufferParameters CreateBufferParameters(client.Graphic polygon, client.Map mwMap)
      {
         client.Tasks.BufferParameters bufferParameters = new client.Tasks.BufferParameters()
         {
            Unit = client.Tasks.LinearUnit.SurveyMile,
            BufferSpatialReference = mwMap.SpatialReference,
            OutSpatialReference = mwMap.SpatialReference,
            UnionResults = true,
         };
         bufferParameters.Distances.AddRange(new List<double> { 0.5 });
         bufferParameters.Features.AddRange(new List<client.Graphic>() { polygon });
         return bufferParameters;
      }

      void GeometryTaskFailed(object sender, client.Tasks.TaskFailedEventArgs e)
      {
         MessageBox.Show("fail to calculate buffer, error: " + e.Error);
      }

      //Validate if the input data source can be used as a team data source
      private bool IsValidTeamDataSource(DataSource dataSrc)
      {
         //If the dataSource has the field "SearchAreaName",
         //and its associated feature layer is a polygon layer
         //return true
         if (dataSrc.Fields.Any(f => f.Name == "SearchAreaName") == false)
            return false;

         MapWidget mw = MapWidget.FindMapWidget(dataSrc);
         client.FeatureLayer layer = mw.FindFeatureLayer(dataSrc);
         if (layer != null && layer.LayerInfo.GeometryType == client.Tasks.GeometryType.Point)
            return true;
         else
            return false;
      }
      #endregion
   }
}
