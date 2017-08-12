// <copyright>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using org.kcionline.bricksandmortarstudio.Utils;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace org_kcionline.Groups
{
    [DisplayName( "Cell Group Line List" )]
    [Category( "Bricks and Mortar Studio" )]
    [Description( "Lists all group that the person is a member of or is responsible for using a Lava template." )]
    [LinkedPage( "Detail Page", "", false )]
    [CodeEditorField( "Lava Template", "The lava template to use to format the group list.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, "{% include '~~/Assets/Lava/GroupListSidebar.lava' %}", "", 1 )]
    [BooleanField( "Enable Debug", "Shows the fields available to merge in lava.", false, "", 2 )]
    public partial class GroupListPersonalizedLava : RockBlock
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
                ListGroups();
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
                ListGroups();
            }
            else
            {
                // TODO
            }
        }

        #endregion

        #region Internal Methods

        private void ListGroups()
        {
            var rockContext = new RockContext();

            var cellGroupsInLine = LineQuery.GetCellGroupsInLine( CurrentPerson, rockContext, false ).ToList();
            var cellGroupdTypeGuid = org.kcionline.bricksandmortarstudio.SystemGuid.GroupType.CELL_GROUP.AsGuid();

            var cellMemberGroups =
                new GroupMemberService( rockContext).Queryable()
                                                   .Where(
                                                       gm =>
                                                           gm.PersonId == CurrentPersonId &&
                                                           gm.Group.GroupType.Guid == cellGroupdTypeGuid ).Select( gm => gm.Group ).ToList();

            var allCellGroups = cellGroupsInLine.Union( cellMemberGroups );

            var groups = new List<GroupInvolvementSummary>();

            int totalCount = 0;
            foreach ( var group in allCellGroups )
            {
                bool isLeader = group.Members.Any( p => p.GroupRole.IsLeader && p.Person == CurrentPerson );
                bool isMember = group.Members.Any( p => p.Person == CurrentPerson );
                totalCount += group.Members.Count;
                groups.Add( new GroupInvolvementSummary
                {
                    Group = group,
                    IsLeader = isLeader,
                    MemberCount = group.Members.Count,
                    IsMember = isMember
                } );
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Groups", groups );
            mergeFields.Add( "TotalCount", totalCount );
            var linkedPages = new Dictionary<string, object>();
            linkedPages.Add( "DetailPage", LinkedPageRoute( "DetailPage" ) );
            mergeFields.Add( "LinkedPages", linkedPages );

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

        [DotLiquid.LiquidType( "Group", "IsLeader", "MemberCount" )]
        public class GroupInvolvementSummary
        {
            public Group Group { get; set; }
            public bool IsLeader { get; set; }
            public int MemberCount { get; set; }

            public bool IsMember { get; set; }
        }

        #endregion
    }
}