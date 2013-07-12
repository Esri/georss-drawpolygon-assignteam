OpsDashboard-GeoRSS_Draw_TeamAssignment
===============================================

Add-in for Operations Dashboard for ArcGIS containing a GeoRSS widget, a draw polygon map tool and a team assignment feature action. Additional tools may be added inthe future.
This addin is also available to you in ArcGIS Online at http://nitro.maps.arcgis.com/home/item.html?id=8f2d5c5632254c57b355f0296632b813. 

## Features

* Earthquake widget consuming GeoRSS feed.
* Search nearby features feature action.
* Export selected features to CSV map tool.

## Instructions

### Running this add-in

1. Ensure all requirements below are met.
2. Open and compile project in Visual Studio 2012.
3. Update Project Debug properties to correctly locate add-in build path in /addinpath command line argument.
4. Run project in Debug mode.
5. When Operations Dashboard application starts, edit an operation view that contains a map widget, choose Earthquake Widget from Add Widget dialog box, and click OK. 
6. Select a map widget for the earthquake widget to consume and click OK.
7. The widget should be added to the operation view. Click on an earthquake in the list to highlight it in the map.
8. To draw polygon map tool, first add the tool from map tool. Start the tool and click Draw Search Area and Edit attribute to edit the shape and attribute
9. Click Done when finish drawing the polygon to commit the edit.
10. Alternatively, build the project, and upload the .opdashboardaddin file from the build output directory to ArcGIS Online, and then download using Manage Add-Ins in Operations Dashboard, then follow the steps above from step 5.

### General Help
[New to Github? Get started here.](http://htmlpreview.github.com/?https://github.com/Esri/esri.github.com/blob/master/help/esri-getting-to-know-github.html)

## Requirements
### Data schema requirement

* To use the team assignment tool, the map must contain at least a polygon layer (the search area layer) and a point layer (the rescue team layer). 
* The polygon layer must have a display name field (usually automatically set when there's a string field) that stores the search area name;
* The point layer must also have a display name field and a string field "SearchAreaName" for the name of the assigned search area.
* The sending SMS function will be enabled if this layer also has the fields "PhoneNumber" and "Carrier".

### ArcGIS for Organizations

* Operations Dashboard for ArcGIS requires an [ArcGIS for Organizations account] (http://www.esri.com/software/arcgis/arcgisonline/evaluate).

### ArcGIS Runtime SDK for WPF

* Requires an installation of the Esri ArcGIS Runtime SDK for WPF version 10.1.1 or later [Esri website](http://resources.arcgis.com/en/communities/runtime-wpf/)
* Also requires an installation of Microsoft Visual Studio 2012.

## Resources

Learn more about Esri's [Operations Dashboard for ArcGIS](http://www.esri.com/software/arcgis/arcgisonline/features/operations-dashboard).

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Anyone and everyone is welcome to contribute.

## Licensing

Copyright 2013 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0
         
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
                                 
A copy of the license is available in the repository's
[license.txt](https://github.com/esri/georss-drawpolygon-asignteam/blob/master/license.txt) file.
                                                                  
[](Esri Tags: User Conference)
[](Esri Tags: 2013)
[](Esri Language: C-Sharp)
                                                                                                               
                                                                                                                                                            
                                                                                                                                                            
