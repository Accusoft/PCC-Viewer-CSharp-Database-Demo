<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html>
<html>
    <head>
        <meta name="viewport" content="width=device-width, initial-scale=1, user-scalable=no">
        <meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
        <title>PCC Samples (ASP.NET Webforms)</title>
        <link rel="icon" type="image/png" href="assets/img/favicon.ico" />
        <link href="http://fonts.googleapis.com/css?family=Raleway:300,400" rel="stylesheet" type="text/css" />
        <link rel="stylesheet" href="assets/css/splash.css" type="text/css" />
        <link rel="stylesheet" href="assets/css/fontello.css" type="text/css" />
        <meta charset="utf-8">
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <script src="//ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js"></script>
        <script> window.jQuery || document.write('<script src="assets/js/jquery-1.10.2.min.js"><\/script>');</script>
    </head>
    <body>
        <header class="page-header" role="banner">
            <div class="container">
                <div class="branding">
                    <div class="logo">
                        <a href="https://www.accusoft.com">
                            <img src="assets/img/accusoft_logo.png" alt="Accusoft">
                        </a>
                    </div>
                </div>
                <div class="product-name">
                    <h1>PrizmDoc</h1>
                </div>
            </div>
        </header>
        <div class="title-bar">
            <div class="container">
                <h2>PrizmDoc <span>Viewer Sample</span></h2>
            </div>
        </div>
        <div class="choose-viewer">
            <div class="container">
                <h3>select a viewer</h3>
                <div class="control-wrapper">
                    <div class="segmented-control">
                        <button type="button" id="viewer-type-full" data-viewer-select="full-viewer" class="selected">
                            <i class="icon-ok-circled"></i>
                            Full Viewer
                        </button>
                        <button type="button" id="viewer-type-book-reader" data-viewer-select="book-reader">
                            <i class="icon-ok-circled"></i>
                            Book Reader
                        </button>
                    </div>
                </div>
            </div>
        </div>
        <div class="choose-document">
            <h3>select a document</h3>
            <div class="instructions">       
                <p><em>Choose a document to load in the viewer from the list or drag one from your desktop in the drop zone below.</em></p>
            </div>
            <div class="container">
                <ul class="document-list">
                    <li><a data-document="WordDemoSample.doc">Word Document</a></li>
                    <li><a data-document="PdfDemoSample.pdf">PDF Document</a></li>
                    <li><a data-document="DxfDemoSample.dxf">AutoCAD</a></li>
                    <li><a data-document="TiffDemoSample.tif">Multi-Page TIFF</a></li>
                    <li><a data-document="JPegDemoSample.jpg">JPEG</a></li>
                    <li><a data-document="EmailSample.msg">Email</a></li>
                </ul>                
                <div class="upload-zone">
                    <h3>upload a document</h3>
                    <div class="upload-button">
                        <button class="btn" id="upload">Upload</button></a>
                    </div>                    
                    <div class="drop-zone" id="drop_zone">
                        <p><span id="dragdropText">Drag and drop a file here</span>
                        <br> 
                        <span id="clickText">or click to select a file</span></p>
                        <button class="btn btn-small" id="dz_skip">Skip</button>
                    </div>
                </div>
                
            </div>
        </div>
    </body>
    <script type="text/javascript" src="assets/js/splash-config.js"></script>
    <script type="text/javascript" src="assets/js/main-splash.js"></script>
</html>