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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ESRI.ArcGIS.Client;

namespace OpsDashboardAddIns_MoreDemo.MapTool_DrawPolygon
{
   /// <summary>
   /// This page allows user to input the attributes for the search area to be created
   /// </summary>
   public partial class AttributePage : Window
   {
      public IDictionary<string, object> Attributes { get; private set; }
      private FeatureLayer _searchAreaLayer;

      public AttributePage(FeatureLayer SearchAreaLayer)
      {
         InitializeComponent();

         //Make a copy of the polygonLayer
         _searchAreaLayer = SearchAreaLayer;

         //Get all the fields from the feature layer
         //Then filter out the fields that are not to be edited by user
         List<Field> fields = SearchAreaLayer.LayerInfo.Fields;
         var editableFields = fields.Where(f => IsValidDataType(f.Type));

         //Initialize the Attributes dictionary to store the input attributes
         Attributes = new Dictionary<string, object>();

         //Set up the attribute form
         InitializeAttributeForm(editableFields);
      }

      /// <summary>
      /// Get the values entered by user and save them as the attributes of the new feature
      /// </summary>
      private void OKButton_Click_1(object sender, RoutedEventArgs e)
      {
         int childIndex = 0;
         string fieldName, fieldValue;
         try
         {
            for (int i = 0; i < AttributesPanel.Children.Count; i += 2)
            {
               //Get the field name
               fieldName = (AttributesPanel.Children[i] as TextBlock).Text;
               childIndex = i;

               //Get the field value input by user
               fieldValue = (AttributesPanel.Children[++childIndex] as TextBox).Text;

               //Save the input value
               Attributes.Add(fieldName, fieldValue);
            }
            DialogResult = true;
            this.Close();
         }
         catch (Exception)
         {
            DialogResult = false;
         }
      }

      #region Helper methods
      //Mark the following fields as valid field (the rest are not to be edited by users)
      private bool IsValidDataType(ESRI.ArcGIS.Client.Field.FieldType fieldType)
      {
         return fieldType == Field.FieldType.Date
                            || fieldType == Field.FieldType.Double
                            || fieldType == Field.FieldType.Integer
                            || fieldType == Field.FieldType.SmallInteger
                            || fieldType == Field.FieldType.String;
      }

      //Construct the attribute form to be displayed in the stack panel
      private void InitializeAttributeForm(IEnumerable<Field> editableFields)
      {
         bool IsFirstField = true;
         foreach (Field field in editableFields)
         {
            //For simplicity purpose, we skip the fields with domains
            //and we use textbox for date fields
            //Users of this program should implement logic that deals with these two situations
            if (field.Domain != null)
               continue;

            //field caption
            AttributesPanel.Children.Add(UIElementConstructor.CreateFieldTextBlock(field.Name));

            //field input textbox
            AttributesPanel.Children.Add(UIElementConstructor.CreateFieldTextbox(field.Name));

            //Focus on the first textbox
            if (IsFirstField)
            {
               (AttributesPanel.Children[1] as TextBox).Focus();
               IsFirstField = false;
            }
         }
      }
      #endregion
   }

   #region Helper class that construct the textblocks and textboxes for the attribute form
   internal class UIElementConstructor
   {
      public static UIElement CreateFieldTextBlock(string caption)
      {
         TextBlock tbk = new TextBlock() {
            Text = caption,
            Margin = new System.Windows.Thickness(2,2,2,2)
         };
         return tbk;
      }

      public static UIElement CreateFieldTextbox(string caption)
      {
         TextBox txtBox = new TextBox()
         {
            MaxWidth = 200,          
            Margin = new System.Windows.Thickness(2,2,2,2)
         };
         return txtBox;
      }
   }
   #endregion 
}
