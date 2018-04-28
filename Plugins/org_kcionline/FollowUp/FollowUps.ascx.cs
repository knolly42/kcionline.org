
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using org.kcionline.bricksandmortarstudio.Extensions;
using org.kcionline.bricksandmortarstudio.Utils;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using View = DocumentFormat.OpenXml.Wordprocessing.View;

namespace org_kcionline.FollowUp
{
    [DisplayName("Follow Up List")]
    [Category("Bricks and Mortar Studio")]
    [Description("Lists a person's follow ups or their line's follow ups using a Lava template.")]
    [CodeEditorField("Lava Template", "The lava template to use to format the group list.", CodeEditorMode.Lava,
         CodeEditorTheme.Rock, 400, true, "{% include '~~/Assets/Lava/FollowUps.lava' %}", "", 1)]
    [BooleanField("Enable Debug", "Shows the fields available to merge in lava.", false, "", 2)]
    public partial class FollowUps : RockBlock
    {
        private static readonly Dictionary<ViewOption, string> ViewOptionText = new Dictionary<ViewOption, string>
        {
            {ViewOption.All, "All People"},
            {ViewOption.New, "New to Church" },
            {ViewOption.InGroup, "In a Group" },
            {ViewOption.NotInGroup, "Not in a Group" },
            {ViewOption.Ongoing, "Ongoing" },
            {ViewOption.Ready, "Ready for Group" }
        };

        public bool PersonPickerIsLinePicker = false;

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            this.BlockUpdated += Block_BlockUpdated;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                SetupChoices();
                Route();
                SetppFilterLabel();
                if (CurrentPerson.HasALine())
                {
                    tViewLineType.Checked = false;
                }
                GetFollowUps();
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated(object sender, EventArgs e)
        {
            if (CurrentPerson != null)
            {
                GetFollowUps();
            }
        }

        #endregion

        #region Internal Methods

        private void SetupChoices()
        {
            var listItems = new List<ListItem>();
            foreach (var option in Enum.GetValues(typeof(ViewOption)).Cast<ViewOption>())
            {
                listItems.Add(new ListItem(ViewOptionText[option], option.ToString()));
            }
            ddlChoices.Items.AddRange(listItems.ToArray());
        }

        private void Route()
        {
            if (!CurrentPerson.HasALine())
            {
                return;
            }
            if (Request.QueryString["view"] != null)
            {
                ViewOption option;
                bool success = Enum.TryParse( Request.QueryString["view"], out option );
                if (success)
                {
                    //ddlChoices.SetValue( ViewOptionText[option] ); ToInt32(option) fixes ?view=number being ignored 
                    ddlChoices.SelectedIndex = Convert.ToInt32(option);
                }
            }
        }

        private ViewOption GetOption()
        {
            return ( ViewOption ) Enum.Parse( typeof( ViewOption ), ddlChoices.SelectedValue );
        }

