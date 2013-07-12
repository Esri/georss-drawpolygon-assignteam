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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using client = ESRI.ArcGIS.Client;

namespace OpsDashboardAddIns_MoreDemo.FeatureAction_SMS
{
   /// <summary>
   /// Assign teams to the search area, and send SMS to the teams based on user's selection and/or whether teams' phone informration (phone number and carrier) is available 
   /// Note that this tool only assign teams to search areas, it does not clear the assigned area proerty of teams 
   /// </summary>
   public partial class TeamAssignmentPage : Window
   {
      #region class properties and members
      public string MessageBody { get; set; } //Body of the SMS to send
      public ObservableCollection<MyCheckedListItem> CheckListItems { get; set; } //List of items to populate the team check boxes on the page
      public bool SMSEnabled { get; private set; }  //true if team's phone information is available
      public bool SendSMS { get; set; } //true if user select to send SMS to the teams assigned    
      private int commitEditCount;  //count of how many times a feature edit has been saved. Used to avoid re-committing saved edits
      private string searchAreaName;  //name of the search area to assign

      #region properties of the team layer
      private client.FeatureLayer teamFeatureLayer;
      private List<client.Graphic> recommendedTeams = new List<client.Graphic>(); //the selected teams based on the search result done by the TeamAssignmentFA clas s
      private List<client.Graphic> seletcedTeams = new List<client.Graphic>();  //the actual selected teams based on user's selection
      private string nameAttribute = "";
      private readonly string areaAttribute = "SearchAreaName";
      private readonly string numAttribute = "PhoneNumber";
      private readonly string carrierAttribute = "Carrier";
      #endregion
      #endregion

      public TeamAssignmentPage(string searchAreaName, client.FeatureLayer teamFeatureLayer, List<client.Graphic> teamsNearby, bool smsEnabled)
      {
         InitializeComponent();
         DataContext = this;

         #region Initialize the default values for team assignment page
         this.teamFeatureLayer = teamFeatureLayer;
         this.recommendedTeams = teamsNearby;
         this.searchAreaName = searchAreaName;
         SMSEnabled = smsEnabled;
         SendSMS = true;
         nameAttribute = teamFeatureLayer.LayerInfo.DisplayField;
         #endregion

         #region Populate the team checkboxes
         CheckListItems = new ObservableCollection<MyCheckedListItem>();
         PopulateTeamsComboboxes();
         #endregion

         #region Construct the default message body
         if (!SMSEnabled)
            MessageBody = "<Teams do not contain phone information>";
         else
         {
            foreach (client.Graphic team in teamsNearby)
               MessageBody += team.Attributes[nameAttribute].ToString() + ", ";
            MessageBody += " please search in: " + searchAreaName;
         }
         #endregion
      }

      /// <summary>
      /// When user clicks the button, check if there's any selected team and the message to send is empty
      /// If there is/are selected team(s) and the message is not empty, send the SMS
      /// </summary>
      private void btnAssign_Click(object sender, RoutedEventArgs e)
      {
         #region Error checking: no team selected or empty message
         if (CheckListItems.Any(i => i.IsChecked) == false || string.IsNullOrEmpty(txtMessage.Text))
         {
            MessageBox.Show("Make sure one or more team is selected, and the message is not empty");
            return;
         }
         #endregion

         try
         {
            #region Get the selected teams and check if they have already been assigned to the designated area
            //1. Get the captions of the checked list items
            var checkedItemCaptions = from item in CheckListItems
                                      where item.IsChecked == true
                                      select item.Caption;

            //2. Fom the caption, get the actual team items that are now checked 
            seletcedTeams = teamFeatureLayer.Graphics.Where(team => checkedItemCaptions.Contains(team.Attributes[nameAttribute].ToString())).ToList();

            //3. Check if the same teams have already been assigned. Do not allow assignment if all selected teams have been assiged to that area
            if (seletcedTeams.Any(team => team.Attributes[areaAttribute].ToString() != searchAreaName) == false)
            {
               MessageBox.Show("Teams have been assigned to the designated area already", "Teams Assigned ");
               return;
            }
            #endregion

            #region Update team assignment
            //Assign teams by updating their SearchAreaName attributes
            foreach (client.Graphic team in seletcedTeams)
               team.Attributes[areaAttribute] = searchAreaName;

            //Save the edits
            commitEditCount = 0;
            teamFeatureLayer.EndSaveEdits += teamFeatureLayer_EndSaveEdits;
            teamFeatureLayer.SaveEdits();
            #endregion
         }
         catch (Exception)
         {
            MessageBox.Show("Error sending message");
            this.Close();
         }
      }

