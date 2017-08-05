// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rock.Data;
using Rock.Model;
using org.kcionline.bricksandmortarstudio.Utils;
using OfficeOpenXml.FormulaParsing.Exceptions;
using Rock;
using Rock.Attribute;
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

            if ( !Page.IsPostBack )
            {
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

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            var connectionRequests = LineQuery.GetPeopleInLineFollowUpRequests(CurrentPerson);

            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var groupTypeRoleService = new GroupTypeRoleService(rockContext);
            var groupTypeRole = groupTypeRoleService.Get(org.kcionline.bricksandmortarstudio.SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid());

            var followUps = new List<FollowUp>(connectionRequests.Count());
            foreach (var request in connectionRequests)
            {
                var firstOrDefault = groupMemberService.GetKnownRelationship(request.PersonAlias.PersonId, groupTypeRole.Id).FirstOrDefault();
                if (firstOrDefault != null)
                {
                    var consolidator = firstOrDefault.Person;
                    followUps.Add(new FollowUp(request, consolidator));
                }
            }

            gList.DataSource = followUps;
            gList.DataBind();
        }
        #endregion

        protected void gList_OnRowSelected(object sender, RowEventArgs e)
        {
            NavigateToLinkedPage( "DetailPage", "ConnectionRequestId", e.RowKeyValues["Id"].ToString().AsInteger() );
        }
    }

    internal class FollowUp
    {
        public FollowUp (ConnectionRequest connectionRequest, Person consolidator)
        {
            ConnectionRequest = connectionRequest;
            Consolidator = consolidator;
            Id = connectionRequest.Id;
        }
        public int Id { get; set; }
        public Person Consolidator { get; set; }
        public ConnectionRequest ConnectionRequest {get; set;}
    }
}