(function () {
    var config = window.splashConfig;

    var viewerPaths = {
        'template-designer-sample': config.viewers.esignDesigner,
        'e-signer-sample': config.viewers.esign
    };

    var designerViewerPath, signerViewerPath, selectedViewerPath;

    // test if this browser supports media queries
    var mediaQueryCapable = (function () {
        return (window.matchMedia || window.msMatchMedia) ? true : false;
    })();

    $(document).ready(function () {
        updateSelectedViewer('template-designer-sample', 'e-signer-sample');

        (function () {
            var DropZone = function (opts) {
                //save scope
                var that = this;

                opts = opts || {};
                //TODO opts.url is required
                opts.done = opts.done || function () { };
                opts.id = opts.id || "drop_zone";
                opts.fallback = !!opts.fallback;

                this.opts = opts;

                //get dropzone
                this.DOM = document.getElementById(opts.id);
                this.$DOM = $(this.DOM);

                //fallback event listener needed for older IE
                addEvent(that.DOM, "dragover", handleDragOver);
                addEvent(that.DOM, "drop", function (evt) {
                    handleFileSelect(evt, that.opts);
                });

                //add show/hide ability
                this.show = function () {
                    that.DOM.style.display = ''; /* removes inline style */
                };
                this.hide = function () {
                    that.DOM.style.display = 'none';
                };

                if (document.documentMode < 10) {
                    this.$DOM.find(".dragdropText").text("Click to Upload document");
                    //$("#clickText").text("or use 'skip' button to view default document.");
                    this.$DOM.find(".sampleTitleText").text("Choose a document to load in the viewer from the list or upload one from your desktop in the upload zone below.");
                }
                var leaveTimer;
                var onLeave = function () {
                    //execute leave only if not followed by another enter
                    leaveTimer = setTimeout(function () {
                        that.hide();
                    }, 50);
                };
                var onEnter = function () {
                    //wait for leave to execute first
                    setTimeout(function () {
                        clearTimeout(leaveTimer);
                        that.show();
                    }, 5);
                };

                addEvent(document, "dragenter", onEnter);
                addEvent(document, "dragleave", onLeave);

                //manual select for a file upload
                var manualUpload = function () {
                    //create dummy file input
                    var file = document.createElement("input");
                    file.type = "file";

                    //modern browsers
                    if (window.FormData) {
                        //fix fallback for IE
                        file.style.display = "none";

                        //add file change handler
                        addEvent(file, "change", function (evt) {
                            handleFileSelect(evt, that.opts, file);
                        });

                        document.body.appendChild(file);

                        //activate file select
                        file.click();

                        document.body.removeChild(file);
                    }
                        //IE8/9
                    else {
                        //fallback to upload form
                        injectFallbackDOM(file, that.opts);
                        handleFileIE(that.hide, that.opts);
                    }
                };

                //add option to manually trigger upload
                this.upload = manualUpload;

                //fallback for native file selector
                if (opts.fallback) addEvent(that.DOM, 'click', manualUpload, false); /* */
            };

            function handleFileSelect(evt, opts, file) {
                evt.stopPropagation ? evt.stopPropagation() : evt.cancelBubble = true;
                evt.preventDefault ? evt.preventDefault() : evt.returnValue = false;
                if (window.FormData) {
                    var formData = new FormData();
                    var files = (evt.dataTransfer) ? evt.dataTransfer.files : evt.target.files;
                    formData.append("file", files[0]);

                    sendForm(formData, opts);
                }
            }

            function sendForm(formData, opts) {
                //TODO better error handling
                var xhr = window.myReq = new XMLHttpRequest();
                xhr.open('POST', opts.url, true);
                xhr.onload = function (ev) {
                    opts.done(null, ev.target.response);
                };
                xhr.send(formData);
            }

            var uploadIFrame;

            //IE8/9 FormData fallback
            function handleFileIE(hideFunction, opts) {
                hideFunction();

                //Opening an iframe to make the request to
                if (!uploadIFrame) {
                    uploadIFrame = document.createElement('iframe');
                    uploadIFrame.id = 'IEframe';
                    uploadIFrame.name = 'IEframe';
                    uploadIFrame.style.display = 'none';

                    document.body.appendChild(uploadIFrame);
                }

                function cleanUp() {
                    document.body.removeChild(uploadIFrame);
                }

                //add iFrame onload event
                addEvent(uploadIFrame, 'load', function () {
                    var content = uploadIFrame.contentWindow.res;

                    if (content && content.filename && content.filename !== "") {
                        //execute original callback
                        window.location.href = selectedViewerPath + "?document=" + content.filename;
                        opts.done(null, content);

                        cleanUp();
                    }
                });
            }

            function injectFallbackDOM(fileDOM, opts) {
                var $dz = $('#' + opts.id);

                if (!$dz.siblings('.modal').length) {

                    //create new modal
                    var overlay = document.createElement('div');
                    overlay.className = 'drop-zone';

                    //create a new form
                    var form = document.createElement("form");
                    form.method = 'POST';
                    form.action = opts.url + '?f=jsonp&userAgent=' + escape(navigator.userAgent);
                    form.target = 'IEframe';
                    //For IE8 you need both. Obnoxious
                    form.encoding = form.enctype = "multipart/form-data";

                    //create file input element
                    fileDOM.id = "file";
                    fileDOM.name = "file";

                    form.appendChild(fileDOM);

                    //create Upload (form submit) button
                    var submit = document.createElement('button');
                    submit.value = 'Upload';
                    submit.innerHTML = 'Upload';
                    submit.className = 'btn btn-small';

                    form.appendChild(submit);

                    if (opts.skip) {
                        //create Skip button
                        var cancel = document.createElement('button');
                        cancel.innerHTML = 'Skip';
                        cancel.className = 'btn btn-small';
                        addEvent(cancel, 'click', function (evt) {
                            evt.stopPropagation ? evt.stopPropagation() : evt.cancelBubble = true;
                            evt.preventDefault ? evt.preventDefault() : evt.returnValue = false;

                            //execute callback with 'skip' error
                            opts.done('skip');
                        });

                        form.appendChild(cancel);
                    }

                    overlay.appendChild(form);

                    $dz.after(overlay);
                }
            }

            //helper -- makes dragging a bit prettier
            function handleDragOver(evt) {
                evt.stopPropagation ? evt.stopPropagation() : evt.cancelBubble = true;
                evt.preventDefault ? evt.preventDefault() : evt.returnValue = false;

                evt.dataTransfer.dropEffect = 'copy'; // Explicitly show this is a copy.
            }

            //helper -- event listener fallback for IE8
            var addEvent = function addEvent(obj, event, handler) {
                if (obj.addEventListener) obj.addEventListener(event, handler, false);
                else if (obj.attachEvent) obj.attachEvent('on' + event, handler);
            };
            var removeEvent = function (obj, event, handler) {
                if (obj.removeEventListener) obj.removeEventListener(event, handler, false);
                else if (obj.detachEvent) obj.detachEvent('on' + event, handler);
            };

            //expose to window
            window.DropZone = DropZone;
        }());

        (function () {
            var GetFormsList = function (opts) {
                var that = this;
                this.formslist = {};
                opts = opts || {};
                //TODO opts.url is required
                this.url = opts.url;
                opts.done = opts.done || function () { };

                this.opts = opts;

                getFormDefinitions(opts);

            };

            var formDefinitionsListError = function (err) {
                console.log('Error obtained retrieving Form Definitions List', err.message);
            };

            var formsObtainedDone = function (err, resp, opts) {
                if (err) {
                    formDefinitionsListError(err);
                    return;
                }

                //TODO make sure JSON.parse works
                var listData = (typeof resp === 'string') ? JSON.parse(resp) : resp;

                if (!listData) {
                    listData = [];
                }

                listData.sort(function (a, b) {
                    var name1 = a.name.toUpperCase();
                    var name2 = b.name.toUpperCase();
                    return (name1 < name2) ? -1 : (name1 > name2) ? 1 : 0;
                });

                GetFormsList.formslist = listData;

                var $table = $("#splashFormsList tbody");
                var options = opts;

                var rowCount = $('#splashFormsList >tbody >tr').length;
                if (rowCount > 0) {
                    $table.empty();
                }

                var fragment = document.createDocumentFragment();

                if (listData.length) {
                    $.each(listData, function (idx, obj) {
                        var designer, signer;
                        var row = $("<tr/>");

                        if (options && options.designer) {
                            designer = options.designer;
                        } else if (designerViewerPath) {
                            designer = designerViewerPath;
                        } else {
                            designer = config.viewers.esignDesigner;
                        }

                        if (options && options.signer) {
                            signer = options.signer;
                        } else if (signerViewerPath) {
                            signer = signerViewerPath;
                        } else {
                            signer = config.viewers.esign;
                        }

                        row.append($('<td class="table-template-name">').text(obj.name));

                        var editElem = $('<td class="table-button">');

                        editElem.append($('<a>', {
                            text: 'Edit',
                            href: designer + "?form=" + obj.formDefinitionId
                        })).appendTo(row);

                        var viewItem = $('<td class="table-button">');

                        var viewButton = $('<a>', {
                            text: !$.isEmptyObject(obj.formRoles) ? 'Sign As...' : 'Sign',
                            href: !$.isEmptyObject(obj.formRoles) ? 'javascript:void(0)' : signer + "?form=" + obj.formDefinitionId
                        });
                        if (!$.isEmptyObject(obj.formRoles)) {
                            $(viewButton).on('click', function (e) {
                                e.preventDefault();

                                window.roleDialog.show(obj, 'Sign As...');
                            });
                        }
                        viewItem.append(viewButton).appendTo(row);

                        var deleteItem = $('<td class="table-button">');

                        var deleteButton = $('<a>', {
                            text: 'Delete',
                            href: "javascript:void(0)"
                        });
                        var formId = obj.formDefinitionId;
                        $(deleteButton).on('click', function (e) {
                            e.preventDefault();

                            var response = confirm('Are you sure you want to delete this template?');

                            if (response) {
                                window.GetFormsList.deleteForm(formId);
                            }
                        });
                        deleteItem.append(deleteButton);

                        row.append(deleteItem);

                        fragment.appendChild(row.get(0));

                        designer = undefined;
                        signer = undefined;
                    });
                } else {
                    var placeholder = $('<tr><td class="table-template-name" colspan="4">No documents currently exist.</td></tr>');
                    fragment.appendChild(placeholder.get(0));
                }

                $table.append(fragment);
            };

            //get templates
            var getFormDefinitions = function (opts) {
                $.ajax({
                    url: config.webTier + "/FormDefinitions",
                    cache: false
                }).done(function (resp) {
                    formsObtainedDone(null, resp, opts);
                });
            };

            var deleteForm = function (formId) {
                var url = config.webTier + "/FormDefinitions/" + formId;
                $.ajax({
                    headers: {
                        'X-HTTP-Method-Override': 'DELETE'
                    },
                    type: 'POST',
                    url: config.webTier + "/FormDefinitions/" + formId,
                    cache: false,
                    success: function (data) {
                        // reload the list
                        getFormDefinitions();
                    }
                });
            };

            GetFormsList.deleteForm = deleteForm;
            window.GetFormsList = GetFormsList;
        }());

        (function () {
            //cross-browser event listeners
            var addEvent = function addEvent(obj, event, handler) {
                if (obj.addEventListener) obj.addEventListener(event, handler, false);
                else if (obj.attachEvent) obj.attachEvent('on' + event, handler);
            };
            var removeEvent = function (obj, event, handler) {
                if (obj.removeEventListener) obj.removeEventListener(event, handler, false);
                else if (obj.detachEvent) obj.detachEvent('on' + event, handler);
            };

            var dropzoneError = function (err) {
                switch (err) {
                    case 'skip':
                        window.location.href = selectedViewerPath + "?document=WordDemoSample.doc";
                        break;
                    default:
                        console.log('upload error', err);
                }
            };

            var fileUploadDone = function (err, resp) {
                if (err) {
                    dropzoneError(err);
                    return;
                }

                //TODO make sure JSON.parse works
                var data = (typeof resp === 'string') ? JSON.parse(resp) : resp;

                //save globals
                options = data.options;
                common = data.common;
                //vsID = data.viewingSessionId;
                var fileName = data.filename;


                //hide overlay
                dropzone && dropzone.hide();

                window.location.href = selectedViewerPath + "?document=" + encodeURIComponent(fileName);
            };

            //create viewing session
            var createSessionByName = function (name) {
                $.ajax({
                    url: config.upload + "?document=" + name
                }).done(function (resp) {
                    fileUploadDone(null, resp);
                });
            };
            var formsObtainedDone = function (err, resp) {
                if (err) {
                    formDefinitionsListError(err);
                    return;
                }

                //TODO make sure JSON.parse works
                var listData = (typeof resp === 'string') ? JSON.parse(resp) : resp;

            };

            //create initialization dropzone
            var dropzone = new DropZone({
                url: config.upload,
                fallback: true,
                skip: true,
                done: fileUploadDone
            });

            //get forms list
            var formsList = new GetFormsList({
                url: config.webTier,
                designer: config.viewers.esignDesigner,
                signer: config.viewers.esign,
                done: formsObtainedDone
            });

            //skip option for dropzone
            //                addEvent(document.getElementById('dz_skip'), 'click', function (ev) {
            //                    //IE8 compatible cancel of event
            //                    ev.stopPropagation ? ev.stopPropagation() : ev.cancelBubble = true;
            //                    ev.preventDefault ? ev.preventDefault() : ev.returnValue = false;
            //
            //                    window.location.href = selectedViewerPath + "?document=WordDemoSample.doc";
            //                });

            //document.querySelector('#upload').onclick = function () {
            //    dropzone.upload();
            //};
        }());

        (function () {

            function Dialog() {
                this.$elem = $('.dialog');
                this.$container = this.$elem.find('.dialog-container');
                this.$content = this.$elem.find('.dialog-content');

                this.init();
            }

            Dialog.prototype.init = function () {
                var self = this;

                this.$elem.on('click', '.dialog-close-button', function (e) {
                    e.preventDefault();

                    self.hide();
                });
            };

            Dialog.prototype.show = function (content, title) {
                var self = this,
                    $dialogTitle;

                // Remove previously added content
                this.$content.empty();

                // Add the dialog title
                if (title) {
                    $dialogTitle = $('<h3 class="dialog-title" />').text(title);

                    this.$content.append($dialogTitle);
                }

                // Add form roles to a dropdown and add an event handler.
                // - If the selected role is first, open the signer.
                // - If it isn't, load a dropzone.
                // Once a document is uploaded, open the signer with the selected role and document
                if (content.formRoles) {
                    var $rolesDropdown = $('<select />').css('min-width', '50%').append('<option value="">Choose a role...</option>'),
                        roles = [];

                    // Translate the roles object into an array
                    for (var roleId in content.formRoles) {
                        roles.push(content.formRoles[roleId]);
                    }

                    // Sort the roles in order of sortIndex
                    roles.sort(function (a, b) {
                        var aSort = a.sortIndex,
                            bSort = b.sortIndex;

                        return aSort < bSort ? -1 : aSort > bSort ? 1 : 0;
                    });

                    // Create an option element for each role and add it to the select element
                    for (var i = 0; i < roles.length; i++) {
                        $('<option />')
                            .text(roles[i].displayName)
                            .val(roles[i].formRoleId)
                            .appendTo($rolesDropdown);
                    }

                    $rolesDropdown.on('change', function () {
                        var selectedRoleId = $rolesDropdown.val();

                        // Remove the drop zone since we need to specify a new callback
                        self.$content.find('.document-drop').remove();

                        // If the selected value is the first role, open the signer.
                        if (selectedRoleId === roles[0].formRoleId) {
                            window.location.href = config.viewers.esign + "?form=" + content.formDefinitionId + '&role=' + selectedRoleId;
                        }

                            // If the selected value is not the first role, create a drop zone.
                        else if (selectedRoleId) {

                            self.createDropZone('dz_role', self.$content, function (filename) {
                                window.location.href = config.viewers.esign + "?form=" + content.formDefinitionId + '&document=' + encodeURIComponent(filename) + '&role=' + selectedRoleId;
                            });
                        }

                        self.resizeDialog();
                    });

                    this.$content.append($rolesDropdown);
                }

                // Show the dialog in a timeout so the DOM elements can get a height value
                window.setTimeout(function () {
                    self.$elem.show();
                    self.resizeDialog();
                }, 0);
            };

            Dialog.prototype.hide = function () {
                this.$container.removeAttr('style');
                this.$elem.hide();
            };

            Dialog.prototype.createDropZone = function (id, $parent, cb) {

                function fileUploadDone(err, resp) {

                    if (err) {
                        console.log('upload error', err);
                        return;
                    }

                    //TODO make sure JSON.parse works
                    var data = (typeof resp === 'string') ? JSON.parse(resp) : resp;

                    cb(data.filename);
                }

                var dz = '<div class="document-drop"><div class="drop-zone"><div id="' + id + '" class="modal">' +
                    '<div class="dragdropText">Drop a burned document here to continue signing,<br> or click to select a file</h3></div>' +
                    '</div></div></div>';

                $parent.append(dz);

                this.dropzone = new DropZone({
                    id: id,
                    url: config.upload,
                    fallback: true,
                    done: fileUploadDone
                });
            };

            Dialog.prototype.resizeDialog = function () {

                if (mediaQueryCapable) {
                    this.$container.css({
                        'max-height': this.$content.outerHeight()
                    });
                } else {
                    this.$container.css({
                        'height': this.$content.outerHeight()
                    });
                }
            };

            window.roleDialog = new Dialog();
        }());

        if (!mediaQueryCapable) {
            $('body').addClass('pcc-legacy');
        }
    });

    function updateSelectedViewer(viewer1, viewer2) {
        selectedViewerPath = viewerPaths[viewer1];
        designerViewerPath = viewerPaths[viewer1];
        signerViewerPath = viewerPaths[viewer2];

        window.localStorage.setItem('splash-designer-sample', designerViewerPath);
        window.localStorage.setItem('splash-esigner-sample', signerViewerPath);
    }
})();