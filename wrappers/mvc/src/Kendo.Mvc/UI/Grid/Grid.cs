namespace Kendo.Mvc.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using System.Web.Script.Serialization;
    using System.Web.UI;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.Infrastructure;
    using Infrastructure.Implementation;
    using Kendo.Mvc.Resources;
    using Kendo.Mvc.UI.Fluent;
    using Kendo.Mvc.UI.Html;

    public class Grid<T> : ViewComponentBase, IGridColumnContainer<T>, IGrid where T : class
    {
        private readonly IGridHtmlBuilderFactory htmlBuilderFactory;

        private IGridUrlBuilder urlBuilder;

        private IGridDataKeyStore dataKeyStore;

        private string clientRowTemplate;

        public Grid(ViewContext viewContext,
                    IJavaScriptInitializer initializer,
                    IUrlGenerator urlGenerator,
                    IGridHtmlBuilderFactory htmlBuilderFactory) : base(viewContext, initializer)
        {
            this.htmlBuilderFactory = htmlBuilderFactory;

            UrlGenerator = urlGenerator;

            PrefixUrlParameters = true;
            RowTemplate = new HtmlTemplate<T>();
            DetailTemplate = new HtmlTemplate<T>();
            Columns = new List<GridColumnBase<T>>();
            DataKeys = new List<IDataKey>();

            Pageable = new PageableSettings();
            Sortable = new GridSortableSettings();
            Scrollable = new GridScrollableSettings();
            Navigatable = new GridNavigatableSettings(this);
            ColumnContextMenu = new GridColumnContextMenuSettings(this);
            Filterable = new GridFilterableSettings();

            Editable = new GridEditableSettings<T>(this)
            { 
                PopUp = new Window(viewContext, Initializer)
                {
                    Modal = true,
                    Draggable = true
                }                
            };

            Grouping = new GridGroupableSettings();
            Resizable = new GridResizableSettings();
            Reorderable = new GridReorderableSettings();

            TableHtmlAttributes = new RouteValueDictionary();

            Footer = true;
            IsEmpty = true;

            Selectable = new GridSelectableSettings();

            ToolBar = new GridToolBarSettings<T>(this);            
            NoRecordsTemplate = new HtmlTemplate();

            ValidationMetadata = new Dictionary<string, object>();

            AutoGenerateColumns = true;

            DataSource = new DataSource()
            {
                Type = DataSourceType.Server,
                ServerAggregates = true,
                ServerFiltering = true,
                ServerGrouping = true,
                ServerPaging = true,
                ServerSorting = true
            };

            DataSource.ModelType(typeof(T));
        }

        public DataSource DataSource
        {
            get;
            private set;
        }

        public IDictionary<string, object> ValidationMetadata
        {
            get;
            private set;
        }

        public IDictionary<string, object> TableHtmlAttributes
        {
            get;
            private set;
        }

        public GridResizableSettings Resizable
        {
            get;
            private set;
        }

        public GridReorderableSettings Reorderable
        {
            get;
            private set;
        }

        public bool Footer
        {
            get;
            set;
        }

        public GridToolBarSettings<T> ToolBar
        {
            get;
            private set;
        }

        public GridGroupableSettings Grouping
        {
            get;
            private set;
        }

        public GridEditableSettings<T> Editable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the selection configuration
        /// </summary>
        public GridSelectableSettings Selectable
        {
            get;
            private set;
        }

        internal IList<IDataKey> DataKeys
        {
            get;
            private set;
        }

        //TODO: Implement command button types
        private object Button<TButton>(T dataItem/*, GridButtonType buttonType*/, object htmlAttributes, object imageHtmlAttributes)
            where TButton : GridActionCommandBase, new()
        {
            var command = new TButton();
            
            //command.ButtonType = buttonType;
            command.ButtonType = GridButtonType.ImageAndText;

            //TODO: Implement command button html attributes
            //command.HtmlAttributes = htmlAttributes.ToDictionary();
            //command.ImageHtmlAttributes = imageHtmlAttributes.ToDictionary();

            var buttons = command.CreateDisplayButtons(UrlBuilder, new GridHtmlHelper<T>(ViewContext, DataKeyStore));

            var fragment = new HtmlFragment();

            buttons.Each(button => button.Create(dataItem).AppendTo(fragment));

            return MvcHtmlString.Create(fragment.ToString());
        }

        private object CustomButton<TButton>(
                   string name,
                   string text,
                    
                   string url,
                   string actionName,
                   string controllerName,
                   string routeName,
                   object routeValues,
                   //bool ajax,
                 //TODO: Implement command button types
                 //  GridButtonType buttonType,
                   object htmlAttributes,
                   object imageHtmlAttributes)

                   where TButton : GridCustomCommandBase, new()
        {
            var command = new TButton();

            //TODO: Implement command button types
            //command.ButtonType = buttonType;
            command.ButtonType = GridButtonType.ImageAndText;
            //TODO: Implement command button html attributes
            //command.HtmlAttributes = htmlAttributes.ToDictionary();
            //command.ImageHtmlAttributes = imageHtmlAttributes.ToDictionary();
            command.Text = text;            
            //command.Ajax = ajax;
            command.Name = name;


            if (url.HasValue())
            {
                command.Url(url);
            }

            if (actionName.HasValue())
            {
                command.Action(actionName, controllerName, routeValues);
                text = actionName;
            }

            if (routeName.HasValue())
            {
                command.Route(routeName, routeValues);
                text = routeName;
            }

            if (string.IsNullOrEmpty(command.Text))
            {
                command.Text = text;
            }

            var buttons = command.CreateDisplayButtons(UrlBuilder, new GridHtmlHelper<T>(ViewContext, DataKeyStore));
            var fragment = new HtmlFragment();
            buttons.Each(button => button.Create(null).AppendTo(fragment));

            return MvcHtmlString.Create(fragment.ToString());
        }

        private object CustomCommandToolBarButton(
            string name,
            string text,            
            string url,
            string actionName,
            string controllerName,
            string routeName,
            object routeValues,
            //bool ajax,
          //TODO: Implement command button types
          //  GridButtonType buttonType,
            object htmlAttributes,
            object imageHtmlAttributes)
        {
            return CustomButton<GridToolBarCustomCommand<T>>(name, text, url, actionName, controllerName, routeName, routeValues/*, ajax, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        public object CustomCommandToolBarButton(
            string name,
            string text,
            string actionName,
            string controllerName,
            object routeValues,
            //bool ajax,
            //TODO: Implement command button types
            //GridButtonType buttonType,
            object htmlAttributes,
            object imageHtmlAttributes)
        {
            return CustomCommandToolBarButton(name, text, null, actionName, controllerName, null, routeValues/*, ajax, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        
        ////TODO: Implement command button types
        public object CustomCommandToolBarButton(string name, string text, string actionName, string controllerName/*, bool ajax, GridButtonType buttonType*/, object htmlAttributes, object imageHtmlAttributes)
        {
            return CustomCommandToolBarButton(name, text, actionName, controllerName, null/*, ajax, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        //TODO: Implement command button types
        public object CustomCommandToolBarButton(string name, string text, string actionName, string controllerName, object routeValues/*, bool ajax, GridButtonType buttonType*/)
        {
            return CustomCommandToolBarButton(name, text, null, actionName, controllerName, null, routeValues/*, ajax, buttonType*/, null, null);
        }

        ////TODO: Implement command button types
        public object CustomCommandToolBarButton(string name, string text, string routeName, RouteValueDictionary routeValues/*, bool ajax, GridButtonType buttonType*/, object htmlAttributes, object imageHtmlAttributes)
        {
            return CustomCommandToolBarButton(name, text, null, null, null, routeName, routeValues/*, ajax, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        ////TODO: Implement command button types
        //public object CustomCommandToolBarButton(string name, string text, string routeName, RouteValueDictionary routeValues, bool ajax/*, GridButtonType buttonType*/)
        //{
        //    return CustomCommandToolBarButton(name, text, routeName, routeValues, ajax/*, buttonType*/, null, null);
        //}

        //public object CustomCommandToolBarButton(string name, string text, string routeName, RouteValueDictionary routeValues)
        //{
        //    return CustomCommandToolBarButton(name, text, routeName, routeValues, false/*, GridButtonType.ImageAndText*/, null, null);
        //}
        
        //public object CustomCommandToolBarButton(string name, string text/*, string url, GridButtonType buttonType*/, object htmlAttributes, object imageHtmlAttributes)
        //{
        //    return CustomCommandToolBarButton(name, text, url/*, null, null, null, null, false, buttonType*/, htmlAttributes, imageHtmlAttributes);
        //}

        //TODO: Implement command button types
        public object CustomCommandToolBarButton(string name, string text, string url/*, GridButtonType buttonType*/)
        {
            return CustomCommandToolBarButton(name, text, url, null, null, null, null, null, null);
            //return CustomCommandToolBarButton(name, text, url, buttonType, null, null);
        }

        //public object CustomCommandToolBarButton(string name, string text, string url)
        //{
        //    return CustomCommandToolBarButton(name, text, url);
        //}

        //TODO: Implement custom commands
        internal object CustomCommandToolBarButton(string name, string text)
        {
            return CustomCommandToolBarButton(name, text, null);
        }

        //TODO: Implement command button types
        public object EditButton(T dataItem/*, GridButtonType buttonType*/, object htmlAttributes, object imageHtmlAttributes)
        {
            Editable.Enabled = true;

            return Button<GridEditActionCommand>(dataItem/*, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        //TODO: Implement command button types
        public object EditButton(T dataItem/*, GridButtonType buttonType*/, object htmlAttributes)
        {
            return EditButton(dataItem/*, buttonType*/, htmlAttributes, null);
        }

        //TODO: Implement command button types
        public object EditButton(T dataItem/*, GridButtonType buttonType*/)
        {
            return EditButton(dataItem/*, buttonType*/, null);
        }

        //TODO: Implement command button types
        //public object EditButton(T dataItem)
        //{
        //    return EditButton(dataItem, GridButtonType.ImageAndText);
        //}

        //TODO: Implement command button types
        public object DestroyButton(T dataItem/*, GridButtonType buttonType*/, object htmlAttributes, object imageHtmlAttributes)
        {
            Editable.Enabled = true;
            return Button<GridDestroyActionCommand>(dataItem/*, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        public object DestroyButton(T dataItem/*, GridButtonType buttonType*/, object htmlAttributes)
        {
            return DestroyButton(dataItem/*, buttonType*/, htmlAttributes, null);
        }

        public object DestroyButton(T dataItem/*, GridButtonType buttonType*/)
        {
            return DestroyButton(dataItem/*, buttonType*/, null);
        }

        //TODO: Implement command button types
        //public object DeleteButton(T dataItem)
        //{
        //    return DeleteButton(dataItem, GridButtonType.ImageAndText);
        //}

        public object CreateButton(/*GridButtonType buttonType,*/ object htmlAttributes, object imageHtmlAttributes)
        {
            Editable.Enabled = true;
            InitializeEditors();
            return Button<GridToolBarCreateCommand<T>>(null/*, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        public object CreateButton(/*GridButtonType buttonType,*/ object htmlAttributes)
        {
            return CreateButton(/*buttonType,*/ htmlAttributes, null);
        }

        public object CreateButton(/*GridButtonType buttonType*/)
        {
            return CreateButton(/*buttonType,*/ null);
        }

        //public object InsertButton()
        //{
        //    return InsertButton(GridButtonType.ImageAndText);
        //}

        public object SaveButton(/*GridButtonType buttonType,*/ object htmlAttributes, object imageHtmlAttributes)
        {
            Editable.Enabled = true;
            InitializeEditors();
            return Button<GridToolBarSaveCommand<T>>(null/*, buttonType*/, htmlAttributes, imageHtmlAttributes);
        }

        public object SaveButton(/*GridButtonType buttonType,*/ object htmlAttributes)
        {
            return SaveButton(/*buttonType,*/ htmlAttributes, null);
        }

        public object SaveButton(/*GridButtonType buttonType*/)
        {
            return SaveButton(/*buttonType,*/ null);
        }

        //public object SubmitChangesButton()
        //{
        //    return SubmitChangesButton(GridButtonType.ImageAndText);
        //}

        public string ClientRowTemplate
        {
            get
            {
                return clientRowTemplate;
            }
            set
            {
                clientRowTemplate = HttpUtility.HtmlDecode(value);
            }
        }

        IEnumerable<IDataKey> IGrid.DataKeys
        {
            get
            {
                return DataKeys.Cast<IDataKey>();
            }
        }

        /// <summary>
        /// Gets the template which the grid will use to render a row
        /// </summary>
        public HtmlTemplate<T> RowTemplate
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the filtering configuration.
        /// </summary>
        public GridFilterableSettings Filterable
        {
            get;
            private set;
        }        

        /// <summary>
        /// Gets the scrolling configuration.
        /// </summary>
        public GridScrollableSettings Scrollable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the keyboard navigation configuration.
        /// </summary>
        public GridNavigatableSettings Navigatable
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the column context menu configuration.
        /// </summary>
        public GridColumnContextMenuSettings ColumnContextMenu
        {
            get;
            private set;
        }    

        public IUrlGenerator UrlGenerator
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether custom binding is enabled.
        /// </summary>
        /// <value><c>true</c> if custom binding is enabled; otherwise, <c>false</c>. The default value is <c>false</c></value>
        public bool EnableCustomBinding
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the paging configuration.
        /// </summary>
        public PageableSettings Pageable
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the columns of the grid.
        /// </summary>
        public IList<GridColumnBase<T>> Columns
        {
            get;
            private set;
        }

        IEnumerable<IGridColumn> IGrid.Columns
        {
            get
            {
                return Columns.Cast<IGridColumn>();
            }
        }

        public IList<GridColumnBase<T>> VisibleColumns
        {
            get
            {                
                return Columns.Where(c => c.Visible).ToList();
            }
        }

        /// <summary>
        /// Gets the page size of the grid.
        /// </summary>
        public int PageSize
        {
            get
            {
                if (!Pageable.Enabled)
                {
                    return 0;
                }

                return DataSource.PageSize;
            }
        }

        public int CurrentPage
        {
            get
            {
                return DataSource.Page;
            }
        }

        /// <summary>
        /// Gets the sorting configuration.
        /// </summary>
        /// <value>The sorting.</value>
        public GridSortableSettings Sortable
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to add the <see cref="Name"/> property of the grid as a prefix in url parameters.
        /// </summary>
        /// <value><c>true</c> if prefixing is enabled; otherwise, <c>false</c>. The default value is <c>true</c></value>
        public bool PrefixUrlParameters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the action executed when rendering a row.
        /// </summary>
        public Action<GridRow<T>> RowAction
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the action executed when rendering a cell.
        /// </summary>
        public Action<GridCell<T>> CellAction
        {
            get;
            set;
        }

        public HtmlTemplate NoRecordsTemplate
        {
            get;
            private set;
        }

        public string Prefix(string parameter)
        {
            return PrefixUrlParameters ? Id + "-" + parameter : parameter;
        }

        public IEnumerable<AggregateDescriptor> Aggregates
        {
            get 
            {
                return DataSource.Aggregates;
            }
        }

        public override void WriteInitializationScript(TextWriter writer)
        {
            var options = new Dictionary<string, object>(Events);

            var autoBind = DataSource.Type != DataSourceType.Server;

            var columns = VisibleColumns.Select(c => c.ToJson());

            var idPrefix = "#";
            if (IsInClientTemplate)
            {
                idPrefix = "\\" + idPrefix;
            }

            if (columns.Any())
            {
                options["columns"] = columns;
            }
            
            if (Grouping.Enabled)
            {
                options["groupable"] = Grouping.ToJson();
            }

            if (Pageable.Enabled)
            {
                Pageable.AutoBind = autoBind;

                options["pageable"] = Pageable.ToJson();
            }

            if (Sortable.Enabled)
            {
                var sorting = Sortable.ToJson();
                options["sortable"] = sorting.Any() ? (object)sorting : true;
            }

            if (Selectable.Enabled)
            {
                options["selectable"] = String.Format("{0}, {1}", Selectable.Mode, Selectable.Type);
            }

            if (Filterable.Enabled)
            {
                var filtering = Filterable.ToJson();
                options["filterable"] = filtering.Any() ? (object)filtering : true;
            }

            if (Resizable.Enabled)
            {
                options["resizable"] = true;
            }

            if (Reorderable.Enabled)
            {
                options["reorderable"] = true;
            }

            if (!Scrollable.Enabled)
            {
                options["scrollable"] = false;
            }
            else
            {
                var scrolling = Scrollable.ToJson();
                if (scrolling.Any())
                {
                    options["scrollable"] = scrolling;
                }
            }

            if (Editable.Enabled && IsClientBinding)
            {
                options["editable"] = Editable.ToJson();
            }

            if (ToolBar.Enabled)
            {
                options["toolbar"] = ToolBar.ToJson();
            }

            if (autoBind == false)
            {
                options["autoBind"] = autoBind;
            }

            options["dataSource"] = DataSource.ToJson();

            if (!String.IsNullOrEmpty(ClientDetailTemplateId))
            {
                options["detailTemplate"] = new ClientEvent { HandlerName = String.Format("kendo.template($('{0}{1}').html())", idPrefix, ClientDetailTemplateId) };                                
            }

            if (!String.IsNullOrEmpty(ClientRowTemplate))
            {
                options["rowTemplate"] = ClientRowTemplate;
            }

            if (Navigatable.Enabled)
            {
                options["navigatable"] = true;
            }

            writer.Write(Initializer.Initialize(Selector, "Grid", options));

            base.WriteInitializationScript(writer);
        }

        internal int Colspan
        {
            get
            {
                int colspan = DataSource.Groups.Count + VisibleColumns.Count;

                if (DetailTemplate != null)
                {
                    colspan++;
                }

                return colspan;
            }
        }

        private string currentItemMode;

        private GridItemMode CurrentItemMode
        {
            get
            {
                if (currentItemMode == null)
                {
                    currentItemMode = this.ViewContext.Controller.ValueOf<string>(Prefix(GridUrlParameters.Mode));
                }

                return currentItemMode.ToEnum(GridItemMode.Default);
            }
        }

        protected override void WriteHtml(HtmlTextWriter writer)
        {
            if (!Columns.Any() && AutoGenerateColumns)
            {
                foreach (GridColumnBase<T> column in new GridColumnGenerator<T>(this).GetColumns())
                {
                    Columns.Add(column);
                }
            }

            var orignalClientValidationEnabled = ViewContext.ClientValidationEnabled;
            var originalFormContext = ViewContext.FormContext;

            try
            {
                ViewContext.ClientValidationEnabled = true;
                ViewContext.FormContext = new FormContext
                {
                    FormId = Name + "form"
                };

                if (Editable.Enabled && IsClientBinding)
                {
                    InitializeEditors();
                }
                
                AdjustColumnsTypesFromDynamic();

                if (!HtmlAttributes.ContainsKey("id"))
                {
                    HtmlAttributes["id"] = Id;
                }

                var builder = htmlBuilderFactory.CreateBuilder(Scrollable.Enabled);

                ProcessDataSource();

                var renderingData = CreateRenderingData();

                var functionalData = CreateFunctionalData();

                var container = builder.CreateGrid(HtmlAttributes, functionalData, renderingData);

                if (Editable.Mode == GridEditMode.PopUp && (CurrentItemMode == GridItemMode.Insert || CurrentItemMode == GridItemMode.Edit))
                {
                    AppendPopupEditor(container, renderingData);
                }

                container.WriteTo(writer);

                if (ViewContext.FormContext != null)
                {
                    ValidationMetadata.Add("Fields", ProcessValidationMetadata());

                    ValidationMetadata.Add("FormId", ViewContext.FormContext.FormId);
                }
            }
            finally
            {
                ViewContext.FormContext = originalFormContext;
                ViewContext.ClientValidationEnabled = orignalClientValidationEnabled;
            }

            base.WriteHtml(writer);
        }

        private GridFunctionalData CreateFunctionalData()
        {
            return new GridFunctionalData
            {
                Pager = Pageable.Enabled,
                ToolBar = ToolBar.Enabled,
                GroupHeader = Grouping.Enabled && Grouping.Visible,
                PagerData = CreatePagerData(),
                GroupingData = CreateGroupingData(),
                ToolBarData = CreateToolbarData()                
            };
        }
        
        private GridToolBarData CreateToolbarData()
        {
            return new GridToolBarData
            {
                Commands = ToolBar.Commands.Cast<IGridActionCommand>(),
                UrlBuilder = UrlBuilder,
                Template = ToolBar.Template
            };
        }

        private GridGroupingData CreateGroupingData()
        {
            return new GridGroupingData
            {
                GetTitle = VisibleColumns.Cast<IGridColumn>().GroupTitleForMember,
                GroupDescriptors = DataSource.Groups,
                Messages = Grouping.Messages,
                UrlBuilder = UrlBuilder
            };
        }

        private GridPagerData CreatePagerData()
        {
            return new GridPagerData
            {
                Page = DataSource.Page,
                TotalPages = DataSource.TotalPages,
                UrlBuilder = UrlBuilder,
                Total = DataSource.Total,
                Colspan = Colspan,
                PageSize = DataSource.PageSize,
                Numeric = Pageable.Numeric,
                Input = Pageable.Input,
                Info = Pageable.Info,
                PreviousNext = Pageable.PreviousNext,
                Refresh = Pageable.Refresh,
                PageSizes = Pageable.PageSizes,
                Messages = Pageable.Messages,
                IsInClientTemplate = IsInClientTemplate
            };
        }

        private void AppendPopupEditor(IHtmlNode container, GridRenderingData renderingData)
        {
            var popup = Editable.PopUp;
            var cancelUrl = renderingData.UrlBuilder.CancelUrl(null);

            new WindowBuilder(popup)
                .Content(renderingData.PopUpContainer.InnerHtml)                
                //TODO: Add positioning of the window
                //.HtmlAttributes(new { style = "top:10%;left:50%;margin-left: -" + (popup.Width == 0 ? 360 : popup.Width) / 4 + "px" })                
                .Actions(buttons => buttons
                    .Close()
                );

            if (!IsClientBinding)
            {
                popup.Events["close"] = new ClientEvent { InlineCodeBlock = obj => "function(e) { e.preventDefault();" + string.Format("window.location.href = \"{0}\";", cancelUrl) + "}" };
            }
            
            if (!popup.Name.HasValue())
            {
                popup.Name = Name + "PopUp";
            }

            if (!popup.Title.HasValue())
            {
                //todo: grid popup localization
                popup.Title = CurrentItemMode == GridItemMode.Edit ? "Edit" : "Add new record";
            }

            new LiteralNode(popup.ToHtmlString()).AppendTo(container);
        }

        private void ProcessDataSource()
        {
            if (Pageable.Enabled && DataSource.PageSize == 0)
            {
                DataSource.PageSize = 10;
            }

            var binder = new DataSourceRequestModelBinder();

            if (this.PrefixUrlParameters)
            {
                binder.Prefix = Name;

                if (DataSource.Type == DataSourceType.Server)
                {
                    DataSource.Transport.Prefix = Name + "-";
                }
            }

            var controller = ViewContext.Controller;
            var bindingContext = new ModelBindingContext() { ValueProvider = controller.ValueProvider };

            var request = (DataSourceRequest)binder.BindModel(controller.ControllerContext, bindingContext);

            DataSource.Process(request, !EnableCustomBinding);

            if (DataSource.Schema.Model.Id != null)
            {
                DataKeys.Add(DataSource.Schema.Model.Id);
            }
        }

        private GridRenderingData CreateRenderingData()
        {
            var renderingData = new GridRenderingData
            {
                TableHtmlAttributes = TableHtmlAttributes,
                DataKeyStore = DataKeyStore,
                HtmlHelper = new GridHtmlHelper<T>(ViewContext, DataKeyStore),
                UrlBuilder = UrlBuilder,
                DataSource = DataSource.Data,
                Columns = VisibleColumns.Cast<IGridColumn>(),
                GroupMembers = DataSource.Groups.Select(g => g.Member),
                Mode = CurrentItemMode,
                EditMode = Editable.Mode,
                HasDetailTemplate = HasDetailTemplate,
                //TODO: Implement hidden columns
                Colspan = Colspan /*- Columns.Count(column => column.Hidden)*/,
                DetailTemplate = MapDetailTemplate(HasDetailTemplate ? DetailTemplate : null),
                NoRecordsTemplate = FormatNoRecordsTemplate(),
                ScrollingHeight = Scrollable.Height,
                //EditFormHtmlAttributes = Editing.FormHtmlAttributes,
                ShowFooter = Footer && VisibleColumns.Any(c => c.FooterTemplate.HasValue() || c.ClientFooterTemplate.HasValue()),
                AggregateResults = DataSource.AggregateResults ?? new List<AggregateResult>(),
                Aggregates = Aggregates.SelectMany(aggregate => aggregate.Aggregates),
                GroupsCount = DataSource.Groups.Count,
                ShowGroupFooter = Aggregates.Any() && VisibleColumns.OfType<IGridBoundColumn>().Any(c => c.GroupFooterTemplate.HasValue()),
                PopUpContainer = new HtmlFragment(),
                CreateNewDataItem = () => Editable.DefaultDataItem(),
                //TODO: Implement insert row position
                //InsertRowPosition = Editing.InsertRowPosition,
                EditTemplateName = Editable.TemplateName,
                AdditionalViewData = Editable.AdditionalViewData,
                FormId = ViewContext.FormContext.FormId,
                Callback = RowActionCallback
            };

            if (RowTemplate.HasValue())
            {
                renderingData.RowTemplate = (dataItem, container) => RowTemplate.Apply((T)dataItem, container);
            }

            return renderingData;
        }

        private void RowActionCallback(GridItem item)
        {
            IsEmpty = false;

            if (RowAction != null)
            {
                var row = new GridRow<T>(this, (T)item.DataItem, item.Index);
                if (HasDetailTemplate)
                {
                    row.DetailRow = new GridDetailRow<T>
                    {
                        Html = item.DetailRowHtml
                    };
                }
                row.InEditMode = item.Type == GridItemType.EditRow;
                row.InInsertMode = item.Type == GridItemType.InsertRow;
                row.Selected = (item.State & GridItemStates.Selected) == GridItemStates.Selected;
                RowAction(row);

                if (HasDetailTemplate)
                {
                    item.Expanded = row.DetailRow.Expanded;
                    item.DetailRowHtml = row.DetailRow.Html;
                    item.DetailRowHtmlAttributes = row.DetailRow.HtmlAttributes;
                }

                if (row.Selected)
                {
                    item.State |= GridItemStates.Selected;
                }
                if (row.InEditMode)
                {
                    item.Type = GridItemType.EditRow;
                }
                else if (row.InInsertMode)
                {
                    item.Type = GridItemType.InsertRow;
                }
                else
                {
                    item.Type = GridItemType.DataRow;
                }

                item.HtmlAttributes = row.HtmlAttributes;
            }
        }

        private Action<object, IHtmlNode> MapDetailTemplate(HtmlTemplate<T> detailTemplate)
        {
            return (dataItem, container) =>
            {
                if (detailTemplate != null && detailTemplate.HasValue())
                    detailTemplate.Apply((T)dataItem, container);
            };
        }

        private HtmlTemplate FormatNoRecordsTemplate()
        {
            return NoRecordsTemplate;
        }

        private IEnumerable<FieldValidationMetadata> ProcessValidationMetadata()
        {
            var validators = ViewContext.FormContext
                              .FieldValidators
                              .Values
                              .Where(IsBooleanField)
                              .ToArray();

            if (Name != null && Name.Contains("<#="))
            {
                validators = validators.Select((metadata) => EncodeRegularExpressionValidators(metadata)).ToArray();
            }
            
            return validators;
        }

        private static FieldValidationMetadata EncodeRegularExpressionValidators(FieldValidationMetadata metadata)
        {
            metadata.ValidationRules.Each(validationRule =>
            {
                if (validationRule.ValidationType == "regularExpression" || validationRule.ValidationType == "regex")
                {
                    if (validationRule.ValidationParameters.ContainsKey("pattern"))
                    {
                        validationRule.ValidationParameters["pattern"] =
                            new JavaScriptSerializer().Serialize(validationRule.ValidationParameters["pattern"]).Trim('"');
                    }
                }
            });
            return metadata;
        }

        private bool IsBooleanField(FieldValidationMetadata validationMetadata)
        {
            ModelMetadata modelMetadata = ModelMetadata.FromStringExpression(validationMetadata.FieldName, ViewContext.ViewData);

            return modelMetadata.ModelType != typeof(bool);
        }

        private void AdjustColumnsTypesFromDynamic()
        {
            if (!typeof (T).IsDynamicObject() || DataSource.Data == null ||
                !Columns.OfType<IGridBoundColumn>().Any(c => c.MemberType == null && c.Member.HasValue())
                ) 
                return;

            var processedDataSource = DataSource.Data;
            var firstItem = GetFirstItemFromGroups(processedDataSource);
            if (firstItem != null)
            {
                Columns.OfType<IGridBoundColumn>().Where(
                    c => c.MemberType == null && c.Member.HasValue()).Each(
                        c => c.MemberType = BindingHelper.ExtractMemberTypeFromObject(firstItem, c.Member));
            }
        }

        private static object GetFirstItemFromGroups(IEnumerable dataSource)
        {
            var groupItem = dataSource.OfType<IGroup>().FirstOrDefault();
            if (groupItem != null)
            {
                return groupItem.Leaves().Cast<object>().FirstOrDefault();
            }
            return dataSource.OfType<object>().FirstOrDefault();
        }

        internal bool OutputValidation
        {
            get
            {
                return (CurrentItemMode == GridItemMode.Insert || CurrentItemMode == GridItemMode.Edit || (Editable.Enabled && IsClientBinding))
                       && !ViewContext.UnobtrusiveJavaScriptEnabled
                    ;
            }
        }

        public bool AutoGenerateColumns { get; set; }

        public bool IsEmpty
        {
            get;
            set;
        }

        public bool IsClientBinding
        {
            get
            {
                return DataSource.Type == DataSourceType.Ajax;                
            }
        }
        
        public override void VerifySettings()
        {
            base.VerifySettings();
            
            this.ThrowIfClassIsPresent("k-grid-rtl", TextResource.Rtl);

            if (!IsClientBinding && Scrollable.Enabled && Scrollable.Virtual)
            {
                throw new NotSupportedException(TextResource.CannotUseVirtualScrollWithServerBinding);
            }

            if (IsClientBinding)
            {
                if (Columns.OfType<IGridTemplateColumn<T>>().Where(c => c.Template != null && string.IsNullOrEmpty(c.ClientTemplate)).Any())
                {
                    throw new NotSupportedException(TextResource.CannotUseTemplatesInAjaxOrWebService);
                }

                if (DetailTemplate != null && DetailTemplate.HasValue() && string.IsNullOrEmpty(ClientDetailTemplateId))
                {
                    throw new NotSupportedException(TextResource.CannotUseTemplatesInAjaxOrWebService);
                }

                if (Columns.OfType<IGridActionColumn>().Any(c => c.Commands.OfType<GridCustomActionCommand<T>>().Any(command => command.HasValue())))
                {
                    throw new NotSupportedException(TextResource.CustomCommandRoutesWithAjaxBinding);
                }
            }

            if (!DataKeys.Any() && (Editable.Enabled || (Selectable.Enabled && !IsClientBinding)))
            {
                throw new NotSupportedException(TextResource.DataKeysEmpty);
            }          

            if (Editable.Enabled)
            {
                if (HasCommandOfType<GridEditActionCommand>())
                {
                    if (!DataSource.Transport.Update.HasValue())
                    {
                        throw new NotSupportedException(TextResource.EditCommandRequiresUpdate);
                    }
                }

                if (HasCommandOfType<GridDestroyActionCommand>())
                {
                    if (!DataSource.Transport.Destroy.HasValue() && Editable.Mode != GridEditMode.InCell)
                    {
                        throw new NotSupportedException(TextResource.DeleteCommandRequiresDelete);
                    }
                }

                if (HasCommandOfType<GridToolBarCreateCommand<T>>())
                {
                    if (!DataSource.Transport.Create.HasValue() && Editable.Mode != GridEditMode.InCell)
                    {
                        throw new NotSupportedException(TextResource.InsertCommandRequiresInsert);
                    }
                }

                if (HasCommandOfType<GridToolBarSaveCommand<T>>())
                {
                    if (Editable.Mode != GridEditMode.InCell)
                    {
                        throw new NotSupportedException(TextResource.BatchUpdatesRequireInCellMode);
                    }

                    if (!DataSource.Transport.Update.HasValue())
                    {
                        throw new NotSupportedException(TextResource.BatchUpdatesRequireUpdate);
                    }
                }                

                if (Editable.Mode == GridEditMode.InCell) 
                {
                    if (!IsClientBinding)
                    {
                        throw new NotSupportedException(TextResource.InCellModeNotSupportedInServerBinding);
                    }

                    if (ClientRowTemplate.HasValue() || RowTemplate.HasValue())
                    {
                        throw new NotSupportedException(TextResource.InCellModeNotSupportedWithRowTemplate);
                    }
                }

                if(typeof(T) == typeof(System.Data.DataRowView) && Editable.Mode == GridEditMode.InLine 
                    && Columns.OfType<IGridBoundColumn>().Where(c => c.EditorTemplateName.HasValue()).Any())
                {
                    throw new NotSupportedException(TextResource.DataTableInLineEditingWithCustomEditorTemplates);
                }

                if (typeof(T) == typeof(System.Data.DataRowView) && Editable.Mode == GridEditMode.PopUp
                    && !Editable.TemplateName.HasValue())
                {
                    throw new NotSupportedException(TextResource.DataTablePopUpTemplate);
                }
            }
        }

        private bool HasCommandOfType<TCommand>()
        {
            return Columns.OfType<GridActionColumn<T>>().SelectMany(c => c.Commands).OfType<TCommand>().Any() ||
                ToolBar.Commands.OfType<TCommand>().Any();
        }        

        private void InitializeEditors()
        {            
            var skip = ViewContext.HttpContext.Items["$SelfInitialize$"] != null && (bool)ViewContext.HttpContext.Items["$SelfInitialize$"] == true;

            ViewContext.HttpContext.Items["$SelfInitialize$"] = true;            
            
            var dataItem = Editable.DefaultDataItem();

            var htmlHelper = new GridHtmlHelper<T>(ViewContext, DataKeyStore);

            if (Editable.Mode != GridEditMode.InLine && Editable.Mode != GridEditMode.InCell)
            {
                var container = new HtmlElement("div").AddClass(UIPrimitives.Grid.InFormContainer);

                htmlHelper.EditorForModel(dataItem, Editable.TemplateName, Columns.OfType<IGridForeignKeyColumn>().Select(c => c.SerializeSelectList), Editable.AdditionalViewData).AppendTo(container);

                EditorHtml = container.InnerHtml;
            }
            else
            {
                var cellBuilderFactory = new GridCellBuilderFactory();

                
                VisibleColumns.Each(column =>
                {
                    var cellBuilder = cellBuilderFactory.CreateEditCellBuilder(column, htmlHelper);
                    
                    var editor = cellBuilder.CreateCell(dataItem);

                    column.EditorHtml = editor.InnerHtml;
                });
            }

            if (!skip)
            {
                ViewContext.HttpContext.Items.Remove("$SelfInitialize$");
            }
        }
        public string EditorHtml { get; set; }

        public bool HasDetailTemplate
        {
            get
            {
                return DetailTemplate.HasValue();
            }
        }

        public HtmlTemplate<T> DetailTemplate
        {
            get;
            private set;
        }

        public string ClientDetailTemplateId
        {
            get;
            set;
        }

        private IGridDataKeyStore DataKeyStore
        {
            get
            {
                if (dataKeyStore == null)
                {
                    var dataKeys = DataKeys.Cast<IDataKey>();
                    var currentKeyValues = DataKeys.Select(key => key.GetCurrentValue(ViewContext.Controller.ValueProvider)).ToArray();

                    dataKeyStore = new GridDataKeyStore(dataKeys, currentKeyValues);
                }
                return dataKeyStore;
            }
        }

        public IGridUrlBuilder UrlBuilder
        {
            get
            {
                if (urlBuilder == null)
                {
                    urlBuilder = new GridUrlBuilder(this, DataKeyStore);
                }

                return urlBuilder;   
            }
        }
    }
}