        private void GetFollowUps()
        {
            if (!CurrentPerson.HasALine())
            {
                leaderControlRow.Visible = false;
            }

            var rockContext = new RockContext();
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields(this.RockPage, this.CurrentPerson);

            IQueryable<Person> people;
            bool isMyLine = tViewLineType.Checked;
            if (isMyLine)
            {
                people = CurrentPerson.GetPersonsLine(rockContext);
            }
            else // My Line's Follow Ups
            {
                people = LineQuery.GetLineMembersAndFollowUps( new PersonService(rockContext), CurrentPerson,
                    rockContext, false);
            }

            var option = GetOption();
            switch (option)
            {
                case ViewOption.All:
                    break;
                case ViewOption.New:
                    people = people.Where(
                        f => f.ConnectionStatusValueId == org.kcionline.bricksandmortarstudio.Constants.DefinedValue.GET_CONNECTED);
                    break;
                case ViewOption.Ready:
                    break;
                case ViewOption.InGroup:
                    people = people.ToList().Where(f => f.InAGroup(rockContext)).AsQueryable();
                    break;
                case ViewOption.NotInGroup:
                    people = people.ToList().Where( f => !f.InAGroup( rockContext ) ).AsQueryable();
                    break;
                case ViewOption.Ongoing:
                    people = people.Where(
                        f => f.ConnectionStatusValueId == org.kcionline.bricksandmortarstudio.Constants.DefinedValue.CONNECTED );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            IQueryable<FollowUpSummary> followUpSummaries;
            if (isMyLine)
            {

                followUpSummaries =
                    people.ToList().Select( f => new FollowUpSummary() { Person = f, Consolidator = CurrentPerson, Group = CurrentPerson.GetPersonsPrimaryKciGroup(rockContext)} )
                             .AsQueryable();
            }
            else
            {
                followUpSummaries =
                    people.ToList().Select( f => new FollowUpSummary() { Person = f, Consolidator = f.GetConsolidator(), Group = f.GetPersonsPrimaryKciGroup(rockContext)} )
                             .AsQueryable();
            }

            if (ppFilter.SelectedValue.HasValue)
            {
                if (option.IsGuaranteedToHaveConsolidator())
                {
                    followUpSummaries = followUpSummaries.Where(f => f.Consolidator != null && f.Consolidator.Id == ppFilter.SelectedValue);

                    followUpSummaries = followUpSummaries
                      .OrderByDescending( f => f.Person.ConnectionStatusValue.Value == "GetConnected" )
                      .ThenBy( f => f.Consolidator.Id );
                }
           // Will do the group leader filtering in lava instead for now
           //     else
           //     {
                    // Defer this work in order to avoid hammering queries unless we need to
           //         foreach (var followUpSummary in followUpSummaries)
           //         {
           //             followUpSummary.Group = followUpSummary.Person.GetPersonsPrimaryKciGroup(rockContext);
           //         }
           //         var person = new PersonService(rockContext).Get(ppFilter.SelectedValue.Value);
           //         var groups = person.GetGroupsLead(rockContext);
           //         followUpSummaries = followUpSummaries
           //             .Where(f => f.Group != null && groups.Select(g => g.Id).Contains(f.Group.Id));

           //         followUpSummaries = followUpSummaries
           //             .OrderByDescending( f => f.Group.Id)
           //             .ThenBy( f => f.Consolidator.Id );
           //      }
           }
           //         if (option == ViewOption.All)
           
           // Sort all by LastName
                    {
                        followUpSummaries = followUpSummaries
                            .OrderBy(p => p.Person.LastName);
                    }
           //         else
           //         {
           //             followUpSummaries = followUpSummaries
           //                 .OrderByDescending( f => f.Group.Id )
           //                 .ThenBy( f => f.Consolidator.Id );
           //         }

            mergeFields["FollowUps"] = followUpSummaries.ToList();
            mergeFields["MyLine"] = isMyLine;
            mergeFields["View"] = ViewOptionText[option];
           // add the leader filter for lava template
            mergeFields["LeaderFilter"] = ppFilter.SelectedValue;

            string template = GetAttributeValue("LavaTemplate");

            // show debug info
            bool enableDebug = GetAttributeValue("EnableDebug").AsBoolean();
            if (enableDebug && IsUserAuthorized(Authorization.EDIT))
            {
                lDebug.Visible = true;
                lDebug.Text = mergeFields.lavaDebugInfo();
            }

            lContent.Text = template.ResolveMergeFields(mergeFields);
        }

        private void SetppFilterLabel()
        {
            var option = GetOption();
            ppFilter.Label = option.IsGuaranteedToHaveConsolidator() ? "Filter By Consolidator" : "Filter By Group Leader";
        }
// extend to include the group if applicable
        [DotLiquid.LiquidType("Person", "Consolidator", "Group")]
        public class FollowUpSummary
        {
            public Person Person { get; set; }
            public Person Consolidator { get; set; }

            public Group Group { get; set; }
        }

        #endregion

        protected void tViewLineType_OnCheckedChanged(object sender, EventArgs e)
        {
            GetFollowUps();
        }

        protected void ppFilter_OnSelectPerson(object sender, EventArgs e)
        {
            GetFollowUps();
        }

        protected void ddlChoices_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            SetppFilterLabel();
            GetFollowUps();
        }
    }

    internal enum ViewOption
    {
        All = 0,
        New = 1,
        Ready = 2,
        InGroup = 3,
        NotInGroup = 4,
        Ongoing = 5
    }

    static class ViewOptionExtensions
    {

        public static bool IsGuaranteedToHaveConsolidator( this ViewOption o )
        {
            switch ( o )
            {
                case ViewOption.All:
                    return false;
                case ViewOption.New:
                    return true;
                case ViewOption.Ready:
                    return true;
                case ViewOption.InGroup:
                    return false;
                case ViewOption.NotInGroup:
                    return false;
                case ViewOption.Ongoing:
                    return true;
                default:
                    return false;
            }
        }
    }
}
