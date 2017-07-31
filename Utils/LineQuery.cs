using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.kcionline.bricksandmortarstudio.Utils
{
    public static class LineQuery
    {
        public static IQueryable<Person> GetPeopleInLine( Person currentPerson )
        {
            var rockContext = new RockContext();
            return GetPeopleInLine( new PersonService( rockContext ), currentPerson, rockContext );
        }

        public static IQueryable<Person> GetPeopleInLine( PersonService personService, Person currentPerson )
        {
            return GetPeopleInLine( personService, currentPerson, new RockContext() );
        }

        public static IQueryable<Person> GetPeopleInLine( PersonService personService, Person currentPerson, RockContext rockContext )
        {
            bool isStaff = false;
            var staffTeamGuid = Rock.SystemGuid.Group.GROUP_STAFF_MEMBERS.AsGuidOrNull();
            if ( staffTeamGuid != null )
            {
                var staffTeam = new GroupService( rockContext ).Get( staffTeamGuid.Value );
                if ( staffTeam != null )
                {
                    var adminPersonIds = staffTeam.Members.Select( m => m.PersonId ).ToList();
                    if ( adminPersonIds.Contains( currentPerson.Id ) )
                    {
                        isStaff = true;
                    }
                }
            }

            var staffLikeTeamGuid = Rock.SystemGuid.Group.GROUP_STAFF_LIKE_MEMBERS.AsGuidOrNull();
            if ( staffLikeTeamGuid != null )
            {
                var staffLikeTeam = new GroupService( rockContext ).Get( staffLikeTeamGuid.Value );
                if ( staffLikeTeam != null )
                {
                    var adminPersonIds = staffLikeTeam.Members.Select( m => m.PersonId ).ToList();
                    if ( adminPersonIds.Contains( currentPerson.Id ) )
                    {
                        isStaff = true;
                    }
                }
            }

            if ( isStaff )
            {
                return personService.Queryable();
            }
            else
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groupType = GroupTypeCache.Read( SystemGuid.Group.CELL_GROUP.AsGuid() );
                Group currentPersonsCellGroup = null;

                if ( groupType != null )
                {
                    currentPersonsCellGroup = groupMemberService.GetByPersonId( currentPerson.Id )
                                          .FirstOrDefault( gm => gm.Group.GroupTypeId == groupType.Id && gm.GroupRole.IsLeader )?.Group;
                }

                if ( currentPersonsCellGroup == null )
                {
                    return null;
                }

                var rootGroupId = currentPersonsCellGroup.ParentGroupId.HasValue ? currentPersonsCellGroup.ParentGroupId.Value : currentPersonsCellGroup.Id;

                var descendentGroups =
                    new GroupService( rockContext ).GetAllDescendents( rootGroupId )
                                                 .Where( g => g.GroupTypeId == groupType.Id )
                                                 .Select( g => g.Id );
                var peopleInLine =
                    groupMemberService.Queryable()
                                      .Where( gm => descendentGroups.Contains( gm.GroupId ) )
                                      .Select( gm => gm.Person );
                return peopleInLine;
            }
        }

        public static IQueryable<ConnectionRequest> GetPeopleInLineFollowUpRequests( Person currentPerson )
        {
            var rockContext = new RockContext();

            var connectionRequestService = new ConnectionRequestService( rockContext );
            int getConnectedOpportunityId =
                new ConnectionOpportunityService( rockContext ).Get(
                    SystemGuid.ConnectionOpportunity.GET_CONNECTED.AsGuid() ).Id;
            var connectionRequests = connectionRequestService.Queryable()
                                    .AsNoTracking()
                                    .Where( c => c.ConnectionOpportunityId == getConnectedOpportunityId && c.ConnectorPersonAliasId == currentPerson.PrimaryAliasId && c.ConnectionState == ConnectionState.Active );

            return connectionRequests;
        }
    }
}
