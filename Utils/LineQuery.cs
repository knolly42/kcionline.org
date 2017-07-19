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
            return GetPeopleInLine( new PersonService(rockContext), currentPerson, rockContext );
        }

        public static IQueryable<Person> GetPeopleInLine( PersonService personService, Person currentPerson )
        {
            return GetPeopleInLine( personService, currentPerson, new RockContext() );
        }

        public static IQueryable<Person> GetPeopleInLine( PersonService personService, Person currentPerson, RockContext rockContext )
        {
            var groupMemberService = new GroupMemberService( rockContext );
            var groupType = GroupTypeCache.Read( SystemGuid.Group.CELL_GROUP.AsGuid() );
            var currentPersonsCellGroup =
                groupMemberService.GetByPersonId( currentPerson.Id )
                                  .FirstOrDefault( gm => gm.Group.GroupTypeId == groupType.Id )?.Group;

            if ( currentPersonsCellGroup == null )
            {
                return null;
            }

            var descendentGroups =
                new GroupService( rockContext ).GetAllDescendents( currentPersonsCellGroup.Id )
                                             .Where( g => g.GroupTypeId == groupType.Id )
                                             .Select( g => g.Id );
            var peopleInLine =
                groupMemberService.Queryable()
                                  .Where( gm => descendentGroups.Contains( gm.GroupId ) )
                                  .Select( gm => gm.Person );
            return peopleInLine;
        }

        public static IQueryable<ConnectionRequest> GetPeopleInLineFollowUpRequests(Person currentPerson)
        {
            var rockContext = new RockContext();

            var connectionRequestService = new ConnectionRequestService(rockContext);
            int getConnectedOpportunityId =
                new ConnectionOpportunityService(rockContext).Get(
                    SystemGuid.ConnectionOpportunity.GET_CONNECTED.AsGuid()).Id; 
            var connectionRequests = connectionRequestService.Queryable()
                                    .AsNoTracking()
                                    .Where(c => c.ConnectionOpportunityId == getConnectedOpportunityId && c.ConnectorPersonAliasId == currentPerson.PrimaryAliasId && c.ConnectionState == ConnectionState.Active);

            return connectionRequests;
        }
    }
}
