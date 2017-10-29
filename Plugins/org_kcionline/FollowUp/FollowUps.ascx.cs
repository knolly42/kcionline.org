
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using org.kcionline.bricksandmortarstudio.Extensions;
using org.kcionline.bricksandmortarstudio.Utils;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace org_kcionline.FollowUp
{
    [DisplayName( "Follow Up List" )]
    [Category( "Bricks and Mortar Studio" )]
    [Description( "Lists a person's follow ups or their line's follow ups using a Lava template." )]
    [CodeEditorField( "Lava Template", "The lava template to use to format the group list.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, "{% include '~~/Assets/Lava/FollowUps.lava' %}", "", 1 )]
    [BooleanField( "Enable Debug", "Shows the fields available to merge in lava.", false, "", 2 )]
    public partial class FollowUps : RockBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                if (!CurrentPerson.HasALine())
                {
                    tViewLineType.Visible = false;
                }
                GetFollowUps();
            }

            base.OnLoad( e );
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            if ( CurrentPerson != null )
            {
                GetFollowUps();
            }
            else
            {
                // TODO
            }
        }

        #endregion

        #region Internal Methods

        private void GetFollowUps()
        {
            var rockContext = new RockContext();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            IQueryable<FollowUpSummary> followUpSummaries;

            // My Line
            if (tViewLineType.Checked)
            {
                var followUps = CurrentPerson.GetFollowUps();
                followUpSummaries =
                    followUps.Select(f => new FollowUpSummary() {Person = f, Consolidator = CurrentPerson});
            }
            else // My Line's Follow Ups
            {
                var followUps = LineQuery.GetPeopleInLineFollowUps(new PersonService(rockContext), CurrentPerson,
                    rockContext, false);
                followUpSummaries =
                    followUps.Select(f => new FollowUpSummary() {Person = f, Consolidator = f.GetConsolidator()});
            }

            followUpSummaries = followUpSummaries
                .OrderBy(f => f.Person.ConnectionStatusValue.Value == "GetConnected")
                .ThenBy(f => f.Consolidator.Id);

            mergeFields["FollowUps"] = followUpSummaries.ToList();

            string template = GetAttributeValue( "LavaTemplate" );

            // show debug info
            bool enableDebug = GetAttributeValue( "EnableDebug" ).AsBoolean();
            if ( enableDebug && IsUserAuthorized( Authorization.EDIT ) )
            {
                lDebug.Visible = true;
                lDebug.Text = mergeFields.lavaDebugInfo();
            }

            lContent.Text = template.ResolveMergeFields( mergeFields );
        }

        [DotLiquid.LiquidType( "Person", "Coordinator" )]
        public class FollowUpSummary
        {
            public Person Person { get; set; }
            public Person Consolidator { get; set; }
        }

        #endregion
    }
}