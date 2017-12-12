using System;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using org.kcionline.bricksandmortarstudio.Utils;
using Rock.Web.Cache;

namespace org.kcionline.bricksandmortarstudio.Extensions
{
    public static class PersonExtensions
    {

        public static bool HasALine(this Person person)
        {
            return person.HasALine(new RockContext());
        }

        /// <summary>
        /// Check if the person is a leader of a KCI small group or oversees a KCI line as a coordinator
        /// </summary>
        /// <param name="person"></param>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        public static bool HasALine(this Person person, RockContext rockContext)
        {
            var cellGroupType = Rock.Web.Cache.GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            var consolidatorCoordinatorGuid = SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            return new GroupMemberService( rockContext ).Queryable().Any( gm => gm.Group.GroupTypeId == cellGroupType.Id && ( gm.GroupRole.IsLeader || gm.GroupRole.Guid == consolidatorCoordinatorGuid ) && gm.PersonId == person.Id );
        }

        public static bool InAGroup(this Person person, RockContext rockContext )
        {
            var cellGroupType = GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            return new GroupMemberService( rockContext ).Queryable().Any( gm => gm.Group.GroupTypeId == cellGroupType.Id);
        }


        /// <summary>
        /// Returns a person's consolidator if they have one 
        /// </summary>
        /// <param name="followUp"></param>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        public static Person GetConsolidator( this Person followUp, RockContext rockContext )
        {
            var groupMemberService = new GroupMemberService( rockContext );
            var consolidatedBy = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid() );
            return groupMemberService.GetKnownRelationship( followUp.Id, consolidatedBy.Id ).FirstOrDefault()?.Person;
        }

        /// <summary>
        /// Gets a persons consolidator if they have one
        /// </summary>
        /// <param name="followUp"></param>
        /// <returns></returns>
        public static Person GetConsolidator( this Person followUp )
        {
            return GetConsolidator( followUp, new RockContext() );
        }

        /// <summary>
        /// Gets a persons followups
        /// </summary>
        /// <param name="consolidator"></param>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        public static IQueryable<Person> GetFollowUps( this Person consolidator, RockContext rockContext )
        {
            var groupMemberService = new GroupMemberService( rockContext );
            var consolidatedBy = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATOR.AsGuid() );
            if ( consolidatedBy != null )
            {
                return groupMemberService.GetKnownRelationship( consolidator.Id, consolidatedBy.Id ).Select( gm => gm.Person );
            }
            return null;
        }

        /// <summary>
        /// Gets a person's followups
        /// </summary>
        /// <param name="consolidator"></param>
        /// <returns></returns>
        public static IQueryable<Person> GetFollowUps( this Person consolidator )
        {
            return GetFollowUps( consolidator, new RockContext() );
        }

        /// <summary>
        /// Gets the people that are group members in a group where the person is a leader
        /// </summary>
        public static IQueryable<Person> GetPeopleLeadInKCIGroups(this Person leader, RockContext rockContext)
        {
            var groupMemberService = new GroupMemberService( rockContext );
            var cellGroupType = GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            IQueryable<GroupMember> currentPersonsCellGroups = null;

            var consolidatorCoordinatorGuid = SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            if (cellGroupType == null)
            {
                throw new Exception("Cannot locate KCI Group type");
            }
            currentPersonsCellGroups = groupMemberService
                .GetByPersonId( leader.Id )
                .Where( gm => gm.Group.GroupTypeId == cellGroupType.Id && ( gm.GroupRole.IsLeader || gm.GroupRole.Guid == consolidatorCoordinatorGuid ) ).Distinct();
            return
                groupMemberService.GetByGuids(currentPersonsCellGroups.Select(g => g.Guid).ToList())
                                  .Select(gm => gm.Person);
        }

        public static IQueryable<Group> GetGroupsLead(this Person leader, RockContext rockContext)
        {
            var groupMemberService = new GroupMemberService( rockContext );
            var cellGroupType = GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            IQueryable<GroupMember> currentPersonsCellGroups = null;

            var consolidatorCoordinatorGuid = SystemGuid.GroupTypeRole.CONSOLIDATION_COORDINATOR.AsGuid();
            if ( cellGroupType == null )
            {
                throw new Exception( "Cannot locate KCI Group type" );
            }
            return groupMemberService
                .GetByPersonId( leader.Id )
                .Where( gm => gm.Group.GroupTypeId == cellGroupType.Id && ( gm.GroupRole.IsLeader || gm.GroupRole.Guid == consolidatorCoordinatorGuid ) ).Select(gm => gm.Group).Distinct();
        }

        /// <summary>
        ///  Get a person's group members they lead and their followups
        /// </summary>
        /// <param name="leader"></param>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        public static IQueryable<Person> GetPersonsLine(this Person leader, RockContext rockContext)
        {
            return leader.GetPeopleLeadInKCIGroups(rockContext).Union(leader.GetFollowUps(rockContext));
        }

        public static Group GetPersonsPrimaryKciGroup(this Person person, RockContext rockContext)
        {
            var groupMemberService = new GroupMemberService( rockContext );
            var cellGroupType = GroupTypeCache.Read( SystemGuid.GroupType.CELL_GROUP.AsGuid() );
            return groupMemberService.GetByPersonId(person.Id).Select(gm => gm.Group).FirstOrDefault(g => g.GroupTypeId == cellGroupType.Id);
        }

        public static bool HasConsolidator( this Person person )
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var consolidatedBy = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid() );
            return groupMemberService.GetKnownRelationship(person.Id, consolidatedBy.Id) != null;
        }

        public static void SetConsolidator( this Person person, Person newConsolidator )
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            if ( person.HasConsolidator() )
            {
                throw new Exception( person.FullName + " has a consolidator already" );
            }
            var consolidatedBy = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid() );
            groupMemberService.CreateKnownRelationship( person.Id, newConsolidator.Id, consolidatedBy.Id );
            rockContext.SaveChanges();
        }

        public static void RemoveConsolidator( this Person person, Person newConsolidator )
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            if ( !person.HasConsolidator() )
            {
                throw new Exception( person.FullName + " doesn't have a consolidator" );
            }
            var consolidatedBy = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid() );
            groupMemberService.DeleteKnownRelationship( person.Id, newConsolidator.Id, consolidatedBy.Id );
            rockContext.SaveChanges();
        }
    }
}
