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

namespace OpsDashboardAddIns_MoreDemo
{
   /// <summary>
   /// Represents a feed, in this example an Atom feed from USGS, but could be used to store any
   /// feed name and Uri.
   /// </summary>
   public class Feed
   {
      /// <summary>
      /// User friendly display name of the feed.
      /// </summary>
      public string DisplayName { get; set; }

      /// <summary>
      /// Uri of the feed location.
      /// </summary>
      public Uri FeedUri { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="displayName">The user friendly display name of the feed.</param>
      /// <param name="uri">The Uri representing the location of the feed.</param>
      public Feed(string displayName, Uri uri)
      {
         this.DisplayName = displayName;
         this.FeedUri = uri;
      }
   }

   /// <summary>
   /// A collection of Atrom feeds from the USGS of recent earthquakes, varying by magnitude and time span.
   /// </summary>
   public class Feeds : List<Feed>
   {
      public Feeds()
      {
         this.Add(new Feed("Past hour, Magnitude > 1.0", new Uri(@"http://earthquake.usgs.gov/earthquakes/feed/atom/1.0/hour")));
         this.Add(new Feed("Past day, Magnitude > 1.0", new Uri(@"http://earthquake.usgs.gov/earthquakes/feed/atom/1.0/day")));
         this.Add(new Feed("Past day, Magnitude > 2.5", new Uri(@"http://earthquake.usgs.gov/earthquakes/feed/atom/2.5/day")));
         this.Add(new Feed("Past week, Magnitude > 2.5", new Uri(@"http://earthquake.usgs.gov/earthquakes/feed/atom/2.5/week")));
         this.Add(new Feed("Past week, Magnitude > 4.5", new Uri(@"http://earthquake.usgs.gov/earthquakes/feed/atom/4.5/week")));
      }
   }
}
