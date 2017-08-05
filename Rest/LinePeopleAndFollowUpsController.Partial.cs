using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Controllers;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace org.kcionline.bricksandmortarstudio.Rest
{
    public class LinePeopleAndFollowUpsController : Rock.Rest.ApiController<Person>
    {
        public LinePeopleAndFollowUpsController() : base( new PersonService( new RockContext() ) ) { }

        /// <summary>
        /// Returns results to the Person Picker
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="includeHtml">if set to <c>true</c> [include HTML].</param>
        /// <param name="includeDetails">if set to <c>true</c> [include details].</param>
        /// <param name="includeBusinesses">if set to <c>true</c> [include businesses].</param>
        /// <param name="includeDeceased">if set to <c>true</c> [include deceased].</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/com_bricksandmortarstudio/LineAndFollowUpsSearch" )]
        public IQueryable<PersonSearchResult> Search( string name, bool includeHtml, bool includeDetails, bool includeBusinesses = false, bool includeDeceased = false )
        {
            int count = 20;
            bool showFullNameReversed;
            bool allowFirstNameOnly = false;

            var searchComponent = Rock.Search.SearchContainer.GetComponent( typeof( Rock.Search.Person.Name ).FullName );
            if ( searchComponent != null )
            {
                allowFirstNameOnly = searchComponent.GetAttributeValue( "FirstNameSearch" ).AsBoolean();
            }

            var activeRecordStatusValue = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );
            int activeRecordStatusValueId = activeRecordStatusValue != null ? activeRecordStatusValue.Id : 0;


            var sortedPersonQry = GetByFullName( name, true, includeBusinesses, allowFirstNameOnly, out showFullNameReversed ).Take( count );

            if ( includeDetails == false )
            {
                var simpleResultQry = sortedPersonQry.Select( a => new { a.Id, a.FirstName, a.NickName, a.LastName, a.SuffixValueId, a.RecordTypeValueId, a.RecordStatusValueId } );
                var simpleResult = simpleResultQry.ToList().Select( a => new PersonSearchResult
                {
                    Id = a.Id,
                    Name = showFullNameReversed
                    ? Person.FormatFullNameReversed( a.LastName, a.NickName, a.SuffixValueId, a.RecordTypeValueId )
                    : Person.FormatFullName( a.NickName, a.LastName, a.SuffixValueId, a.RecordTypeValueId ),
                    IsActive = a.RecordStatusValueId.HasValue && a.RecordStatusValueId == activeRecordStatusValueId
                } );

                return simpleResult.AsQueryable();
            }
            List<PersonSearchResult> searchResult = SearchWithDetails( sortedPersonQry, showFullNameReversed );
            return searchResult.AsQueryable();
        }

        private List<PersonSearchResult> SearchWithDetails( IQueryable<Person> sortedPersonQry, bool showFullNameReversed )
        {
            var sortedPersonList = sortedPersonQry.Include( a => a.PhoneNumbers ).AsNoTracking().ToList();
            Guid activeRecord = new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE );

            List<PersonSearchResult> searchResult = new List<PersonSearchResult>();
            foreach ( var person in sortedPersonList )
            {
                PersonSearchResult personSearchResult = new PersonSearchResult();
                personSearchResult.Id = person.Id;
                personSearchResult.Name = showFullNameReversed ? person.FullNameReversed : person.FullName;
                if ( person.RecordStatusValueId.HasValue )
                {
                    var recordStatus = DefinedValueCache.Read( person.RecordStatusValueId.Value );
                    personSearchResult.RecordStatus = recordStatus.Value;
                    personSearchResult.IsActive = recordStatus.Guid.Equals( activeRecord );
                }
                else
                {
                    personSearchResult.RecordStatus = string.Empty;
                    personSearchResult.IsActive = false;
                }

                GetPersonSearchDetails( personSearchResult, person );

                searchResult.Add( personSearchResult );
            }

            return searchResult;
        }

        private void GetPersonSearchDetails( PersonSearchResult personSearchResult, Person person )
        {
            var rockContext = this.Service.Context as RockContext;
            string itemDetailFormat = @"
<div class='picker-select-item-details clearfix' style='display: none;'>
	{0}
	<div class='contents'>
        {1}
	</div>
</div>
";

            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );
            int adultRoleId = familyGroupType.Roles.First( a => a.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).Id;

            int groupTypeFamilyId = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() ).Id;

            // figure out Family, Address, Spouse
            var groupMemberService = new GroupMemberService( rockContext );

            Guid? recordTypeValueGuid = null;
            if ( person.RecordTypeValueId.HasValue )
            {
                recordTypeValueGuid = DefinedValueCache.Read( person.RecordTypeValueId.Value ).Guid;
            }

            personSearchResult.ImageHtmlTag = Person.GetPersonPhotoImageTag( person, 50, 50 );
            personSearchResult.Age = person.Age.HasValue ? person.Age.Value : -1;
            personSearchResult.ConnectionStatus = person.ConnectionStatusValueId.HasValue ? DefinedValueCache.Read( person.ConnectionStatusValueId.Value ).Value : string.Empty;
            personSearchResult.Gender = person.Gender.ConvertToString();
            personSearchResult.Email = person.Email;

            string imageHtml = string.Format(
                "<div class='person-image' style='background-image:url({0}&width=65);background-size:cover;background-position:50%'></div>",
                Person.GetPersonPhotoUrl( person, 200, 200 ) );

            string personInfoHtml = string.Empty;
            Guid matchLocationGuid;
            bool isBusiness;
            if ( recordTypeValueGuid.HasValue && recordTypeValueGuid == Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() )
            {
                isBusiness = true;
                matchLocationGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid();
            }
            else
            {
                isBusiness = false;
                matchLocationGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid();
            }

            var familyGroupMember = groupMemberService.Queryable()
                .Where( a => a.PersonId == person.Id )
                .Where( a => a.Group.GroupTypeId == groupTypeFamilyId )
                .Select( s => new
                {
                    s.GroupRoleId,
                    GroupLocation = s.Group.GroupLocations.Where( a => a.GroupLocationTypeValue.Guid == matchLocationGuid ).Select( a => a.Location ).FirstOrDefault()
                } ).FirstOrDefault();

            int? personAge = person.Age;

            if ( familyGroupMember != null )
            {
                if ( isBusiness )
                {
                    personInfoHtml += "Business";
                }
                else
                {
                    personInfoHtml += familyGroupType.Roles.First( a => a.Id == familyGroupMember.GroupRoleId ).Name;
                }

                if ( personAge != null )
                {
                    personInfoHtml += " <em>(" + personAge.ToString() + " yrs old)</em>";
                }

                if ( familyGroupMember.GroupRoleId == adultRoleId )
                {
                    var personService = this.Service as PersonService;
                    var spouse = personService.GetSpouse( person, a => new
                    {
                        a.Person.NickName,
                        a.Person.LastName,
                        a.Person.SuffixValueId
                    } );

                    if ( spouse != null )
                    {
                        string spouseFullName = Person.FormatFullName( spouse.NickName, spouse.LastName, spouse.SuffixValueId );
                        personInfoHtml += "<p><strong>Spouse:</strong> " + spouseFullName + "</p>";
                        personSearchResult.SpouseName = spouseFullName;
                    }
                }
            }
            else
            {
                if ( personAge != null )
                {
                    personInfoHtml += personAge + " yrs old";
                }
            }

            if ( familyGroupMember != null )
            {
                var location = familyGroupMember.GroupLocation;

                if ( location != null )
                {
                    string addressHtml = "<h5>Address</h5>" + location.GetFullStreetAddress().ConvertCrLfToHtmlBr();
                    personSearchResult.Address = location.GetFullStreetAddress();
                    personInfoHtml += addressHtml;
                }
            }

            // Generate the HTML for Email and PhoneNumbers
            if ( !string.IsNullOrWhiteSpace( person.Email ) || person.PhoneNumbers.Any() )
            {
                string emailAndPhoneHtml = "<p class='margin-t-sm'>";
                emailAndPhoneHtml += person.Email;
                string phoneNumberList = string.Empty;
                foreach ( var phoneNumber in person.PhoneNumbers )
                {
                    var phoneType = DefinedValueCache.Read( phoneNumber.NumberTypeValueId ?? 0 );
                    phoneNumberList += string.Format(
                        "<br>{0} <small>{1}</small>",
                        phoneNumber.IsUnlisted ? "Unlisted" : phoneNumber.NumberFormatted,
                        phoneType != null ? phoneType.Value : string.Empty );
                }

                emailAndPhoneHtml += phoneNumberList + "<p>";

                personInfoHtml += emailAndPhoneHtml;
            }

            personSearchResult.PickerItemDetailsImageHtml = imageHtml;
            personSearchResult.PickerItemDetailsPersonInfoHtml = personInfoHtml;
            personSearchResult.PickerItemDetailsHtml = string.Format( itemDetailFormat, imageHtml, personInfoHtml );
        }

        private IQueryable<Person> GetByFullName( string fullName, bool includeDeceased, bool includeBusinesses, bool allowFirstNameOnly, out bool reversed )
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var allowedPersons = Utils.LineQuery.GetPeopleInLineAndTheirFollowUps( personService, GetPerson(), rockContext, true );

            var firstNames = new List<string>();
            var lastNames = new List<string>();
            string singleName = string.Empty;

            fullName = fullName.Trim();

            var nameParts = fullName.Trim().Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ).ToList();

            if ( fullName.Contains( ',' ) )
            {
                reversed = true;

                // only split by comma if there is a comma present (for example if 'Smith Jones, Sally' is the search, last name would be 'Smith Jones')
                nameParts = fullName.Split( ',' ).ToList();
                if ( nameParts.Count >= 1 )
                {
                    lastNames.Add( nameParts[0].Trim() );
                }
                if ( nameParts.Count >= 2 )
                {
                    firstNames.Add( nameParts[1].Trim() );
                }
            }
            else if ( fullName.Contains( ' ' ) )
            {
                reversed = false;

                for ( int i = 1; i < nameParts.Count; i++ )
                {
                    firstNames.Add( nameParts.Take( i ).ToList().AsDelimited( " " ) );
                    lastNames.Add( nameParts.Skip( i ).ToList().AsDelimited( " " ) );
                }
            }
            else
            {
                // no spaces, no commas
                reversed = true;
                singleName = fullName;
            }

            if ( !string.IsNullOrWhiteSpace( singleName ) )
            {
                int? personId = singleName.AsIntegerOrNull();
                if ( personId.HasValue )
                {
                    return allowedPersons
                        .Where( p => p.Aliases.Any( a => a.AliasPersonId == personId.Value ) );
                }

                Guid? personGuid = singleName.AsGuidOrNull();
                if ( personGuid.HasValue )
                {
                    return allowedPersons
                        .Where( p => p.Aliases.Any( a => a.AliasPersonGuid == personGuid.Value ) );
                }

                var previousNamesQry = new PersonPreviousNameService( rockContext ).Queryable();

                if ( allowFirstNameOnly )
                {
                    return allowedPersons
                        .Where( p =>
                            p.LastName.StartsWith( singleName ) ||
                            p.FirstName.StartsWith( singleName ) ||
                            p.NickName.StartsWith( singleName ) ||
                            previousNamesQry.Any( a => a.PersonAlias.PersonId == p.Id && a.LastName.StartsWith( singleName ) ) );
                }
                return allowedPersons
                    .Where( p =>
                        p.LastName.StartsWith( singleName ) ||
                        previousNamesQry.Any( a => a.PersonAlias.PersonId == p.Id && a.LastName.StartsWith( singleName ) ) );
            }
            if ( firstNames.Any() && lastNames.Any() )
            {
                var qry = GetByFirstLastName( firstNames.Any() ? firstNames[0] : "", lastNames.Any() ? lastNames[0] : "", includeDeceased, includeBusinesses, rockContext, allowedPersons );
                for ( var i = 1; i < firstNames.Count; i++ )
                {
                    qry = qry.Union( GetByFirstLastName( firstNames[i], lastNames[i], includeDeceased, includeBusinesses, rockContext, allowedPersons ) );
                }

                // always include a search for just last name using the last two parts of name search
                if ( nameParts.Count >= 2 )
                {
                    var lastName = string.Join( " ", nameParts.TakeLast( 2 ) );

                    qry = qry.Union( GetByLastName( lastName, includeDeceased, includeBusinesses, allowedPersons ) );
                }

                return qry;
            }
            // Blank string was used, return empty list
            return new List<Person>().AsQueryable();
        }

        private IQueryable<Person> GetByFirstLastName( string firstName, string lastName, bool includeDeceased, bool includeBusinesses, RockContext rockContext, IQueryable<Person> people )
        {
            string fullname = !string.IsNullOrWhiteSpace( firstName ) ? firstName + " " + lastName : lastName;

            var previousNamesQry = new PersonPreviousNameService( rockContext ).Queryable();

            var qry = people;
            if ( includeBusinesses )
            {
                int recordTypeBusinessId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;

                // if a we are including businesses, compare fullname against the Business Name (Person.LastName)
                qry = qry.Where( p =>
                    ( p.RecordTypeValueId.HasValue && p.RecordTypeValueId.Value == recordTypeBusinessId && p.LastName.StartsWith( fullname ) )
                    ||
                    ( ( p.LastName.StartsWith( lastName ) || previousNamesQry.Any( a => a.PersonAlias.PersonId == p.Id && a.LastName.StartsWith( lastName ) ) ) &&
                    ( p.FirstName.StartsWith( firstName ) ||
                    p.NickName.StartsWith( firstName ) ) ) );
            }
            else
            {
                qry = qry.Where( p =>
                    ( p.LastName.StartsWith( lastName ) || previousNamesQry.Any( a => a.PersonAlias.PersonId == p.Id && a.LastName.StartsWith( lastName ) ) ) &&
                    ( p.FirstName.StartsWith( firstName ) ||
                      p.NickName.StartsWith( firstName ) ) );
            }

            return qry;
        }

        public IQueryable<Person> GetByLastName( string lastName, bool includeDeceased, bool includeBusinesses, IQueryable<Person> people )
        {
            lastName = lastName.Trim();

            var lastNameQry = people
                                    .Where( p => p.LastName.StartsWith( lastName ) );

            return lastNameQry;
        }
    }
}
