using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Humanizer;
using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Attribute = Rock.Model.Attribute;

using org.kcionline.bricksandmortarstudio.Utils;

namespace RockWeb.Plugins.com_bricksandmortarstudio.KingsChurch
{
    [DisplayName( "Add Line Group" )]
    [Category( "com_bricksandmortarstudio > KingsChurch" )]
    [Description( "Displays the details of the given group." )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.MAP_STYLES, "Map Style", "The style of maps to use", false, false, Rock.SystemGuid.DefinedValue.MAP_STYLE_ROCK, "", 0 )]
    [BooleanField( "Show Time Field", "Whether or not to show the time field", false, order: 1 )]

    public partial class AddLineGroup : RockBlock, IDetailBlock
    {
        #region Constants

        private const string MEMBER_LOCATION_TAB_TITLE = "Leader Location";
        private const string OTHER_LOCATION_TAB_TITLE = "Other Location";

        #endregion

        #region Fields

        private readonly List<string> _tabs = new List<string> { MEMBER_LOCATION_TAB_TITLE, OTHER_LOCATION_TAB_TITLE };

        #endregion

        #region Properties

        private string LocationTypeTab { get; set; }

        private int CurrentGroupTypeId { get; set; }

        private List<GroupLocation> GroupLocationsState { get; set; }

        private List<InheritedAttribute> GroupMemberAttributesInheritedState { get; set; }

        private List<Attribute> GroupMemberAttributesState { get; set; }

        private List<GroupRequirement> GroupRequirementsState { get; set; }

        private bool AllowMultipleLocations { get; set; }

        private List<GroupMemberWorkflowTrigger> MemberWorkflowTriggersState { get; set; }

        private GroupTypeCache CurrentGroupTypeCache
        {
            get
            {
                return GroupTypeCache.Read( CurrentGroupTypeId );
            }

            set
            {
                CurrentGroupTypeId = value != null ? value.Id : 0;
            }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            LocationTypeTab = ViewState["LocationTypeTab"] as string ?? MEMBER_LOCATION_TAB_TITLE;
            CurrentGroupTypeId = ViewState["CurrentGroupTypeId"] as int? ?? 0;

            // NOTE: These things are converted to JSON prior to going into ViewState, so the json variable could be null or the string "null"!
            string json = ViewState["GroupLocationsState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupLocationsState = new List<GroupLocation>();
            }
            else
            {
                GroupLocationsState = JsonConvert.DeserializeObject<List<GroupLocation>>( json );
            }

            json = ViewState["GroupMemberAttributesInheritedState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupMemberAttributesInheritedState = new List<InheritedAttribute>();
            }
            else
            {
                GroupMemberAttributesInheritedState = JsonConvert.DeserializeObject<List<InheritedAttribute>>( json );
            }

            json = ViewState["GroupMemberAttributesState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupMemberAttributesState = new List<Attribute>();
            }
            else
            {
                GroupMemberAttributesState = JsonConvert.DeserializeObject<List<Attribute>>( json );
            }

            json = ViewState["GroupRequirementsState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                GroupRequirementsState = new List<GroupRequirement>();
            }
            else
            {
                GroupRequirementsState = JsonConvert.DeserializeObject<List<GroupRequirement>>( json ) ?? new List<GroupRequirement>();
            }

            // get the GroupRole for each GroupRequirement from the database it case it isn't serialized, and we'll need it
            var groupRoleIds = GroupRequirementsState.Where( a => a.GroupRoleId.HasValue && a.GroupRole == null ).Select( a => a.GroupRoleId.Value ).Distinct().ToList();
            if ( groupRoleIds.Any() )
            {
                var groupRoles = new GroupTypeRoleService( new RockContext() ).GetByIds( groupRoleIds );
                GroupRequirementsState.ForEach( a =>
                {
                    if ( a.GroupRoleId.HasValue )
                    {
                        a.GroupRole = groupRoles.FirstOrDefault( b => b.Id == a.GroupRoleId );
                    }
                } );
            }

            AllowMultipleLocations = ViewState["AllowMultipleLocations"] as bool? ?? false;

            json = ViewState["MemberWorkflowTriggersState"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                MemberWorkflowTriggersState = new List<GroupMemberWorkflowTrigger>();
            }
            else
            {
                MemberWorkflowTriggersState = JsonConvert.DeserializeObject<List<GroupMemberWorkflowTrigger>>( json );
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gLocations.DataKeyNames = new string[] { "Guid" };
            gLocations.Actions.AddClick += gLocations_Add;
            gLocations.GridRebind += gLocations_GridRebind;

            rblScheduleSelect.BindToEnum<ScheduleType>();

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlGroupDetail );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                var parentGroupId = PageParameter( "ParentGroupId" ).AsIntegerOrNull();
                if ( parentGroupId != null )
                {
                    ShowDetail( parentGroupId.Value );
                }
                else
                {
                    pnlDetails.Visible = false;
                }
            }
            else
            {
                ShowDialog();
            }

            // Rebuild the attribute controls on postback based on group type
            if ( pnlDetails.Visible )
            {
                if ( CurrentGroupTypeId > 0 )
                {
                    var group = new Group { GroupTypeId = CurrentGroupTypeId };
                    ShowGroupTypeEditDetails( CurrentGroupTypeCache, group, false );
                }
            }
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState["LocationTypeTab"] = LocationTypeTab;
            ViewState["CurrentGroupTypeId"] = CurrentGroupTypeId;
            ViewState["GroupLocationsState"] = JsonConvert.SerializeObject( GroupLocationsState, Formatting.None, jsonSetting );
            ViewState["GroupMemberAttributesInheritedState"] = JsonConvert.SerializeObject( GroupMemberAttributesInheritedState, Formatting.None, jsonSetting );
            ViewState["GroupMemberAttributesState"] = JsonConvert.SerializeObject( GroupMemberAttributesState, Formatting.None, jsonSetting );
            ViewState["GroupRequirementsState"] = JsonConvert.SerializeObject( GroupRequirementsState, Formatting.None, jsonSetting );
            ViewState["AllowMultipleLocations"] = AllowMultipleLocations;
            ViewState["MemberWorkflowTriggersState"] = JsonConvert.SerializeObject( MemberWorkflowTriggersState, Formatting.None, jsonSetting );

            return base.SaveViewState();
        }

        /// <summary>
        /// Returns breadcrumbs specific to the block that should be added to navigation
        /// based on the current page reference.  This function is called during the page's
        /// oninit to load any initial breadcrumbs.
        /// </summary>
        /// <param name="pageReference">The <see cref="Rock.Web.PageReference" />.</param>
        /// <returns>
        /// A <see cref="System.Collections.Generic.List{BreadCrumb}" /> of block related <see cref="Rock.Web.UI.BreadCrumb">BreadCrumbs</see>.
        /// </returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int? groupId = PageParameter( pageReference, "GroupId" ).AsIntegerOrNull();
            if ( groupId != null )
            {
                Group group = new GroupService( new RockContext() ).Get( groupId.Value );
                if ( group != null )
                {
                    breadCrumbs.Add( new BreadCrumb( group.Name, pageReference ) );
                }
                else
                {
                    breadCrumbs.Add( new BreadCrumb( "New Group", pageReference ) );
                }
            }
            else
            {
                // don't show a breadcrumb if we don't have a pageparam to work with
            }

            return breadCrumbs;
        }

        #endregion

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            bool wasSecurityRole = false;
            bool triggersUpdated = false;

            RockContext rockContext = new RockContext();

            GroupService groupService = new GroupService( rockContext );
            ScheduleService scheduleService = new ScheduleService( rockContext );

            var roleGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() );
            int roleGroupTypeId = roleGroupType != null ? roleGroupType.Id : int.MinValue;

            int? parentGroupId = PageParameter( "ParentGroupId" ).AsIntegerOrNull();
            if ( parentGroupId != null )
            {
                var parentGroup = groupService.Get( parentGroupId.Value );
                if ( parentGroup != null )
                {
                    CurrentGroupTypeId = parentGroup.GroupTypeId;

                    if ( !lppLeader.PersonId.HasValue )
                    {
                        nbMessage.Text = "A Leader is required.";
                        nbMessage.Visible = true;
                        return;
                    }

                    if ( CurrentGroupTypeId == 0 )
                    {
                        return;
                    }

                    Group group = new Group();
                    group.IsSystem = false;
                    group.Name = string.Empty;

                    // add/update any group locations that were added or changed in the UI (we already removed the ones that were removed above)
                    foreach ( var groupLocationState in GroupLocationsState )
                    {
                        GroupLocation groupLocation = group.GroupLocations.Where( l => l.Guid == groupLocationState.Guid ).FirstOrDefault();
                        if ( groupLocation == null )
                        {
                            groupLocation = new GroupLocation();
                            group.GroupLocations.Add( groupLocation );
                        }
                        else
                        {
                            groupLocationState.Id = groupLocation.Id;
                            groupLocationState.Guid = groupLocation.Guid;

                            var selectedSchedules = groupLocationState.Schedules.Select( s => s.Guid ).ToList();
                            foreach ( var schedule in groupLocation.Schedules.Where( s => !selectedSchedules.Contains( s.Guid ) ).ToList() )
                            {
                                groupLocation.Schedules.Remove( schedule );
                            }
                        }

                        groupLocation.CopyPropertiesFrom( groupLocationState );

                        var existingSchedules = groupLocation.Schedules.Select( s => s.Guid ).ToList();
                        foreach ( var scheduleState in groupLocationState.Schedules.Where( s => !existingSchedules.Contains( s.Guid ) ).ToList() )
                        {
                            var schedule = scheduleService.Get( scheduleState.Guid );
                            if ( schedule != null )
                            {
                                groupLocation.Schedules.Add( schedule );
                            }
                        }
                    }

                    GroupMember leader = new GroupMember();
                    leader.GroupMemberStatus = GroupMemberStatus.Active;
                    leader.PersonId = lppLeader.PersonId.Value;
                    leader.Person = new PersonService( rockContext ).Get( lppLeader.PersonId.Value );
                    leader.GroupRole = parentGroup.GroupType.Roles.Where( r => r.IsLeader ).FirstOrDefault() ?? parentGroup.GroupType.DefaultGroupRole;

                    group.Name = String.Format( "{0}, {1}", leader.Person.LastName, leader.Person.NickName );
                    group.Description = tbDescription.Text;
                    group.CampusId = parentGroup.CampusId;
                    group.GroupTypeId = CurrentGroupTypeId;
                    group.ParentGroupId = parentGroupId;
                    group.IsSecurityRole = false;
                    group.IsActive = true;
                    group.IsPublic = true;

                    if ( dpStartDate.SelectedDate.HasValue )
                    {
                        group.CreatedDateTime = dpStartDate.SelectedDate.Value;
                    }

                    group.Members.Add( leader );

                    string iCalendarContent = string.Empty;

                    // If unique schedule option was selected, but a schedule was not defined, set option to 'None'
                    var scheduleType = rblScheduleSelect.SelectedValueAsEnum<ScheduleType>( ScheduleType.None );
                    if ( scheduleType == ScheduleType.Custom )
                    {
                        iCalendarContent = sbSchedule.iCalendarContent;
                        var calEvent = ScheduleICalHelper.GetCalenderEvent( iCalendarContent );
                        if ( calEvent == null || calEvent.DTStart == null )
                        {
                            scheduleType = ScheduleType.None;
                        }
                    }

                    if ( scheduleType == ScheduleType.Weekly )
                    {
                        if ( !dowWeekly.SelectedDayOfWeek.HasValue )
                        {
                            scheduleType = ScheduleType.None;
                        }
                    }

                    int? oldScheduleId = hfUniqueScheduleId.Value.AsIntegerOrNull();
                    if ( scheduleType == ScheduleType.Custom || scheduleType == ScheduleType.Weekly )
                    {
                        if ( !oldScheduleId.HasValue || group.Schedule == null )
                        {
                            group.Schedule = new Schedule();
                        }

                        if ( scheduleType == ScheduleType.Custom )
                        {
                            group.Schedule.iCalendarContent = iCalendarContent;
                            group.Schedule.WeeklyDayOfWeek = null;
                            group.Schedule.WeeklyTimeOfDay = null;
                        }
                        else
                        {
                            group.Schedule.iCalendarContent = null;
                            group.Schedule.WeeklyDayOfWeek = dowWeekly.SelectedDayOfWeek;
                            group.Schedule.WeeklyTimeOfDay = timeWeekly.SelectedTime;
                        }
                    }
                    else
                    {
                        // If group did have a unique schedule, delete that schedule
                        if ( oldScheduleId.HasValue )
                        {
                            var schedule = scheduleService.Get( oldScheduleId.Value );
                            if ( schedule != null && string.IsNullOrEmpty( schedule.Name ) )
                            {
                                scheduleService.Delete( schedule );
                            }
                        }

                        if ( scheduleType == ScheduleType.Named )
                        {
                            group.ScheduleId = spSchedule.SelectedValueAsId();
                        }
                        else
                        {
                            group.ScheduleId = null;
                        }
                    }

                    group.LoadAttributes();
                    Rock.Attribute.Helper.GetEditValues( phGroupAttributes, group );

                    group.GroupType = new GroupTypeService( rockContext ).Get( group.GroupTypeId );
                    if ( group.ParentGroupId.HasValue )
                    {
                        group.ParentGroup = groupService.Get( group.ParentGroupId.Value );
                    }

                    if ( !Page.IsValid )
                    {
                        return;
                    }

                    // if the groupMember IsValid is false, and the UI controls didn't report any errors, it is probably because the custom rules of GroupMember didn't pass.
                    // So, make sure a message is displayed in the validation summary
                    cvGroup.IsValid = group.IsValid;

                    if ( !cvGroup.IsValid )
                    {
                        cvGroup.ErrorMessage = group.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                        return;
                    }

                    // use WrapTransaction since SaveAttributeValues does it's own RockContext.SaveChanges()
                    rockContext.WrapTransaction( () =>
                    {
                        var adding = group.Id.Equals( 0 );
                        if ( adding )
                        {
                            groupService.Add( group );
                        }

                        rockContext.SaveChanges();

                        if ( adding )
                        {
                            // add ADMINISTRATE to the person who added the group 
                            Rock.Security.Authorization.AllowPerson( group, Authorization.ADMINISTRATE, this.CurrentPerson, rockContext );
                        }

                        group.SaveAttributeValues( rockContext );
                    } );

                    bool isNowSecurityRole = group.IsActive && ( group.GroupTypeId == roleGroupTypeId );

                    if ( group != null && wasSecurityRole )
                    {
                        if ( !isNowSecurityRole )
                        {
                            // if this group was a SecurityRole, but no longer is, flush
                            Rock.Security.Role.Flush( group.Id );
                            Rock.Security.Authorization.Flush();
                        }
                    }
                    else
                    {
                        if ( isNowSecurityRole )
                        {
                            // new security role, flush
                            Rock.Security.Authorization.Flush();
                        }
                    }

                    AttributeCache.FlushEntityAttributes();

                    if ( triggersUpdated )
                    {
                        GroupMemberWorkflowTriggerService.FlushCachedTriggers();
                    }

                    pnlDetails.Visible = false;
                    pnlSuccess.Visible = true;
                    nbSuccess.Text = string.Format( "{0} has been added", group.Name );
                }
            }
        }

        #endregion

        #region Control Events

        /// <summary>
        /// Handles the Click event of the lbProperty control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbLocationType_Click( object sender, EventArgs e )
        {
            LinkButton lb = sender as LinkButton;
            if ( lb != null )
            {
                LocationTypeTab = lb.Text;

                rptLocationTypes.DataSource = _tabs;
                rptLocationTypes.DataBind();
            }

            ShowSelectedPane();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            var parentGroupId = PageParameter( "ParentGroupId" ).AsIntegerOrNull();
            if ( parentGroupId != null )
            {
                ShowDetail( parentGroupId.Value );
            }
            else
            {
                pnlDetails.Visible = false;
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblScheduleSelect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void rblScheduleSelect_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetScheduleDisplay();
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="dialog">The dialog.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( string dialog, bool setValues = false )
        {
            hfActiveDialog.Value = dialog.ToUpper().Trim();
            ShowDialog( setValues );
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( bool setValues = false )
        {
            switch ( hfActiveDialog.Value )
            {
                case "LOCATIONS":
                    dlgLocations.Show();
                    break;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="parentGroupId">The parent group identifier.</param>
        public void ShowDetail( int parentGroupId )
        {
            RockContext rockContext = new RockContext();
            //var allowedGroupIds = LineQuery.GetCellGroupIdsInLine( CurrentPerson, rockContext );
            //if ( !allowedGroupIds.Contains( parentGroupId ) )
            //{
            //    nbMessage.Text = "You are not allowed to add a group to this parent group.";
            //    nbMessage.Visible = true;
            //    pnlEditDetails.Visible = false;
            //}
            //else
            //{
            Group group = new Group { Id = 0, IsActive = true, IsPublic = true, ParentGroupId = parentGroupId, Name = string.Empty };
            GroupLocationsState = group.GroupLocations.ToList();

            var parentGroup = new GroupService( rockContext ).Get( parentGroupId );
            if ( parentGroup != null )
            {
                group.ParentGroup = parentGroup;
                group.GroupType = parentGroup.GroupType;
                group.GroupTypeId = parentGroup.GroupTypeId;
                CurrentGroupTypeId = parentGroup.GroupTypeId;
            }

            lReadOnlyTitle.Text = ActionTitle.Add( Group.FriendlyTypeName ).FormatAsHtmlTitle();

            pnlEditDetails.Visible = true;

            SetScheduleControls( group.GroupType, group );
            ShowGroupTypeEditDetails( GroupTypeCache.Read( group.GroupTypeId ), group, true );
            //}                     
        }

        /// <summary>
        /// Shows the group type edit details.
        /// </summary>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="group">The group.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowGroupTypeEditDetails( GroupTypeCache groupType, Group group, bool setValues )
        {
            if ( group != null )
            {
                // Save value to viewstate for use later when binding location grid
                AllowMultipleLocations = groupType != null && groupType.AllowMultipleLocations;

                if ( groupType != null && groupType.LocationSelectionMode != GroupLocationPickerMode.None )
                {
                    gLocations.Visible = true;
                    BindLocationsGrid();
                }
                else
                {
                    gLocations.Visible = false;
                }

                gLocations.Columns[2].Visible = groupType != null && ( groupType.EnableLocationSchedules ?? false );
                spSchedules.Visible = groupType != null && ( groupType.EnableLocationSchedules ?? false );

                phGroupAttributes.Controls.Clear();
                group.LoadAttributes();

                if ( group.Attributes != null && group.Attributes.Any() )
                {
                    Rock.Attribute.Helper.AddEditControls( group, phGroupAttributes, setValues, BlockValidationGroup );
                }
            }
        }

        private void SetScheduleControls( GroupType groupType, Group group )
        {

            rblScheduleSelect.Items.Clear();

            ListItem liNone = new ListItem( "None", "0" );
            liNone.Selected = group != null && ( group.Schedule == null || group.Schedule.ScheduleType == ScheduleType.None );
            rblScheduleSelect.Items.Add( liNone );

            if ( groupType != null && ( groupType.AllowedScheduleTypes & ScheduleType.Weekly ) == ScheduleType.Weekly )
            {
                ListItem li = new ListItem( "Weekly", "1" );
                li.Selected = group != null && group.Schedule != null && group.Schedule.ScheduleType == ScheduleType.Weekly;
                rblScheduleSelect.Items.Add( li );
            }

            if ( groupType != null && ( groupType.AllowedScheduleTypes & ScheduleType.Custom ) == ScheduleType.Custom )
            {
                ListItem li = new ListItem( "Custom", "2" );
                li.Selected = group != null && group.Schedule != null && group.Schedule.ScheduleType == ScheduleType.Custom;
                rblScheduleSelect.Items.Add( li );
            }

            if ( groupType != null && ( groupType.AllowedScheduleTypes & ScheduleType.Named ) == ScheduleType.Named )
            {
                ListItem li = new ListItem( "Named", "4" );
                li.Selected = group != null && group.Schedule != null && group.Schedule.ScheduleType == ScheduleType.Named;
                rblScheduleSelect.Items.Add( li );
            }

            SetScheduleDisplay();
        }

        /// <summary>
        /// Gets the tab class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        protected string GetTabClass( object property )
        {
            if ( property.ToString() == LocationTypeTab )
            {
                return "active";
            }

            return string.Empty;
        }

        /// <summary>
        /// Shows the selected pane.
        /// </summary>
        private void ShowSelectedPane()
        {
            if ( LocationTypeTab.Equals( MEMBER_LOCATION_TAB_TITLE ) )
            {
                pnlMemberSelect.Visible = true;
                pnlLocationSelect.Visible = false;
            }
            else if ( LocationTypeTab.Equals( OTHER_LOCATION_TAB_TITLE ) )
            {
                pnlMemberSelect.Visible = false;
                pnlLocationSelect.Visible = true;
            }
        }

        private void SetScheduleDisplay()
        {
            dowWeekly.Visible = false;
            timeWeekly.Visible = false;
            spSchedule.Visible = false;
            sbSchedule.Visible = false;

            if ( !string.IsNullOrWhiteSpace( rblScheduleSelect.SelectedValue ) )
            {
                switch ( rblScheduleSelect.SelectedValueAsEnum<ScheduleType>() )
                {
                    case ScheduleType.None:
                        {
                            break;
                        }

                    case ScheduleType.Weekly:
                        {
                            dowWeekly.Visible = true;
                            timeWeekly.Visible = GetAttributeValue("ShowTimeField").AsBoolean();
                            break;
                        }

                    case ScheduleType.Custom:
                        {
                            sbSchedule.Visible = true;
                            break;
                        }

                    case ScheduleType.Named:
                        {
                            spSchedule.Visible = true;
                            break;
                        }
                }
            }
        }

        #endregion

        #region Location Grid and Picker

        /// <summary>
        /// Handles the Add event of the gLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gLocations_Add( object sender, EventArgs e )
        {
            gLocations_ShowEdit( Guid.Empty );
        }

        /// <summary>
        /// Handles the Edit event of the gLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gLocations_Edit( object sender, RowEventArgs e )
        {
            Guid locationGuid = (Guid)e.RowKeyValue;
            gLocations_ShowEdit( locationGuid );
        }

        /// <summary>
        /// Gs the locations_ show edit.
        /// </summary>
        /// <param name="locationGuid">The location unique identifier.</param>
        protected void gLocations_ShowEdit( Guid locationGuid )
        {
            var rockContext = new RockContext();
            ddlMember.Items.Clear();
            int? groupTypeId = this.CurrentGroupTypeId;
            if ( groupTypeId.HasValue )
            {
                var groupType = GroupTypeCache.Read( groupTypeId.Value );
                if ( groupType != null )
                {
                    GroupLocationPickerMode groupTypeModes = groupType.LocationSelectionMode;
                    if ( groupTypeModes != GroupLocationPickerMode.None )
                    {
                        // Set the location picker modes allowed based on the group type's allowed modes
                        LocationPickerMode modes = LocationPickerMode.None;
                        if ( ( groupTypeModes & GroupLocationPickerMode.Named ) == GroupLocationPickerMode.Named )
                        {
                            modes = modes | LocationPickerMode.Named;
                        }

                        if ( ( groupTypeModes & GroupLocationPickerMode.Address ) == GroupLocationPickerMode.Address )
                        {
                            modes = modes | LocationPickerMode.Address;
                        }

                        if ( ( groupTypeModes & GroupLocationPickerMode.Point ) == GroupLocationPickerMode.Point )
                        {
                            modes = modes | LocationPickerMode.Point;
                        }

                        if ( ( groupTypeModes & GroupLocationPickerMode.Polygon ) == GroupLocationPickerMode.Polygon )
                        {
                            modes = modes | LocationPickerMode.Polygon;
                        }

                        bool displayMemberTab = ( groupTypeModes & GroupLocationPickerMode.GroupMember ) == GroupLocationPickerMode.GroupMember;
                        bool displayOtherTab = modes != LocationPickerMode.None;

                        ulNav.Visible = displayOtherTab && displayMemberTab;
                        pnlMemberSelect.Visible = displayMemberTab;
                        pnlLocationSelect.Visible = displayOtherTab && !displayMemberTab;

                        if ( displayMemberTab )
                        {
                            int? leaderId = lppLeader.PersonId;
                            if ( leaderId != null )
                            {
                                var personService = new PersonService( rockContext );
                                Guid previousLocationType = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid();
                                var leader = personService.Get( lppLeader.PersonId.Value );

                                foreach ( Group family in personService.GetFamilies( leaderId.Value ) )
                                {
                                    foreach ( GroupLocation familyGroupLocation in family.GroupLocations
                                        .Where( l => l.IsMappedLocation && !l.GroupLocationTypeValue.Guid.Equals( previousLocationType ) ) )
                                    {
                                        ListItem li = new ListItem(
                                                string.Format( "{0} {1} ({2})", leader.FullName, familyGroupLocation.GroupLocationTypeValue.Value, familyGroupLocation.Location.ToString() ),
                                                string.Format( "{0}|{1}", familyGroupLocation.Location.Id, leader.Id ) );

                                        ddlMember.Items.Add( li );
                                    }
                                }

                            }
                        }

                        if ( displayOtherTab )
                        {
                            locpGroupLocation.AllowedPickerModes = modes;
                            locpGroupLocation.CurrentPickerMode = locpGroupLocation.GetBestPickerModeForLocation( null );
                        }

                        ddlLocationType.DataSource = groupType.LocationTypeValues.ToList();
                        ddlLocationType.DataBind();

                        var groupLocation = GroupLocationsState.FirstOrDefault( l => l.Guid.Equals( locationGuid ) );
                        if ( groupLocation != null && groupLocation.Location != null )
                        {
                            if ( displayOtherTab )
                            {
                                locpGroupLocation.CurrentPickerMode = locpGroupLocation.GetBestPickerModeForLocation( groupLocation.Location );

                                locpGroupLocation.MapStyleValueGuid = GetAttributeValue( "MapStyle" ).AsGuid();

                                if ( groupLocation.Location != null )
                                {
                                    locpGroupLocation.Location = new LocationService( rockContext ).Get( groupLocation.Location.Id );
                                }
                            }

                            if ( displayMemberTab && ddlMember.Items.Count > 0 && groupLocation.GroupMemberPersonAliasId.HasValue )
                            {
                                LocationTypeTab = MEMBER_LOCATION_TAB_TITLE;
                                int? personId = new PersonAliasService( rockContext ).GetPersonId( groupLocation.GroupMemberPersonAliasId.Value );
                                if ( personId.HasValue )
                                {
                                    ddlMember.SetValue( string.Format( "{0}|{1}", groupLocation.LocationId, personId.Value ) );
                                }
                            }
                            else if ( displayOtherTab )
                            {
                                LocationTypeTab = OTHER_LOCATION_TAB_TITLE;
                            }

                            ddlLocationType.SetValue( groupLocation.GroupLocationTypeValueId );

                            spSchedules.SetValues( groupLocation.Schedules );

                            hfAddLocationGroupGuid.Value = locationGuid.ToString();
                        }
                        else
                        {
                            hfAddLocationGroupGuid.Value = string.Empty;
                            LocationTypeTab = ( displayMemberTab && ddlMember.Items.Count > 0 ) ? MEMBER_LOCATION_TAB_TITLE : OTHER_LOCATION_TAB_TITLE;
                        }

                        rptLocationTypes.DataSource = _tabs;
                        rptLocationTypes.DataBind();
                        ShowSelectedPane();

                        ShowDialog( "Locations", true );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Delete event of the gLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gLocations_Delete( object sender, RowEventArgs e )
        {
            Guid rowGuid = (Guid)e.RowKeyValue;
            GroupLocationsState.RemoveEntity( rowGuid );

            BindLocationsGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gLocations_GridRebind( object sender, EventArgs e )
        {
            BindLocationsGrid();
        }

        /// <summary>
        /// Handles the SaveClick event of the dlgLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dlgLocations_SaveClick( object sender, EventArgs e )
        {
            Location location = null;
            int? memberPersonAliasId = null;
            RockContext rockContext = new RockContext();

            if ( LocationTypeTab.Equals( MEMBER_LOCATION_TAB_TITLE ) )
            {
                if ( ddlMember.SelectedValue != null )
                {
                    var ids = ddlMember.SelectedValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).AsIntegerList().ToArray();
                    if ( ids.Length == 2 )
                    {
                        int locationId = ids[0];
                        int primaryAliasId = ids[1];
                        var dbLocation = new LocationService( rockContext ).Get( locationId );
                        if ( dbLocation != null )
                        {
                            location = new Location();
                            location.CopyPropertiesFrom( dbLocation );
                        }

                        memberPersonAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( primaryAliasId );
                    }
                }
            }
            else
            {
                if ( locpGroupLocation.Location != null )
                {
                    location = new Location();
                    location.CopyPropertiesFrom( locpGroupLocation.Location );
                }
            }

            if ( location != null )
            {
                GroupLocation groupLocation = null;

                Guid guid = hfAddLocationGroupGuid.Value.AsGuid();
                if ( !guid.IsEmpty() )
                {
                    groupLocation = GroupLocationsState.FirstOrDefault( l => l.Guid.Equals( guid ) );
                }

                if ( groupLocation == null )
                {
                    groupLocation = new GroupLocation();
                    GroupLocationsState.Add( groupLocation );
                }

                groupLocation.GroupMemberPersonAliasId = memberPersonAliasId;
                groupLocation.Location = location;
                groupLocation.LocationId = groupLocation.Location.Id;
                groupLocation.GroupLocationTypeValueId = ddlLocationType.SelectedValueAsId();

                var selectedIds = spSchedules.SelectedValuesAsInt();
                groupLocation.Schedules = new ScheduleService( rockContext ).Queryable()
                    .Where( s => selectedIds.Contains( s.Id ) ).ToList();

                if ( groupLocation.GroupLocationTypeValueId.HasValue )
                {
                    groupLocation.GroupLocationTypeValue = new DefinedValue();
                    var definedValue = new DefinedValueService( rockContext ).Get( groupLocation.GroupLocationTypeValueId.Value );
                    if ( definedValue != null )
                    {
                        groupLocation.GroupLocationTypeValue.CopyPropertiesFrom( definedValue );
                    }
                }
            }

            BindLocationsGrid();

            dlgLocations.Hide();
            hfActiveDialog.Value = string.Empty;

        }

        /// <summary>
        /// Binds the locations grid.
        /// </summary>
        private void BindLocationsGrid()
        {
            gLocations.Actions.ShowAdd = AllowMultipleLocations || !GroupLocationsState.Any();

            gLocations.DataSource = GroupLocationsState
                .Select( gl => new
                {
                    gl.Guid,
                    gl.Location,
                    Type = gl.GroupLocationTypeValue != null ? gl.GroupLocationTypeValue.Value : string.Empty,
                    Order = gl.GroupLocationTypeValue != null ? gl.GroupLocationTypeValue.Order : 0,
                    Schedules = gl.Schedules != null ? gl.Schedules.Select( s => s.Name ).ToList().AsDelimited( ", " ) : string.Empty
                } )
                .OrderBy( i => i.Order )
                .ToList();
            gLocations.DataBind();
        }

        #endregion
    }
}