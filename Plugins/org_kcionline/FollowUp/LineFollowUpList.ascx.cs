using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using Microsoft.Ajax.Utilities;
using Mono.CSharp;
using Rock.Data;
using Rock.Model;
using org.kcionline.bricksandmortarstudio.Utils;
using Rock;
using Rock.Attribute;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org_kcionline.FollowUp
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Consolidator Coordinator List" )]
    [Category( "Bricks and Mortar Studio" )]
    [Description( "A block that displays all the follow ups assigned to you." )]
    [LinkedPage( "Detail Page" )]

    public partial class LineFollowUpList : Rock.Web.UI.RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbBox.Visible = false;
            var personId = Request.QueryString["personId"].AsIntegerOrNull();
            if (!Request.QueryString["success"].IsNullOrWhiteSpace() && !Request.QueryString["type"].IsNullOrWhiteSpace() && personId != null && LineQuery.IsPersonInLeadersLine( personId.Value, CurrentPerson ) )
            {
                var person = new PersonService(new RockContext()).Get( personId.Value);

                switch ( Request.QueryString["type"] )
                {
                    case "transfer":
                        nbBox.Title = person.FullName;
                        nbBox.Text =" has been transferred successfully";
                        break;
                    case "place":
                        nbBox.Title = person.FullName;
                        nbBox.Text = " has been placed into their group successfully";
                        break;
                }
                nbBox.Visible = true;
                nbBox.Dismissable = true;
            }
            if ( !Page.IsPostBack )
            {
                SetFilter();
                BindGrid();
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }





        protected void gList_OnRowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "ConnectionRequestId", e.RowKeyValues["Id"].ToString().AsInteger() );
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gFilter.SaveUserPreference( "Consolidator", ppConsolidator.PersonId.ToString() );
            gFilter.SaveUserPreference( "Status", ddlStatus.SelectedValue );
            gFilter.SaveUserPreference( "Submitted", drpDates.DelimitedValues );

            BindGrid();
        }

        protected void gFilter_OnClearFilterClick( object sender, EventArgs e )
        {
            ppConsolidator.SetValue( null );
            ddlStatus.SetValue( "" );
            drpDates.DelimitedValues = "";
            gFilter.SaveUserPreference( "Consolidator", "" );
            gFilter.SaveUserPreference( "Status", "" );
            gFilter.SaveUserPreference( "Submitted", "" );

            BindGrid();
        }

        protected void gFilter_OnDisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Status":
                    {
                        if ( !string.IsNullOrWhiteSpace( e.Value ) )
                        {
                            var connectionStatus = new ConnectionStatusService( new RockContext() ).Get( e.Value.AsGuid() );
                            if ( connectionStatus != null )
                            {

                                e.Value = connectionStatus.Name;
                            }
                        }

                        break;
                    }
                case "Consolidator":
                    {
                        string personName = string.Empty;

                        int? personId = e.Value.AsIntegerOrNull();
                        if ( personId.HasValue )
                        {
                            var personService = new PersonService( new RockContext() );
                            var person = personService.Get( personId.Value );
                            if ( person != null )
                            {
                                personName = person.FullName;
                            }
                        }

                        e.Value = personName;

                        break;
                    }
                case "Submitted":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
            }
        }

        #endregion

        #region Methods

        private void SetFilter()
        {
            var rockContext = new RockContext();
            int followUpConnectionTypeId =
                new ConnectionTypeService( rockContext ).Get(
                                                          org.kcionline.bricksandmortarstudio.SystemGuid.ConnectionType
                                                             .FOLLOW_UP.AsGuid() ).Id;
            var connectionStatuses =
                new ConnectionStatusService( rockContext ).Queryable()
                                                        .Where( c => c.ConnectionTypeId == followUpConnectionTypeId )
                                                        .ToList();

            ddlStatus.Items.Add( new ListItem( "", "" ) );
            connectionStatuses.ForEach( cs => ddlStatus.Items.Add( new ListItem( cs.Name, cs.Guid.ToString() ) ) );

            ddlStatus.SelectedValue = gFilter.GetUserPreference( "Status" );


            int? personId = gFilter.GetUserPreference( "Consolidator" ).AsIntegerOrNull();
            if ( personId.HasValue )
            {
                var personService = new PersonService( new RockContext() );
                var person = personService.Get( personId.Value );
                if ( person != null )
                {
                    ppConsolidator.SetValue( person );
                }
            }

            drpDates.DelimitedValues = gFilter.GetUserPreference( "Submitted" );
            if ( !drpDates.LowerValue.HasValue && !drpDates.UpperValue.HasValue )
            {
                gFilter.SaveUserPreference( "Submitted", drpDates.DelimitedValues );
            }
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {

            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var searchPerson = CurrentPerson;

            // If the person is a group leader, get their coordinator so they can view as them
            var consolidatorCoordinatorGuid =
                org.kcionline.bricksandmortarstudio.SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            var cellGroupType =
                GroupTypeCache.Read( org.kcionline.bricksandmortarstudio.SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            if (
                groupMemberService.Queryable()
                                  .Any( gm => gm.GroupRole.IsLeader && gm.Group.GroupTypeId == cellGroupType.Id ) )
            {
                var baseGroup =
                    // ReSharper disable once PossibleNullReferenceException
                    groupMemberService.Queryable()
                                      .FirstOrDefault(
                                          gm => gm.GroupRole.IsLeader && gm.Group.GroupTypeId == cellGroupType.Id )
                                      .Group;
                while ( baseGroup != null )
                {

                    var coordinator =
                        baseGroup.Members.FirstOrDefault( gm => gm.GroupRole.Guid == consolidatorCoordinatorGuid );
                    if ( coordinator != null )
                    {
                        searchPerson = coordinator.Person;
                        break;
                    }
                    baseGroup = baseGroup.ParentGroup;
                }
            }

            // filtering
            var connectionRequests = LineQuery.GetPeopleInLineFollowUpRequests( searchPerson );
            if ( drpDates.LowerValue.HasValue )
            {
                connectionRequests = connectionRequests.Where( a => a.CreatedDateTime >= drpDates.LowerValue.Value );
            }

            if ( drpDates.UpperValue.HasValue )
            {
                DateTime upperDate = drpDates.UpperValue.Value.Date.AddDays( 1 );
                connectionRequests = connectionRequests.Where( a => a.CreatedDateTime < upperDate );
            }

            if ( !String.IsNullOrEmpty( ddlStatus.SelectedValue ) )
            {
                connectionRequests = connectionRequests.Where( cr => cr.ConnectionStatus.Name == ddlStatus.SelectedValue );
            }

            if ( ppConsolidator.SelectedValue.HasValue )
            {
                connectionRequests =
                    connectionRequests.Where(
                        cr => cr.ConnectorPersonAlias.PersonId == ppConsolidator.SelectedValue.Value );
            }

            // end filtering

            var groupTypeRoleService = new GroupTypeRoleService( rockContext );
            var groupTypeRole =
                groupTypeRoleService.Get(
                    org.kcionline.bricksandmortarstudio.SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid() );

            var followUps = new List<FollowUp>( connectionRequests.Count() );
            foreach ( var request in connectionRequests )
            {
                var firstOrDefault =
                    groupMemberService.GetKnownRelationship( request.PersonAlias.PersonId, groupTypeRole.Id )
                                      .FirstOrDefault();
                if ( firstOrDefault != null )
                {
                    var consolidator = firstOrDefault.Person;
                    followUps.Add( new FollowUp( request, consolidator ) );
                }
            }

            gList.DataSource = followUps;
            gList.DataBind();
        }

        #endregion

        internal class FollowUp
        {
            public FollowUp( ConnectionRequest connectionRequest, Person consolidator )
            {
                ConnectionRequest = connectionRequest;
                Consolidator = consolidator;
                Id = connectionRequest.Id;
            }

            public int Id { get; set; }
            public Person Consolidator { get; set; }
            public ConnectionRequest ConnectionRequest { get; set; }
        }
    }
}