using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.kcionline.bricksandmortarstudio.Lava
{
    public static class LavaFilters
    {
       /// <summary>
       /// Gets the people a person is related to
       /// </summary>
       /// <param name="context"></param>
       /// <param name="input"></param>
       /// <param name="relationshipTypeName">The relationship name you're search for</param>
       /// <returns></returns>
        public static List<Person> Relationship( DotLiquid.Context context, object input, string relationshipTypeName )
        {
            Person person = null;
            var rockContext = new RockContext();
            if ( input is int )
            {
                person = new PersonService( rockContext ).Get( ( int ) input );
            }
            else if ( input is Person )
            {
                person = ( Person ) input;
            }

            if ( person != null )
            {
                var relationshipType = new GroupTypeRoleService(rockContext).GetByGroupTypeId( GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid() ).Id ).FirstOrDefault(r => relationshipTypeName == r.Name);
                if (relationshipType != null)
                {
                    var relatedPersons = new GroupMemberService( rockContext ).GetKnownRelationship( person.Id, relationshipType.Id );
                    return relatedPersons.Select(p => p.Person).ToList();
                }   
            }

            return new List<Person>();
        }
    }

}