      /// <summary>
      /// When edit is saved then send SMS
      /// </summary>
      void teamFeatureLayer_EndSaveEdits(object sender, client.Tasks.EndEditEventArgs e)
      {
         commitEditCount++;
         if (commitEditCount > 1)  //If the edits have been committed then return;
            return;

         #region Check if the edits have been saved successfully
         string assignmentResult = "";
         bool assignTeamsSuccessful = e.Success;
         assignmentResult += assignTeamsSuccessful ? "Teams assigned successfully." : "Failed to assign teams.";
         #endregion

         #region Fail to assign team, or phone information not available in team layer, or user choose not to send SMS
         if (assignTeamsSuccessful == false || SMSEnabled == false || SendSMS == false)
         {
            MessageBox.Show(assignmentResult, "Team assignment result");
            return;
         }
         #endregion

         #region Teams were assigned successfully and user chooses to send SMS to the team
         //In order to know more about sending SMS using SmtpClient, read
         //http://en.wikipedia.org/wiki/List_of_SMS_gateways
         try
         {
            //Get the phone numbers from those teams        
            List<PhoneInfo> phoneInfos = seletcedTeams.Select(
              newSeletcedTeam => new PhoneInfo()
              {
                 PhoneNum = newSeletcedTeam.Attributes[numAttribute].ToString(),
                 Carrier = newSeletcedTeam.Attributes[carrierAttribute].ToString()
              }
              ).ToList();

            //Set up the mail client and the mail content
            MailMessage sms = new MailMessage();
            SmtpClient smtpClient = new SmtpClient("<your email server>");
            sms.From = new MailAddress("<your email address>");
            smtpClient.UseDefaultCredentials = true;
            sms.Body = MessageBody;

            //Set up the recipient information
            foreach (PhoneInfo phoneInfo in phoneInfos)
               sms.To.Add(string.Format("{0}@{1}", phoneInfo.PhoneNum.TrimEnd('@'), phoneInfo.Carrier.TrimStart('@')));

            //Send the SMS and notify user
            smtpClient.Send(sms);
            assignmentResult += " SMSs sent.";
            MessageBox.Show(assignmentResult, "Team assignment result");
            this.Close();
         }
         catch (Exception)
         {
            MessageBox.Show("Error sending SMS", "Error sending SMS");
         }
         #endregion
      }

      /// <summary>
      /// Populate the list of checkboxes with teams' names, 
      /// And check the teams that are selected
      /// </summary>
      private void PopulateTeamsComboboxes()
      {
         CheckListItems.Clear();

         //Get all team features from the layer
         List<client.Graphic> allTeams = new List<client.Graphic>(teamFeatureLayer.Graphics);

         //Listen to the property change event of each feature. Particularly, we will listen to the "Selected" property
         foreach (client.Graphic team in allTeams)
            team.PropertyChanged += team_PropertyChanged;

         //Sort the names of all the teams for better presentation
         List<string> allTeamNames = allTeams.Select(t => t.Attributes[nameAttribute].ToString()).ToList();
         allTeamNames.Sort();

         //populate the check list items
         foreach (string teamName in allTeamNames)
         {
            MyCheckedListItem checkListItem = new MyCheckedListItem()
            {
               Caption = teamName,
               IsChecked = recommendedTeams.Any(selectedTeam => selectedTeam.Attributes[nameAttribute].ToString() == teamName)
            };
            CheckListItems.Add(checkListItem);
         }
      }

      #region Intreation between the teams' checked boxes and the team features on map
      /// <summary>
      /// When user selects/unselects a team feature on map, check/uncheck the corresponding check box on the team assignment page
      /// </summary>
      void team_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "Selected")
         {
            client.Graphic team = sender as client.Graphic;
            MyCheckedListItem checkListItem = CheckListItems.FirstOrDefault(item => item.Caption == team.Attributes[nameAttribute].ToString());
            checkListItem.IsChecked = team.Selected;
         }
      }

      /// <summary>
      /// When user checks a team's check box on the team assignment page, select the featrue on the map
      /// </summary>
      private void CheckBox_Checked(object sender, RoutedEventArgs e)
      {
         string itemName = (e.Source as CheckBox).Content.ToString();
         teamFeatureLayer.Graphics.FirstOrDefault(team => team.Attributes[nameAttribute].ToString() == itemName).Select();
      }

      /// <summary>
      /// When user unchecks a team's check box on the team assignment page, unselect the featrue on the map
      /// </summary>
      private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
      {
         string itemName = (e.Source as CheckBox).Content.ToString();
         teamFeatureLayer.Graphics.FirstOrDefault(team => team.Attributes[nameAttribute].ToString() == itemName).UnSelect();
      }
      #endregion

      /// <summary>
      /// Due to the text limit of an SMS, chop the end of the message if it exceeds 160 characters
      /// </summary>
      private void txtMessage_TextChanged(object sender, TextChangedEventArgs e)
      {
         TextBox msgBox = e.Source as TextBox;
         if (msgBox.Text.Length > 160)
            msgBox.Text = msgBox.Text.Substring(0, 160);
      }

      /// <summary>
      /// Not to send SMS when user unchecks the option
      /// </summary>
      private void chkSendSMS_Unchecked(object sender, RoutedEventArgs e)
      {
         SendSMS = false;
      }

      /// <summary>
      /// Send SMS when user unchecks the option
      /// </summary>
      private void chkSendSMS_Checked(object sender, RoutedEventArgs e)
      {
         SendSMS = true;
      }
   }

   public class MyCheckedListItem : INotifyPropertyChanged
   {
      public string Caption { get; set; }

      private bool _isChecked;
      public bool IsChecked
      {
         get { return _isChecked; }
         set { SetField(ref _isChecked, value, () => IsChecked); }
      }

      #region INotifyPropertyChanged members and associated functions
      public event PropertyChangedEventHandler PropertyChanged;
      protected virtual void OnPropertyChanged<T>(Expression<Func<T>> expression)
      {
         if (expression == null) return;
         MemberExpression body = expression.Body as MemberExpression;
         if (body == null) return;
         OnPropertyChanged(body.Member.Name);
      }

      private void OnPropertyChanged(string propertyName)
      {
         PropertyChangedEventHandler handler = PropertyChanged;
         if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
      }

      protected bool SetField<T>(ref T field, T value, Expression<Func<T>> expression)
      {
         if (EqualityComparer<T>.Default.Equals(field, value)) return false;
         field = value;
         OnPropertyChanged(expression);
         return true;
      }
      #endregion
   }

   class PhoneInfo
   {
      public string PhoneNum { get; set; }
      public string Carrier { get; set; }
   }
}
