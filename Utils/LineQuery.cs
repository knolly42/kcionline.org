using System;
using System.Collections.Generic;
using System.Linq;
using Quartz.Util;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.kcionline.bricksandmortarstudio.Utils
{
    public static class LineQuery
    {
        public static IQueryable<Person> GetLineCoordinatorsAndLeaders()
        {
            return GetLineCoordinatorsAndLeaders( new RockContext() );
        }

        public static IQueryable<Person> GetLineCoordinatorsAndLeaders( RockContext rockContext )
        {

            var cellGroupType = GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            var consolidatorCoordinatorGuid = SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            //return new GroupMemberService( rockContext ).Queryable().Where( gm => gm.Group.GroupTypeId == cellGroupType.Id && ( gm.GroupRole.IsLeader || gm.GroupRole.Guid == consolidatorCoordinatorGuid ) ).Select( gm => gm.Person ).Distinct();
            var linecoordinatorsandleaders = new GroupMemberService(rockContext).Queryable().Where(gm => gm.Group.GroupTypeId == cellGroupType.Id && (gm.GroupRole.IsLeader || gm.GroupRole.Guid == consolidatorCoordinatorGuid)).Select(gm => gm.Person);
            return linecoordinatorsandleaders.Distinct();
        }

        public static IQueryable<GroupMember> GetGroupMemberInLine( Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( currentPerson == null )
            {
                return new List<GroupMember>().AsQueryable();
            }

           // if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
           // {
           //     return new GroupMemberService( rockContext ).Queryable( "Group, Group.GroupType" ).Where( a => a.Group.GroupType.Guid == SystemGuid.GroupType.CELL_GROUP.AsGuid() );
           // }
            else
            {
                //     var cellGroupsIdsInLine = GetCellGroupIdsInLine( currentPerson, rockContext );

                // Optionally get groups for Leader only or Leader ANd Coordinator
                var cellGroupsIdsInLine = showAllIfStaff ? GetCellGroupIdsInLine(currentPerson, rockContext) : GetLeaderOnlyCellGroupIdsInLine(currentPerson, rockContext);

                var groupMemberInLine =
                    new GroupMemberService( rockContext ).Queryable()
                                      .Where( gm => cellGroupsIdsInLine.Contains( gm.GroupId ) );
                return groupMemberInLine;
            }
        }

        public static IQueryable<Person> GetPeopleInLine( PersonService personService, Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( currentPerson == null )
            {
                return new List<Person>().AsQueryable();
            }

           // if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
           // {
           //     return personService.Queryable();
           // }
            else
            {
                var cellGroupsIdsInLine = GetCellGroupIdsInLine( currentPerson, rockContext );
                var recordStatusIsActiveGuid = Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid();
                var peopleInLine =
                    new GroupMemberService( rockContext ).Queryable()
                                      .Where( gm => cellGroupsIdsInLine.Contains( gm.GroupId ) && gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid )
                                      .Select( gm => gm.Person );
                return peopleInLine.Distinct();
            }
        }

        /// <summary>
        /// Gets the follow ups for the line below a leader or coordinator
        /// </summary>
        /// <param name="personService"></param>
        /// <param name="currentPerson"></param>
        /// <param name="rockContext"></param>
        /// <param name="showAllIfStaff">If a staff member, should they see all people</param>
        /// <returns></returns>
        public static IQueryable<Person> GetPeopleInLineFollowUps( PersonService personService, Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( currentPerson == null )
            {
                return new List<Person>().AsQueryable();
            }

           // if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
           // {
           //     return personService.Queryable();
           // }
            var cellGroupsIdsInLine = GetCellGroupIdsInLine( currentPerson, rockContext );
            var recordStatusIsActiveGuid = Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid();
            var groupMemberService = new GroupMemberService( rockContext );

            // Get person Ids from line
            var linePersonIds =
                groupMemberService.Queryable()
                                  .Where( gm => cellGroupsIdsInLine.Contains( gm.GroupId ) && gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid )
                                  .Select( gm => gm.PersonId ).ToList();

            // Get people's follow ups
            int consolidatorGroupTypeRoleId = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATOR.AsGuid() ).Id;
            var followUpIds = new List<int>();
            foreach ( int personId in linePersonIds )
            {
                followUpIds.AddRange( groupMemberService.GetKnownRelationship( personId, consolidatorGroupTypeRoleId ).Where( gm => gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid ).Select( gm => gm.PersonId ) );
            }

            //Remove people who are in a group as a coordinator or leader
            var cellGroupType = GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            var consolidatorCoordinatorGuid = SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            var idsToRemove =
                groupMemberService.Queryable()
                                  .Where(
                                      gm =>
                                          followUpIds.Any(fId => gm.PersonId == fId) &&
                                          gm.Group.GroupTypeId == cellGroupType.Id &&
                                          (gm.GroupRole.Guid == consolidatorCoordinatorGuid || gm.GroupRole.IsLeader))
                                  .Select(gm => gm.PersonId)
                                  .ToList();
            foreach (int idToRemove in idsToRemove )
            {
                followUpIds.Remove(idToRemove);
            }
            return personService.GetByIds( followUpIds ).Distinct();
        }

        /// <summary>
        /// Gets the line members *and* their follow ups of the line below a leader or coordinator
        /// </summary>
        /// <param name="personService"></param>
        /// <param name="currentPerson"></param>
        /// <param name="rockContext"></param>
        /// <param name="showAllIfStaff"></param>
        /// <returns></returns>
        public static IQueryable<Person> GetLineMembersAndFollowUps(PersonService personService, Person currentPerson, RockContext rockContext, bool showAllIfStaff)
        {

            if (currentPerson == null)
            {
                return new List<Person>().AsQueryable();
            }
           
            // Optionally get groups for Leader only or Leader ANd Coordinator
            var cellGroupsIdsInLine = showAllIfStaff ? GetCellGroupIdsInLine(currentPerson, rockContext) : GetLeaderOnlyCellGroupIdsInLine(currentPerson, rockContext);
            
            var recordStatusIsActiveGuid = Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid();
            var groupMemberService = new GroupMemberService(rockContext);

            // Get person Ids from line
            var linePersonIds =
                groupMemberService.Queryable()
                                  .Where(
                                      gm =>
                                          cellGroupsIdsInLine.Contains(gm.GroupId) &&
                                          gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid)
                                  .Select(gm => gm.PersonId).ToList();

            // Get people's follow ups
            int consolidatorGroupTypeRoleId =
                new GroupTypeRoleService(rockContext).Get(SystemGuid.GroupTypeRole.CONSOLIDATOR.AsGuid()).Id;
            var lineAndFollowUpIds = new List<int>();
            foreach (int personId in linePersonIds)
            {
                lineAndFollowUpIds.AddRange(
                    groupMemberService.GetKnownRelationship(personId, consolidatorGroupTypeRoleId)
                                      .Where(gm => gm.Person.RecordStatusValue.Guid == recordStatusIsActiveGuid)
                                      .Select(gm => gm.PersonId));
            }
                
            lineAndFollowUpIds.AddRange(linePersonIds);
                
            return personService.GetByIds( lineAndFollowUpIds ).Distinct();
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

        /// <summary>
        /// Get's the cell groups in a person's line
        /// </summary>
        /// <param name="currentPerson"></param>
        /// <param name="rockContext"></param>
        /// <param name="showAllIfStaff"></param>
        /// <returns></returns>
        public static IQueryable<Group> GetCellGroupsInLine( Person currentPerson, RockContext rockContext, bool showAllIfStaff )
        {
            if ( currentPerson == null )
            {
                return new List<Group>().AsQueryable();
            }

            //if ( showAllIfStaff && CheckIsStaff( currentPerson, rockContext ) )
            //{
            //    return new GroupService( rockContext ).Queryable();
            // }
            // Optionally get groups for Leader only or Leader ANd Coordinator
            
            if (showAllIfStaff)
            {
                return new GroupService( rockContext ).GetByIds( GetCellGroupIdsInLine( currentPerson, rockContext ).ToList() );
            }
            else
            {
                return new GroupService(rockContext).GetByIds(GetLeaderOnlyCellGroupIdsInLine(currentPerson, rockContext).ToList());
            }
            //return new GroupService( rockContext ).GetByIds( GetCellGroupIdsInLine( currentPerson, rockContext ).ToList() );

        }

        /// <summary>
        /// Gets the cell groups of only the leader, excluding the Coordinator
        /// </summary>
        /// <param name="currentPerson"></param>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        public static IEnumerable<int> GetLeaderOnlyCellGroupIdsInLine(Person currentPerson, RockContext rockContext)
        {
            if (currentPerson == null)
            {
                return new List<int>();
            }

            var groupMemberService = new GroupMemberService(rockContext);
            var cellGroupType = GroupTypeCache.Read(SystemGuid.GroupType.CELL_GROUP.AsGuid());
            IQueryable<GroupMember> currentPersonsCellGroups = null;

            var consolidatorCoordinatorGuid = SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            if (cellGroupType != null)
            {
                currentPersonsCellGroups = groupMemberService
                                      .GetByPersonId(currentPerson.Id)
                                                      .Where(gm => gm.Group.GroupTypeId == cellGroupType.Id && (gm.GroupRole.IsLeader && gm.GroupRole.Guid != consolidatorCoordinatorGuid)).Distinct();
            }

            if (currentPersonsCellGroups == null || !currentPersonsCellGroups.Any())
            {
                return new List<int>();
            }

            var descendentGroups = new List<int>();

            var groupService = new GroupService(rockContext);
            foreach (var groupMember in currentPersonsCellGroups)
            {
                descendentGroups.Add(groupMember.GroupId);
                descendentGroups.AddRange(groupService.GetAllDescendents(groupMember.GroupId)
                                                      .Where(g => g.GroupTypeId == cellGroupType.Id)
                                                      .Select(g => g.Id));
            }

            return descendentGroups.Distinct();
        }


        public static IEnumerable<int> GetCellGroupIdsInLine( Person currentPerson, RockContext rockContext )
        {
            if ( currentPerson == null )
            {
                return new List<int>();
            }

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
                return new List<int>();
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

        /// <summary>
        /// Get's all of the (get connected/ follow up) *requests* for a given person
        /// </summary>
        /// <param name="currentPerson">The leader</param>
        /// <returns></returns>
        public static IQueryable<ConnectionRequest> GetPeopleInLineFollowUpRequests( Person currentPerson )
        {
            if ( currentPerson == null )
            {
                return new List<ConnectionRequest>().AsQueryable();
            }

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

        /// <summary>
        /// Determines whether a given group is overseen by this person either as a leader or as a coordinator
        /// </summary>
        /// <param name="group"></param>
        /// <param name="currentPerson"></param>
        /// <returns></returns>
        public static bool IsGroupInPersonsLine( Group group, Person currentPerson )
        {
            return GetCellGroupsInLine( currentPerson, new RockContext(), false ).ToList().Any( g => g.Id == group.Id );
        }

        /// <summary>
        /// Determines whether a person is in a given leader or coordinator's line
        /// </summary>
        /// <param name="person">The person to check if is in the leader's line</param>
        /// <param name="leader">The leader or coordinator</param>
        /// <returns></returns>
        public static bool IsPersonInLeadersLine(Person person, Person leader)
        {
            var rockContext = new RockContext();
            if (person.PrimaryAliasId.HasValue)
            {
                return GetPeopleInLine( new PersonService( rockContext ), leader, rockContext, true ).Any( p => p.PrimaryAliasId == person.PrimaryAliasId );
            }
            return GetPeopleInLine(new PersonService(rockContext), leader, rockContext, true).Any(p => person.Id == p.Id);
        }

        /// <summary>
        /// Determines whether a person is in a given leader or coordinator's line
        /// </summary>
        /// <param name="personId">The person id to check</param>
        /// <param name="leader">The leader or coordinator</param>
        /// <returns></returns>
        public static bool IsPersonInLeadersLine( int personId, Person leader )
        {
            var rockContext = new RockContext();
            var personService = new PersonService(rockContext);
            var person = personService.Get(personId);
            return GetPeopleInLine( personService, leader, rockContext, true ).Any( p => person.Id == p.Id );
        }

        public static Group FindLineLeader (Person currentPerson)
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService(rockContext);
            var kciSmallGroupType = SystemGuid.GroupType.CELL_GROUP.AsGuid();

            // Attempt to find a group with them in
            var currentGroup =
                groupMemberService.Queryable()
                                  .Where(
                                      gm =>
                                          gm.PersonId == currentPerson.Id &&
                                          gm.Group.GroupType.Guid == kciSmallGroupType && !gm.GroupRole.IsLeader).Select(gm => gm.Group).FirstOrDefault();



            // If only top group found they are the line leader, so we'll grab the top of their line
            if (currentGroup?.ParentGroup == null)
            {
                currentGroup =
                    groupMemberService.Queryable()
                                      .Where(
                                          gm =>
                                              gm.PersonId == currentPerson.Id && gm.GroupRole.IsLeader &&
                                              gm.Group.ParentGroupId == currentGroup.Id)
                                      .Select(gm => gm.Group)
                                      .FirstOrDefault();
            }

            if ( currentGroup == null )
            {
                return null;
            }

            // Keep going up the heirachy until we can't
            Group previousGroup = null;
            while (currentGroup.ParentGroup != null)
            {
                previousGroup = currentGroup;
                currentGroup = previousGroup.ParentGroup;
            }
            return previousGroup;
        } 
    }
}
