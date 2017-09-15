using System;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;

namespace org.kcionline.bricksandmortarstudio.Extensions
{
    public static class PersonExtensions
    {
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

        public static bool HasConsolidator( this Person person )
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var consolidatedBy = new GroupTypeRoleService( rockContext ).Get( SystemGuid.GroupTypeRole.CONSOLIDATED_BY.AsGuid() );
            return groupMemberService.GetKnownRelationship( person.Id, consolidatedBy.Id ) != null
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
        }
    }
}
