# PrizmDoc CSharp Database Demo

The following is sample code for demonstrating one possible approach for storing annotations, created by the PrizmDoc viewer, in a SQLServer database. 

## Strategy

Use this example to learn how to use the new ViewerControl API calls (serializeMarks and deserializeMarks) to prepare annotation marks for manipulation by a developed web tier. Learn more about the ViewerControl API from our [documentation](https://www.accusoft.com/products/prizm-content-connect-pcc/documentation/) This example is not intended as a deployable sample. Instead, it is a demonstration of approach and makes many assumptions, such as database design and the use of [Entity Framework](https://msdn.microsoft.com/en-us/data/ee712907.aspx), when performing its demonstration. Other DBMS’ will offer native support for JSON objects. PostgreSQL and Oracle are two RDBMS’ that have native support for representing JSON objects. Other solutions, like MongoDB a NoSQL database, entirely represent their database objects as JSON objects.

In this example, the database was designed with a couple of assumptions:
   - that there is some document information stored in the documents table
      - for this example, the documents table is populated when annotations are stored
   - all annotation “files” must be associated with one, and only one, document

Each solution will need to answer its own questions regarding how the data generated should be handled. For our purposes, many of those questions were passed over in favor of demonstrating the general approach.

## Setup


1. Open and the included .SLN (found in full-viewer-sample) file in Visual Studio.

Please note that this sample has been updated to work with the latest PrizmDoc realease. Because of this update, this sample will require a PrizmDoc Application Service installation as well as access to a PrizmDoc Server installation in order to work as intended.

### pcc.config

Prior to using the demo, make sure that the default configuration options will work for your environment. The folder `/full-viewer-sample/viewer-webtier` contains `pcc.config`.  Update the value of `WebServiceHost` to point to your installation of the PrizmDoc Server. This file can also be modified to load documents from your directory by changing the value of the `DocumentPath` parameter. 

### SQLServer Connection String

For portability, this sample uses Entity Framework 6 and is configured to use the LocalDB service that comes with most installations of Visual Studio. If you do not have LocalDB installed, or wish to use a different SQLServer instance, the database connection string is defined in `full-viewer-sample/App_Code/AnnotationModel.cs`. 

## Use

Selecting the Annotations tab will show that there are two buttons on the top-right side of the viewer. "Post to DB" will collect the current marks on the page, add a user name to all marks, and post them back to the server for database insertion . "Load from DB" will select the annotations that are associated with the current document, per the document's absolute path, and the current user.

The current user is defined in Default.aspx with `userName`

## Changes from default sample

There are a few minor changes to enable this database insertion/loading sample.

### UI Changes

In `full-viewer-sample/viewer-assets/templates/viewerTemplate.html`, the following lines were added to the "annotate" and "redact" tabs:

    <button class="pcc-icon pcc-icon-save pcc-js-postToDB" title="Post to DB"></button>
    <button class="pcc-icon pcc-icon-load pcc-js-loadFromDB" title="Load from DB"></button>

This addition adds two new buttons to the annotations tab. We’ll use these to trigger database interaction.

### Main Page `Default.aspx` Changes

Two new anonymous jQuery functions were added and bound to the buttons above. You can locate the functions by searching in `full-viewer-sample/index.php` for `postToDB` and `loadFromDB`.

### New files

There are a few new objects located in `full-viewer-sample/App_Code`. `HTTPDatabaseHandler.cs` manages database interactions. The `AnnotationModel.cs` and `DocumentModel.cs` objects serve as the ORM models when creating the database.

### Entity Framework models

This sample code takes the [Code First](https://msdn.microsoft.com/en-us/data/ee712907.aspx#codefirst) approach to handle database definition and assumes that no database exists prior to initial execution.

## Support and Purchasing

For questions or support please visit our [website](https://www.accusoft.com/support/) or contact our Support team directly at support@accusoft.com.

For purchasing information please visit the [PrizmDoc](https://www.accusoft.com/products/prizm-content-connect-pcc/overview/) main page or contact our Sales team directly at sales@accusoft.com