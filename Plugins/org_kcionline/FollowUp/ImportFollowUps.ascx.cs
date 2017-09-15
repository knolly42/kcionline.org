
using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using org.kcionline.bricksandmortarstudio.Extensions;
using Quartz.Util;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org_kcionline.FollowUp
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Follow Up Import Tool" )]
    [Category( "Bricks and Mortar Studio" )]
    [Description( "Imports Follow Ups from a Person Attribute" )]
    [TextField("Attribute Key", "The key for the Person Attribute which contains the person attribute key", true, "FollowUponSponsor", key:"SponsorKey" )]
    public partial class FollowUpImport : Rock.Web.UI.RockBlock
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

        #endregion

        #region Methods

        // helper functional methods (like BindGrid(), etc.)

        private void Import()
        {
            string attributeKey = GetAttributeValue( "SponsorKey" );
            int personEntityTypeId = EntityTypeCache.Read( typeof( Person ) ).Id;
            var rockContext = new RockContext();
            var followUpSponsorAttribute = new AttributeService( rockContext )
                .Queryable()
                .AsNoTracking()
                .FirstOrDefault( a => a.Key == attributeKey && a.EntityTypeId == personEntityTypeId );

            if ( followUpSponsorAttribute == null )
            {
                nbInfo.NotificationBoxType = NotificationBoxType.Danger;
                nbInfo.Title = "Failure";
                nbInfo.Text = "Cannot fetch the Sponsor Person Attribute";
                return;
            }

            var attributeValueService = new AttributeValueService( rockContext );
            var personService = new PersonService(rockContext);
            var personAliasService = new PersonAliasService(rockContext);
            var peopleWithFollowUpSponsors = attributeValueService.GetByAttributeId( followUpSponsorAttribute.Id ).Where( av => av.EntityId != null ).Select( av => new { PersonId = av.EntityId, SponsorAliasGuid = av.Guid } );
            foreach (var pair in peopleWithFollowUpSponsors )
            {
                var followUp = personService.Get(pair.PersonId.Value);
                var sponsor = personAliasService.GetPerson(pair.SponsorAliasGuid);
                if (!followUp.HasConsolidator())
                {
                    followUp.SetConsolidator(sponsor);
                }
            }
            lbImport.Enabled = false;
            lbImport.Text = "Done";
        }

        #endregion
        

        protected void lbImport_OnClick(object sender, EventArgs e)
        {
            if (GetAttributeValue("SponsorKey").IsNullOrWhiteSpace())
            {
                nbInfo.NotificationBoxType = NotificationBoxType.Danger;
                nbInfo.Title = "Failure";
                nbInfo.Text = "No Import Attribute Key Set";
                return;
            }
            Import();
        }
    }
}