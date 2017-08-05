using System;
using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.kcionline.bricksandmortarstudio.Utils
{
    public static class LineQuery
    {
        public static IQueryable<GroupMember> GetGroupMemberInLine( Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
            {
                return new GroupMemberService( rockContext ).Queryable( "Group, Group.GroupType" ).Where( a => a.Group.GroupType.Guid == SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            }
            else
            {
                var cellGroupsIdsInLine = GetCellGroupIdsInLine( currentPerson, rockContext );

                var groupMemberInLine =
                    new GroupMemberService( rockContext ).Queryable()
                                      .Where( gm => cellGroupsIdsInLine.Contains( gm.GroupId ) );
                return groupMemberInLine;
            }
        }

        public static IQueryable<Person> GetPeopleInLine( PersonService personService, Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
            {
                return personService.Queryable();
            }
            else
            {
                var cellGroupsIdsInLine = GetCellGroupIdsInLine( currentPerson, rockContext );
                var recordStatusIsActiveGuid = Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid();
                var peopleInLine =
                    new GroupMemberService( rockContext ).Queryable()
                                      .Where( gm => cellGroupsIdsInLine.Contains( gm.GroupId ) && gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid )
                                      .Select( gm => gm.Person );
                return peopleInLine;
            }
        }

        public static IQueryable<Person> GetPeopleInLineAndTheirFollowUps( PersonService personService, Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
            {
                return personService.Queryable();
            }
            else
            {
                var cellGroupsIdsInLine = GetCellGroupIdsInLine( currentPerson, rockContext );
                var recordStatusIsActiveGuid = Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid();
                var groupMemberService = new GroupMemberService( rockContext );
                var peopleInLine =
                    groupMemberService.Queryable()
                                      .Where( gm => cellGroupsIdsInLine.Contains( gm.GroupId ) && gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid )
                                      .Select( gm => gm.Person ).ToList();

                var people = new List<Person>();

                // Get people's follow ups
                int consolidatedByGroupTypeRoleId = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid() ).Id;
                foreach ( var person in peopleInLine )
                {
                    people.AddRange( groupMemberService.GetKnownRelationship( person.Id, consolidatedByGroupTypeRoleId ).Where( gm => gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid ).Select( p => p.Person ) );
                }

                people.AddRange( peopleInLine );
                return people.AsQueryable();
            }
        }

        private static bool CheckIsStaff( Person currentPerson, RockContext rockContext )
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
            return isStaff;
        }

        public static IQueryable<Group> GetCellGroupsInLine( Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
            {
                return new GroupService( rockContext ).Queryable();
            }

            return new GroupService( rockContext ).GetByIds( GetCellGroupIdsInLine( currentPerson, rockContext ).ToList() );
        }

        private static IEnumerable<int> GetCellGroupIdsInLine( Person currentPerson, RockContext rockContext )
        {

            var groupMemberService = new GroupMemberService( rockContext );
            var cellGroupType = GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            IQueryable<GroupMember> currentPersonsCellGroups = null;

            var consolidatorCoordinatorGuid = SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            if ( cellGroupType != null )
            {
                currentPersonsCellGroups = groupMemberService
                                      .GetByPersonId( currentPerson.Id )
                                                      .Where( gm => gm.Group.GroupTypeId == cellGroupType.Id && ( gm.GroupRole.IsLeader || gm.GroupRole.Guid == consolidatorCoordinatorGuid ) ).Distinct();
            }

            if ( currentPersonsCellGroups == null || !currentPersonsCellGroups.Any() )
            {
                return null;
            }

            var descendentGroups = new List<int>();

            var groupService = new GroupService( rockContext );
            foreach ( var groupMember in currentPersonsCellGroups )
            {
                descendentGroups.Add( groupMember.GroupId );
                descendentGroups.AddRange( groupService.GetAllDescendents( groupMember.GroupId )
                                                      .Where( g => g.GroupTypeId == cellGroupType.Id )
                                                      .Select( g => g.Id ) );
            }

            return descendentGroups.Distinct();
        }

        public static IQueryable<ConnectionRequest> GetPeopleInLineFollowUpRequests( Person currentPerson )
        {
            var rockContext = new RockContext();

            var connectionRequestService = new ConnectionRequestService( rockContext );
            int getConnectedOpportunityId =
                new ConnectionOpportunityService( rockContext ).Get(
                    SystemGuid.ConnectionOpportunity.GET_CONNECTED.AsGuid() ).Id;
            var connectionRequests = connectionRequestService
                                    .Queryable()
                                    .Where( c => c.ConnectionOpportunityId == getConnectedOpportunityId && c.ConnectorPersonAliasId == currentPerson.PrimaryAliasId && c.ConnectionState == ConnectionState.Active );

            return connectionRequests;
        }
    }
}
