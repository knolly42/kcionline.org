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
using System.ComponentModel;
using org.kcionline.bricksandmortarstudio.Extensions;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace org_kcionline.FollowUp
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Follow Up Mover" )]
    [Category( "Bricks and Mortar Studio" )]
    [Description( "Template block for developers to use to start a new detail block." )]
    public partial class FollowUpMover : Rock.Web.UI.RockBlock
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
                // If person provided, set and lock picker
                var personGuid = PageParameter("person").AsGuidOrNull();
                if (personGuid != null)
                {
                    var rockContext = new RockContext();
                    var personService = new PersonService(rockContext);
                    var person = personService.Get(personGuid.Value);
                    ppFollowUp.SetValue(person);
                    ppFollowUp.Enabled = false;
                    SetConsolidatorText(person);
                }
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

        protected void ppFollowUp_OnSelectPerson( object sender, EventArgs e )
        {
            if (!ppFollowUp.SelectedValue.HasValue)
            {
                return;
            }
            var person = new PersonService(new RockContext()).Get(ppFollowUp.SelectedValue.Value);
            SetConsolidatorText(person);
            lbMove.Enabled = ppNewConsolidator.SelectedValue.HasValue;
        }

        protected void ppNewConsolidator_OnSelectPerson( object sender, EventArgs e )
        {
            lbMove.Enabled = ppFollowUp.SelectedValue.HasValue;
        }

        #endregion

        #region Methods

        // helper functional methods (like BindGrid(), etc.)

        private void SetConsolidatorText( Person person )
        {
            var consolidator = person.GetConsolidator();
            lConsolidator.Text = consolidator != null ? person.FullName : "No Current Consolidator";
        }

        private void UpdateFollowUps()
        {
            if (!ppFollowUp.SelectedValue.HasValue || !ppNewConsolidator.SelectedValue.HasValue)
            {
                nbInfo.NotificationBoxType = NotificationBoxType.Danger;
                nbInfo.Text = "Either a follow up or a new consolidator hasn't been chosen";
                return;
            }
            var rockContext = new RockContext();
            var personService = new PersonService(rockContext);
            var followUp = personService.Get(ppFollowUp.SelectedValue.Value);
            var currentConsolidator = followUp.GetConsolidator(rockContext);
            var newConsolidator = personService.Get(ppNewConsolidator.SelectedValue.Value);

            if (newConsolidator == null)
            {
                nbInfo.NotificationBoxType = NotificationBoxType.Danger;
                nbInfo.Text = "Cannot find new consolidator";
            }

            if (currentConsolidator != null)
            {
                followUp.RemoveConsolidator(currentConsolidator);
            }

            followUp.SetConsolidator(newConsolidator);
        }

        #endregion

        protected void lbMove_OnClick(object sender, EventArgs e)
        {
            UpdateFollowUps();
            NavigateToParentPage();
        }
    }
}