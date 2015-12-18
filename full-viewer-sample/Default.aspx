<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>
<%@ Import Namespace="Pcc" %>
<%
    // Create a ViewingSession based on the document defined in the query parameter
    // Example: ?document=sample.doc
    var viewingSessionId = Request.QueryString["viewingSessionId"];

    var documentQueryParameter = Request.QueryString["document"];
    var originalDocumentName = string.Empty;

    if (string.IsNullOrEmpty(documentQueryParameter)){
        originalDocumentName = "PdfDemoSample.pdf";
    }
    else{
        originalDocumentName = documentQueryParameter;
    }


    if (string.IsNullOrEmpty(viewingSessionId)) {

        viewingSessionId = PrizmApplicationServices.CreateSessionFromDocument(originalDocumentName);
    }
%>
<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no">
    
    <title>PCC HTML5 .NET C# Sample</title>
    <link rel="icon" href="favicon.ico" type="image/x-icon" />

    <link rel="stylesheet" href="viewer-assets/css/normalize.min.css">
    <link rel="stylesheet" href="viewer-assets/css/viewercontrol.css">
    <link rel="stylesheet" href="viewer-assets/css/viewer.css">

    <script src="//ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js"></script>
    <script>window.jQuery || document.write('<script src="viewer-assets/js/jquery-1.10.2.min.js"><\/script>');</script>
    <script src="viewer-assets/js/underscore.min.js"></script>
    <script src="viewer-assets/js/jquery.hotkeys.min.js"></script>

    <!--[if lt IE 9]>
        <link rel="stylesheet" href="viewer-assets/css/legacy.css">
        <script src="viewer-assets/js/html5shiv.js"></script>
    <![endif]-->

    <script src="//pcc-assets.accusoft.com/v10.5/js/viewercontrol.js"></script>
    <script src="//pcc-assets.accusoft.com/v10.5/js/viewer.js"></script>
</head>
<body>
    <div id="viewer1"></div>
    
    <div id="attachments" style="display:none;">
        <b>Attachments:</b>
        <p id="attachmentList">
        </p>
    </div>
       
    <script type="text/javascript">
        var viewingSessionId = '<%=HttpUtility.JavaScriptStringEncode(viewingSessionId)%>';
        var languageJson = '<%=languageJson%>';
        var languageItems = jQuery.parseJSON(languageJson);
        var htmlTemplates = <%=htmlTemplates%>;
        var searchTerms = <%=searchJson%>;
        var redactionReasons = <%=redactionReasons%>;
        var originalDocumentName = '<%=HttpUtility.JavaScriptStringEncode(originalDocumentName)%>';
        var userName = "Foo";

        var pluginOptions = {
            documentID: viewingSessionId,
            language: languageItems,
            
            //annotationsMode: "LayeredAnnotations",
            documentDisplayName: originalDocumentName,
			imageHandlerUrl: "viewer-webtier/pcc.ashx",
            immediateActionMenuMode: "hover",
            predefinedSearch: searchTerms,
            template: htmlTemplates,
            redactionReasons: redactionReasons,
			signatureCategories: "Signature,Initials,Title",
			resourcePath: "viewer-assets/img",
            uiElements: {
                download: true,
                fullScreenOnInit: true,
                advancedSearch:true
            }
        };
        
        function processAttachments() {
            // The following javascript will process any attachments for the
            // email message document types (.EML and .MSG).
            
            var countOfAttachmentsRequests = 0;

            function receiveAttachments (data, textStatus, jqXHR) {
                if (data == null || data.status != 'complete') {
                    // The request is not complete yet, try again after a short delay.
                    setTimeout(requestAttachments, countOfAttachmentsRequests * 1000);
                }

                if (data.attachments.length > 0) {
                    var links = '';
                    for (var i = 0; i < data.attachments.length; i++) {
                        var attachment = data.attachments[i];
                        links += '<a href="?viewingSessionId=' + attachment.viewingSessionId + '" target="blank">' + attachment.displayName + '</a><br/>';
                    }

                    $('#attachmentList').html(links);
                    $('#attachments').show();
                }
            }

            function requestAttachments () {
                if (countOfAttachmentsRequests < 10) {
                    countOfAttachmentsRequests++;
                    $.ajax('viewer-webtier/pcc.ashx/ViewingSession/u' + viewingSessionId + '/Attachments', { dataType: 'json' })
                        .done(receiveAttachments)
                        .fail(requestAttachments);
                }
            }

            requestAttachments();
        }
        
        $(document).ready(function () {
            var viewerControl = $("#viewer1").pccViewer(pluginOptions).viewerControl;
                
            // Check if the document has any attachments
            setTimeout(processAttachments, 500);

            // /viewer-webtier/pcc.ashx/DbDemo/q?DocumentID=foo
            // Custom code to insert annotations into DB

            function dbData(method, data, name)
            {
                $.ajax({
                    method: method,
                    contentType: "application/json",
                    url: "viewer-webtier/pcc.ashx/DbDemo/q?DocumentID=u" + pluginOptions.documentID + "&username=" + userName,
                    data: data
                }).done(function(){
                    console.log(arguments);
                });
            }

            $(".pcc-js-postToDB").on("click", function(){
                var allMarks = viewerControl.getAllMarks();

                // Add a username to each mark
                var i;
                for (i=0; i < allMarks.length; ++i){
                    allMarks[i].setData("username" , userName);
                }

                if (allMarks.length > 0){
                    //If there is at least one mark on the page we need to serialize and post to the server
                    viewerControl.serializeMarks(allMarks).then(
						function success (markObjects){
						    var markStr = JSON.stringify(markObjects);
						    dbData("POST", markStr, userName);
						    console.log(markStr);
						});
                }else{
                    //No annotations on the page, no need to serialize. Post an empty array.
                    var markStr = JSON.stringify(allMarks);
                    dbData("POST", markStr);
                }
            });
            
            $(".pcc-js-loadFromDB").on("click", function(){

                // In a real setting, we would pass a param to the server to fetch a certain annotation group, or specific annotation.
                // For thise demonstration, we just fetch the annotation for the document that this user is currently viewing.
                $.ajax({
                    method: "GET",
                    contentType: "application/json",
                    url: "viewer-webtier/pcc.ashx/DbDemo/q?DocumentID=u" + pluginOptions.documentID + "&username=" + userName,
                    success: function(data) {
                        viewerControl.deleteAllMarks();
                        viewerControl.deserializeMarks(data);
                        console.log(data);
                        console.log(arguments);
                    },
                    error: function(xhr, textStatus) {
                        console.log(xhr.status + " - " + xhr.responseText)
                    }
                }).done(function(data){
                    
                });
            });
        });
    </script>
</body>
</html>
