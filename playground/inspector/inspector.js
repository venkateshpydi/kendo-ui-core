(function(){

    var ui = kendo.ui;
    var Widget = ui.Widget;

    var template =
        ('<div class="section visible widget" data-section="widget">' +
         '  <div class="sec-buttons">' +
         '    <button class="k-button grab-widget">Select</button>' +
         '  </div>' +
         '  <div class="sec-title k-header">Widget inspector</div>' +
         '  <div class="sec-content"></div>' +
         '</div>' +
         '<div class="section datasource" data-section="datasource">' +
         '  <div class="sec-title k-header">Data source</div>' +
         '  <div class="sec-content"></div>' +
         '</div>' +
         '<div class="section events" data-section="events">' +
         '  <div class="sec-buttons">' +
         '    <button class="k-button clear-section">Clear</button>' +
         '  </div>' +
         '  <div class="sec-title k-header">Events</div>' +
         '  <div class="sec-content"></div>' +
         "</div>"
        );

    var Inspector = Widget.extend({

        options: {
            name   : "Inspector",
            widget : null
        },

        init: function(element, options) {
            var self = this;
            Widget.fn.init.call(self, element, options);
            element = self.element;
            options = self.options;

            element.addClass("kendo-inspector");
            $(template).appendTo(element);
            self._addListeners();
            self.reset(options.widget);
        },

        reset: function(widget) {
            var self = this, element = self.element;
            element.find(".section .sec-content").empty();

            var cont = element.find(".section.widget .sec-content");

            if (self.widget) {
                self._unwatchWidget();
            }
            self.widget = widget;

            displayJSON(safeValueForJSON(widget)).appendTo(cont);
            displayJSON({ element: safeValueForJSON(widget.element) }).appendTo(cont);

            if (widget.wrapper && $(widget.wrapper)[0] !== $(widget.element)[0]) {
                displayJSON({ wrapper: safeValueForJSON(widget.wrapper) }).appendTo(cont);
            }

            displayJSON({ options: safeValueForJSON(widget.options) }, {
                filterable: true,
                sort: true
            }).appendTo(cont);

            element.find(".section.datasource").removeClass("visible").css({
                display: widget.dataSource ? "block" : "none"
            });

            self._watchWidget();
        },

        _sectionToggled: function(sec, visible) {
            var self = this, secname = sec.data("section");
            switch (secname) {

              case "datasource":
                if (visible) {
                    var cont = sec.find(".sec-content").empty();
                    var ds = getWidgetDataSource(self.widget);

                    displayJSON({ options: ds.options }, {
                        filterable: true,
                        collapsed: true
                    }).appendTo(cont);

                    displayJSON({ data: ds.data }, {
                        filterable: true,
                        onEdit: function(path, val, callback){
                            path = path.slice(1);
                            try {
                                // if we can't parse it, it'll remain a string
                                val = JSON.parse(val);
                            } catch(ex) {};
                            if (setDatasourceProperty(self.widget, path, val)) {
                                callback(val);
                            }
                        }
                    }).appendTo(cont);
                }
                break;
            }
        },

        _onWidgetEvent: function(eventName, ev) {
            var now = new Date().getTime();
            ev = safeValueForJSON(ev);
            ev.__eventName = eventName;
            ev.__timestamp = new Date();
            var cont = this.element.find(".section.events .sec-content");
            var el = displayEvent(ev).prependTo(cont);
            if (this._lastEventTime && now - this._lastEventTime > 100) {
                el.addClass("separator-bottom");
            }
            this._lastEventTime = now;
        },

        _watchWidget: function() {
            var self = this, widget = self.widget;
            self._lastEventTime = null;
            if (!widget.__kendo_inspector_watcher) {
                var running = true;
                widget.__kendo_inspector_watcher = {
                    stop: function() { running = false },
                    resume: function() { running = true }
                };
                function makeHandler(eventName) {
                    return function(ev) {
                        if (running) {
                            self._onWidgetEvent(eventName, ev);
                        }
                    };
                }
                widget.events.forEach(function(eventName){
                    widget.bind(eventName, makeHandler(eventName));
                });
            }
            widget.__kendo_inspector_watcher.resume();
        },

        _unwatchWidget: function() {
            if (this.widget.__kendo_inspector_watcher)
                this.widget.__kendo_inspector_watcher.stop();
        },

        _addListeners: function() {
            var self = this, element = self.element;

            // expand/collapse sections on clicking the title
            element.on("click", ".sec-title", function(ev){
                var sec = $(ev.target).closest(".section");
                sec.toggleClass("visible");
                self._sectionToggled(sec, sec.hasClass("visible"));
            });

            // expand/collapse object properties
            element.on("click", ".codefold-button, .property", function(ev){
                var cf = $(ev.target).closest(".expandable");
                cf.toggleClass("collapsed");
                if (!cf.hasClass("collapsed")) {
                    cf.find(".hidden").removeClass("hidden");
                    if (cf.data("lazy-id")) {
                        var data = getCachedObject(cf.data("lazy-id"));
                        var html = displayJSON(safeValueForJSON(data), { innerObject: true });
                        cf.find(".codefold-body").html(html);
                        cf.removeAttr("data-lazy-id");
                        cf.removeData("lazy-id");
                    }
                }
            });

            // right-clicking an expander deep-expands/collapses subproperties
            element.on("contextmenu", ".codefold-button", function(ev){
                ev.preventDefault();
                ev.stopPropagation();
                var cf = $(ev.target).closest(".expandable");
                if (cf.data("lazy-id")) {
                    var data = getCachedObject(cf.data("lazy-id"));
                    var html = displayJSON(safeValueForJSON(data), { innerObject: true });
                    cf.find(".codefold-body").html(html);
                    cf.removeAttr("data-lazy-id");
                    cf.removeData("lazy-id");
                    cf.removeClass("collapsed");
                } else {
                    cont();
                }
                function cont() {
                    var sub = cf.find(".expandable").not("[data-lazy-id]");
                    if (sub.length > 0) {
                        cf.removeClass("collapsed");
                        if ($(sub[0]).hasClass("collapsed")) {
                            sub.removeClass("collapsed");
                        } else {
                            sub.addClass("collapsed");
                        }
                    } else {
                        cf.toggleClass("collapsed");
                    }
                }
            });

            // support editable fields
            element.on("click", ".editable-object .editable", function(ev){
                var span = $(ev.target).hide();
                var orig = span.attr("data-value");
                var input = $("<input class='editable-input' spellcheck='false' />")
                    .val(orig)
                    .on("keydown", function(ev){
                        if (ev.keyCode == 27) {
                            ev.preventDefault();
                            cancel();
                            return false;
                        }
                        else if (ev.keyCode == 13) {
                            input.off("blur");
                            ev.preventDefault();
                            save();
                            return false;
                        }
                        else setTimeout(resize, 5);
                    })
                    .on("blur", function(ev){
                        // seems more intuitive to save than cancel on blur
                        input.off("blur");
                        ev.preventDefault();
                        save();
                    })
                    .insertBefore(span)
                    .focus();

                input[0].select();

                resize();

                var done = false;
                function resize() {
                    if (done) return;
                    span.text(input.val());
                    span.show();
                    var width = span.width();
                    span.hide();
                    if (width > 0) {
                        input.width(width + 20);
                    }
                }

                function cancel() {
                    input.remove();
                    span.show();
                    span.text(orig);
                }

                function save() {
                    span.trigger("editable:save", [ input.val(), function(val){
                        if (arguments.length == 0) {
                            cancel();
                        } else {
                            val = JSON.stringify(val);
                            span.text(val);
                            span.attr("data-value", val);
                            input.remove();
                            span.show();
                            done = true;
                        }
                    } ]);
                }
            });

            // highlight widget on hover
            element.on("mouseenter", "[data-widget-id]", function(ev){
                var id = $(ev.target).attr("data-widget-id");
                highlightWidget(id);
            });

            // highlight element on hover
            element.on("mouseenter", "[data-element-id]", function(ev){
                var id = $(ev.target).attr("data-element-id");
                highlightElement(id);
            });

            element.on("mouseleave", "[data-widget-id], [data-element-id]", function(ev){
                removeHighlight();
            });

            element.on("mouseenter", ".codefold-button", function(ev){
                $(ev.target).closest(".codefold").addClass("highlight");
            });

            element.on("mouseleave", ".codefold-button", function(ev){
                $(ev.target).closest(".codefold").removeClass("highlight");
            });

            // grab widget
            element.on("click", ".grab-widget", function(ev){
                self._grabWidget();
            });

            element.on("click", ".clear-section", function(ev){
                var cont = $(ev.currentTarget).closest(".section").find(".sec-content");
                cont.empty();
                self._lastEventTime = null;
            });

            element.on("click", "[data-object-id]", function(ev){
                var el = $(ev.currentTarget);
                if (el.is("[data-element-id]")) {
                    console.log(getCachedObject(el.data("element-id")));
                } else {
                    window.$K = getCachedObject(el.data("object-id"));
                    console.log(window.$K);
                }
            });

            element.kendoTooltip({
                filter: "[data-object-id]",
                content : function(data) {
                    if (data.target.is("[data-element-id]")) {
                        return "<div style='white-space: nowrap'>Click to dump with console.log</div>";
                    } else {
                        return "<div style='white-space: nowrap'>Click to dump with console.log.<br />Places reference in global $K variable.</div>";
                    }
                },
            });

        },

        _grabWidget: function() {
            var self = this, grabbed = null;
            $(document.body)
                .addClass("kendo-inspector-grabbing")
                .on("mouseenter.kendoInspector", "[data-role]", function(ev){
                    var el = $(ev.currentTarget);
                    if (el.data("role") == "draggable") {
                        return;
                    }
                    grabbed = kendoWidgetInstance(el);
                    if (grabbed) {
                        highlightWidget(grabbed);
                    }
                })
                .on("mouseleave.kendoInspector", ".kendo-inspector-highlight", function(ev){
                    removeHighlight();
                    grabbed = null;
                })
                .on("mousedown.kendoInspector", function(ev){
                    $(document.body).off(".kendoInspector");
                    $(document.body).removeClass("kendo-inspector-grabbing");
                    removeHighlight();
                    if (grabbed) {
                        self.reset(grabbed);
                    }
                    ev.preventDefault();
                    ev.stopPropagation();
                });
        }

    });

    ui.plugin(Inspector);

    function kendoWidgetInstance(el) {
        el = $(el);
        return kendo.widgetInstance(el, kendo.ui) ||
            kendo.widgetInstance(el, kendo.mobile.ui) ||
            kendo.widgetInstance(el, kendo.dataviz.ui);
    }

    var OBJECT_ID = 0;
    var CACHE = {};

    function cacheObject(w) {
        if (w.__kendo_inspector_id) {
            return w.__kendo_inspector_id;
        }
        var id = w.__kendo_inspector_id = ++OBJECT_ID;
        CACHE[id] = w;
        return id;
    }

    function setDatasourceProperty(wid, path, val) {
        var w = getCachedObject(wid);
        var ds = w.dataSource;
        var data = ds.data();
        var i = 0;
        while (i < path.length) {
            var prop = path[i++];
            if (data instanceof kendo.data.ObservableObject) {
                if (i < path.length) {
                    data = data.get(prop);
                } else {
                    data.set(prop, val);
                    return true;
                }
            }
            else if (data instanceof kendo.data.ObservableArray || Array.isArray(data)) {
                if (i < path.length) {
                    data = data[prop];
                } else {
                    data.splice(prop, 1, val);
                    return true;
                }
            }
            else if (data instanceof Date) {
                var new_data = new Date(data);
                val = parseInt(val, 10);
                if (isNaN(val)) return false;
                switch (prop) {
                  case "year"         : new_data.setFullYear(val)     ; break;
                  case "month"        : new_data.setMonth(val)        ; break;
                  case "date"         : new_data.setDate(val)         ; break;
                  case "hours"        : new_data.setHours(val)        ; break;
                  case "minutes"      : new_data.setMinutes(val)      ; break;
                  case "seconds"      : new_data.setSeconds(val)      ; break;
                  case "milliseconds" : new_data.setMilliseconds(val) ; break;
                  default:
                    return false;
                }
                tools.setDatasourceProperty(w, path.slice(0, -1), new_data);
                return new_data;
            }
            else if (typeof data == "object") {
                if (i < path.length) {
                    data = data[prop];
                } else {
                    data[prop] = val;
                    return true;
                }
            }
            else {
                console.error("Can't setDatasourceProperty on", data);
                return false;
            }
        }
        return false;
    }

    function removeFromCache(w) {
        var id = w.__kendo_inspector_id;
        if (id) {
            delete CACHE[id];
            delete w.__kendo_inspector_id;
            return id;
        }
    }

    function getCachedObject(obj) {
        if (typeof obj == "object") return obj;
        return CACHE[obj];
    }

    function elementInView(el) {
        var pos = $(el).offset().top;
        var top = $(window).scrollTop();
        var bot = top + $(window).height();
        return pos > top + 10 && pos < bot - 10;
    }

    function _highlightElement(el, text) {
        removeHighlight();
        el = $(el);
        if (el.is(":visible")) {
            var hl = "<div class='kendo-inspector-highlight' style='position: absolute; z-index: 32700; color: #000; font-size: 16px; text-align: center; background-color: rgba(109, 188, 233, 0.6); border-color: rgba(191, 241, 174, 0.67); border-style: solid; box-sizing: border-box;'>";
            if (text) hl += htmlescape(text);
            hl += "</div>";
            hl = $(hl);
            var pos = el.offset();
            hl.css({
                "border-left-width"   : el.css("padding-left"),
                "border-top-width"    : el.css("padding-top"),
                "border-right-width"  : el.css("padding-right"),
                "border-bottom-width" : el.css("padding-bottom"),
                "left"                : pos.left,
                "top"                 : pos.top,
                "width"               : el.outerWidth(),
                "height"              : el.outerHeight(),
                "line-height"         : el.outerHeight() + "px"
            });
            hl.appendTo(el[0].ownerDocument.body);
            if (!elementInView(el))
                el.get(0).scrollIntoView();
        }
    }

    function highlightElement(id) {
        var el = getCachedObject(id);
        _highlightElement(el, "<" + $(el)[0].tagName.toLowerCase() + ">");
    }

    function highlightWidget(id) {
        var w = getCachedObject(id);
        var el = w.wrapper || w.element;
        _highlightElement(el, w.options.name);
    }

    function removeHighlight() {
        $(".kendo-inspector-highlight").remove();
    }

    function makeSpecial(type, props) {
        props.__kendo_inspector_type = type;
        return props;
    }

    function makeWidgetInfo(w, extended) {
        var el = w.wrapper || w.element;
        var ret = {
            id             : cacheObject(w),
            type           : w.options.name,
            prefix         : w.options.prefix,
            events         : w.events && w.events.length > 0 ? w.events.map(function(evname){
                return makeSpecial("doclink-event", {
                    event  : evname,
                    widget : w.options.name,
                    prefix : w.options.prefix,
                });
            }) : undefined,
            element_tag    : w.element.get(0).tagName,
            element_id     : w.element.attr("id"),
            element_class  : w.element.attr("class"),
            visible        : el.is(":visible"),
            hasModel       : !!w.dataSource,
            wasInspected   : w === window.$K,
        };
        if (typeof w.value == "function") {
            ret.value = safeValueForJSON(w.value());
        }
        if (extended) {
            ret.widget = safeValueForJSON(w);
            ret.element = safeValueForJSON(w.element.get(0));
            if (w.wrapper) {
                ret.wrapper = safeValueForJSON(w.wrapper.get(0));
            }
        }
        return ret;
    }

    function getWidgetDataSource(id) {
        var w = getCachedObject(id);
        var ds = w.dataSource;
        var data = ds.data();
        return {
            data    : safeValueForJSON(data.toJSON()),
            options : safeValueForJSON(ds.options),
        };
    }

    // <xxx> reliability isn't on the list of JavaScript's main features.
    function isObject(x, type) {
        if (Array.isArray(x)) return false;
        var rx = new RegExp("^\\[object " + type);
        return typeof x == "object" && rx.test(x.toString());
        // try {
        //     return typeof x == "object" && rx.test(Object.prototype.toString.call(x));
        // } catch(ex) {
        //     return false;
        // }
    }
    function isDomNode(x) {
        return (x instanceof Node
                || (typeof x == "object" &&
                    typeof x.insertBefore == "function" &&
                    typeof x.cloneNode == "function"));
    }
    // </xxx>

    function safeValueForJSON(x, options) {
        if (!options) options = {};
        function shouldSendLazy(obj) {
            if (obj === x) return false;
            if (options.lazy) return true;
            if (Array.isArray(obj))
                return obj.length > 20;
            return Object.keys(obj).length > 20;
        }
        var seen = [];
        var count = 0;
        var TOO_BIG = {};
        try {
            return saferize(x);
        } catch(ex) {
            if (ex === TOO_BIG) {
                return makeSpecial("too-big", {
                    id: cacheObject(x)
                });
            }
            throw ex;
        }
        function saferize(x) {
            ++count;
            //if (count > 3000) throw TOO_BIG;
            if (count > 50) options.lazy = true;
            try {
                if (x === null) {
                    return x;
                }
                if (x instanceof Date) {
                    return makeSpecial("Date", {
                        year         : x.getFullYear(),
                        month        : x.getMonth(),
                        date         : x.getDate(),
                        hours        : x.getHours(),
                        minutes      : x.getMinutes(),
                        seconds      : x.getSeconds(),
                        milliseconds : x.getMilliseconds(),
                    });
                }
                if (x instanceof $) {
                    return makeSpecial("jQuery", {
                        selector : x.selector,
                        elements : x.get().map(saferize),
                    });
                }
                if (isObject(x, "HTML")) {
                    return makeSpecial("Element", {
                        id            : cacheObject(x),
                        tag           : x.tagName,
                        element_id    : x.id,
                        element_class : x.className,
                    });
                }
                if (isDomNode(x)) {
                    return "### DOM NODE ###";
                }
                if (isObject(x, "Window")) {
                    return "### Window ###";
                }
                if (x instanceof kendo.ui.Widget) {
                    return makeSpecial("kendo.ui.Widget", {
                        id   : cacheObject(x),
                        type : x.options.name
                    });
                }
                if (typeof x == "function") {
                    return "### FUNCTION ###";
                }
                if (typeof x == "object") {
                    if (seen.indexOf(x) >= 0) {
                        return "### CIRCULAR ###";
                    }
                    seen.push(x);
                    if (typeof x.toJSON == "function") {
                        return saferize(x.toJSON());
                    }
                    if (Array.isArray(x)) {
                        // var n = x.length;
                        // if (n > 102) {
                        //     x = x.slice(0, 100);
                        //     x.push("### Array too big (" + (n - 100) + " elements not displayed) ###");
                        // }
                        if (shouldSendLazy(x)) {
                            return {
                                __kendo_inspector_type: "Array",
                                id: cacheObject(x),
                                length: x.length
                            };
                        }
                        return x.map(saferize);
                    }
                    var tmp = {};
                    for (var i in x) if (i != "dataSource" && x.hasOwnProperty(i)) {
                        if (x[i] === window[i]) {
                            // dumping global objects is a bad idea.
                            continue;
                        }
                        else if (typeof x[i] == "function") {
                            continue;
                        }
                        else if (/(string|boolean|number|function)/.test(typeof x[i])
                                 || x[i] instanceof kendo.ui.Widget
                                 || x[i] instanceof Date
                                 || x[i] === null
                                 || isDomNode(x[i])
                                 || x[i] instanceof $
                                 || isObject(x[i], "Window")
                                 || isObject(x[i], "HTML")
                                 || !shouldSendLazy(x[i])) {
                            tmp[i] = saferize(x[i]);
                        }
                        else if (Array.isArray(x[i])) {
                            tmp[i] = {
                                __kendo_inspector_type: "Array",
                                id: cacheObject(x[i]),
                                length: x[i].length
                            };
                        }
                        else {
                            tmp[i] = {
                                __kendo_inspector_type: "Object",
                                id: cacheObject(x[i]),
                                length: Object.keys(x[i]).length - 1
                            };
                        }
                    }
                    return tmp;
                }
                return x;
            } catch(ex) {
                if (ex === TOO_BIG) throw ex;
                console.error("saferize failed: ", ex, x);
                return "### ERROR ###";
            }
        }
    }

    function displayEvent(ev, includeWidget) {
        var type = ev.__eventName;
        delete ev.__eventName;
        var ts = ev.__timestamp;
        delete ev.__timestamp;
        var time = kendo.format("{0:HH:mm:ss}", ts) + "." + ts.getMilliseconds();
        var name = "[" + time + "] " + type;
        if (includeWidget && ev.sender) {
            name += " in " + wrapWidget(ev.sender);
        }
        var obj = {};
        obj[name] = ev;
        return displayJSON(obj, {
            collapsed: true,
            wrapProperty: function(key, val, currentPath) {
                if (key === name) {
                    return "<span class='property'>" + key + "</span>";
                }
            }
        });
        function wrapWidget(obj) {
            return "<span class='widget' data-object-id='" + obj.id + "' data-widget-id='" + obj.id + "'>" + obj.type + "</span>";
        }
    }

    function displayJSON(orig_obj, options) {
        if (!options) options = {};

        var currentPath = [];
        var onEdit = options.onEdit;

        function currentPathAttr() {
            return currentPath.length > 0 ? htmlescape(JSON.stringify(currentPath)) : "";
        }
        function wrapEditable(thing) {
            var val = htmlescape(thing);
            if (onEdit) {
                return "<span class='editable' data-value='" + val + "' data-path='" + currentPathAttr() + "'>" + val + "</span>";
            }
            return "<span class='value'>" + val + "</span>";
        }
        function wrapProperty(key, val) {
            if (options.wrapProperty) {
                var ret = options.wrapProperty(key, val, currentPath);
                if (ret) return ret;
            }
            return "<span class='property'>" + htmlescape(key) + "</span>";
        }
        function wrapWidget(obj) {
            return "<span class='widget' data-object-id='" + obj.id + "' data-widget-id='" + obj.id + "'>### Kendo Widget (" + obj.type + ") ###</span>";
        }
        function wrapElement(obj) {
            return "<span class='element' data-object-id='" + obj.id + "' data-element-id='" + obj.id + "'>### DOM element (" + obj.tag + ") ###</span>";
        }
        function wrapLink(link) {
            return "<a target='KENDOUIDOCS' class='" + (link.class || "") + "' href='" + link.url + "'>" + link.text + "</a>";
        }
        function wrapTooBig(obj) {
            return "<span class='object' data-object-id='" + obj.id + "'>### OBJECT TOO BIG ###</span>";
        }
        var html = function rec(obj) {
            function displayProp(name, value) {
                var x = inspectValue(value);
                if (x.expandable && x.objectId)
                    return displayLazyProp(name, value, x);
                var html = "<div class='codefold";
                if (obj !== orig_obj) {
                    // don't make the toplevel objects filterable, or the
                    // whole object will disappear when no property
                    // matches the filter.
                    html += " filterable";
                }
                if (x.expandable) {
                    html += " expandable" + (options.collapsed ? " collapsed" : "") + "'>"
                        + "<span class='codefold-button'></span>";
                } else {
                    html += "'>";
                }
                html += wrapProperty(name, value) + ": ";
                value = rec(value);
                if (x.title) {
                    html += json_heading(x.title);
                }
                if (x.expandable) {
                    html += "<div class='codefold-body'>" + value + "</div>";
                } else {
                    html += value;
                }
                html += "</div>";
                return html;
            }
            function displayLazyProp(name, value, x) {
                var html = "<div class='codefold expandable collapsed' data-lazy-id='"
                    + x.objectId + "'><span class='codefold-button'></span>";
                html += wrapProperty(name, value) + ": ";
                html += json_heading(x.title);
                html += "<div class='codefold-body'>…</div></div>";
                return html;
            }

            if (obj == null) {
                return wrapEditable("null");
            }
            else if (Array.isArray(obj)) {
                return obj.map(function(el, i){
                    currentPath.push(i);
                    var html = displayProp(i+"", el);
                    currentPath.pop();
                    return html;
                }).join("");
            }
            else if (obj instanceof Date) {
                return wrapEditable(rec(obj.toUTCString()));
            }
            else if (typeof obj == "object") {
                delete obj.__kendo_inspector_id;
                switch (obj.__kendo_inspector_type) {
                  case "kendo.ui.Widget":
                    return wrapWidget(obj);
                  case "Element":
                    return wrapElement(obj);
                  case "jQuery":
                    delete obj.__kendo_inspector_type;
                    break;
                  case "Date":
                    delete obj.__kendo_inspector_type;
                    break;
                  case "link":
                    return wrapLink(obj);
                  case "too-big":
                    return wrapTooBig(obj);
                  case "doclink-event":
                    var name = obj.widget;
                    if (obj.prefix == "mobile") {
                        name = "mobile." + name;
                    }
                    var w = KENDO_CONFIG.widgets[name];
                    var url = "http://docs.kendoui.com/api/" + w.category
                        + "/" + obj.widget.toLowerCase()
                        + "#events-" + obj.event;
                    return wrapLink({
                        class: "doclink",
                        text: obj.event,
                        url: url
                    });
                }

                var a = Object.keys(obj);
                if (options.sort) {
                    if (options.sort instanceof Function) {
                        a = a.sort(function(a, b){
                            return options.sort(a, b, obj);
                        });
                    } else {
                        a = a.sort(function(a, b){
                            a = a.toLowerCase();
                            b = b.toLowerCase();
                            return a < b ? -1 : a > b ? 1 : 0;
                        });
                    }
                }
                return a.map(function(key){
                    currentPath.push(key);
                    var html = displayProp(key, obj[key]);
                    currentPath.pop();
                    return html;
                }).join("");
	    }
	    else {
                return wrapEditable(JSON.stringify(obj));
            }
        }(orig_obj);

        function inspectValue(thing) {
            if (Array.isArray(thing)) return {
                expandable : thing.length > 0,
                title      : "Array[" + thing.length + "]",
            };
            if (thing == null) return { expandable: false };
            if (typeof thing == "object") {
                switch (thing.__kendo_inspector_type) {
                  case "Array":
                    return {
                        expandable : thing.length > 0,
                        objectId   : thing.id,
                        title      : "Array[" + thing.length + "]"
                    };
                  case "Object":
                    return {
                        expandable : thing.length > 0,
                        objectId   : thing.id,
                        title      : "Object{" + thing.length + "}"
                    };
                  case "kendo.ui.Widget":
                  case "Element":
                  case "link":
                  case "doclink-event":
                  case "too-big":
                    return { expandable: false };
                  case "jQuery":
                  case "Date":
                    return {
                        expandable : true,
                        title      : thing.__kendo_inspector_type,
                    };
                }
                var keys = Object.keys(thing);
                return {
                    expandable : keys.length > 0,
                    title      : "Object{" + keys.length + "}"
                };
            }
            return false;
        }

        if (options.innerObject) return html;

        var has_expandable_props = (function(){
            var props = Object.keys(orig_obj);
            for (var i = props.length; --i >= 0;) {
                if (inspectValue(orig_obj[props[i]]).expandable)
                    return true;
            }
            return false;
        })();

        if (has_expandable_props) {
            html += "<div class='codefold-operations'>";
            if (options.filterable) {
                html += "<label class='filter'><input /></label>";
            }
            // html += "<span class='codefold-operation expand-all'></span>";
            // html += "<span class='codefold-operation collapse-all'></span>";
            html += "</div>";
        }

        var pre = $("<pre class='editable-object'></pre>")
            .html(html)
            .on("editable:save", ".editable", function(ev, val, callback){
                var path = JSON.parse($(ev.target).attr("data-path"));
                onEdit(path, val, callback);
            });

        if (options.filterable) (function(){
            var filter = $(".filter", pre);
            var input = $("input", filter);
            function apply(orig) {
                var val = input.val().toLowerCase();
                if (val != "") {
                    filter.addClass("has-query");
                    filter.closest(".codefold-operations").addClass("has-query");
                } else {
                    filter.removeClass("has-query");
                    filter.closest(".codefold-operations").removeClass("has-query");
                }
                if (orig == null || val != orig) {
                    pre.find(".filterable").each(function(){
                        var el = $(this);
                        var txt = el.text();
                        if (txt.toLowerCase().indexOf(val) >= 0) {
                            el.removeClass("hidden");
                        } else {
                            el.addClass("hidden");
                        }
                    });
                }
            }
            input.on("keydown", function(ev){
                if (ev.keyCode == 27) {
                    input.val("");
                    apply(null);
                    ev.preventDefault();
                    return false;
                }
                var orig = input.val();
                setTimeout(function(){
                    apply(orig);
                }, 10);
            });
            filter.on("mousedown", function(ev){
                if (ev.target === this && $(this).hasClass("has-query")) {
                    input.val("");
                    apply(null);
                    input.blur();
                    ev.preventDefault();
                    ev.stopPropagation();
                }
            });
        }());

        return pre;
    }

    function propertyChanged(object, path, value) {
        path = path.slice();
        var x = object;
        while (x != null && path.length > 0)
            x = x[path.shift()];
        if (path.length > 0)
            return true;
        return !equals(x, value);
    }

    function equals(a, b) {
        if (a == null) return b == null;
        if (b == null) return a == null;
        if (Array.isArray(a) && Array.isArray(b)) {
            if (a.length != b.length) return false;
            for (var i = a.length; --i >= 0;) {
                if (!equals(a[i], b[i])) return false;
            }
            return true;
        }
        if (typeof a != typeof b) return false;
        if (typeof a == "object") {
            var keys = Object.keys(a);
            for (var i = keys.length; --i >= 0;) {
                if (!equals(a[i], b[i])) return false;
            }
            return true;
        }
        return a == b;
    }

    function htmlescape(txt) {
        if (txt == undefined) return "null";
        return txt.replace(/&/g, "&amp;")
            .replace(/\x22/g, "&quot;")
            .replace(/\x27/g, "&#x27;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\u00A0/g, "&#xa0;");
    }

    function json_heading(title) {
        return "<span class='json-heading'>" + title + "</span> ";
    }

})();
